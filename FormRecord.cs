using Microsoft.VisualBasic.FileIO;
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
using WMPLib;

namespace Suconbu.Sumacon
{
    public partial class FormRecord : FormBase
    {
        RecordContext recordContext;
        DeviceManager deviceManager;
        GridPanel uxFileGridPanel;
        ContextMenuStrip fileGridContextMenu = new ContextMenuStrip();
        BindingList<FileInfo> fileInfos = new BindingList<FileInfo>();
        List<FileInfo> selectedFileInfos = new List<FileInfo>();
        Timer timer = new Timer();
        int sequenceNo = 1;

        readonly int baseBitrateNormal = 4_000_000;
        readonly int baseBitrateEconomy = 1_000_000;

        public FormRecord(DeviceManager deviceManager)
        {
            InitializeComponent();

            this.deviceManager = deviceManager;
            this.deviceManager.ActiveDeviceChanged += (s, e) => this.SafeInvoke(this.UpdateControlState);

            this.uxSaveDirectoryText.Text = @".\screenrecord";
            this.uxPatternText.Text = Properties.Resources.FormRecord_DefaultFileNamePattern;

            this.uxSize1.Checked = true;
            this.uxQualityNormal.Checked = true;

            this.uxTimeNumeric.Minimum = 1;
            this.uxTimeNumeric.Maximum = RecordContext.TimeLimitSecondsMax;
            this.uxTimeNumeric.ValueChanged += (s, e) => this.OnVideoSettingChanged();
            this.uxTimeNumeric.Value = 10;
            this.uxTimeBar.Minimum = 0;
            this.uxTimeBar.Maximum = (int)this.uxTimeNumeric.Maximum;
            this.uxTimeBar.TickFrequency = 10;
            this.uxTimeBar.SmallChange = 10;
            this.uxTimeBar.LargeChange = 10;
            this.uxTimeBar.Value = (int)this.uxTimeNumeric.Value;
            this.uxTimeBar.ValueChanged += (s, e) => this.uxTimeNumeric.Value = Math.Max(this.uxTimeNumeric.Minimum, this.uxTimeBar.Value);

            this.uxSize1.CheckedChanged += (s, e) => this.OnVideoSettingChanged();
            this.uxSize2.CheckedChanged += (s, e) => this.OnVideoSettingChanged();
            this.uxSize4.CheckedChanged += (s, e) => this.OnVideoSettingChanged();
            this.uxQualityNormal.CheckedChanged += (s, e) => this.OnVideoSettingChanged();
            this.uxQuarityEconomy.CheckedChanged += (s, e) => this.OnVideoSettingChanged();

            this.SetupContextMenu();

            this.uxFileGridPanel = new GridPanel();
            this.uxFileGridPanel.Dock = DockStyle.Fill;
            this.uxFileGridPanel.AutoGenerateColumns = false;
            this.uxFileGridPanel.DataSource = this.fileInfos;
            this.uxFileGridPanel.Columns.Add(
                this.CreateColumn(Properties.Resources.General_Name, nameof(FileInfo.Name), 240));
            this.uxFileGridPanel.Columns.Add(
                this.CreateColumn(Properties.Resources.General_Size, nameof(FileInfo.Length), 50, "#,##0 KB", DataGridViewContentAlignment.MiddleRight));
            this.uxFileGridPanel.Columns.Add(
                this.CreateColumn(Properties.Resources.General_DateTime, nameof(FileInfo.LastWriteTime), 120, "G"));
            this.uxFileGridPanel.CellFormatting += (s, e) =>
            {
                if (this.uxFileGridPanel.Columns[e.ColumnIndex].Name == Properties.Resources.General_Size)
                {
                    e.Value = (long)e.Value / 1024;
                }
            };
            foreach (DataGridViewColumn column in this.uxFileGridPanel.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            this.uxFileGridPanel.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.uxFileGridPanel.ContextMenuStrip = this.fileGridContextMenu;
            this.uxFileGridPanel.SelectionChanged += this.FileGridPanel_SelectionChanged;
            this.uxFileGridPanel.KeyDown += this.UxFileGridPanel_KeyDown;
            this.uxFileGridPanel.CellDoubleClick += (s, ee) => this.OpenSelectedFile();
            this.uxSplitContainer.Panel1.Controls.Add(this.uxFileGridPanel);

            // 経過時間の表示更新用
            this.timer.Interval = 1000;
            this.timer.Tick += (s, e) => this.UpdateControlState();

            this.uxStartButton.Click += this.UxStartButton_Click;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            this.uxSplitContainer.SplitterDistance = 450;
            this.uxSplitContainer.FixedPanel = FixedPanel.Panel1;
        }

        void SetupContextMenu()
        {
            var openFileItem = this.fileGridContextMenu.Items.Add(
                Properties.Resources.Menu_OpenFile,
                this.uxImageList.Images["page.png"],
                (s, e) => this.OpenSelectedFile()) as ToolStripMenuItem;

            this.fileGridContextMenu.Items.Add(
                Properties.Resources.Menu_OpenDirectory,
                this.uxImageList.Images["folder.png"],
                (s, e) => this.OpenSelectedFileDirectory());

            this.fileGridContextMenu.Items.Add(new ToolStripSeparator());

            var deleteItem = this.fileGridContextMenu.Items.Add(
                Properties.Resources.Menu_Delete,
                this.uxImageList.Images["cross.png"],
                (s, e) => this.DeleteSelectedFile()) as ToolStripMenuItem;
            deleteItem.ShortcutKeys = Keys.Delete;

            this.fileGridContextMenu.Opening += (s, e) =>
            {
                var count = this.uxFileGridPanel.SelectedRows.Count;
                openFileItem.Enabled = (count == 1);
                e.Cancel = (count <= 0);
            };
        }

        void FileGridPanel_SelectionChanged(object sender, EventArgs e)
        {
            this.selectedFileInfos.Clear();
            foreach (DataGridViewRow row in this.uxFileGridPanel.SelectedRows)
            {
                this.selectedFileInfos.Add(this.fileInfos[row.Index]);
            }

            var fileInfo = this.selectedFileInfos.FirstOrDefault();
            if (fileInfo != null)
            {
                this.axWindowsMediaPlayer1.URL = new Uri(fileInfo.FullName, UriKind.Absolute).ToString();
                this.axWindowsMediaPlayer1.Ctlcontrols.play();
                bool startAndPause = true;
                this.axWindowsMediaPlayer1.PlayStateChange += (s, ee) =>
                {
                    int state = ee.newState;
                    if (startAndPause && state == (int)WMPPlayState.wmppsPlaying)
                    {
                        this.axWindowsMediaPlayer1.Ctlcontrols.pause();
                        startAndPause = false;
                    }
                };
            }
            else
            {
                this.axWindowsMediaPlayer1.Ctlcontrols.stop();
                this.axWindowsMediaPlayer1.URL = null; ;
            }
        }

        void UxFileGridPanel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.OpenSelectedFile();
                e.SuppressKeyPress = true;
            }
        }

