using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Suconbu.Sumacon
{
    // デバイスIDとPIDをキーとしてLogReceiverを保持
    public class LogReceiverManager
    {
        class ReceiverEntry
        {
            public LogReceiver Receiver;
            public HashSet<LogSubscriber> Subscribers = new HashSet<LogSubscriber>();
            public HashSet<LogSubscriber> SuspendedSubscribers = new HashSet<LogSubscriber>();
        }

        readonly Dictionary<string, ReceiverEntry> entries = new Dictionary<string, ReceiverEntry>();

        public LogReceiverManager() { }

        public LogSubscriber AddSubscriber(Device device, LogReceiveSetting setting, EventHandler<LogReceiveEventArgs> onReceived)
        {
            var key = $"{device.Id}:{setting.GetHashCode()}";

            var newSubscriber = new LogSubscriber(key, onReceived);
            newSubscriber.SuspendedChanged += (s, suspended) => this.OnSubscriberSuspendedChanged(newSubscriber, suspended);
            newSubscriber.Disposing += (s, e) => this.OnSubscriberDisposing(newSubscriber);

            ReceiverEntry entry;
            lock (this.entries)
            {
                if (!this.entries.TryGetValue(key, out entry))
                {
                    entry = new ReceiverEntry();
                    entry.Receiver = new LogReceiver(device, setting);
                    entry.Receiver.Received += (s, log) =>
                    {
                        foreach (var subscriber in entry.Subscribers)
                        {
                            subscriber.OnReceived(new LogReceiveEventArgs()
                            {
                                DeviceId = device.Id,
                                Receiver = entry.Receiver,
                                Log = log
                            });
                        }
                    };
                    this.entries.Add(key, entry);
                }
                entry.Subscribers.Add(newSubscriber);
            }
            entry.Receiver.Start();
            return newSubscriber;
        }

        void OnSubscriberSuspendedChanged(LogSubscriber subscriber, bool suspended)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            int suspendedCount = 0;
            ReceiverEntry entry;
            lock (this.entries)
            {
                if (!this.entries.TryGetValue(subscriber.Key, out entry)) return;
                if (suspended)
                {
                    entry.SuspendedSubscribers.Add(subscriber);
                }
                else
                {
                    entry.SuspendedSubscribers.Remove(subscriber);
                }
                suspendedCount = entry.SuspendedSubscribers.Count;
            }
            entry.Receiver.Enabled = (suspendedCount == 0);
        }

        void OnSubscriberDisposing(LogSubscriber subscriber)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            lock (this.entries)
            {
                if (!this.entries.TryGetValue(subscriber.Key, out var entry)) return;
                entry.SuspendedSubscribers.Remove(subscriber);
                entry.Subscribers.Remove(subscriber);
                if (entry.Subscribers.Count == 0)
                {
                    entry.Receiver.Stop();
                    this.entries.Remove(subscriber.Key);
                }
                else
                {
                    if (entry.SuspendedSubscribers.Count == 0 && !entry.Receiver.Enabled)
                    {
                        entry.Receiver.Start();
                    }
                }
            }
        }
    }

    public class LogReceiveEventArgs : EventArgs
    {
        public string DeviceId;
        public LogReceiver Receiver;
        public Log Log;
    }

    public class LogSubscriber : IDisposable
    {
        internal string Key { get; private set; }

        internal event EventHandler<bool> SuspendedChanged = delegate { };
        internal event EventHandler Disposing = delegate { };

        readonly EventHandler<LogReceiveEventArgs> onRceived;
        bool suspended;

        internal LogSubscriber(string key, EventHandler<LogReceiveEventArgs> onReceived)
        {
            this.Key = key;
            this.onRceived = onReceived;
        }

        public bool Suspended
        {
            get { return this.suspended; }
            set
            {
                if (this.suspended != value)
                {
                    this.suspended = value;
                    this.SuspendedChanged(this, value);
                }
            }
        }

        internal void OnReceived(LogReceiveEventArgs args)
        {
            this.onRceived?.Invoke(this, args);
        }

        #region IDisposable Support
        bool disposed = false;

        public virtual void Dispose()
        {
            if (this.disposed) return;
            this.Disposing(this, EventArgs.Empty);
            this.disposed = true;
        }
        #endregion
    }
}
