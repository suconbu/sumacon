using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Suconbu.Mobile
{
    class TopContext : IDisposable
    {
        public IReadOnlyList<TopSnapshot> Snapshots { get { return this.snapshots; } }

        CommandContext context;
        List<TopSnapshot> snapshots = new List<TopSnapshot>();
        TopSnapshot currentTop;
        readonly int maxSnapshots = 100;
        EventHandler<TopSnapshot> onReceived;
        bool oldStyle;

        static ConcurrentDictionary<Device, bool> oldStyleByDevice = new ConcurrentDictionary<Device, bool>();

        public static TopContext Start(Device device, int intervalSeconds, EventHandler<TopSnapshot> onReceived)
        {
            var instance = new TopContext();
            instance.onReceived = onReceived;

            instance.oldStyle = oldStyleByDevice.GetOrAdd(device, d =>
            {
                bool result = false;
                // 変なコマンド入れた時の反応をみて判別
                // 新形式「See top --help」旧形式「Invalid argument "0".」
                device.RunCommandOutputTextAsync("shell top 0", (output, error) => result = !error.StartsWith("See")).Wait();
                return result;
            });

            var command = instance.oldStyle ?
                $"shell top -H -m 50 -d {intervalSeconds} -s cpu" :
                $"shell top -H -d {intervalSeconds} -s 3 -o PID,TID,%CPU";
            instance.context = instance.context = device.RunCommandAsync(command, instance.OnOutput);

            return instance;
        }

        public void Close()
        {
            this.Dispose();
        }

        void OnOutput(string output)
        {
            if (string.IsNullOrEmpty(output)) return;

            if (!this.oldStyle && output.StartsWith("tasks", StringComparison.CurrentCultureIgnoreCase) ||
                !this.oldStyle && output.StartsWith("mem:", StringComparison.CurrentCultureIgnoreCase) ||
                this.oldStyle && output.StartsWith("user", StringComparison.CurrentCultureIgnoreCase))
            {
                // はじまり
                if (this.currentTop == null || this.currentTop.CpuByTid.Count > 0)
                {
                    this.PushSnapshot(this.currentTop);
                    this.currentTop = new TopSnapshot(DateTime.Now);
                }
                return;
            }

            if (this.currentTop == null) return;

            if (!this.ParseLine(output, out var pid, out var tid, out var cpu)) return;

            if (cpu <= 0.0f)
            {
                // 以降は全部0%なので無視
                this.PushSnapshot(this.currentTop);
                this.currentTop = null;
            }
            else
            {
                this.currentTop.CpuByPid[pid] = this.currentTop.CpuByPid.TryGetValue(pid, out var pCpu) ? (pCpu + cpu) : cpu;
                this.currentTop.CpuByTid[tid] = cpu;
                this.currentTop.TotalCpu += cpu;
            }
        }

        bool ParseLine(string input, out int pid, out int tid, out float cpu)
        {
            // Old: 27732 27732 shell    20   0  24% R   9080K   2488K  fg top             top

            pid = 0;
            tid = 0;
            cpu = 0.0f;

            var tokens = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (!int.TryParse(tokens[0], out pid)) return false;
            if (!int.TryParse(tokens[1], out tid)) return false;
            var result = this.oldStyle ?
                float.TryParse(tokens[5].TrimEnd('%'), out cpu) :
                float.TryParse(tokens[2], out cpu);
            return result;
        }

        void PushSnapshot(TopSnapshot top)
        {
            if (top == null) return;
            if (this.snapshots.Count >= this.maxSnapshots) this.snapshots.RemoveAt(0);
            this.snapshots.Add(top);
            this.onReceived(this, top);
        }

        #region IDisposable Support
        bool disposed = false;

        public virtual void Dispose()
        {
            if (this.disposed) return;
            this.context?.Cancel();
            this.context = null;
            this.disposed = true;
        }
        #endregion
    }

    class TopSnapshot
    {
        //public float this[int tid] { get { return this.CpuUsageByThreadId.TryGetValue(tid, out var value) ? value : 0.0f; } }
        public DateTime Timestamp { get; private set; }
        public float TotalCpu { get; internal set; }

        internal Dictionary<int, float> CpuByTid = new Dictionary<int, float>();
        internal Dictionary<int, float> CpuByPid = new Dictionary<int, float>();

        public float GetThreadCpu(int tid)
        {
            return this.CpuByTid.TryGetValue(tid, out var cpu) ? cpu : 0.0f;
        }

        public float GetProcessCpu(int pid)
        {
            return this.CpuByPid.TryGetValue(pid, out var cpu) ? cpu : 0.0f;
        }

        internal TopSnapshot(DateTime timestamp)
        {
            this.Timestamp = timestamp;
        }
    }
}