        void OpenSelectedFile()
        {
            var fileInfo = this.selectedFileInfos.FirstOrDefault();
            if (fileInfo == null) return;
            Process.Start(fileInfo.FullName);
        }

        void OpenSelectedFileDirectory()
        {
            var fileInfo = this.selectedFileInfos.FirstOrDefault();
            if (fileInfo == null) return;
            Process.Start("EXPLORER.EXE", $"/select,\"{fileInfo.FullName}\"");
        }

        void DeleteSelectedFile()
        {
            var deletes = this.selectedFileInfos.ToList();
            deletes.ForEach(f => this.fileInfos.Remove(f));
            foreach (var fileInfo in deletes)
            {
                try
                {
                    FileSystem.DeleteFile(fileInfo.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
        }

        void OnVideoSettingChanged()
        {
            var seconds = (int)this.uxTimeNumeric.Value;

            var mega = this.GetBitrate() / 8_000_000.0f * seconds;
            this.uxApproxLabel.Text = mega.ToString(Properties.Resources.FormRecord_ApproxFormat);
        }

        DataGridViewColumn CreateColumn(string name, string propertyName, int minimulWidth = -1, string format = null, DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleLeft)
        {
            var column = new DataGridViewTextBoxColumn();
            column.Name = name;
            column.DataPropertyName = propertyName;
            column.MinimumWidth = minimulWidth;
            if (format != null)
            {
                column.DefaultCellStyle.Format = format;
            }
            column.DefaultCellStyle.Alignment = alignment;
            return column;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.UpdateControlState();
        }

        private void UxStartButton_Click(object sender, EventArgs e)
        {
            if (this.recordContext == null)
            {
                var device = this.deviceManager.ActiveDevice;
                if (device == null) return;
                var setting = this.CreateSetting(device);
                this.recordContext = RecordContext.StartNew(device, setting, state =>
                {
                    this.SafeInvoke(() => this.OnRecordContextStateChanged(state));
                });
                this.timer.Start();
            }
            else
            {
                this.timer.Stop();
                this.recordContext.Stop();
            }
            this.UpdateControlState();
        }

        RecordSetting CreateSetting(Device device)
        {
            var setting = new RecordSetting();
            setting.DirectoryPath = this.uxSaveDirectoryText.Text;
            setting.FileNamePattern = this.uxPatternText.Text;
            setting.SequenceNo = this.sequenceNo;

            setting.TimeLimitSeconds = this.GetLimitTimeSeconds();
            setting.SizeMultiply = this.GetViewSizeMultiply();
            setting.Bitrate = this.GetBitrate();

            return setting;
        }

        int GetLimitTimeSeconds()
        {
            return (int)this.uxTimeNumeric.Value;
        }

        float GetViewSizeMultiply()
        {
            return
                this.uxSize2.Checked ? (1 / 2.0f) :
                this.uxSize4.Checked ? (1 / 4.0f) :
                1.0f;
        }

        int GetBitrate()
        {
            var baseBitrate = this.uxQuarityEconomy.Checked ? this.baseBitrateEconomy : this.baseBitrateNormal;
            return (int)(baseBitrate * this.GetViewSizeMultiply());
        }

        void OnRecordContextStateChanged(RecordContext.RecordState state)
        {
            if (state == RecordContext.RecordState.Pulling)
            {
                this.timer.Stop();
            }
            else if (state == RecordContext.RecordState.Aborted)
            {
                this.recordContext = null;
            }
            else if (state == RecordContext.RecordState.Finished)
            {
                this.fileInfos.Add(new FileInfo(this.recordContext.FilePath));
                //TODO: これGridPanel側でできるようにしたい...
                var rowCount = this.uxFileGridPanel.Rows.Count;
                var lastBeforeRow = (rowCount >= 2) ? this.uxFileGridPanel.Rows[rowCount - 2] : null;
                if (lastBeforeRow != null && lastBeforeRow.Selected)
                {
                    if (this.uxFileGridPanel.SelectedRows.Count == 1)
                    {
                        lastBeforeRow.Selected = false;
                    }
                    this.uxFileGridPanel.Rows[rowCount - 1].Selected = true;
                    this.uxFileGridPanel.FirstDisplayedScrollingRowIndex = rowCount - 1;
                }
                this.recordContext = null;
                this.sequenceNo++;
            }
            else
            {
                ;
            }
            this.UpdateControlState();
        }

        void UpdateControlState()
        {
            if (this.recordContext != null)
            {
                if (this.recordContext.State == RecordContext.RecordState.Recording)
                {
                    var seconds = (int)(DateTime.Now - this.recordContext.StartedAt).TotalSeconds;
                    seconds = Math.Min(seconds, this.GetLimitTimeSeconds());
                    this.uxStartButton.Text = string.Format(
                        Properties.Resources.FormRecord_ButtonLabel_Recording,
                        seconds);
                }
                else
                {
                    this.uxStartButton.Text = string.Format(Properties.Resources.FormRecord_ButtonLabel_Stop);
                }
            }
            else
            {
                this.uxStartButton.Text = Properties.Resources.FormRecord_ButtonLabel_Start;
            }

            this.uxStartButton.Enabled = true;
            this.uxSettingPanel.Enabled = (this.recordContext == null);

            if (this.deviceManager.ActiveDevice == null)
            {
                this.uxStartButton.Enabled = false;
                this.uxSettingPanel.Enabled = false;
            }
        }

        class RecordFileInfo
        {
            public string FullPath { get; set; }
            public string Name { get; set; }
            public long KiroBytes { get; set; }
            public DateTime DateTime { get; set; }
            public int RecordingSeconds { get; set; }
        }
    }
}
