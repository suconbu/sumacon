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

namespace Suconbu.Sumacon
{
    public partial class FormCapture : FormBase
    {
        class CaptureFileInfo
        {
            public string Name { get; private set; }
            public long KiroBytes { get; private set; }
            public DateTime DateTime { get; private set; }

            string fullPath;
            Image cachedImage;

            public CaptureFileInfo(string path)
            {
                var fileInfo = new FileInfo(path);
                this.Name = fileInfo.Name;
                this.KiroBytes = fileInfo.Length / 1024;
                this.DateTime = fileInfo.LastWriteTime;
                this.fullPath = fileInfo.FullName;
                this.cachedImage = null;
            }

            public Image GetImage()
            {
                if (this.cachedImage == null)
                {
                    this.cachedImage = Image.FromFile(this.fullPath);
                }
                return this.cachedImage;
            }
        }

        DeviceManager deviceManager;
        CaptureContext captureContext;
        GridPanel uxFileGridPanel;
        BindingList<CaptureFileInfo> capturedFileInfos = new BindingList<CaptureFileInfo>();
        List<List<PictureBox>> pictureBoxLists = new List<List<PictureBox>>();
        List<TableLayoutPanel> pictureTablePanels = new List<TableLayoutPanel>();
        int sequenceNo;

        readonly int defaultInterval = 5;
        readonly int defaultCount = 10;
        readonly string defaultSaveDirectory = @".\screencapture";
        readonly string defaultPattern = "{device-model}_{date}_{time}_{no}.png";
        readonly string labelStart = "Capture";
        readonly string labelStop = "Stop";
        readonly int startOfNo = 1;
        readonly int endOfNo = 9999;
        readonly string patternToolTipText;
        readonly int picturePreviewCountMax = 4;

        public FormCapture(DeviceManager deviceManager)
        {
            InitializeComponent();

            this.deviceManager = deviceManager;
            this.deviceManager.ActiveDeviceChanged += (s, e) => this.SafeInvoke(this.UpdateControlState);

            this.sequenceNo = this.startOfNo;
            var sb = new StringBuilder();
            sb.AppendLine("{device-id} : 'HXC8KSKL99XYZ'");
            sb.AppendLine("{device-model} : 'Nexus_9'");
            sb.AppendLine("{device-name} : 'MyTablet'");
            sb.AppendLine("{date} : '2018-12-31'");
            sb.AppendLine("{time} : '12-34-56'");
            sb.AppendLine("{no} : '0001' (Single shot) / '0002-0034' (Continuous mode)");
            sb.AppendLine("* {no} is reset in application start.");
            this.patternToolTipText = sb.ToString();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.SetupPicturePreview();

            this.uxSaveDirectoryText.Text = this.defaultSaveDirectory;
            this.uxPatternText.Text = this.defaultPattern;
            this.uxToolTip.SetToolTip(this.uxPatternText, this.patternToolTipText);
            this.uxToolTip.AutoPopDelay = 30000;

            this.uxIntervalNumeric.Minimum = 1;
            this.uxIntervalNumeric.Value = this.defaultInterval;
            this.uxCountNumeric.Minimum = 1;
            this.uxCountNumeric.Value = this.defaultCount;

            this.uxContinuousCheck.CheckedChanged += (s, ee) => this.UpdateControlState();
            this.uxCountCheck.CheckedChanged += (s, ee) => this.UpdateControlState();

            this.uxStartButton.Enabled = (this.deviceManager.ActiveDevice != null);
            this.uxStartButton.Click += this.UxStartButton_Click;

            this.uxFileGridPanel = new GridPanel();
            this.uxSplitContainer.Panel1.Controls.Add(this.uxFileGridPanel);
            this.uxFileGridPanel.Dock = DockStyle.Fill;
            this.uxFileGridPanel.AutoGenerateColumns = false;
            this.uxFileGridPanel.DataSource = this.capturedFileInfos;
            this.uxFileGridPanel.Columns.Add(this.CreateColumn("Name", nameof(CaptureFileInfo.Name), 220));
            this.uxFileGridPanel.Columns.Add(this.CreateColumn("Size", nameof(CaptureFileInfo.KiroBytes), 60, "#,##0 KB"));
            this.uxFileGridPanel.Columns.Add(this.CreateColumn("Date", nameof(CaptureFileInfo.DateTime), 100));
            this.uxFileGridPanel.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.uxFileGridPanel.SelectionChanged += this.FileGridPanel_SelectionChanged;

            this.UpdateControlState();
        }

