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

namespace Suconbu.Sumacon
{
    public partial class FormCapture : FormBase
    {
        Sumacon sumacon;
        CaptureContext captureContext;
        GridPanel uxFileGridPanel;
        BindingList<FileInfo> capturedFileInfos = new BindingList<FileInfo>();
        List<FileInfo> selectedCapturedFileInfos = new List<FileInfo>();
        // PictureBox.TagにはFileInfoを設定
        List<List<PictureBox>> pictureBoxLists = new List<List<PictureBox>>();
        List<TableLayoutPanel> pictureTablePanels = new List<TableLayoutPanel>();
        ContextMenuStrip fileGridContextMenu = new ContextMenuStrip();
        // Image.Tagにはファイルフルパスを設定
        LinkedList<Image> previewImageCache = new LinkedList<Image>();
        int sequenceNo = 1;

        //readonly int defaultInterval = 1;
        //readonly int defaultCount = 10;
        readonly string patternToolTipText;
        readonly int picturePreviewCountMax = 5;
        readonly int previewImageCacheCapacity = (int)(5 * 5 * 1.2);
        readonly int previewImageSizeLimit = 800;

        public FormCapture(Sumacon sumacon)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.sumacon = sumacon;
            this.sumacon.DeviceManager.ActiveDeviceChanged += DeviceManager_ActiveDeviceChanged;

            this.SetupContextMenu();
            this.SetupPicturePreview();

            this.uxIntervalNumeric.Minimum = 1;
            this.uxCountNumeric.Minimum = 1;

            this.uxContinuousCheck.CheckedChanged += (s, ee) => this.UpdateControlState();
            this.uxCountCheck.CheckedChanged += (s, ee) => this.UpdateControlState();
            this.uxStartButton.Click += this.UxStartButton_Click;

            this.uxFileGridPanel = new GridPanel();
            this.uxFileGridPanel.Dock = DockStyle.Fill;
            this.uxFileGridPanel.AutoGenerateColumns = false;
            this.uxFileGridPanel.DataSource = this.capturedFileInfos;
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

            this.uxToolTip.SetToolTip(this.uxPatternText, this.patternToolTipText);
            this.uxToolTip.AutoPopDelay = 30000;

            this.patternToolTipText = Properties.Resources.FileNamePatternHelp;
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

            Properties.Settings.Default.CaptureSplitterDistance = this.uxSplitContainer.SplitterDistance;
            Properties.Settings.Default.CaptureSaveDirectoryPath = this.uxSaveDirectoryText.Text;
            Properties.Settings.Default.CaptureFileNamePattern = this.uxPatternText.Text;
            Properties.Settings.Default.CaptureContinuousEnabled = this.uxContinuousCheck.Checked;
            Properties.Settings.Default.CaptureIntervalSeconds = (int)this.uxIntervalNumeric.Value;
            Properties.Settings.Default.CaptureCountEnabled = this.uxCountCheck.Checked;
            Properties.Settings.Default.CaptureCount = (int)this.uxCountNumeric.Value;
            Properties.Settings.Default.CaptureSkipDuplicateEnabled = this.uxSkipDuplicatedImageCheck.Checked;

            this.captureContext?.Stop();
            this.captureContext = null;

            this.sumacon.DeviceManager.ActiveDeviceChanged -= DeviceManager_ActiveDeviceChanged;
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

            var copyImaegItem = this.fileGridContextMenu.Items.Add(
                Properties.Resources.Menu_CopyImageToClipboard,
                this.uxImageList.Images["page_copy.png"],
                (s, e) => this.CopyImageToClipboard()) as ToolStripMenuItem;

            this.fileGridContextMenu.Items.Add(new ToolStripSeparator());

            var deleteItem = this.fileGridContextMenu.Items.Add(
                Properties.Resources.Menu_Delete + " (Del)",
                this.uxImageList.Images["cross.png"],
                (s, e) => this.DeleteSelectedFile()) as ToolStripMenuItem;
            deleteItem.ShortcutKeys = Keys.Delete;

            this.fileGridContextMenu.Opening += (s, e) =>
            {
                var count = this.uxFileGridPanel.SelectedRows.Count;
                openFileItem.Enabled = (count == 1);
                copyImaegItem.Enabled = (count == 1);
                e.Cancel = (count <= 0);
            };
        }

        void OpenSelectedFile()
        {
            var fileInfo = this.selectedCapturedFileInfos.FirstOrDefault();
            if (fileInfo == null) return;
            Process.Start(fileInfo.FullName);
        }

        void OpenSelectedFileDirectory()
        {
            var fileInfo = this.selectedCapturedFileInfos.FirstOrDefault();
            if (fileInfo == null) return;
            Process.Start("EXPLORER.EXE", $"/select,\"{fileInfo.FullName}\"");
        }

        void CopyImageToClipboard()
        {
            var fileInfo = this.selectedCapturedFileInfos.FirstOrDefault();
            if (fileInfo == null) return;
            try
            {
                var image = Image.FromFile(fileInfo.FullName);
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
            //var result = MessageBox.Show(
            //    string.Format(Properties.Resources.DialogMessage_DeleteXFiles, this.selectedCapturedFileInfos.Count),
            //    Properties.Resources.DialogTitle_DeleteFile,
            //    MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            //if (result != DialogResult.OK) return;

            var removedFileInfos = this.selectedCapturedFileInfos.ToList();
            // 削除中に描画されないよう先にグリッドビューから消しとく
            removedFileInfos.ForEach(f => this.capturedFileInfos.Remove(f));
            foreach (var fileInfo in removedFileInfos)
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

        void DeviceManager_ActiveDeviceChanged(object sender, Device previousDevice)
        {
            this.SafeInvoke(this.UpdateControlState);
        }

        private void UxFileGridPanel_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                this.OpenSelectedFile();
                e.SuppressKeyPress = true;
            }
        }

