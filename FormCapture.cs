﻿using Suconbu.Toolbox;
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
        DeviceManager deviceManager;
        CommandContext captureContext;
        string timeoutId;
        DateTime captureStartedAt;
        bool continuousCapturing;
        int sequenceNo;
        int remainingCount;
        int capturedCount;
        string previousBitmapMd5;
        FileSystemWatcher watcher = new FileSystemWatcher();
        List<FileInfo> fileInfos = new List<FileInfo>();
        List<ListViewItem> listItems = new List<ListViewItem>();
        string selectedFilePath;

        readonly int defaultInterval = 5;
        readonly int defaultCount = 10;
        readonly string defaultSaveDirectory = @".\screencapture";
        readonly string defaultPattern = "{device-model}_{date}_{time}_{no}.png";
        readonly string labelStart = "Capture";
        readonly string labelStop = "Stop";
        readonly int sequenceNoStart = 1;
        readonly int sequenceNoMax = 9999;
        readonly string patternToolTipText;
        readonly string fileNameFilter = "*.png";
        //readonly string deviceSaveDirectory = "/sdcard/Pictures/Screenshots";

        public FormCapture(DeviceManager deviceManager)
        {
            InitializeComponent();

            this.deviceManager = deviceManager;
            this.deviceManager.ActiveDeviceChanged += (s, e) => this.SafeInvoke(this.UpdateControlState);

            this.watcher.Filter = this.fileNameFilter;
            this.watcher.Changed += (s, e) => Delay.SetTimeout(() => this.UpdateFileList(), 100, this, Util.GetCurrentMethodName(true));
            this.watcher.Created += (s, e) => Delay.SetTimeout(() => this.UpdateFileList(), 100, this, Util.GetCurrentMethodName(true));
            this.watcher.Renamed += (s, e) => Delay.SetTimeout(() => this.UpdateFileList(), 100, this, Util.GetCurrentMethodName(true));
            this.watcher.Deleted += (s, e) => Delay.SetTimeout(() => this.UpdateFileList(), 100, this, Util.GetCurrentMethodName(true));
            this.watcher.SynchronizingObject = this;

            this.sequenceNo = this.sequenceNoStart;
            var sb = new StringBuilder();
            sb.AppendLine("{device-id} : e.g. 'HXC8KSKL99XYZ'");
            sb.AppendLine("{device-model} : e.g. 'Nexus_9'");
            sb.AppendLine("{device-name} : e.g. 'MyTablet'");
            sb.AppendLine("{date} : '2018-12-31'");
            sb.AppendLine("{time} : '12-34-56'");
            sb.AppendLine("{no} : Sequential number based on '0001'. This is reset in application start.");
            this.patternToolTipText = sb.ToString();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.uxPreviewPicture.BackColor = Color.Black;

            this.uxSaveDirectoryText.TextChanged += (s, ee) => Delay.SetTimeout(() => this.SaveDirectoryChanged(), 500, this, Util.GetCurrentMethodName(true));
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

            this.uxFileListView.FullRowSelect = true;
            this.uxFileListView.Columns.Add("Name");
            this.uxFileListView.Columns.Add("Date");
            this.uxFileListView.Columns.Add("Size");
            //this.uxFileListView.RetrieveVirtualItem += (s, ee) =>
            //{
            //    ee.Item = (ee.ItemIndex < this.listItems.Count) ? this.listItems[ee.ItemIndex] : ee.Item;
            //    ee.Item.Selected = (ee.Item.Name == this.selectedFilePath);
            //};
            //this.uxFileListView.VirtualMode = true;

            this.UpdateControlState();
        }

        void SaveDirectoryChanged()
        {
            var directoryPath = this.uxSaveDirectoryText.Text;
            if(Directory.Exists(directoryPath))
            {
                this.watcher.Path = directoryPath;
                this.watcher.EnableRaisingEvents = true;
            }
            else
            {
                this.watcher.EnableRaisingEvents = false;
                this.watcher.Path = directoryPath;
            }
            this.UpdateFileList();
        }

        void UpdateFileList()
        {
            Trace.TraceInformation("UpdateFileList");
            var directoryPath = this.uxSaveDirectoryText.Text;
            this.fileInfos.Clear();
            this.listItems.Clear();
            if (Directory.Exists(directoryPath))
            {
                var paths = Directory.EnumerateFiles(directoryPath, this.fileNameFilter, SearchOption.TopDirectoryOnly);
                var selectedFileName = Path.GetFileName(this.selectedFilePath);
                foreach (var path in paths)
                {
                    var fileInfo = new FileInfo(path);
                    this.fileInfos.Add(fileInfo);
                    var item = new ListViewItem(new string[3]);
                    item.Name = fileInfo.FullName;
                    item.SubItems[0].Text = fileInfo.Name;
                    item.SubItems[1].Text = $"{fileInfo.Length / 1024:#,##0} KB";
                    item.SubItems[2].Text = fileInfo.LastWriteTime.ToString();
                    this.listItems.Add(item);
                }
                var index = (this.uxFileListView.SelectedIndices.Count) > 0 ? this.uxFileListView.SelectedIndices[0] : -1;
                this.uxFileListView.Items.Clear();
                this.uxFileListView.Items.AddRange(this.listItems.ToArray());
                if (index >= 0)
                {
                    this.uxFileListView.Items[index].Selected = true;
                }
                //this.uxFileListView.VirtualListSize = this.fileInfos.Count;
            }
            else
            {
                //this.uxFileListView.VirtualListSize = 0;
                this.uxFileListView.Items.Clear();
            }

            this.uxFileListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        void UxStartButton_Click(object sender, EventArgs e)
        {
            if (!this.continuousCapturing)
            {
                this.capturedCount = 0;
                this.continuousCapturing = this.uxContinuousCheck.Checked;
                if (this.continuousCapturing)
                {
                    // 撮影枚数
                    this.remainingCount = this.uxCountCheck.Checked ? (int)this.uxCountNumeric.Value : -1;
                }
                this.StartCapture();
            }
            else
            {
                this.StopCapture();
            }

            this.UpdateControlState();
        }

        void StartCapture()
        {
            this.captureContext?.Cancel();
            this.captureContext = null;

            var device = this.deviceManager.ActiveDevice;
            if (device == null) return;

            this.captureStartedAt = DateTime.Now;
            Trace.TraceInformation("StartCapture - CaptureAsync");

            //var saveTo = $"{this.deviceSaveDirectory}/{this.GetNextFileName()}";
            //this.captureContext = device.Screen.CaptureIntoDeviceAsync(saveTo, path =>
            this.captureContext = device.Screen.CaptureAsync(this.Captured);

            this.UpdateControlState();
        }

        void StopCapture()
        {
            this.captureContext?.Cancel();
            this.captureContext = null;
            Delay.ClearTimeout(this.timeoutId);
            this.timeoutId = null;
            this.remainingCount = 0;
            this.capturedCount = 0;
            this.continuousCapturing = false;
            this.previousBitmapMd5 = null;

            this.UpdateControlState();
        }

        void Captured(Bitmap bitmap)
        {
            this.captureContext = null;
            if (bitmap == null) return;

            var skip = false;
            if (this.continuousCapturing && this.uxSkipCheck.Checked)
            {
                // 同一画像判定
                var md5 = bitmap.ComputeMD5();
                if (this.previousBitmapMd5 == md5)
                {
                    skip = true;
                }
                this.previousBitmapMd5 = md5;
            }

            if (!skip)
            {
                if (this.remainingCount > 0)
                {
                    this.remainingCount--;
                }
                this.capturedCount++;
            }

            if (this.continuousCapturing &&
                (this.remainingCount > 0 || this.remainingCount == -1))
            {
                // 連続撮影中なら撮影に掛かった時間を勘案して次へ
                var elapsed = (int)(DateTime.Now - this.captureStartedAt).TotalMilliseconds;
                var nextInterval = (int)this.uxIntervalNumeric.Value * 1000;
                nextInterval = Math.Max(1, nextInterval - elapsed);
                Trace.TraceInformation($"StartCapture - elapsed: {elapsed} ms, nextInterval: {nextInterval} ms");
                this.timeoutId = Delay.SetTimeout(() => this.StartCapture(), nextInterval, this);
                this.SafeInvoke(() => this.UpdateControlState());
            }
            else
            {
                // 撮影終了
                this.SafeInvoke(() => this.StopCapture());
            }

            if (!skip)
            {
                // SaveとPictureBoxの描画がかち合うとInvalidOperationException出るのでこの順番
                this.SaveCapture(bitmap);
                this.SafeInvoke(() => this.uxPreviewPicture.Image = bitmap);
            }
        }

        void SaveCapture(Bitmap bitmap)
        {
            try
            {
                var directoryPath = this.uxSaveDirectoryText.Text;
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    this.watcher.EnableRaisingEvents = true;
                }
                var fileName = this.GetNextFileName();
                var saveTo = Path.Combine(directoryPath, fileName);
                bitmap.Save(saveTo);
                this.selectedFilePath = Path.GetFullPath(saveTo);
            }
            catch(Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        void UpdateControlState()
        {
            if (this.continuousCapturing)
            {
                this.uxSettingPanel.Enabled = false;
                var sb = new StringBuilder();
                sb.AppendLine(this.labelStop);
                if (this.remainingCount >= 0)
                {
                    sb.Append($"({this.remainingCount} shots remains)");
                }
                else
                {
                    sb.Append($"({this.capturedCount} captured)");
                }
                this.uxStartButton.Text = sb.ToString();
            }
            else
            {
                this.uxSettingPanel.Enabled = true;
                this.uxStartButton.Text = this.labelStart;
                this.uxStartButton.Enabled = (this.captureContext == null);
            }

            this.uxConinuousPanel.Enabled = this.uxContinuousCheck.Checked;
            this.uxCountNumeric.Enabled = this.uxCountCheck.Checked;

            this.uxOuterPanel.Enabled = (this.deviceManager.ActiveDevice != null);
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
                this.sequenceNo = ++this.sequenceNo > this.sequenceNoMax ? this.sequenceNoStart : this.sequenceNo;
            }
            return name;
        }
    }
}
