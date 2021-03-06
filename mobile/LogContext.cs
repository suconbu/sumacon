﻿using Suconbu.Mobile;
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
    public class LogContext : IDisposable
    {
        public event EventHandler<Log> Received = delegate { };

        public Device Device { get; private set; }
        public LogSetting Setting { get; private set; }
        public int Count { get { return this.receivedLogs.Count; } }

        readonly List<Log> receivedLogs = new List<Log>();
        CommandContext logcatContext;
        bool suspended;
        int nextNo = 0;

        public static LogContext Open(Device device, LogSetting setting)
        {
            var intance = new LogContext();
            intance.Device = device ?? throw new ArgumentNullException(nameof(device));
            intance.Setting = setting ?? throw new ArgumentNullException(nameof(setting));
            intance.StartInternal(false);
            return intance;
        }

        public void Close()
        {
            this.Dispose();
        }

        /// <summary>
        /// Get logs from log buffer.
        /// </summary>
        public List<Log> GetRange(int index = 0, int count = int.MaxValue)
        {
            var takens = new List<Log>();
            lock (this.receivedLogs)
            {
                count = Math.Min(index + count, this.receivedLogs.Count) - index;
                takens.AddRange(this.receivedLogs.GetRange(index, count));
            }
            return takens;
        }

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

        string MakeLogcatCommand(LogSetting setting, bool resume)
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

            // 一時停止からの再開だったら前回の続きから、そうでなければ現在時間から
            var startAt = (resume && this.receivedLogs.Count > 0) ? this.receivedLogs.Last().Timestamp : this.Device.Date;
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
            var log = Log.FromString(this.nextNo, output, this.Device.Processes);
            if (log == null) return;
            this.nextNo++;
            lock (this.receivedLogs)
            {
                this.receivedLogs.Add(log);
            }
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

        public int No { get; private set; }
        public DateTime Timestamp { get; private set; }
        public PriorityCode Priority { get; private set; }
        public string Tag { get; private set; }
        public int Pid { get; private set; }
        public string ProcessName { get; private set; }
        public int Tid { get; private set; }
        public string ThreadName { get; private set; }
        public string Message { get; private set; }

        public static Log FromString(int no, string input, EntryCollection<int, ProcessEntry> processes = null)
        {
            var timestampLength = 18;
            if (string.IsNullOrEmpty(input) || input.Length < timestampLength) return null;
            // 01-05 23:38:00.175  1740  4493 D EventNotificationJob: Running EventNotificationJob, isDetail=true
            // ~~~~~~~~~~~~~~~~~~  ~~~~  ~~~~ ~ ~~~~~~~~~~~~~~~~~~~~  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // timestamp           pid   tid  | tag                   message
            //                                priority
            var yyyy = DateTime.Now.Year.ToString("0000");
            var time = $"{yyyy}-{input.Substring(0, timestampLength)}";
            var match = Regex.Match(input.Substring(timestampLength), @"(\d+)\s+(\d+)\s+([A-Z])\s+([^:]+)\s*: (.+)");
            if (!match.Success) return null;
            try
            {
                var instance = new Log();
                instance.No = no;
                instance.Timestamp = DateTime.Parse(time);
                instance.Pid = int.Parse(match.Groups[1].Value);
                instance.Tid = int.Parse(match.Groups[2].Value);
                instance.ProcessName = string.Empty;
                instance.ThreadName = string.Empty;
                if (processes != null)
                {
                    var processInfo = processes[instance.Pid];
                    if (processInfo != null)
                    {
                        instance.ProcessName = processInfo.Name;
                        instance.ThreadName = processInfo.Threads[instance.Tid]?.Name ?? string.Empty;
                    }
                }
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

    public class LogSetting
    {
        public int Pid { get; set; } = 0;
        public Dictionary<string, Log.PriorityCode> PriorityEachTags { get; private set; } = new Dictionary<string, Log.PriorityCode>();
        public bool DefaultSlilent { get; set; } = false;
    }
}
