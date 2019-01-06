using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Suconbu.Mobile
{
    public class LogReceiver : IDisposable
    {
        public event EventHandler<Log> Received = delegate { };

        //public int FilterPid { get; set; }
        //public Dictionary<string, Log.PriorityCode> FilterPriorityEachTags { get; private set; } = new Dictionary<string, Log.PriorityCode>();
        //public string FilterPattern { get; set; }
        //public bool DefaultSlilent { get; set; }
        public LogReceiveSetting Setting { get; private set; }
        public Device Device { get; private set; }
        public int Count { get { lock (this.logs) { return this.logs.Count; } } }

        readonly List<Log> logs = new List<Log>();
        CommandContext context;
        bool receiveEnabled;
        DateTime firstStartedAt = DateTime.MinValue;

        /// <summary>
        /// Initialize instance.
        /// Does not start receiving until you call the 'Start' method.
        /// </summary>
        public LogReceiver(Device device, LogReceiveSetting setting = null)
        {
            this.Device = device ?? throw new ArgumentNullException(nameof(device));
            this.Setting = setting ?? new LogReceiveSetting();
        }

        /// <summary>
        /// Get or set receiving enable status.
        /// </summary>
        public bool Enabled
        {
            get { return this.receiveEnabled; }
            set
            {
                if(this.receiveEnabled != value)
                {
                    this.receiveEnabled = value;
                    if (this.receiveEnabled)
                    {
                        this.StartInternal();
                    }
                    else
                    {
                        this.StopInternal();
                    }
                }
            }
        }

        /// <summary>
        /// Start log receiving using current filter settings.
        /// </summary>
        public void Start()
        {
            this.Enabled = true;
        }

        /// <summary>
        /// Stop log receiving. Can be resume by call 'Start' method.
        /// </summary>
        public void Stop()
        {
            this.Enabled = false;
        }

        /// <summary>
        /// Get logs from log buffer.
        /// </summary>
        public List<Log> GetRange(int index, int count = int.MaxValue)
        {
            var takens = new List<Log>();
            lock (this.logs)
            {
                count = Math.Min(index + count, this.logs.Count) - index;
                takens.AddRange(this.logs.GetRange(index, count));
            }
            return takens;
        }

        /// <summary>
        /// Clear log buffer.
        /// </summary>
        public void Clear()
        {
            lock (this.logs)
            {
                this.logs.Clear();
            }
        }

        void StartInternal()
        {
            var command = new StringBuilder();
            command.Append("shell logcat");
            if (this.Setting.Pid > 0)
            {
                command.Append($" --pid={this.Setting.Pid}");
            }

            if(!string.IsNullOrEmpty(this.Setting.Pattern))
            {
                command.Append($" -e {this.Setting.Pattern}");
            }

            if(this.Setting.DefaultSlilent)
            {
                command.Append(" -s");
            }

            if(this.firstStartedAt == DateTime.MinValue)
            {
                this.firstStartedAt = DateTime.Now;
            }
            // 一時停止からの再開なら前回の続きから
            var from = (this.logs.Count > 0) ? this.logs.Last().Timestamp : this.firstStartedAt;
            command.Append($" -T '{from.ToString("yyyy-MM-dd HH:mm:ss.fff")}'");

            foreach (var pair in this.Setting.PriorityEachTags)
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

            this.context = this.Device.RunCommandAsync(command.ToString(), this.OnOutput, null);
        }

        void StopInternal()
        {
            this.context?.Cancel();
            this.context = null;
        }

        void OnOutput(string output)
        {
            if (output == null || output.StartsWith("-")) return;

            var log = new Log(output);
            lock (this.logs)
            {
                this.logs.Add(log);
            }
            this.Received(this, log);
        }

        #region IDisposable Support
        bool disposed = false;

        public virtual void Dispose()
        {
            if (this.disposed) return;
            this.Stop();
            this.Clear();
            this.disposed = true;
        }
        #endregion
    }

    public class Log
    {
        //public enum PriorityCode { Fatal, Error, Warning, Info, Debug, Verbose }
        public enum PriorityCode { None, S, F, E, W, I, D, V }

        public DateTime Timestamp { get; private set; }
        public PriorityCode Priority { get; private set; }
        public string Tag { get; private set; }
        public int Pid { get; private set; }
        public int Tid { get; private set; }
        public string Message { get; private set; }

        static readonly string yyyy = DateTime.Now.Year.ToString("0000");

        public Log(string input)
        {
            // 01-05 23:38:00.175  1740  4493 D EventNotificationJob: Running EventNotificationJob, isDetail=true
            // ~~~~~~~~~~~~~~~~~~  ~~~~  ~~~~ ~ ~~~~~~~~~~~~~~~~~~~~  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // timestamp           pid   tid  | tag                   message
            //                                priority
            var timeLength = 18;
            var time = $"{yyyy}-{input.Substring(0, timeLength)}";
            this.Timestamp = DateTime.Parse(time);
            var match = Regex.Match(input.Substring(timeLength), @"(\d+)\s+(\d+)\s+([A-Z])\s+(\w+)\s*:(.+)");
            if(match.Success)
            {
                this.Pid = int.Parse(match.Groups[1].Value);
                this.Tid = int.Parse(match.Groups[2].Value);
                this.Priority = (PriorityCode)Enum.Parse(typeof(PriorityCode), match.Groups[3].Value);
                this.Tag = match.Groups[4].Value;
                this.Message = match.Groups[5].Value;
            }
        }
    }

    public class LogReceiveSetting
    {
        public int Pid { get; set; } = 0;
        public Dictionary<string, Log.PriorityCode> PriorityEachTags { get; private set; } = new Dictionary<string, Log.PriorityCode>();
        public string Pattern { get; set; } = null;
        public bool DefaultSlilent { get; set; } = false;
    }
}
