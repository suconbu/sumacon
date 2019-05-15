using Suconbu.Toolbox;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Suconbu.Mobile
{
    public class ProcessInfoCollection
    {
        public IEnumerable<ProcessInfo> ProcessInfos { get { return this.processInfoByPid.Values; } }
        public ProcessInfo this[int pid] { get { return this.processInfoByPid.TryGetValue(pid, out var p) ? p : null; } }

        Dictionary<int, ProcessInfo> processInfoByPid = new Dictionary<int, ProcessInfo>();

        internal ProcessInfoCollection(Dictionary<int, ProcessInfo> processInfoByPid)
        {
            this.processInfoByPid = processInfoByPid;
        }
    }

    public class ProcessInfo
    {
        // Process id
        public int Pid { get { return this.psEntry.Pid; } }
        // User
        public string User { get { return this.psEntry.User; } }
        // Parent process id
        public int Ppid { get { return this.psEntry.Ppid; } }
        // Priority
        public int Priority { get { return this.psEntry.Priority; } }
        // Virtual memory size [KB]
        public uint Vsize { get { return this.psEntry.Vsize; } }
        // Resident set size [KB]
        public uint Rsize { get { return this.psEntry.Rsize; } }
        // Name
        public string Name { get { return this.psEntry.ProcessName; } }

        // Key:ThreadInfo.Tid
        public IReadOnlyDictionary<int, ThreadInfo> Threads { get { return this.threadInfoByTid; } }
        //public IEnumerable<ThreadInfo> ThreadInfos { get { return this.threadInfoByTid.Values; } }
        //public ThreadInfo this[int tid] { get { return this.threadInfoByTid.TryGetValue(tid, out var t) ? t : null; } }

        PsEntry psEntry;
        Dictionary<int, ThreadInfo> threadInfoByTid = new Dictionary<int, ThreadInfo>();
        static ConcurrentDictionary<Device, bool> oldStyleByDevice = new ConcurrentDictionary<Device, bool>();

        public static CommandContext GetAsync(Device device, Action<ProcessInfoCollection> onFinished)
        {
            var processInfoByPid = new Dictionary<int, ProcessInfo>();
            ProcessInfo currentProcess = null;
            string state = "header"; // header -> process -> thread -> process -> ...

            var oldStyle = oldStyleByDevice.GetOrAdd(device, d =>
            {
                bool result = false;
                // 新形式「」旧形式「bad pid '0'」
                device.RunCommandOutputTextAsync("shell ps 0", (output, error) => result = output.StartsWith("bad")).Wait();
                return result;
            });

            string command = "shell ps -eTwO PRI,NAME";
            if (oldStyle)
            {
                // 旧形式
                command = "shell ps -p -t";
            }

            string[] columnNames = null;
            return device.RunCommandAsync(command, output =>
            {
                if (output == null)
                {
                    onFinished?.Invoke(new ProcessInfoCollection(processInfoByPid));
                    return;
                }

                PsEntry psEntry = null;
                if (columnNames != null)
                {
                    psEntry = new PsEntry(output, columnNames);
                }

                if (state == "thread" && currentProcess != null)
                {
                    var tid = psEntry.Tid;
                    var pid = psEntry.Pid;
                    var priority = psEntry.Priority;
                    var name = psEntry.ThreadName;
                    if (tid == 0)
                    {
                        // 旧形式ケア
                        tid = psEntry.Pid;
                        pid = psEntry.Ppid;
                        name = psEntry.ProcessName;
                    }

                    if (pid == currentProcess.Pid)
                    {
                        var threadInfo = new ThreadInfo(tid, priority, name, currentProcess);
                        currentProcess.threadInfoByTid.Add(tid, threadInfo);
                    }
                    else
                    {
                        state = "process";
                    }
                }
                if (state == "process")
                {
                    currentProcess = new ProcessInfo(psEntry);
                    processInfoByPid.Add(currentProcess.Pid, currentProcess);
                    state = "thread";
                }
                if (state == "header")
                {
                    if (oldStyle)
                    {
                        output = output.Replace("PC  NAME", "PC S NAME");
                    }
                    columnNames = output.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    state = "process";
                }
            });
        }

        ProcessInfo(PsEntry psEntry)
        {
            this.psEntry = psEntry;
        }
    }

    public class ThreadInfo
    {
        // Thread id
        public int Tid { get; private set; }
        // Priority
        public int Priority { get; private set; }
        // Name
        public string Name { get; private set; }
        // Owner process
        public ProcessInfo Process { get; private set; }

        internal ThreadInfo(int tid, int priority, string name, ProcessInfo process)
        {
            this.Tid = tid;
            this.Priority = priority;
            this.Name = name;
            this.Process = process;
        }
    }

    internal class PsEntry
    {
        // User
        public string User { get; private set; }
        // Process id
        public int Pid { get; private set; }
        // Parent process id
        public int Ppid { get; private set; }
        // Thread id
        public int Tid { get; private set; }
        // Virtual memory size [KB]
        public uint Vsize { get; private set; }
        // Resident set size [KB]
        public uint Rsize { get; private set; }
        // Priority
        public int Priority { get; private set; }
        // Process name
        public string ProcessName { get; private set; }
        // Thread name
        public string ThreadName { get; private set; }

        static readonly Dictionary<string, string> patterns = new Dictionary<string, string>
        {
            { "user", "USER" },
            { "pid", "PID" },
            { "ppid", "PPID" },
            { "tid", "TID" },
            { "vsize", "VSZ|VSIZE" },
            { "rsize", "RSS" },
            { "pri", "PRI|PRIO" },
            { "pname", "NAME" },
            { "tname", "CMD" }
        };

        public PsEntry(string input, string[] columnNames)
        {
            var tokens = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < columnNames.Length)
            {
                // WCHANが空っぽの時があるの・・・
                var t = tokens.ToList();
                t.Insert(tokens.Length - 2, "-");
                tokens = t.ToArray();
            }

            this.User = this.GetValue(columnNames, tokens, "user");
            this.Pid = int.TryParse(this.GetValue(columnNames, tokens, "pid"), out var pid) ? pid : 0;
            this.Ppid = int.TryParse(this.GetValue(columnNames, tokens, "ppid"), out var ppid) ? ppid : 0;
            this.Tid = int.TryParse(this.GetValue(columnNames, tokens, "tid"), out var tid) ? tid : 0;
            this.Vsize = uint.TryParse(this.GetValue(columnNames, tokens, "vsize"), out var vsize) ? vsize : 0;
            this.Rsize = uint.TryParse(this.GetValue(columnNames, tokens, "rsize"), out var rsize) ? rsize : 0;
            this.Priority = int.TryParse(this.GetValue(columnNames, tokens, "pri"), out var pri) ? pri : 0;
            this.ProcessName = this.GetValue(columnNames, tokens, "pname");
            this.ThreadName = this.GetValue(columnNames, tokens, "tname");
        }

        string GetValue(string[] columnNames, string[] tokens, string columnKey)
        {
            if (!PsEntry.patterns.TryGetValue(columnKey, out var pattern)) return null;
            var index = columnNames.TakeWhile(c => !Regex.IsMatch(c, pattern)).Count();
            return (index < columnNames.Length) ? tokens[index] : null;
        }
    }
}