        void SetupPicturePreview()
        {
            for (var i = 1; i <= this.picturePreviewCountMax; i++)
            {
                var tablePanel = new TableLayoutPanel();
                tablePanel.ColumnCount = i;
                tablePanel.RowCount = i;
                tablePanel.Dock = DockStyle.Fill;
                tablePanel.Visible = false;
                tablePanel.BackColor = Color.Black;
                tablePanel.Margin = new Padding(1);
                var boxes = new List<PictureBox>();
                for (var y = 0; y < i; y++)
                {
                    tablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
                    tablePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f));
                }
                for (var y = 0; y < i; y++)
                {
                    for (var x = 0; x < i; x++)
                    {
                        var pictureBox = new PictureBox();
                        pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                        pictureBox.Dock = DockStyle.Fill;
                        tablePanel.Controls.Add(pictureBox, x, y);
                        boxes.Add(pictureBox);
                    }
                }
                this.pictureBoxLists.Add(boxes);
                this.pictureTablePanels.Add(tablePanel);
                this.uxSplitContainer.Panel2.Controls.Add(tablePanel);
            }
        }

        private void FileGridPanel_SelectionChanged(object sender, EventArgs e)
        {
            var selectedCount = this.uxFileGridPanel.SelectedRows.Count;
            if (selectedCount <= 0) return;

            var visibleIndex = 0;
            for (var i = 1; i <= this.picturePreviewCountMax; i++)
            {
                if (selectedCount <= i * i || i == this.picturePreviewCountMax)
                {
                    var boxes = this.pictureBoxLists[i - 1];
                    for(var boxIndex = 0; boxIndex < (i * i); boxIndex++)
                    {
                        if (boxIndex < selectedCount)
                        {
                            var rowIndex = this.uxFileGridPanel.SelectedRows[selectedCount - boxIndex - 1].Index;
                            boxes[boxIndex].Image = this.capturedFileInfos[rowIndex].GetImage();
                        }
                        else
                        {
                            boxes[boxIndex].Image = null;
                        }
                    }
                    visibleIndex = i - 1;
                    break;
                }
            }

            this.pictureTablePanels[visibleIndex].Visible = true;
            for (var i = 0; i < this.picturePreviewCountMax; i++)
            {
                if(i != visibleIndex)
                {
                    this.pictureTablePanels[i].Visible = false;
                }
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            this.uxSplitContainer.SplitterDistance = 400;
            this.uxSplitContainer.FixedPanel = FixedPanel.Panel1;
        }

        DataGridViewColumn CreateColumn(string name, string propertyName, int minimulWidth = -1, string format = null)
        {
            var column = new DataGridViewTextBoxColumn();
            column.Name = name;
            column.DataPropertyName = propertyName;
            column.MinimumWidth = minimulWidth;
            if (format != null)
            {
                column.DefaultCellStyle.Format = format;
            }
            return column;
        }

        void UxStartButton_Click(object sender, EventArgs e)
        {
            if(this.captureContext == null || this.captureContext.Mode == CaptureContext.CaptureMode.Single)
            {
                if (this.uxContinuousCheck.Checked)
                {
                    var intervalMilliseconds = (int)this.uxIntervalNumeric.Value * 1000;
                    var count = this.uxCountCheck.Checked ? (int)this.uxCountNumeric.Value : 0;
                    var skipSame = this.uxSkipSameImageCheck.Checked;
                    this.captureContext = CaptureContext.ContinuousCapture(this.deviceManager, intervalMilliseconds, skipSame, count);
                }
                else
                {
                    this.captureContext = CaptureContext.SingleCapture(this.deviceManager);
                }

                if (this.captureContext != null)
                {
                    this.captureContext.Captured += this.CaptureContext_Captured;
                    this.captureContext.Finished += this.CaptureContext_Finished;
                    this.captureContext.Start();
                }
            }
            else
            {
                // 連続撮影中止
                this.captureContext.Dispose();
                this.captureContext = null;
            }

            this.UpdateControlState();
        }

        void CaptureContext_Captured(object sender, Bitmap bitmap)
        {
            var filePath = this.SaveCapture(bitmap);
            this.SafeInvoke(() =>
            {
                var lastRowSelected = false;
                var rowCount = this.uxFileGridPanel.Rows.Count;
                if (rowCount > 0)
                {
                    lastRowSelected = this.uxFileGridPanel.Rows[rowCount - 1].Selected;
                }
                this.capturedFileInfos.Add(new CaptureFileInfo(filePath));
                rowCount++;
                if (lastRowSelected)
                {
                    this.uxFileGridPanel.ClearSelection();
                    this.uxFileGridPanel.Rows[rowCount - 1].Selected = true;
                    this.uxFileGridPanel.FirstDisplayedScrollingRowIndex = rowCount - 1;
                }
                this.UpdateControlState();
            });
        }

        void CaptureContext_Finished(object sender, EventArgs e)
        {
            this.captureContext?.Dispose();
            this.captureContext = null;
            this.SafeInvoke(() => this.UpdateControlState());
        }

        string SaveCapture(Bitmap bitmap)
        {
            try
            {
                var directoryPath = this.uxSaveDirectoryText.Text;
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                var fileName = this.GetNextFileName();
                var filePath = Path.Combine(directoryPath, fileName);
                bitmap.Save(filePath);
                return filePath;
            }
            catch(Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return null;
            }
        }

        void UpdateControlState()
        {
            if (this.captureContext != null && this.captureContext.Mode == CaptureContext.CaptureMode.Continuous)
            {
                var sb = new StringBuilder();
                sb.AppendLine(this.labelStop);
                if (this.captureContext.RemainingCount >= 0 && this.captureContext.RemainingCount != int.MaxValue)
                {
                    sb.Append($"({this.captureContext.RemainingCount} shots remains)");
                }
                else
                {
                    sb.Append($"({this.captureContext.CapturedCount} captured)");
                }
                this.uxStartButton.Text = sb.ToString();
                this.uxStartButton.Enabled = true;
                this.uxSettingPanel.Enabled = false;
            }
            else
            {
                this.uxStartButton.Text = this.labelStart;
                this.uxStartButton.Enabled = (this.captureContext == null);
                this.uxSettingPanel.Enabled = true;
            }

            if (this.deviceManager.ActiveDevice == null)
            {
                this.uxStartButton.Enabled = false;
                this.uxSettingPanel.Enabled = false;
            }

            this.uxConinuousPanel.Enabled = this.uxContinuousCheck.Checked;
            this.uxCountNumeric.Enabled = this.uxCountCheck.Checked;
        }

        string GetNextFileName()
        {
            var device = this.deviceManager.ActiveDevice;
            var name = this.uxPatternText.Text;
            lock (this)
            {
                name = name.Replace("{device-id}", device?.Id ?? "-");
                name = name.Replace("{device-model}", device?.Model ?? "-");
                name = name.Replace("{device-name}", device?.Name ?? "-");
                name = name.Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"));
                name = name.Replace("{time}", DateTime.Now.ToString("hh-mm-ss"));
                name = name.Replace("{no}", this.sequenceNo.ToString("0000"));
                this.sequenceNo = ++this.sequenceNo > this.endOfNo ? this.startOfNo : this.sequenceNo;
            }
            return name;
        }
    }
}
