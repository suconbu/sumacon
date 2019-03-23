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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using WMPLib;

namespace Suconbu.Sumacon
{
    public partial class FormRecord : FormBase
    {
        Sumacon sumacon;
        RecordContext recordContext;
        GridPanel uxFileGridPanel;
        ContextMenuStrip fileGridContextMenu = new ContextMenuStrip();
        BindingList<FileInfo> fileInfos = new BindingList<FileInfo>();
        List<FileInfo> selectedFileInfos = new List<FileInfo>();
        Button[] timeButtons;
        Timer elapsedTimeRedrawTimer = new Timer();
        int sequenceNo = 1;
        Dictionary<string, RadioButton> viewSizeRadioMap;
        Dictionary<string, RadioButton> qualityRadioMap;

        readonly string patternToolTipText;
        readonly int baseBitrateNormal = 4_000_000;
        readonly int baseBitrateEconomy = 1_000_000;

        public FormRecord(Sumacon sumacon)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.sumacon = sumacon;
            this.sumacon.DeviceManager.ActiveDeviceChanged += this.DeviceManager_ActiveDeviceChanged;

            this.SetupContextMenu();

            // viewSizeRadioMap, qualityRadioMapのキーはSettingの文字列と対応(ボタンのラベルじゃない)

            this.uxSize1.Checked = true;
            this.viewSizeRadioMap = new Dictionary<string, RadioButton>()
            {
                { "1/1", this.uxSize1 }, { "1/2", this.uxSize2 }, { "1/4", this.uxSize4 }
            };
            foreach(var entry in this.viewSizeRadioMap)
            {
                entry.Value.Tag = entry.Key;
                entry.Value.CheckedChanged += (s, e) => this.UpdateApproxSize();
            }

            this.uxQualityNormal.Checked = true;
            this.qualityRadioMap = new Dictionary<string, RadioButton>()
            {
                { "Normal", this.uxQualityNormal }, { "Economy", this.uxQuarityEconomy }
            };
            foreach (var entry in this.qualityRadioMap)
            {
                entry.Value.Tag = entry.Key;
                entry.Value.CheckedChanged += (s, e) => this.UpdateApproxSize();
            }

            this.uxLimitTimeNumeric.Minimum = 1;
            this.uxLimitTimeNumeric.Maximum = RecordContext.TimeLimitSecondsMax;
            this.uxLimitTimeNumeric.ValueChanged += (s, e) => this.UpdateApproxSize();

            this.timeButtons = new[] { this.uxLimitTime10, this.uxLimitTime30, this.uxLimitTime60, this.uxLimitTime180 };
            foreach(var button in this.timeButtons)
            {
                button.Click += this.UxLimitTime_Clicked;
            }

            this.uxStartButton.Click += this.UxStartButton_Click;

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
            this.elapsedTimeRedrawTimer.Interval = 1000;
            this.elapsedTimeRedrawTimer.Tick += (s, e) => this.UpdateControlState();

            this.uxToolTip.SetToolTip(this.uxPatternText, this.patternToolTipText);
            this.uxToolTip.AutoPopDelay = 30000;

            this.patternToolTipText = Properties.Resources.FileNamePatternHelp;
        }

