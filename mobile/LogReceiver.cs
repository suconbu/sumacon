using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Suconbu.Mobile
{
    public class LogReceiveContext : IDisposable
    {
        public event EventHandler<Log> Received = delegate { };

        public Device Device { get; private set; }
        public LogReceiveSetting Setting { get; private set; }
        public int Count { get { return this.receivedLogs.Count; } }

        readonly List<Log> receivedLogs = new List<Log>();
        readonly List<Log> temporaryLogs = new List<Log>();
        SemaphoreSlim receivedLogsSemaphore = new SemaphoreSlim(1);
        CommandContext logcatContext;
        bool suspended;

        /// <summary>
        /// Initialize instance.
        /// Does not start receiving until you call the 'Start' method.
        /// </summary>
        public LogReceiveContext(Device device, LogReceiveSetting setting)
        {
            this.Device = device ?? throw new ArgumentNullException(nameof(device));
            this.Setting = setting ?? throw new ArgumentNullException(nameof(setting));
            this.StartInternal(false);
        }

        public IReadOnlyList<Log> LockLogs()
        {
            this.receivedLogsSemaphore.Wait();
            return this.receivedLogs;
        }

        public void UnlockLogs()
        {
            this.receivedLogsSemaphore.Release();
        }

        ///// <summary>
        ///// Get logs from log buffer.
        ///// </summary>
        //public List<Log> GetRange(int index = 0, int count = int.MaxValue)
        //{
        //    var takens = new List<Log>();
        //    lock (this.receivedLogs)
        //    {
        //        var to = index + count;
        //        var from = Math.Max(0, index);
        //        count = Math.Min(to, this.receivedLogs.Count) - from;
        //        takens.AddRange(this.receivedLogs.GetRange(from, count));
        //    }
        //    return takens;
        //}

        //public List<Log> Where(Func<Log, bool> predicate)
        //{
        //    var takens = new List<Log>();
        //    lock (this.receivedLogs)
        //    {
        //        foreach(var log in this.receivedLogs)
        //        {
        //            if(predicate(log))
        //            {
        //                takens.Add(log);
        //            }
        //        }
        //    }
        //    return takens;
        //}

        public bool Suspended
        {
            get { return this.suspended; }
            set
            {
                if(this.suspended != value)
                {
                    this.suspended = value;
                    if (this.suspended)
                    {
                        this.StopInternal();
                    }
                    else
                    {
                        this.StartInternal(true);
                    }
                }
            }
        }

        void StartInternal(bool resume)
        {
            this.logcatContext = this.Device.RunCommandAsync("shell", this.OnOutput, null);
            var command = this.MakeLogcatCommand(this.Setting, resume);
            this.logcatContext.PushInput(command);
        }


        void StopInternal()
        {
            this.logcatContext?.Cancel();
            this.logcatContext = null;
        }

        string MakeLogcatCommand(LogReceiveSetting setting, bool resume)
        {
            var command = new StringBuilder("logcat");

            if (setting.Pid > 0)
            {
                command.Append($" --pid={this.Setting.Pid}");
            }

            if (setting.DefaultSlilent)
            {
                command.Append(" -s");
            }

            var startAt = setting.StartAt;
            if (resume && this.receivedLogs.Count > 0)
            {
                // 一時停止からの再開なら前回の続きから
                startAt = this.receivedLogs.Last().Timestamp;
            }
            if (startAt != DateTime.MinValue)
            {
                command.Append($" -T '{startAt.ToString("yyyy-MM-dd HH:mm:ss.fff")}'");
            }

            foreach (var pair in setting.PriorityEachTags)
            {
                if (pair.Value == Log.PriorityCode.None)
                {
                    command.Append($" {pair.Key}");
                }
                else
                {
                    command.Append($" {pair.Key}:{pair.Value}");
                }
            }

            return command.ToString();
        }

        void OnOutput(string output)
        {
            var log = Log.FromText(output);
            if (log == null) return;
            this.receivedLogsSemaphore.Wait();
            this.receivedLogs.Add(log);
            this.receivedLogsSemaphore.Release();
            //lock (this.receivedLogs)
            //{
            //    this.receivedLogs.Add(log);
            //}
            this.Received(this, log);
        }

        #region IDisposable Support
        bool disposed = false;

        public virtual void Dispose()
        {
            if (this.disposed) return;
            this.StopInternal();
            this.disposed = true;
        }
        #endregion
    }

    public class Log
    {
        public enum PriorityCode { None, S, F, E, W, I, D, V }

        public DateTime Timestamp { get; private set; }
        public PriorityCode Priority { get; private set; }
        public string Tag { get; private set; }
        public int Pid { get; private set; }
        public int Tid { get; private set; }
        public string Message { get; private set; }

        public static Log FromText(string input)
        {
            var timestampLength = 18;
            if (string.IsNullOrEmpty(input) || input.Length < timestampLength) return null;
            // 01-05 23:38:00.175  1740  4493 D EventNotificationJob: Running EventNotificationJob, isDetail=true
            // ~~~~~~~~~~~~~~~~~~  ~~~~  ~~~~ ~ ~~~~~~~~~~~~~~~~~~~~  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // timestamp           pid   tid  | tag                   message
            //                                priority
            var yyyy = DateTime.Now.Year.ToString("0000");
            var time = $"{yyyy}-{input.Substring(0, timestampLength)}";
            var match = Regex.Match(input.Substring(timestampLength), @"(\d+)\s+(\d+)\s+([A-Z])\s+([^:]+)\s*:(.+)");
            if (!match.Success) return null;
            try
            {
                var instance = new Log();
                instance.Timestamp = DateTime.Parse(time);
                instance.Pid = int.Parse(match.Groups[1].Value);
                instance.Tid = int.Parse(match.Groups[2].Value);
                instance.Priority = (PriorityCode)Enum.Parse(typeof(PriorityCode), match.Groups[3].Value);
                instance.Tag = match.Groups[4].Value;
                instance.Message = match.Groups[5].Value;
                return instance;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return null;
            }
        }

        Log() { }
    }

    public class LogReceiveSetting
    {
        public DateTime StartAt { get; set; } = DateTime.MinValue;
        public int Pid { get; set; } = 0;
        public Dictionary<string, Log.PriorityCode> PriorityEachTags { get; private set; } = new Dictionary<string, Log.PriorityCode>();
        public bool DefaultSlilent { get; set; } = false;
    }
}
