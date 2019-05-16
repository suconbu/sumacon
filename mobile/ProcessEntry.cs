using Suconbu.Toolbox;
using System;
using System.Collections;
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
    public class ProcessEntry
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
        public uint Vss { get { return this.psEntry.Vsize; } }
        // Resident set size [KB]
        public uint Rss { get { return this.psEntry.Rsize; } }
        // Name
        public string Name { get { return this.psEntry.ProcessName; } }

        // Key:ThreadEntry.Tid
        public EntryCollection<int, ThreadEntry> Threads { get; private set; }
        //public IReadOnlyDictionary<int, ThreadEntry> Threads { get { return this.threadByTid; } }

        PsEntry psEntry;
        //Dictionary<int, ThreadEntry> threadByTid = new Dictionary<int, ThreadEntry>();

        static ConcurrentDictionary<Device, bool> oldStyleByDevice = new ConcurrentDictionary<Device, bool>();

        public static CommandContext GetAsync(Device device, Action<EntryCollection<int, ProcessEntry>> onFinished)
        {
            var processByPid = new Dictionary<int, ProcessEntry>();
            var threadByTid = new Dictionary<int, ThreadEntry>();
            ProcessEntry currentProcess = null;
            string state = "header"; // header -> process -> thread -> process -> ...

            var oldStyle = oldStyleByDevice.GetOrAdd(device, d =>
            {
                bool result = false;
                // 新形式「」旧形式「bad pid '0'」
                device.RunCommandOutputTextAsync("shell ps 0", (output, error) => result = output.StartsWith("bad")).Wait();
                return result;
            });

            var command = oldStyle ?
                "shell ps -p -t" :
                "shell ps -eTwO PRI,NAME";

            string[] columnNames = null;
            return device.RunCommandAsync(command, output =>
            {
                if (output == null)
                {
                    onFinished?.Invoke(new EntryCollection<int, ProcessEntry>(processByPid));
                    return;
                }

                PsEntry psEntry = null;
                if (columnNames != null)
                {
                    psEntry = new PsEntry(output, columnNames);
                }

                if (state == "thread" && currentProcess != null)
                {
                    var tid = oldStyle ? psEntry.Pid : psEntry.Tid;
                    var pid = oldStyle ? psEntry.Ppid : psEntry.Pid;
                    var priority = psEntry.Priority;
                    var name = oldStyle ? psEntry.ProcessName : psEntry.ThreadName;

                    if (pid == currentProcess.Pid)
                    {
                        var thread = new ThreadEntry(tid, priority, name, currentProcess);
                        threadByTid.Add(tid, thread);
                    }
                    else
                    {
                        state = "process";
                    }
                }
                if (state == "process")
                {
                    threadByTid = new Dictionary<int, ThreadEntry>();
                    currentProcess = new ProcessEntry(psEntry);
                    currentProcess.Threads = new EntryCollection<int, ThreadEntry>(threadByTid);
                    processByPid.Add(currentProcess.Pid, currentProcess);
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

        ProcessEntry(PsEntry psEntry)
        {
            this.psEntry = psEntry;
        }
    }

    public class ThreadEntry
    {
        // Thread id
        public int Tid { get; private set; }
        // Priority
        public int Priority { get; private set; }
        // Name
        public string Name { get; private set; }
        // Owner process
        public ProcessEntry Process { get; private set; }

        internal ThreadEntry(int tid, int priority, string name, ProcessEntry process)
        {
            this.Tid = tid;
            this.Priority = priority;
            this.Name = name;
            this.Process = process;
        }
    }

    public class EntryCollection<TKey, TValue> : IEnumerable<TValue>
    {
        public TValue this[TKey key] { get { return this.entries.TryGetValue(key, out var entry) ? entry : default(TValue); } }

        Dictionary<TKey, TValue> entries;

        internal EntryCollection(Dictionary<TKey, TValue> entries)
        {
            this.entries = entries;
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            foreach (var entry in this.entries.Values) yield return entry;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    class PsEntry
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

            this.User = this.GetValue(columnNames, tokens, "USER");
            this.Pid = int.TryParse(this.GetValue(columnNames, tokens, "PID"), out var pid) ? pid : 0;
            this.Ppid = int.TryParse(this.GetValue(columnNames, tokens, "PPID"), out var ppid) ? ppid : 0;
            this.Tid = int.TryParse(this.GetValue(columnNames, tokens, "TID"), out var tid) ? tid : 0;
            this.Vsize = uint.TryParse(this.GetValue(columnNames, tokens, "VSZ|VSIZE"), out var vsize) ? vsize : 0;
            this.Rsize = uint.TryParse(this.GetValue(columnNames, tokens, "RSS"), out var rsize) ? rsize : 0;
            this.Priority = int.TryParse(this.GetValue(columnNames, tokens, "PRI|PRIO"), out var pri) ? pri : 0;
            this.ProcessName = this.GetValue(columnNames, tokens, "NAME");
            this.ThreadName = this.GetValue(columnNames, tokens, "CMD");
        }

        string GetValue(string[] columnNames, string[] tokens, string pattern)
        {
            var index = columnNames.TakeWhile(c => !Regex.IsMatch(c, pattern)).Count();
            return (index < columnNames.Length) ? tokens[index] : null;
        }
    }
}
