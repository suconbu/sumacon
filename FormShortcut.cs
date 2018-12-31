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
        class CommandSet
        {
            public string Key;
            public string Name;
            public string[] Commands = new string[0];

            public CommandSet(string path)
            {
                var lines = File.ReadAllLines(path);
                var first = lines.First().Trim();
                this.Key = Path.GetFileNameWithoutExtension(path);
                this.Name = first.StartsWith("#") ? first.Substring(1).Trim() : string.Empty;
                this.Commands = lines;
            }

            public CommandContext RunAsync(Device device, CommandReceiver commandReceiver)
            {
                var sw = Stopwatch.StartNew();
                var label = $"{this.Key} - {this.Name}";
                var context = device.RunCommandAsync("shell", output =>
                {
                    if (output != null)
                    {
                        commandReceiver?.WriteOutput(output);
                    }
                    else
                    {
                        commandReceiver?.WriteOutput($"# FINISH '{label}' ({sw.ElapsedMilliseconds} ms)");
                        sw = null;
                    }
                });
                commandReceiver?.WriteOutput($"# RUN '{label}'");
                foreach (var command in this.Commands)
                {
                    context.PushInput($"echo '> {command}'");
                    context.PushInput(command);
                }
                context.PushInput("exit");
                return context;
            }
        }

        Dictionary<string, CommandSet> commandSets = new Dictionary<string, CommandSet>();
        DeviceManager deviceManager;
        CommandReceiver commandReceiver;

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

            this.LoadCommandFiles("shortcut");
            this.SetupList();
            this.UpdateList();
        }

        void LoadCommandFiles(string directoryPath)
        {
            // 設定ファイル読み込み
            var paths = Directory.EnumerateFiles(directoryPath, "*.txt", SearchOption.TopDirectoryOnly);
            foreach (var path in paths.OrEmptyIfNull())
            {
                try
                {
                    var keyName = Path.GetFileNameWithoutExtension(path);
                    // ファンクションキーに限定
                    if (Regex.IsMatch(keyName, @"F\d+") && Enum.TryParse<Keys>(keyName, out var key))
                    {
                        var command = new CommandSet(path);
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
            foreach(var command in this.commandSets)
            {
                var item = new ListViewItem(command.Key.ToString());
                item.SubItems.Add(command.Value.Name);
                this.uxShortcutList.Items.Add(item);
            }
            this.uxShortcutList.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.ColumnContent);
        }
    }
}
