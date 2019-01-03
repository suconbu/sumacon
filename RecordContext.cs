using Suconbu.Mobile;
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
        //public Size Size;
        public float SizeMultiply;
        public int Bitrate;
    }

    // Start -> (Recording...) -> Stop/Timeout -> (Copying...) -> Finished
    class RecordContext : IDisposable
    {
        public enum RecordState { Recording, Pulling, Finished, Aborted }

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
        public string FilePath { get; private set; }

        Device device;
        //string saveTo;
        RecordSetting setting;
        CommandContext recordCommandContext;
        CommandContext pullCommandContext;
        string fileName;
        string filePathInDevice;
        string filePathInPc;
        RecordState state = RecordState.Recording;

        Action<RecordState> onStateChanged = delegate { };

        readonly string deviceTemporaryDirectoryPath = "/sdcard";

        RecordContext() { }

        public static RecordContext StartNew(Device device, RecordSetting setting, Action<RecordState> onStateChanged)
        {
            var instance = new RecordContext();
            instance.device = device;
            instance.setting = setting;
            instance.onStateChanged = onStateChanged;
            instance.StartRecord();
            return instance;
        }

        void StartRecord()
        {
            Debug.Print(Util.GetCurrentMethodName(true));

            if (!Directory.Exists(this.setting.DirectoryPath))
            {
                Directory.CreateDirectory(this.setting.DirectoryPath);
            }

            var size = this.device.ScreenSize;
            if (this.device.CurrentRotation == Screen.RotationCode.Landscape ||
                this.device.CurrentRotation == Screen.RotationCode.LandscapeReversed)
            {
                size = size.Swapped();
            }
            if (this.setting.SizeMultiply != 1.0f)
            {
                size = size.Multiplied(this.setting.SizeMultiply);
            }

            this.fileName = this.GetFileName(this.setting.FileNamePattern, size);
            this.filePathInDevice = $"{this.deviceTemporaryDirectoryPath}/{this.fileName}";
            this.filePathInPc = Path.Combine(this.setting.DirectoryPath, this.fileName);

            var option = new StringBuilder();
            if (0 < this.setting.TimeLimitSeconds && this.setting.TimeLimitSeconds <= RecordContext.TimeLimitSecondsMax)
            {
                option.Append($" --time-limit {this.setting.TimeLimitSeconds}");
            }
            if(size != this.device.ScreenSize)
            {
                option.Append($" --size {size.Width}x{size.Height}");
            }
            option.Append($" --bit-rate {this.setting.Bitrate}");

            this.StartedAt = DateTime.Now;

            var command = $"shell screenrecord {option} {this.filePathInDevice}";
            this.recordCommandContext = this.device.RunCommandOutputTextAsync(command, this.OnRecordCommandFinished);
        }

        public void Stop()
        {
            if (this.State != RecordState.Recording) return;

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

            this.OnRecordFinished();
        }

        void OnRecordFinished()
        {
            Debug.Assert(this.State == RecordState.Recording);

            this.State = RecordState.Pulling;

            var command = $"pull {this.filePathInDevice} {this.filePathInPc}";
            this.pullCommandContext = this.device.RunCommandOutputTextAsync(command, this.OnPullCommandFinished);
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
            this.device.RunCommandAsync(command);

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
            pattern = this.device.ToString(pattern);
            return pattern.Replace(replacer);
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
