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
        CommandContext captureContext;
        string timeoutId;
        DateTime captureStartedAt;
        bool continuousCapturing;
        int sequenceNo;
        int remainingCount;
        string previousBitmapMd5;

        readonly int defaultInterval = 5;
        readonly int defaultCount = 10;
        readonly string defaultSaveDirectory = @".\screencapture";
        readonly string defaultPattern = "{device-model}_{date}_{time}_{no}.png";
        readonly string labelStart = "Capture";
        readonly string labelStop = "Stop";
        readonly int sequenceNoStart = 1;
        readonly int sequenceNoMax = 9999;
        //readonly string deviceSaveDirectory = "/sdcard/Pictures/Screenshots";

        public FormCapture(DeviceManager deviceManager)
        {
            InitializeComponent();

            this.deviceManager = deviceManager;
            this.deviceManager.ActiveDeviceChanged += (s, e) =>
            {
                this.SafeInvoke(() => this.uxStartButton.Enabled = (this.deviceManager.ActiveDevice != null));
            };

            this.sequenceNo = this.sequenceNoStart;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.uxPreviewPicture.BackColor = Color.Black;

            this.uxSaveDirectoryText.Text = this.defaultSaveDirectory;
            this.uxIntervalNumeric.Minimum = 1;
            this.uxIntervalNumeric.Value = this.defaultInterval;
            this.uxCountNumeric.Minimum = 1;
            this.uxCountNumeric.Value = this.defaultCount;
            this.uxPatternText.Text = this.defaultPattern;

            this.uxContinuousCheck.CheckedChanged += (s, ee) => this.UpdateControlState();
            this.uxCountCheck.CheckedChanged += (s, ee) => this.UpdateControlState();

            this.uxStartButton.Enabled = (this.deviceManager.ActiveDevice != null);
            this.uxStartButton.Click += this.UxStartButton_Click;

            this.UpdateControlState();
        }

        void UxStartButton_Click(object sender, EventArgs e)
        {
            if (!this.continuousCapturing)
            {
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
        }

        void StopCapture()
        {
            this.captureContext?.Cancel();
            this.captureContext = null;
            Delay.ClearTimeout(this.timeoutId);
            this.timeoutId = null;
            this.remainingCount = 0;
            this.continuousCapturing = false;
            this.previousBitmapMd5 = null;
            this.UpdateControlState();
        }

        void Captured(Bitmap bitmap)
        {
            this.captureContext = null;
            if (bitmap == null) return;

            var skip = false;
            if (this.uxSkipCheck.Checked)
            {
                // 同一画像判定
                var md5 = bitmap.ComputeMD5();
                if (this.previousBitmapMd5 == md5)
                {
                    skip = true;
                }
                this.previousBitmapMd5 = md5;
            }

            if (!skip && this.remainingCount > 0)
            {
                this.remainingCount--;
            }

            if (this.continuousCapturing &&
                (this.remainingCount > 0 || this.remainingCount == -1))
            {
                // 連続撮影中なら撮影に掛かった時間を勘案して次へ
                var elapsed = (int)(DateTime.Now - this.captureStartedAt).TotalMilliseconds;
                var nextInterval = (int)this.uxIntervalNumeric.Value * 1000;
                nextInterval = Math.Max(1, nextInterval - elapsed);
                Trace.TraceInformation($"StartCapture - elapsed: {elapsed} ms, nextInterval: {nextInterval} ms");
                this.timeoutId = Delay.SetTimeout(() => this.StartCapture(), nextInterval);

                this.SafeInvoke(() => this.UpdateControlState());
            }
            else
            {
                // 撮影終了
                this.SafeInvoke(() => this.StopCapture());
            }

            if (!skip)
            {
                this.SafeInvoke(() => this.uxPreviewPicture.Image = bitmap);
                this.SaveCapture(bitmap);
            }
        }

        void SaveCapture(Bitmap bitmap)
        {
            try
            {
                var directory = this.uxSaveDirectoryText.Text;
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                var fileName = this.GetNextFileName();
                var saveTo = Path.Combine(directory, fileName);
                bitmap.Save(saveTo);
            }
            catch(Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        void UpdateControlState()
        {
            if(this.continuousCapturing)
            {
                this.uxSettingPanel.Enabled = false;
                if(this.remainingCount > 0)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine(this.labelStop);
                    sb.Append($"({this.remainingCount} shots remains)");
                    this.uxStartButton.Text = sb.ToString();
                }
                else
                {
                    this.uxStartButton.Text = this.labelStop;
                }
            }
            else
            {
                this.uxSettingPanel.Enabled = true;
                this.uxStartButton.Text = this.labelStart;
                this.uxStartButton.Enabled = (this.captureContext == null);
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
                this.sequenceNo = ++this.sequenceNo > this.sequenceNoMax ? this.sequenceNoStart : this.sequenceNo;
            }
            return name;
        }
    }
}
