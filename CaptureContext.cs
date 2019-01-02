using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Suconbu.Sumacon
{
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

        //TODO: DeviceManagerじゃなくてDevice渡すべき
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

        public void Stop()
        {
            this.Dispose();
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
            Debug.Print("RunCapture");
            this.commandContext = device.Screen.CaptureAsync(this.ScreenCaptured);
            if (this.commandContext == null)
            {
                this.Finished(this, EventArgs.Empty);
                return;
            }
        }

        void ScreenCaptured(Bitmap bitmap)
        {
            if (bitmap == null || this.disposed)
            {
                this.Finished(this, EventArgs.Empty);
                return;
            }
            this.commandContext = null;

            var skip = false;
            if (this.skipSame)
            {
                var hash = bitmap.ComputeMD5();
                if (this.previousImageHash == hash)
                {
                    skip = true;
                }
                this.previousImageHash = hash;
            }

            if (!skip)
            {
                this.capturedCount++;
            }

            if (this.Mode == CaptureMode.Continuous && this.RemainingCount > 0)
            {
                // 連続撮影中なら撮影に掛かった時間を勘案して次を予約しておく
                var elapsed = (int)(DateTime.Now - this.startedAt).TotalMilliseconds;
                var nextInterval = Math.Max(1, this.intervalMilliseconds - elapsed);
                Debug.Print($"ScreenCaptured - elapsed: {elapsed} ms, nextInterval: {nextInterval} ms");
                this.continuousTimeoutId = Delay.SetTimeout(() => this.RunCapture(), nextInterval);
            }

            if (!skip)
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
