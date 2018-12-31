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
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Suconbu.Sumacon
{
    public partial class FormShortcut : FormBase
    {
        class CommandEntry
        {
            public string Name;
            public string[] Commands;

            public CommandEntry(string path)
            {
                var lines = File.ReadAllLines(path);
                var first = lines.First().Trim();
                if(first.StartsWith("#"))
                {
                    this.Name = first.Substring(1).Trim();
                    this.Commands = lines.Skip(1).ToArray();
                }
                else
                {
                    this.Name = string.Empty;
                    this.Commands = lines;
                }
            }

            public CommandContext RunAsync(Device device)
            {
                return null;
            }
        }

        Dictionary<string, CommandEntry> commandEntries = new Dictionary<string, CommandEntry>();

        public FormShortcut()
        {
            InitializeComponent();
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
                    if (Enum.TryParse<Keys>(keyName, out var key))
                    {
                        var command = new CommandEntry(path);
                        this.commandEntries.Add(keyName, command);
                    }
                    else
                    {
                        Trace.TraceWarning($"Unsupported key '{keyName}'");
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
                this.uxCommandText.Lines = e.IsSelected ? this.commandEntries[e.Item.Text].Commands : null;
            };
            this.uxShortcutList.DoubleClick += (s, e) =>
            {
                if (this.uxShortcutList.SelectedItems.Count > 0)
                {
                    var item = this.uxShortcutList.SelectedItems[0];
                    //this.commandEntries[item.Text].RunAsync(this.device);
                }
            };
        }

        void UpdateList()
        {
            this.uxShortcutList.Items.Clear();
            foreach(var command in this.commandEntries)
            {
                var item = new ListViewItem(command.Key.ToString());
                item.SubItems.Add(command.Value.Name);
                this.uxShortcutList.Items.Add(item);
            }
        }
    }
}
