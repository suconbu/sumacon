using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Suconbu.Mobile
{
    public class ProcessInfoList
    {
        public IReadOnlyList<ProcessInfo> Processes { get; private set; }
        public ProcessInfo this[int pid] { get { return this.Processes.FirstOrDefault(p => p.Pid == pid); } }

        public static CommandContext GetAsync(Device device, Action<ProcessInfoList> onFinished)
        {
            var processInfos = new List<ProcessInfo>();
            var processInfoByPid = new Dictionary<int, ProcessInfo>();
            ProcessInfo currentProcess = null;
            string state = "header"; // header -> process -> thread -> process -> ...
            return device.RunCommandAsync("shell ps -p -t", output =>
            {
                if (output != null)
                {
                    if(state == "thread")
                    {
                        var psEntry = new PsEntry(output);
                        if(psEntry.Ppid == currentProcess.Pid)
                        {
                            currentProcess.AddThread(new ThreadInfo(psEntry, currentProcess));
                        }
                        else
                        {
                            state = "process";
                        }
                    }
                    if(state == "process")
                    {
                        var psEntry = new PsEntry(output);
                        currentProcess = new ProcessInfo(psEntry);
                        processInfos.Add(currentProcess);
                        processInfoByPid.Add(currentProcess.Pid, currentProcess);
                        state = "thread";
                    }
                    if (state == "header")
                    {
                        state = "process";
                    }
                }
                else
                {
                    onFinished?.Invoke(new ProcessInfoList() { Processes = processInfos });
                }
            });
        }

        ProcessInfoList() { }
    }

    internal class PsEntry
    {
        // User
        public string User { get; private set; }
        // Process id
        public int Pid { get; private set; }
        // Parent process id
        public int Ppid { get; private set; }
        // Virtual memory size [KB]
        public uint Vsize { get; private set; }
        // Resident set size [KB]
        public uint Rsize { get; private set; }
        // Priority
        public int Priority { get; private set; }
        // Name
        public string Name { get; private set; }

        public PsEntry(string input)
        {
            //USER      PID   PPID  VSIZE  RSS  PRIO  NICE  RTPRI SCHED  WCHAN              PC  NAME
            //u0_a95    5027  1064  1897944 21184 16    -4    0     0     SyS_epoll_ 0000000000 S jp.co.yahoo.android.apps.navi
            var tokens = Regex.Split(input, @"\s+");

            this.User = tokens[0];
            this.Pid = int.Parse(tokens[1]);
            this.Ppid = int.Parse(tokens[2]);
            this.Vsize = uint.Parse(tokens[3]);
            this.Rsize = uint.Parse(tokens[4]);
            this.Priority = int.Parse(tokens[5]);
            this.Name = tokens.Last();
        }
    }

    public class ProcessInfo
    {
        // User
        public string User { get { return this.psEntry.User; } }
        // Process id
        public int Pid { get { return this.psEntry.Pid; } }
        // Parent process id
        public int Ppid { get { return this.psEntry.Ppid; } }
        // Virtual memory size [KB]
        public uint Vsize { get { return this.psEntry.Vsize; } }
        // Resident set size [KB]
        public uint Rsize { get { return this.psEntry.Rsize; } }
        // Priority
        public int Priority { get { return this.psEntry.Priority; } }
        // Name
        public string Name { get { return this.psEntry.Name; } }

        public IReadOnlyList<ThreadInfo> Threads { get { return this.threads; } }
        public ThreadInfo this[int tid] { get { return this.threads.FirstOrDefault(t => t.Tid == tid); } }

        PsEntry psEntry;
        List<ThreadInfo> threads = new List<ThreadInfo>();

        internal ProcessInfo(PsEntry psEntry)
        {
            this.psEntry = psEntry;
        }

        internal void AddThread(ThreadInfo thread)
        {
            this.threads.Add(thread);
        }
    }

    public class ThreadInfo
    {
        // Thread id
        public int Tid { get { return this.psEntry.Pid; } }
        // Priority
        public int Priority { get { return this.psEntry.Priority; } }
        // Name
        public string Name { get { return this.psEntry.Name; } }

        public ProcessInfo Process { get; private set; }

        PsEntry psEntry;

        internal ThreadInfo(PsEntry psEntry, ProcessInfo process)
        {
            this.psEntry = psEntry;
            this.Process = process;
        }
    }
}
