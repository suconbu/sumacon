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

        readonly string directoryPath = "command";
        readonly string fileNamePattern = "*.txt";

        public FormShortcut(DeviceManager deviceManager, CommandReceiver commandReceiver)
        {
            InitializeComponent();

            this.deviceManager = deviceManager;
            this.commandReceiver = commandReceiver;
        }

        public void NotifyKeyDown(KeyEventArgs e)
        {
            var keyName = e.KeyCode.ToString();
            if(this.commandSets.TryGetValue(keyName, out var command))
            {
                command.RunAsync(this.deviceManager.ActiveDevice, this.commandReceiver);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.LoadCommandFiles(this.directoryPath);
            this.SetupList();
            this.UpdateList();
        }

        void LoadCommandFiles(string directoryPath)
        {
            // 設定ファイル読み込み
            var paths = Directory.EnumerateFiles(directoryPath, this.fileNamePattern, SearchOption.TopDirectoryOnly);
            foreach (var path in paths.OrEmptyIfNull())
            {
                try
                {
                    var keyName = Path.GetFileNameWithoutExtension(path);
                    // ファンクションキーに限定
                    if (Regex.IsMatch(keyName, @"F\d+") && Enum.TryParse<Keys>(keyName, out var key))
                    {
                        var command = new CommandSet(path, key);
                        this.commandSets.Add(keyName, command);
                    }
                    else
                    {
                        Trace.TraceWarning($"Unsupported key name. ignored '{Path.GetFileName(path)}'.");
                    }
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
            this.uxShortcutList.Columns.Add("Key");
            this.uxShortcutList.Columns.Add("Name");
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
            foreach (var command in sets)
            {
                var item = new ListViewItem(command.KeyCode.ToString());
                item.SubItems.Add(command.Name);
                this.uxShortcutList.Items.Add(item);
            }
            this.uxShortcutList.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        class CommandSet
        {
            public Keys KeyCode;
            public string Name;
            public string[] Commands = new string[0];

            public CommandSet(string path, Keys keyCode)
            {
                var lines = File.ReadAllLines(path);
                var first = lines.FirstOrDefault(line => !string.IsNullOrWhiteSpace(line))?.Trim() ?? string.Empty;
                this.KeyCode = keyCode;
                this.Name = first.StartsWith("#") ? first.Substring(1).Trim() : first;
                this.Commands = lines;
            }

            public CommandContext RunAsync(Device device, CommandReceiver commandReceiver)
            {
                var sw = Stopwatch.StartNew();
                var label = $"{this.KeyCode.ToString()} - {this.Name}";
                var context = device.RunCommandAsync("shell", output =>
                {
                    if (output != null)
                    {
                        commandReceiver?.WriteOutput(output);
                    }
                    else
                    {
                        commandReceiver?.WriteOutput($"# Finish '{label}' ({sw.ElapsedMilliseconds} ms)");
                        sw = null;
                    }
                });
                commandReceiver?.WriteOutput($"# Run '{label}'");
                foreach (var command in this.Commands)
                {
                    context.PushInput($"echo '> {command}'");
                    context.PushInput(command);
                }
                context.PushInput("exit");
                return context;
            }
        } // class CommandSet
    }
}