        void PictureBox_Click(object sender, EventArgs e)
        {
            var pictureBox = sender as PictureBox;
            if (pictureBox == null) return;
            var fileInfo = pictureBox.Tag as FileInfo;
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
                            var image = this.GetPreviewImage(fileInfo.FullName);
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

        void UxStartButton_Click(object sender, EventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());

            if (this.captureContext == null || this.captureContext.Mode == CaptureContext.CaptureMode.Single)
            {
                var device = this.sumacon.DeviceManager.ActiveDevice;
                if (device == null) return;
                if (this.uxContinuousCheck.Checked)
                {
                    var setting = this.GetContinuousCaptureSetting();
                    this.captureContext = CaptureContext.StartContinuousCapture(device, setting, this.OnCaptured, this.OnFinished);
                }
                else
                {
                    this.captureContext = CaptureContext.StartSingleCapture(device, this.OnCaptured, this.OnFinished);
                }
                this.sumacon.DeviceManager.SuspendPropertyUpdate(device);
            }
            else
            {
                // 連続撮影中止
                this.captureContext.Stop();
            }

            this.UpdateControlState();
        }

        ContinuousCaptureSetting GetContinuousCaptureSetting()
        {
            var setting = new ContinuousCaptureSetting();
            setting.IntervalMilliseconds = (int)this.uxIntervalNumeric.Value * 1000;
            setting.LimitCount = this.uxCountCheck.Checked ? (int)this.uxCountNumeric.Value : 0;
            setting.SkipDuplicatedImage = this.uxSkipDuplicatedImageCheck.Checked;
            return setting;
        }

        void OnCaptured(Bitmap bitmap)
        {
            var filePath = this.SaveCaptureToFile(bitmap);
            if (filePath == null) return;
            this.SafeInvoke(() =>
            {
                this.capturedFileInfos.Add(new FileInfo(filePath));
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

        void OnFinished()
        {
            var device = this.captureContext?.Device;
            this.captureContext = null;
            this.SafeInvoke(() =>
            {
                this.sumacon.DeviceManager.ResumePropertyUpdate(device);
                this.UpdateControlState();
            });
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
                var fileName = this.GetFileName(bitmap);
                var filePath = Path.Combine(directoryPath, fileName);
                bitmap.Save(filePath);
                this.sequenceNo++;
                return filePath;
            }
            catch(Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return null;
            }
        }

        void ApplySettings()
        {
            this.uxSplitContainer.SplitterDistance = Properties.Settings.Default.CaptureSplitterDistance;
            this.uxSaveDirectoryText.Text = Properties.Settings.Default.CaptureSaveDirectoryPath;
            this.uxPatternText.Text = Properties.Settings.Default.CaptureFileNamePattern;
            this.uxContinuousCheck.Checked = Properties.Settings.Default.CaptureContinuousEnabled;
            this.uxIntervalNumeric.Value = Properties.Settings.Default.CaptureIntervalSeconds;
            this.uxCountCheck.Checked = Properties.Settings.Default.CaptureCountEnabled;
            this.uxCountNumeric.Value = Properties.Settings.Default.CaptureCount;
            this.uxSkipDuplicatedImageCheck.Checked = Properties.Settings.Default.CaptureSkipDuplicateEnabled;

            this.UpdateControlState();
        }

        void UpdateControlState()
        {
            if (this.captureContext != null && this.captureContext.Mode == CaptureContext.CaptureMode.Continuous)
            {
                if (this.captureContext.RemainingCount >= 0 && this.captureContext.RemainingCount != int.MaxValue)
                {
                    this.uxStartButton.Text = string.Format(
                        Properties.Resources.FormCapture_ButtonLabel_ContinousLimited,
                        this.captureContext.RemainingCount);
                }
                else
                {
                    this.uxStartButton.Text = string.Format(
                        Properties.Resources.FormCapture_ButtonLabel_ContinousLimitless,
                        this.captureContext.CapturedCount);
                }
                this.uxStartButton.BackColor = Color.FromName(Properties.Resources.RecordingButtonColorName);
                this.uxStartButton.Enabled = true;
                this.uxSettingPanel.Enabled = false;
            }
            else
            {
                this.uxStartButton.Text = Properties.Resources.FormCapture_ButtonLabel_Start;
                this.uxStartButton.Enabled = (this.captureContext == null);
                this.uxStartButton.UseVisualStyleBackColor = true;
                this.uxSettingPanel.Enabled = true;
            }

            if (this.sumacon.DeviceManager.ActiveDevice == null)
            {
                this.uxStartButton.Enabled = false;
                this.uxSettingPanel.Enabled = false;
            }

            this.uxContinuousPanel.Enabled = this.uxContinuousCheck.Checked;
            this.uxCountNumeric.Enabled = this.uxCountCheck.Checked;
        }

        string GetFileName(Bitmap bitmap)
        {
            var no = (this.sequenceNo % 10000).ToString("0000");
            if (this.captureContext != null && this.captureContext.Mode == CaptureContext.CaptureMode.Continuous)
            {
                var subNo = (this.captureContext.CapturedCount % 10000).ToString("0000");
                no = $"{no}-{subNo}";
            }
            var now = DateTime.Now;
            var replacer = new Dictionary<string, string>()
            {
                { "date", now.ToString("yyyy-MM-dd") },
                { "time", now.ToString("HHmmss") },
                { "width", bitmap.Width.ToString() },
                { "height", bitmap.Height.ToString() },
                { "no", no }
            };
            var pattern = this.sumacon.DeviceManager.ActiveDevice.ToString(this.uxPatternText.Text);
            return pattern.Replace(replacer, "-");
        }
    }
}
