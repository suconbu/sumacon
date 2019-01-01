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
        DeviceManager deviceManager;
        CaptureContext captureContext;
        int sequenceNo;
        List<FileInfo> capturedFileInfos = new List<FileInfo>();
        List<ListViewItem> listItems = new List<ListViewItem>();
        string selectedFilePath;

        readonly int defaultInterval = 5;
        readonly int defaultCount = 10;
        readonly string defaultSaveDirectory = @".\screencapture";
        readonly string defaultPattern = "{device-model}_{date}_{time}_{no}.png";
        readonly string labelStart = "Capture";
        readonly string labelStop = "Stop";
        readonly int startOfNo = 1;
        readonly int endOfNo = 9999;
        readonly string patternToolTipText;
        //readonly string fileNameFilter = "*.png";
        //readonly string deviceSaveDirectory = "/sdcard/Pictures/Screenshots";

        public FormCapture(DeviceManager deviceManager)
        {
            InitializeComponent();

            this.deviceManager = deviceManager;
            this.deviceManager.ActiveDeviceChanged += (s, e) => this.SafeInvoke(this.UpdateControlState);

            //this.watcher.Filter = this.fileNameFilter;
            //this.watcher.Changed += (s, e) => Delay.SetTimeout(() => this.UpdateFileList(), 100, this, Util.GetCurrentMethodName(true));
            //this.watcher.Created += (s, e) => Delay.SetTimeout(() => this.UpdateFileList(), 100, this, Util.GetCurrentMethodName(true));
            //this.watcher.Renamed += (s, e) => Delay.SetTimeout(() => this.UpdateFileList(), 100, this, Util.GetCurrentMethodName(true));
            //this.watcher.Deleted += (s, e) => Delay.SetTimeout(() => this.UpdateFileList(), 100, this, Util.GetCurrentMethodName(true));
            //this.watcher.SynchronizingObject = this;

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

            this.uxPreviewPicture.BackColor = Color.Black;

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

        //void UpdateFileList()
        //{
        //    Trace.TraceInformation("UpdateFileList");
        //    var directoryPath = this.uxSaveDirectoryText.Text;
        //    this.capturedFileInfos.Clear();
        //    this.listItems.Clear();
        //    if (Directory.Exists(directoryPath))
        //    {
        //        var paths = Directory.EnumerateFiles(directoryPath, this.fileNameFilter, SearchOption.TopDirectoryOnly);
        //        var selectedFileName = Path.GetFileName(this.selectedFilePath);
        //        foreach (var path in paths)
        //        {
        //            var fileInfo = new FileInfo(path);
        //            this.capturedFileInfos.Add(fileInfo);
        //            var item = new ListViewItem(new string[3]);
        //            item.Name = fileInfo.FullName;
        //            item.SubItems[0].Text = fileInfo.Name;
        //            item.SubItems[1].Text = $"{fileInfo.Length / 1024:#,##0} KB";
        //            item.SubItems[2].Text = fileInfo.LastWriteTime.ToString();
        //            this.listItems.Add(item);
        //        }
        //        var index = (this.uxFileListView.SelectedIndices.Count) > 0 ? this.uxFileListView.SelectedIndices[0] : -1;
        //        this.uxFileListView.Items.Clear();
        //        this.uxFileListView.Items.AddRange(this.listItems.ToArray());
        //        if (index >= 0)
        //        {
        //            this.uxFileListView.Items[index].Selected = true;
        //        }
        //        //this.uxFileListView.VirtualListSize = this.fileInfos.Count;
        //    }
        //    else
        //    {
        //        //this.uxFileListView.VirtualListSize = 0;
        //        this.uxFileListView.Items.Clear();
        //    }

        //    this.uxFileListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        //}

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
                    this.captureContext.Captured += (s, bitmap) =>
                    {
                        this.SaveCapture(bitmap);
                        this.SafeInvoke(() =>
                        {
                            this.uxPreviewPicture.Image = bitmap;
                            this.UpdateControlState();
                        });
                    };
                    this.captureContext.Finished += (s, ee) =>
                    {
                        this.captureContext?.Dispose();
                        this.captureContext = null;
                        this.SafeInvoke(() => this.UpdateControlState());
                    };
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

        void SaveCapture(Bitmap bitmap)
        {
            try
            {
                var directoryPath = this.uxSaveDirectoryText.Text;
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    //this.watcher.EnableRaisingEvents = true;
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
            if (this.captureContext != null && this.captureContext.Mode == CaptureContext.CaptureMode.Continuous)
            {
                this.uxSettingPanel.Enabled = false;
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
                this.sequenceNo = ++this.sequenceNo > this.endOfNo ? this.startOfNo : this.sequenceNo;
            }
            return name;
        }

        class CaptureContext : IDisposable
        {
            public enum CaptureMode { Single, Continuous }

            public event EventHandler<Bitmap> Captured = delegate { };
            public event EventHandler Finished = delegate { };

            public CaptureMode Mode { get; private set; }
            public int RemainingCount
            {
                get { return (this.Mode == CaptureMode.Continuous && this.continuousCaptureTo > 0) ? (this.continuousCaptureTo - this.capturedCount) : int.MaxValue; }
            }
            public int CapturedCount { get { return this.capturedCount; } }

            DateTime startedAt;
            int continuousCaptureTo;
            int capturedCount;
            int intervalMilliseconds;
            bool skipSame;
            public string previousImageHash;
            public string continuousTimeoutId;
            CommandContext commandContext;
            DeviceManager deviceManager;

            public static CaptureContext SingleCapture(DeviceManager deviceManager)
            {
                var instance = new CaptureContext();
                instance.deviceManager = deviceManager;
                instance.Mode = CaptureMode.Single;
                return instance;
            }

            public static CaptureContext ContinuousCapture(DeviceManager deviceManager, int intervalMilliseconds, bool skipSame, int count = 0)
            {
                var instance = new CaptureContext();
                instance.deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
                instance.Mode = CaptureMode.Continuous;
                instance.intervalMilliseconds = (intervalMilliseconds >= 1) ? intervalMilliseconds : 1;
                instance.continuousCaptureTo = (count >= 0) ? count : 0;
                instance.skipSame = skipSame;
                return instance;
            }

            public void Start()
            {
                this.RunCapture();
            }

            void RunCapture()
            {
                var device = this.deviceManager.ActiveDevice;
                if (device == null)
                {
                    this.Finished(this, EventArgs.Empty);
                    return;
                }
                this.startedAt = DateTime.Now;
                Trace.TraceInformation("StartCapture - CaptureAsync");
                this.commandContext = device.Screen.CaptureAsync(this.ScreenCaptured);
                if (this.commandContext == null)
                {
                    this.Finished(this, EventArgs.Empty);
                    return;
                }
            }

            void ScreenCaptured(Bitmap bitmap)
            {
                if(bitmap == null || this.disposed)
                {
                    this.Finished(this, EventArgs.Empty);
                    return;
                }
                this.commandContext = null;

                var skip = false;
                if(this.skipSame)
                {
                    var hash = bitmap.ComputeMD5();
                    if(this.previousImageHash == hash)
                    {
                        skip = true;
                    }
                    this.previousImageHash = hash;
                }

                if(!skip)
                {
                    this.capturedCount++;
                }

                if (this.Mode == CaptureMode.Continuous && this.RemainingCount > 0)
                {
                    // 連続撮影中なら撮影に掛かった時間を勘案して次を予約しておく
                    var elapsed = (int)(DateTime.Now - this.startedAt).TotalMilliseconds;
                    var nextInterval = Math.Max(1, this.intervalMilliseconds - elapsed);
                    Trace.TraceInformation($"StartCapture - elapsed: {elapsed} ms, nextInterval: {nextInterval} ms");
                    this.continuousTimeoutId = Delay.SetTimeout(() => this.RunCapture(), nextInterval);
                }
                
                if(!skip)
                {
                    this.Captured(this, bitmap);
                }

                if (this.Mode == CaptureMode.Single ||
                    (this.Mode == CaptureMode.Continuous && this.RemainingCount == 0))
                {
                    this.Finished(this, EventArgs.Empty);
                }
            }

            #region IDisposable Support
            bool disposed = false;

            public virtual void Dispose()
            {
                if (this.disposed) return;

                this.commandContext?.Cancel();
                this.commandContext = null;

                this.disposed = true;
            }
            #endregion
        }

    }
}
