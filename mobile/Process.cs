using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Suconbu.Mobile
{
    public class ProcessSnapshot
    {
        public IReadOnlyList<ProcessInfo> ProcessInfos { get; private set; }
        public ProcessInfo this[int pid]
        {
            get
            {
                return this.processInfoByPid.TryGetValue(pid, out var processInfo) ? processInfo : null;
            }
        }

        Dictionary<int, ProcessInfo> processInfoByPid;

        public static CommandContext GetAsync(Device device, Action<ProcessSnapshot> onFinished)
        {
            var processInfos = new List<ProcessInfo>();
            var processInfoByPid = new Dictionary<int, ProcessInfo>();
            int count = 0;
            return device.RunCommandAsync("shell ps -p", output =>
            {
                if (output != null)
                {
                    if (count++ > 0)
                    {
                        var p = ProcessInfo.FromString(output);
                        processInfos.Add(p);
                        processInfoByPid.Add(p.Pid, p);
                    }
                }
                else
                {
                    onFinished?.Invoke(new ProcessSnapshot()
                    {
                        ProcessInfos = processInfos.OrderBy(p => p.Pid).ToList(),
                        processInfoByPid = processInfoByPid
                    });
                }
            });
        }
    }

    public class ProcessInfo
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

        public static ProcessInfo Empty { get; } = new ProcessInfo();

        public static ProcessInfo FromString(string input)
        {
            //USER      PID   PPID  VSIZE  RSS  PRIO  NICE  RTPRI SCHED  WCHAN              PC  NAME
            //u0_a95    5027  1064  1897944 21184 16    -4    0     0     SyS_epoll_ 0000000000 S jp.co.yahoo.android.apps.navi
            var tokens = Regex.Split(input, @"\s+");

            return new ProcessInfo()
            {
                User = tokens[0],
                Pid = int.Parse(tokens[1]),
                Ppid = int.Parse(tokens[2]),
                Vsize = uint.Parse(tokens[3]),
                Rsize = uint.Parse(tokens[4]),
                Priority = int.Parse(tokens[5]),
                Name = tokens.Last()
            };
        }
    }
}
