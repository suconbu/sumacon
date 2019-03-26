using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
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
        int maxSnapshots;

        public static TopContext Start(Device device, int intervalSeconds = 3, int maxSnapshots = 100)
        {
            var instance = new TopContext();
            instance.maxSnapshots = maxSnapshots;

            var command = $"shell top -b -q -H -s 1 -d {intervalSeconds} -o TID,%CPU";
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

            if (this.currentTopInfo == null)
            {
                this.currentTopInfo = new TopInfo(DateTime.Now);
            }

            var tokens = output.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                var tid = int.Parse(tokens[0]);
                var cpu = float.Parse(tokens[1]);
                var threadTopInfo = new ThreadTopInfo(tid, cpu);
                this.currentTopInfo.Push(threadTopInfo);
                if(tid == 1)
                {
                    if (this.snapshots.Count >= this.maxSnapshots) this.snapshots.RemoveAt(0);
                    this.snapshots.Add(this.currentTopInfo);
                    this.currentTopInfo = null;
                }
            }
            catch(Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
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
        public IReadOnlyList<ThreadTopInfo> Threads { get { return this.threads.Values.ToList(); } }
        public ThreadTopInfo this[int tid] { get { return this.threads[tid]; } }
        public DateTime Timestamp { get; private set; }

        Dictionary<int, ThreadTopInfo> threads = new Dictionary<int, ThreadTopInfo>();

        internal TopInfo(DateTime timestamp)
        {
            this.Timestamp = timestamp;
        }

        internal void Push(ThreadTopInfo threadTopInfo)
        {
            this.threads.Add(threadTopInfo.Tid, threadTopInfo);
        }
    }

    struct ThreadTopInfo
    {
        public int Tid { get; private set; }
        public float Cpu { get; private set; }

        internal ThreadTopInfo(int tid, float cpu)
        {
            this.Tid = tid;
            this.Cpu = cpu;
        }
    }
}
