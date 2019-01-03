using Suconbu.Mobile;
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
    struct ContinuousCaptureSetting
    {
        public int IntervalMilliseconds;
        public bool SkipSameImage;
        public int LimitCount;
    }

    class CaptureContext : IDisposable
    {
        public enum CaptureMode { Single, Continuous }

        public Device Device { get; private set; }
        public CaptureMode Mode { get; private set; }
        public int RemainingCount
        {
            get
            {
                return
                    (this.Mode == CaptureMode.Continuous && this.continousSetting.LimitCount > 0) ?
                    (this.continousSetting.LimitCount - this.capturedCount) :
                    int.MaxValue;
            }
        }
        public int CapturedCount { get { return this.capturedCount; } }

        CommandContext commandContext;
        ContinuousCaptureSetting continousSetting;
        DateTime startedAt;
        int capturedCount;
        string previousImageHash;
        string continuousTimeoutId;

        Action<Bitmap> onCaptured = delegate { };
        Action onFinished = delegate { };

        public static CaptureContext StartSingleCapture(Device device, Action<Bitmap> onCaptured, Action onFinished)
        {
            var instance = new CaptureContext();
            instance.Device = device ?? throw new ArgumentNullException(nameof(device));
            instance.Mode = CaptureMode.Single;
            instance.onCaptured = onCaptured;
            instance.onFinished = onFinished;
            instance.RunCapture();
            return instance;
        }

        public static CaptureContext StartContinuousCapture(Device device, ContinuousCaptureSetting setting, Action<Bitmap> onCaptured, Action onFinished)
        {
            var instance = new CaptureContext();
            instance.Device = device ?? throw new ArgumentNullException(nameof(device));
            instance.Mode = CaptureMode.Continuous;
            instance.continousSetting = setting;
            instance.onCaptured = onCaptured;
            instance.onFinished = onFinished;
            instance.RunCapture();
            return instance;
        }

        public void Stop()
        {
            this.Dispose();
        }

        void RunCapture()
        {
            this.startedAt = DateTime.Now;
            this.commandContext = this.Device.Screen.CaptureAsync(this.ScreenCaptured);
            if (this.commandContext == null)
            {
                this.onFinished();
            }
        }

        void ScreenCaptured(Bitmap bitmap)
        {
            if (bitmap == null || this.disposed)
            {
                this.onFinished();
                return;
            }

            this.commandContext = null;

            var skip = false;
            if (this.Mode == CaptureMode.Continuous && this.continousSetting.SkipSameImage)
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
                var nextInterval = Math.Max(1, this.continousSetting.IntervalMilliseconds - elapsed);
                Debug.Print($"ScreenCaptured - elapsed: {elapsed} ms, nextInterval: {nextInterval} ms");
                this.continuousTimeoutId = Delay.SetTimeout(() => this.RunCapture(), nextInterval);
            }

            if (!skip)
            {
                this.onCaptured(bitmap);
            }

            if (this.Mode == CaptureMode.Single ||
                (this.Mode == CaptureMode.Continuous && this.RemainingCount == 0))
            {
                this.onFinished();
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
