﻿using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Suconbu.Sumacon
{
    struct RecordSetting
    {
        public int SequenceNo;
        public string DirectoryPath;
        public string FileNamePattern;
        public int TimeLimitSeconds;
        public float ViewSizeMultiply;
        public int Bitrate;
        public bool Timestamp;
    }

    class RecordContext : IDisposable
    {
        public enum RecordState { Recording, ManualStopping, Pulling, Finished, Aborted }

        public Device Device { get; private set; }
        public static int TimeLimitSecondsMax { get; } = 180;
        public RecordState State
        {
            get { return this.state; }
            set
            {
                if(this.state != value)
                {
                    this.state = value;
                    this.onStateChanged(value);
                }
            }
        }
        public DateTime StartedAt { get; private set; } = DateTime.MaxValue;
        public TimeSpan Elapsed
        {
            get
            {
                var elapsed = (this.stoppedAt == DateTime.MaxValue) ? (DateTime.Now - this.StartedAt) : (this.stoppedAt - this.StartedAt);
                return (elapsed.TotalSeconds <= this.setting.TimeLimitSeconds) ? elapsed : TimeSpan.FromSeconds(this.setting.TimeLimitSeconds);
            }
        }
        public string FilePath { get; private set; }

        RecordSetting setting;
        CommandContext recordCommandContext;
        CommandContext pullCommandContext;
        string fileName;
        string filePathInDevice;
        string filePathInPc;
        RecordState state = RecordState.Recording;
        DateTime stoppedAt = DateTime.MaxValue;

        Action<RecordState> onStateChanged = delegate { };

        readonly string deviceTemporaryDirectoryPath = "/sdcard";

        RecordContext() { }

        public static RecordContext StartNew(Device device, RecordSetting setting, Action<RecordState> onStateChanged)
        {
            var instance = new RecordContext();
            instance.Device = device;
            instance.setting = setting;
            instance.onStateChanged = onStateChanged;
            instance.StartRecord();
            return instance;
        }

        void StartRecord()
        {
            Debug.Print(Util.GetCurrentMethodName());

            if (!Directory.Exists(this.setting.DirectoryPath))
            {
                Directory.CreateDirectory(this.setting.DirectoryPath);
            }

            var size = this.Device.ScreenSize;
            if (this.Device.CurrentRotation == Screen.RotationCode.Landscape ||
                this.Device.CurrentRotation == Screen.RotationCode.LandscapeReversed)
            {
                size = size.Swapped();
            }
            if (this.setting.ViewSizeMultiply != 1.0f)
            {
                size = size.Multiplied(this.setting.ViewSizeMultiply);
            }

            this.fileName = this.GetFileName(this.setting.FileNamePattern, size);
            this.filePathInDevice = $"{this.deviceTemporaryDirectoryPath}/{this.fileName}";
            this.filePathInPc = Path.Combine(this.setting.DirectoryPath, this.fileName);

            var option = new StringBuilder();
            if (0 < this.setting.TimeLimitSeconds && this.setting.TimeLimitSeconds <= RecordContext.TimeLimitSecondsMax)
            {
                option.Append($" --time-limit {this.setting.TimeLimitSeconds}");
            }
            if(size != this.Device.ScreenSize)
            {
                option.Append($" --size {size.Width}x{size.Height}");
            }
            option.Append($" --bit-rate {this.setting.Bitrate}");

            if (this.setting.Timestamp)
            {
                option.Append($" --show-frame-time");
            }

            this.StartedAt = DateTime.Now;

            var command = $"shell screenrecord {option} {this.filePathInDevice}";
            this.recordCommandContext = this.Device.RunCommandOutputTextAsync(command, this.OnRecordCommandFinished);
        }

        public void Stop()
        {
            if (this.State != RecordState.Recording) return;

            this.State = RecordState.ManualStopping;

            // 中断後そのうちOnRecordCommandFinishedが呼ばれる
            this.recordCommandContext?.Cancel();
            this.recordCommandContext = null;
        }

        public void Cancel()
        {
            this.Dispose();
        }

        void OnRecordCommandFinished(string output)
        {
            this.recordCommandContext = null;

            if (!string.IsNullOrEmpty(output))
            {
                // なにかエラー
                Trace.TraceError(output);
                this.Dispose();
                return;
            }

            Debug.Assert(this.State == RecordState.Recording || this.state == RecordState.ManualStopping);
            this.stoppedAt = (this.state == RecordState.ManualStopping) ? DateTime.Now :
                (this.StartedAt.AddSeconds(this.setting.TimeLimitSeconds));
            this.State = RecordState.Pulling;

            var command = $"pull {this.filePathInDevice} {this.filePathInPc}";
            this.pullCommandContext = this.Device.RunCommandOutputTextAsync(command, this.OnPullCommandFinished);
        }

        void OnPullCommandFinished(string output)
        {
            this.pullCommandContext = null;

            if (output.ToLower().Contains("error"))
            {
                // なにかエラー
                Trace.TraceError(output);
                this.Dispose();
                return;
            }

            this.OnPullFinished();
        }

        void OnPullFinished()
        {
            Debug.Assert(this.State == RecordState.Pulling);

            var command = $"shell rm {this.filePathInDevice}";
            this.Device.RunCommandAsync(command);

            if(!File.Exists(this.filePathInPc))
            {
                Trace.TraceError("Not found '{this.filePathInPc}'.");
                this.Dispose();
                return;
            }
            this.FilePath = this.filePathInPc;

            this.State = RecordState.Finished;
        }

        string GetFileName(string pattern, Size size)
        {
            var now = DateTime.Now;
            var replacer = new Dictionary<string, string>()
            {
                { "date", now.ToString("yyyy-MM-dd") },
                { "time", now.ToString("HHmmss") },
                { "width", size.Width.ToString() },
                { "height", size.Height.ToString() },
                { "no", (this.setting.SequenceNo % 10000).ToString("0000") }
            };
            pattern = this.Device.ToString(pattern);
            return pattern.Replace(replacer, "-");
        }

        #region IDisposable Support
        bool disposed = false;

        public virtual void Dispose()
        {
            if (this.disposed) return;

            this.State = RecordState.Aborted;
            this.recordCommandContext?.Cancel();
            this.recordCommandContext = null;
            this.pullCommandContext?.Cancel();
            this.pullCommandContext = null;

            this.disposed = true;
        }
        #endregion
    }
}
