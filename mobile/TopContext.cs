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
        public IReadOnlyList<TopInfo> Snapshots { get { return this.snapshots; } }

        CommandContext context;
        List<TopInfo> snapshots = new List<TopInfo>();
        TopInfo currentTopInfo;
        readonly int maxSnapshots = 100;
        EventHandler<TopInfo> onReceived;

        static ConcurrentDictionary<Device, bool> oldStyleByDevice = new ConcurrentDictionary<Device, bool>();

        public static TopContext Start(Device device, int intervalSeconds, EventHandler<TopInfo> onReceived)
        {
            var instance = new TopContext();
            instance.onReceived = onReceived;

            var oldStyle = oldStyleByDevice.GetOrAdd(device, d =>
            {
                bool result = false;
                // 新形式「See top --help」旧形式「Invalid argument "0".」
                device.RunCommandOutputTextAsync("shell top 0", (output, error) => result = !error.StartsWith("See")).Wait();
                return result;
            });

            var command = $"shell top -H -d {intervalSeconds} -s 2 -o TID,%CPU";
            //var command = $"shell top -b -H -d {intervalSeconds} -s 2 -o TID,%CPU";
            if(oldStyle)
            {
                command = $"shell top -H -m 50 -d {intervalSeconds} -s cpu";
            }
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

            if ((output.StartsWith("Mem") || output.StartsWith("Tasks")) &&
                (this.currentTopInfo == null || this.currentTopInfo.CpuByThreads.Count > 0))
            {
                this.AddToSnapshots(this.currentTopInfo);
                this.currentTopInfo = new TopInfo(DateTime.Now);
            }

            if (this.currentTopInfo == null) return;

            if (!this.ParseLine(output, out var tid, out var cpu)) return;

            if (cpu <= 0.0f)
            {
                // 以降は全部0%なので無視
                this.AddToSnapshots(this.currentTopInfo);
                this.currentTopInfo = null;
            }
            else
            {
                this.currentTopInfo.CpuByThreads[tid] = cpu;
            }
        }

        bool ParseLine(string line, out int tid, out float cpu)
        {
            bool result = false;
            tid = 0;
            cpu = 0.0f;

            var tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(tokens[0], out var dummy)) return false;

            try
            {
                if (tokens.Length == 2)
                {
                    tid = int.Parse(tokens[0]);
                    cpu = float.Parse(tokens[1]);
                    result = true;
                }
                else if (tokens.Length == 12)
                {
                    // 旧形式
                    tid = int.Parse(tokens[1]);
                    cpu = int.Parse(tokens[5].TrimEnd('%')) / 100.0f;
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
            return result;
        }

        void AddToSnapshots(TopInfo topInfo)
        {
            if (topInfo == null) return;
            if (this.snapshots.Count >= this.maxSnapshots) this.snapshots.RemoveAt(0);
            this.snapshots.Add(topInfo);
            this.onReceived(this, topInfo);
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

    class TopInfo
    {
        public float this[int tid] { get { return this.CpuByThreads.TryGetValue(tid, out var value) ? value : 0.0f; } }
        public DateTime Timestamp { get; private set; }

        internal Dictionary<int, float> CpuByThreads = new Dictionary<int, float>();
        //Dictionary<int, TopInfoRecord> records = new Dictionary<int, TopInfoRecord>();

        internal TopInfo(DateTime timestamp)
        {
            this.Timestamp = timestamp;
        }
    }

    //struct TopInfoRecord
    //{
    //    public int Tid { get; private set; }
    //    public float Cpu { get; private set; }

    //    public static TopInfoRecord Null = new TopInfoRecord();

    //    public bool IsNull() { return this.Tid == 0; }

    //    internal TopInfoRecord(int tid, float cpu)
    //    {
    //        this.Tid = tid;
    //        this.Cpu = cpu;
    //    }
    //}
}
