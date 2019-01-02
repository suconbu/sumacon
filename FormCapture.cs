using Microsoft.VisualBasic.FileIO;
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
            public string FullPath { get; private set; }
            public string Name { get; private set; }
            public long KiroBytes { get; private set; }
            public DateTime DateTime { get; private set; }

            public CaptureFileInfo(string path)
            {
                var fileInfo = new FileInfo(path);
                this.FullPath = fileInfo.FullName;
                this.Name = fileInfo.Name;
                this.KiroBytes = fileInfo.Length / 1024;
                this.DateTime = fileInfo.LastWriteTime;
            }
        }
        enum FileGridContextMenuItems { OpenFile, OpenDirectory, Copy, Delete }

        DeviceManager deviceManager;
        CaptureContext captureContext;
        GridPanel uxFileGridPanel;
        BindingList<CaptureFileInfo> capturedFileInfos = new BindingList<CaptureFileInfo>();
        List<CaptureFileInfo> selectedCapturedFileInfos = new List<CaptureFileInfo>();
        // PictureBox.TagにはCaptureFileInfoを設定
        List<List<PictureBox>> pictureBoxLists = new List<List<PictureBox>>();
        List<TableLayoutPanel> pictureTablePanels = new List<TableLayoutPanel>();
        int sequenceNo;
        ContextMenuStrip fileGridContextMenu = new ContextMenuStrip();
        // Image.Tagにはファイルフルパスを設定
        LinkedList<Image> previewImageCache = new LinkedList<Image>();

        readonly int defaultInterval = 5;
        readonly int defaultCount = 10;
        readonly int startOfNo = 1;
        readonly int endOfSequenceNo = 9999;
        readonly string patternToolTipText;
        readonly int picturePreviewCountMax = 5;
        readonly int previewImageCacheCapacity = (int)(5 * 5 * 1.2);
        readonly int previewImageSizeLimit = 800;

        public FormCapture(DeviceManager deviceManager)
        {
            InitializeComponent();

            this.deviceManager = deviceManager;
            this.deviceManager.ActiveDeviceChanged += (s, e) => this.SafeInvoke(this.UpdateControlState);

            this.sequenceNo = this.startOfNo;
            this.patternToolTipText = Properties.Resources.FormCapture_FileNamePatternHelp;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.SetupContextMenu();
            this.SetupPicturePreview();

            this.uxSaveDirectoryText.Text = Properties.Resources.FormCapture_DefaultSaveDirectoryPath;
            this.uxPatternText.Text = Properties.Resources.FormCapture_DefaultFileNamePattern;

            this.uxIntervalNumeric.Minimum = 1;
            this.uxIntervalNumeric.Value = this.defaultInterval;
            this.uxCountNumeric.Minimum = 1;
            this.uxCountNumeric.Value = this.defaultCount;

            this.uxContinuousCheck.CheckedChanged += (s, ee) => this.UpdateControlState();
            this.uxCountCheck.CheckedChanged += (s, ee) => this.UpdateControlState();

            this.uxStartButton.Enabled = (this.deviceManager.ActiveDevice != null);
            this.uxStartButton.Click += this.UxStartButton_Click;

            this.uxFileGridPanel = new GridPanel();
            this.uxFileGridPanel.Dock = DockStyle.Fill;
            this.uxFileGridPanel.AutoGenerateColumns = false;
            this.uxFileGridPanel.DataSource = this.capturedFileInfos;
            this.uxFileGridPanel.Columns.Add(
                this.CreateColumn(Properties.Resources.General_Name, nameof(CaptureFileInfo.Name), 240));
            this.uxFileGridPanel.Columns.Add(
                this.CreateColumn(Properties.Resources.General_Size, nameof(CaptureFileInfo.KiroBytes), 50, "#,##0 KB"));
            this.uxFileGridPanel.Columns.Add(
                this.CreateColumn(Properties.Resources.General_DateTime, nameof(CaptureFileInfo.DateTime), 120, "G"));
            foreach(DataGridViewColumn column in this.uxFileGridPanel.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            this.uxFileGridPanel.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.uxFileGridPanel.ContextMenuStrip = this.fileGridContextMenu;
            this.uxFileGridPanel.SelectionChanged += this.FileGridPanel_SelectionChanged;
            this.uxFileGridPanel.KeyDown += this.UxFileGridPanel_KeyDown;
            this.uxFileGridPanel.CellDoubleClick += (s, ee) => this.fileGridContextMenu.Items[nameof(FileGridContextMenuItems.OpenFile)].PerformClick();
            this.uxSplitContainer.Panel1.Controls.Add(this.uxFileGridPanel);

            this.uxToolTip.SetToolTip(this.uxPatternText, this.patternToolTipText);
            this.uxToolTip.AutoPopDelay = 30000;

            this.UpdateControlState();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            this.uxSplitContainer.SplitterDistance = 420;
            this.uxSplitContainer.FixedPanel = FixedPanel.Panel1;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            this.captureContext.Stop();
            this.captureContext = null;
        }

        void SetupContextMenu()
        {
            this.fileGridContextMenu.Items.Add(
                Properties.Resources.Menu_OpenFile,
                this.uxImageList.Images["page.png"],
                (s, e) => this.OpenSelectedFile())
                .Name = nameof(FileGridContextMenuItems.OpenFile);
            this.fileGridContextMenu.Items.Add(
                Properties.Resources.Menu_OpenDirectory,
                this.uxImageList.Images["folder.png"],
                (s, e) => this.OpenSelectedFileDirectory())
                .Name = nameof(FileGridContextMenuItems.OpenDirectory);
            this.fileGridContextMenu.Items.Add(
                Properties.Resources.Menu_CopyImageToClipboard,
                this.uxImageList.Images["page_copy.png"],
                (s, e) => this.CopyImageToClipboard())
                .Name = nameof(FileGridContextMenuItems.Copy);
            this.fileGridContextMenu.Items.Add(new ToolStripSeparator());
            this.fileGridContextMenu.Items.Add(
                Properties.Resources.Menu_Delete,
                this.uxImageList.Images["cross.png"],
                (s, e) => this.DeleteSelectedFile())
                .Name = nameof(FileGridContextMenuItems.Delete);

            this.fileGridContextMenu.Opening += (s, e) =>
            {
                var count = this.uxFileGridPanel.SelectedRows.Count;
                this.fileGridContextMenu.Items[nameof(FileGridContextMenuItems.OpenFile)].Enabled = (count == 1);
                this.fileGridContextMenu.Items[nameof(FileGridContextMenuItems.Copy)].Enabled = (count == 1);
                e.Cancel = (count <= 0);
            };
        }

        void OpenSelectedFile()
        {
            var fileInfo = this.selectedCapturedFileInfos.FirstOrDefault();
            if (fileInfo == null) return;
            Process.Start(fileInfo.FullPath);
        }

        void OpenSelectedFileDirectory()
        {
            var fileInfo = this.selectedCapturedFileInfos.FirstOrDefault();
            if (fileInfo == null) return;
            Process.Start("EXPLORER.EXE", $"/select,\"{fileInfo.FullPath}\"");
        }

        void CopyImageToClipboard()
        {
            var fileInfo = this.selectedCapturedFileInfos.FirstOrDefault();
            if (fileInfo == null) return;
            try
            {
                var image = Image.FromFile(fileInfo.FullPath);
                Clipboard.SetImage(image);
                image.Dispose();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        void DeleteSelectedFile()
        {
            var result = MessageBox.Show(
                string.Format(Properties.Resources.DialogMessage_DeleteXFiles, this.selectedCapturedFileInfos.Count),
                Properties.Resources.DialogTitle_DeleteFile,
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (result != DialogResult.OK) return;

            var removedFileInfos = this.selectedCapturedFileInfos.ToList();
            // 削除中に描画されないよう先にグリッドビューから消しとく
            removedFileInfos.ForEach(f => this.capturedFileInfos.Remove(f));
            foreach (var fileInfo in removedFileInfos)
            {
                try
                {
                    FileSystem.DeleteFile(fileInfo.FullPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
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
                var pictureBoxList = new List<PictureBox>();
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
                        pictureBox.Click += PictureBox_Click;
                        tablePanel.Controls.Add(pictureBox, x, y);
                        pictureBoxList.Add(pictureBox);
                    }
                }
                this.pictureBoxLists.Add(pictureBoxList);
                this.pictureTablePanels.Add(tablePanel);
                this.uxSplitContainer.Panel2.Controls.Add(tablePanel);
            }
            var blackPanel = new Panel();
            blackPanel.BackColor = Color.Black;
            blackPanel.Dock = DockStyle.Fill;
            this.uxSplitContainer.Panel2.Controls.Add(blackPanel);
        }

        Image GetPreviewImage(string path)
        {
            var cachedImage = this.previewImageCache.FirstOrDefault(i => (string)i.Tag == path);
            if(cachedImage != null)
            {
                this.previewImageCache.Remove(cachedImage);
            }
            else
            {
                try
                {
                    var originalImage = Image.FromFile(path);
                    var size = originalImage.Size;
                    var ratio = (float)size.Width / size.Height;
                    if (size.Width > this.previewImageSizeLimit)
                    {
                        size.Height = size.Height * this.previewImageSizeLimit / size.Width;
                        size.Width = this.previewImageSizeLimit;
                    }
                    if (size.Height > this.previewImageSizeLimit)
                    {
                        size.Width = size.Width * this.previewImageSizeLimit / size.Height;
                        size.Height = this.previewImageSizeLimit;
                    }
                    cachedImage = originalImage.GetThumbnailImage(size.Width, size.Height, null, IntPtr.Zero);
                    cachedImage.Tag = path;
                    originalImage.Dispose();
                    if (this.previewImageCache.Count >= this.previewImageCacheCapacity)
                    {
                        this.previewImageCache.RemoveFirst();
                    }
                }
                catch(Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                    return null;
                }
            }

            this.previewImageCache.AddLast(cachedImage);
            return cachedImage;
        }

        private void UxFileGridPanel_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                this.OpenSelectedFile();
                e.SuppressKeyPress = true;
            }
            else if(e.KeyCode == Keys.Delete)
            {
                this.DeleteSelectedFile();
                e.SuppressKeyPress = true;
            }
        }

        void PictureBox_Click(object sender, EventArgs e)
        {
            var pictureBox = sender as PictureBox;
            if (pictureBox == null) return;
            var fileInfo = pictureBox.Tag as CaptureFileInfo;
            if (fileInfo == null) return;
            this.uxFileGridPanel.ClearSelection();
            var index = this.capturedFileInfos.IndexOf(fileInfo);
            if(index >= 0)
            {
                this.uxFileGridPanel.Rows[index].Selected = true;
            }
        }

        void FileGridPanel_SelectionChanged(object sender, EventArgs e)
        {
            this.selectedCapturedFileInfos.Clear();
            foreach (DataGridViewRow row in this.uxFileGridPanel.SelectedRows)
            {
                this.selectedCapturedFileInfos.Add(this.capturedFileInfos[row.Index]);
            }

            var selectedCount = this.uxFileGridPanel.SelectedRows.Count;
            if (selectedCount <= 0)
            {
                for (var i = 0; i < this.picturePreviewCountMax; i++)
                {
                    this.pictureTablePanels[i].Visible = false;
                }
                return;
            }

            var visibleIndex = 0;
            for (var i = 1; i <= this.picturePreviewCountMax; i++)
            {
                var boxCount = i * i;
                if (selectedCount <= boxCount || i == this.picturePreviewCountMax)
                {
                    var pictureBoxList = this.pictureBoxLists[i - 1];
                    var offset = (selectedCount > boxCount) ? selectedCount - boxCount : 0;
                    for(var boxIndex = 0; boxIndex < boxCount; boxIndex++)
                    {
                        if (boxIndex < selectedCount)
                        {
                            // SelectedRowsには最後に選ばれた項目から順に入ってる
                            var rowIndex = this.uxFileGridPanel.SelectedRows[(selectedCount- offset) - boxIndex - 1].Index;
                            var fileInfo = this.capturedFileInfos[rowIndex];
                            var image = this.GetPreviewImage(fileInfo.FullPath);
                            pictureBoxList[boxIndex].Image = image;
                            pictureBoxList[boxIndex].Tag = (image != null) ? fileInfo : null;
                        }
                        else
                        {
                            pictureBoxList[boxIndex].Image = null;
                            pictureBoxList[boxIndex].Tag = null;
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
                this.captureContext.Stop();
                this.captureContext = null;
            }

            this.UpdateControlState();
        }

        void CaptureContext_Captured(object sender, Bitmap bitmap)
        {
            var filePath = this.SaveCaptureToFile(bitmap);
            this.SafeInvoke(() =>
            {
                this.capturedFileInfos.Add(new CaptureFileInfo(filePath));
                bitmap.Dispose();
                bitmap = null;

                var rowCount = this.uxFileGridPanel.Rows.Count;
                var lastBeforeRow = (rowCount >= 2) ? this.uxFileGridPanel.Rows[rowCount - 2] : null;
                if (lastBeforeRow != null && lastBeforeRow.Selected)
                {
                    if(this.uxFileGridPanel.SelectedRows.Count == 1)
                    {
                        lastBeforeRow.Selected = false;
                    }
                    this.uxFileGridPanel.Rows[rowCount - 1].Selected = true;
                    this.uxFileGridPanel.FirstDisplayedScrollingRowIndex = rowCount - 1;
                }
                this.UpdateControlState();
            });
        }

        void CaptureContext_Finished(object sender, EventArgs e)
        {
            this.captureContext?.Stop();
            this.captureContext = null;
            this.SafeInvoke(() => this.UpdateControlState());
        }

        string SaveCaptureToFile(Bitmap bitmap)
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
                if (this.captureContext.RemainingCount >= 0 && this.captureContext.RemainingCount != int.MaxValue)
                {
                    this.uxStartButton.Text = string.Format(
                        Properties.Resources.FormCapture_CaptureButtonLabel_ContinousLimited,
                        this.captureContext.RemainingCount);
                }
                else
                {
                    this.uxStartButton.Text = string.Format(
                        Properties.Resources.FormCapture_CaptureButtonLabel_ContinousLimitless,
                        this.captureContext.CapturedCount);
                }
                this.uxStartButton.Enabled = true;
                this.uxSettingPanel.Enabled = false;
            }
            else
            {
                this.uxStartButton.Text = Properties.Resources.FormCapture_CaptureButtonLabel_Start;
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
                name = name
                    .Replace("{device-id}", device?.Id ?? "-")
                    .Replace("{device-model}", device?.Model ?? "-")
                    .Replace("{device-name}", device?.Name ?? "-")
                    .Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"))
                    .Replace("{time}", DateTime.Now.ToString("HHmmss"));
                var mainNo = this.sequenceNo.ToString("0000");
                if (this.captureContext != null && this.captureContext.Mode == CaptureContext.CaptureMode.Continuous)
                {
                    var subNo = (this.captureContext.CapturedCount % (this.endOfSequenceNo + 1)).ToString("0000");
                    name = name.Replace("{no}", $"{mainNo}-{subNo}");
                }
                else
                {
                    name = name.Replace("{no}", mainNo);
                }
                this.sequenceNo = ++this.sequenceNo > this.endOfSequenceNo ? this.startOfNo : this.sequenceNo;
            }
            return name;
        }
    }
}
