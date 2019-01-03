using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Suconbu.Sumacon
{
    public partial class FormShortcut : FormBase
    {
        Dictionary<string, CommandSet> commandSets = new Dictionary<string, CommandSet>();
        DeviceManager deviceManager;
        CommandReceiver commandReceiver;
        FileSystemWatcher watcher = new FileSystemWatcher();
        readonly string directoryPath = Properties.Resources.FormShortcut_DirectoryPath;
        readonly string fileNameFilter = Properties.Resources.FormShortcut_FileNameFilter;

        public FormShortcut(DeviceManager deviceManager, CommandReceiver commandReceiver)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.deviceManager = deviceManager;
            this.commandReceiver = commandReceiver;
            if (Directory.Exists(this.directoryPath))
            {
                this.watcher.Path = this.directoryPath;
                this.watcher.Filter = this.fileNameFilter;
                this.watcher.Changed += this.Watcher_Changed;
                this.watcher.Created += this.Watcher_Changed;
                this.watcher.Renamed += this.Watcher_Changed;
                this.watcher.Deleted += this.Watcher_Changed;
                this.watcher.EnableRaisingEvents = true;
                this.watcher.SynchronizingObject = this;
            }
        }

        public void NotifyKeyDown(KeyEventArgs e)
        {
            if(Keys.F1 <= e.KeyCode && e.KeyCode <= Keys.F24)
            {
                Trace.TraceInformation($"{Util.GetCurrentMethodName()} - {e.KeyCode}");

                var keyName = e.KeyCode.ToString();
                if (this.commandSets.TryGetValue(keyName, out var command))
                {
                    command.RunAsync(this.deviceManager.ActiveDevice, this.commandReceiver);
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnLoad(e);

            this.SetupList();

            this.LoadCommandFiles(this.directoryPath);
            this.UpdateList();

            this.uxCommandText.ReadOnly = true;
            this.uxCommandText.BackColor = Color.Black;
            this.uxCommandText.ForeColor = Color.White;
            this.uxCommandText.Font = new Font(Properties.Resources.MonospaceFontName, this.uxCommandText.Font.Size);
        }

        void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            this.LoadCommandFiles(this.directoryPath);
            this.UpdateList();
        }

        void LoadCommandFiles(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return;

            // 設定ファイル読み込み
            this.commandSets.Clear();
            var paths = Directory.EnumerateFiles(directoryPath, this.fileNameFilter, SearchOption.TopDirectoryOnly);
            foreach (var path in paths.OrEmptyIfNull())
            {
                try
                {
                    var name = Path.GetFileNameWithoutExtension(path);
                    var shortcutKey = Keys.None;
                    if (Regex.IsMatch(name, @"^F\d+$"))
                    {
                        Enum.TryParse(name, out shortcutKey);
                    }
                    this.commandSets.Add(name, new CommandSet(path, shortcutKey));
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
        }

        void SetupList()
        {
            this.uxShortcutList.MultiSelect = false;
            this.uxShortcutList.Columns.Add(Properties.Resources.General_Name);
            this.uxShortcutList.Columns.Add(Properties.Resources.General_Description);
            this.uxShortcutList.ItemSelectionChanged += (s, e) =>
            {
                this.uxCommandText.Lines = e.IsSelected ? this.commandSets[e.Item.Text].Commands : null;
            };
            this.uxShortcutList.DoubleClick += (s, e) =>
            {
                if (this.uxShortcutList.SelectedItems.Count > 0)
                {
                    var item = this.uxShortcutList.SelectedItems[0];
                    this.commandSets[item.Text].RunAsync(this.deviceManager.ActiveDevice, this.commandReceiver);
                }
            };
        }

        void UpdateList()
        {
            this.uxShortcutList.Items.Clear();
            var sets = this.commandSets.Values.OrderBy(v => v.KeyCode);
            foreach (var set in sets)
            {
                var item = new ListViewItem(new string[] { set.Name, set.Description });
                this.uxShortcutList.Items.Add(item);
            }
            this.uxShortcutList.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        class CommandSet
        {
            public Keys KeyCode { get; private set; }
            public string Name { get; private set; }
            public string Description { get; private set; }
            public string[] Commands { get; private set; } = new string[0];

            public CommandSet(string path, Keys keyCode = Keys.None)
            {
                var lines = File.ReadAllLines(path);
                var first = lines.FirstOrDefault(line => !string.IsNullOrWhiteSpace(line))?.Trim() ?? string.Empty;
                this.KeyCode = keyCode;
                this.Name = Path.GetFileNameWithoutExtension(path);
                this.Description = first.StartsWith("#") ? first.Substring(1).Trim() : first;
                this.Commands = lines;
            }

            public CommandContext RunAsync(Device device, CommandReceiver commandReceiver)
            {
                var stopwatch = Stopwatch.StartNew();
                var label = $"{this.Name} - {this.Description}";
                var context = device?.RunCommandAsync("shell", output =>
                {
                    if (output != null)
                    {
                        commandReceiver?.WriteOutput(output);
                    }
                    else
                    {
                        commandReceiver?.WriteOutput($"# Finish '{label}' ({stopwatch.ElapsedMilliseconds} ms)");
                        stopwatch = null;
                    }
                });
                if (context != null)
                {
                    commandReceiver?.WriteOutput($"# Run '{label}'");
                    foreach (var command in this.Commands)
                    {
                        context.PushInput($"echo '> {command}'");
                        context.PushInput(command);
                    }
                    context.PushInput("exit");
                }
                return context;
            }
        } // class CommandSet
    }
}
