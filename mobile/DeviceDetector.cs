﻿using SharpAdbClient;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Suconbu.Mobile
{
    class DeviceDetector
    {
        public event EventHandler<string> Connected = delegate { };
        public event EventHandler<string> Disconnected = delegate { };

        public IReadOnlyList<string> Serials { get { return this.serials; } }

        DeviceMonitor monitor;
        readonly List<string> serials = new List<string>();
        readonly string designatedAdbPath;
        readonly AdbClient adbClient = new AdbClient();

        public DeviceDetector(string adbPath = null)
        {
            this.designatedAdbPath = adbPath;
        }

        /// <summary>
        /// デバイス接続の監視を開始します。
        /// </summary>
        public void Start()
        {
            if (this.monitor != null) return;
            if (AdbServer.Instance.GetStatus().IsRunning)
            {
                this.StartMonitor();
            }
            else
            {
                this.StartAdbServerAsync(this.StartMonitor);
            }
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

        void StartAdbServerAsync(Action onFinished)
        {
            CommandContext.StartNew(() =>
            {
                var adbPath = this.designatedAdbPath;
                if (string.IsNullOrWhiteSpace(adbPath))
                {
                    CommandContext.StartNewText("where", "adb.exe", (output, error) =>
                    {
                        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        adbPath = (lines.Length > 0) ? lines[0].Trim() : null;
                    }).Wait();
                }
                AdbServer.Instance.StartServer(adbPath, false);
                onFinished?.Invoke();
            });
        }

        void  StartMonitor()
        {
            this.monitor = new DeviceMonitor(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)));
            this.monitor.DeviceConnected += (s, e) =>
            {
                var serial = e.Device.Serial;
                Trace.TraceInformation($"DeviceConnected - serial:{serial}");
                this.serials.RemoveAll(id => id == serial);
                this.serials.Add(serial);
                Task.Run(() =>
                {
                    // Online状態になるまでちょっと時間かかる
                    try
                    {
                        while (this.adbClient.GetDevices()?.Find(d => d.Serial == serial)?.State != DeviceState.Online)
                        {
                            Thread.Sleep(10);
                        }
                    }
                    catch(Exception ex)
                    {
                        Trace.TraceError(ex.ToString());
                        Thread.Sleep(100);
                    }
                    this.Connected(this, serial);
                });
            };
            this.monitor.DeviceDisconnected += (s, e) =>
            {
                var serial = e.Device.Serial;
                Trace.TraceInformation($"DeviceDisconnected - serial:{serial}");
                this.serials.RemoveAll(id => id == serial);
                Task.Run(() => this.Disconnected(this, serial));
            };
            this.monitor.Start();
        }
    }
}
