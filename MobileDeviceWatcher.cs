using SharpAdbClient;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Suconbu.Mobile
{
    class MobileDeviceWatcher
    {
        public event EventHandler<string> Connected = delegate { };
        public event EventHandler<string> Disconnected = delegate { };

        public IReadOnlyList<string> DeviceIds { get { return this.deviceIds; } }

        DeviceMonitor monitor;
        readonly List<string> deviceIds = new List<string>();

        public MobileDeviceWatcher(string adbPath = null)
        {
            if(string.IsNullOrWhiteSpace(adbPath))
            {
                CommandContext.StartNewText("where", "adb.exe", output => adbPath = output.Trim()).Wait();
            }
            AdbServer.Instance.StartServer(adbPath, false);
        }

        /// <summary>
        /// デバイス接続の監視を開始します。
        /// </summary>
        public void Start()
        {
            if (this.monitor != null) return;
            this.monitor = new DeviceMonitor(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)));
            this.monitor.DeviceConnected += (s, e) =>
            {
                var deviceId = e.Device.Serial;
                Trace.TraceInformation($"DeviceConnected - deviceId:{deviceId}");
                this.deviceIds.RemoveAll(id => id == deviceId);
                this.deviceIds.Add(deviceId);
                Task.Run(() =>
                {
                    // Online状態になるまでちょっと時間かかる
                    while (AdbClient.Instance.GetDevices().Find(d => d.Serial == deviceId).State != DeviceState.Online)
                    {
                        Thread.Sleep(10);
                    }
                    this.Connected(this, deviceId);
                });
            };
            this.monitor.DeviceDisconnected += (s, e) =>
            {
                var deviceId = e.Device.Serial;
                Trace.TraceInformation($"DeviceDisconnected - deviceId:{deviceId}");
                this.deviceIds.RemoveAll(id => id == deviceId);
                Task.Run(() => this.Disconnected(this, deviceId));
            };
            this.monitor.Start();
        }

        /// <summary>
        /// 監視を停止します。
        /// </summary>
        public void Stop()
        {
            if (this.monitor == null) return;
            this.monitor.Dispose();
            this.monitor = null;
        }
    }
}