        private void UxLimitTime_Clicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            this.uxLimitTimeNumeric.Value = int.Parse((string)button.Tag);
        }

        protected override void OnShown(EventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnShown(e);

            this.uxSplitContainer.FixedPanel = FixedPanel.Panel1;

            this.ApplySettings();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnClosing(e);
            this.sumacon.DeviceManager.ActiveDeviceChanged -= this.DeviceManager_ActiveDeviceChanged;

            Properties.Settings.Default.RecordSplitterDistance = this.uxSplitContainer.SplitterDistance;
            Properties.Settings.Default.RecordSaveDirectoryPath = this.uxSaveDirectoryText.Text;
            Properties.Settings.Default.RecordFileNamePattern = this.uxPatternText.Text;
            Properties.Settings.Default.RecordViewSize = (string)this.viewSizeRadioMap.Values.FirstOrDefault(radio => radio.Checked)?.Tag;
            Properties.Settings.Default.RecordQuality = (string)this.qualityRadioMap.Values.FirstOrDefault(radio => radio.Checked)?.Tag;
            Properties.Settings.Default.RecordTimestampEnabled = this.uxTimestampCheck.Checked;
            Properties.Settings.Default.RecordLimitTime = (int)this.uxLimitTimeNumeric.Value;
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

        void UpdateApproxSize()
        {
            var seconds = (int)this.uxLimitTimeNumeric.Value;

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

        void DeviceManager_ActiveDeviceChanged(object sender, Device device)
        {
            this.SafeInvoke(this.UpdateControlState);
        }

        private void UxStartButton_Click(object sender, EventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());

            if (this.recordContext == null)
            {
                var device = this.sumacon.DeviceManager.ActiveDevice;
                if (device == null) return;
                var setting = this.GetRecordSetting(device);
                this.recordContext = RecordContext.StartNew(device, setting, state =>
                {
                    this.SafeInvoke(() => this.OnRecordContextStateChanged(state));
                });
                this.elapsedTimeRedrawTimer.Start();
                this.sumacon.DeviceManager.SuspendPropertyUpdate(device);
            }
            else
            {
                this.elapsedTimeRedrawTimer.Stop();
                this.recordContext.Stop();
            }
            this.UpdateControlState();
        }

        RecordSetting GetRecordSetting(Device device)
        {
            var setting = new RecordSetting();
            setting.DirectoryPath = this.uxSaveDirectoryText.Text;
            setting.FileNamePattern = this.uxPatternText.Text;
            setting.SequenceNo = this.sequenceNo;

            setting.TimeLimitSeconds = this.GetLimitTimeSeconds();
            setting.ViewSizeMultiply = this.GetViewSizeMultiply();
            setting.Bitrate = this.GetBitrate();
            setting.Timestamp = this.GetTimestampEnabled();

            return setting;
        }

        int GetLimitTimeSeconds()
        {
            return (int)this.uxLimitTimeNumeric.Value;
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

        bool GetTimestampEnabled()
        {
            return this.uxTimestampCheck.Checked;
        }

        void OnRecordContextStateChanged(RecordContext.RecordState state)
        {
            if (state == RecordContext.RecordState.Pulling)
            {
                this.elapsedTimeRedrawTimer.Stop();
            }
            else if (state == RecordContext.RecordState.Aborted)
            {
                this.sumacon.DeviceManager.ResumePropertyUpdate(this.recordContext?.Device);
                this.recordContext = null;
            }
            else if (state == RecordContext.RecordState.Finished)
            {
                this.sumacon.DeviceManager.ResumePropertyUpdate(this.recordContext.Device);

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

        void ApplySettings()
        {
            this.uxSplitContainer.SplitterDistance = Properties.Settings.Default.RecordSplitterDistance;
            this.uxSaveDirectoryText.Text = Properties.Settings.Default.RecordSaveDirectoryPath;
            this.uxPatternText.Text = Properties.Settings.Default.RecordFileNamePattern;
            if (this.viewSizeRadioMap.TryGetValue(Properties.Settings.Default.RecordViewSize, out var viewSizeRadio))
            {
                viewSizeRadio.Checked = true;
            }
            if (this.qualityRadioMap.TryGetValue(Properties.Settings.Default.RecordQuality, out var qualityRadio))
            {
                qualityRadio.Checked = true;
            }
            this.uxTimestampCheck.Checked = Properties.Settings.Default.RecordTimestampEnabled;
            this.uxLimitTimeNumeric.Value = Math.Max(1, Math.Min(Properties.Settings.Default.RecordLimitTime, RecordContext.TimeLimitSecondsMax));

            this.UpdateControlState();
        }

        void UpdateControlState()
        {
            if (this.recordContext != null)
            {
                if (this.recordContext.State == RecordContext.RecordState.Recording)
                {
                    this.uxStartButton.Text = string.Format(
                        Properties.Resources.FormRecord_ButtonLabel_Recording,
                        (int)this.recordContext.Elapsed.TotalSeconds);
                    this.uxStartButton.BackColor = Color.FromName(Properties.Resources.RecordingButtonColorName);
                    this.uxStartButton.Enabled = true;
                }
                else
                {
                    this.uxStartButton.Text = string.Format(Properties.Resources.FormRecord_ButtonLabel_Stopping);
                    this.uxStartButton.UseVisualStyleBackColor = true;
                    this.uxStartButton.Enabled = false;
                }
            }
            else
            {
                this.uxStartButton.Text = Properties.Resources.FormRecord_ButtonLabel_Start;
                this.uxStartButton.UseVisualStyleBackColor = true;
                this.uxStartButton.Enabled = true;
            }

            this.uxSettingPanel.Enabled = (this.recordContext == null);

            if (this.sumacon.DeviceManager.ActiveDevice == null)
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
