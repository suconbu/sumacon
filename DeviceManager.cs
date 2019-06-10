using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Collections.Concurrent;

namespace Suconbu.Sumacon
{
    public class DeviceManager : IDisposable
    {
        public event EventHandler ConnectedDevicesChanged = delegate { };
        // ConnectedDevicesChangedの後に通知します(対象はConnectedDevicesにすでに存在します)
        public event EventHandler<Device/* connectedDevice */> DeviceConnected = delegate { };
        // ConnectedDevicesChangedの前に通知します(対象はConnectedDevicesにまだ存在します)
        public event EventHandler<Device/* disconnectingDevice */> DeviceDisconnecting = delegate { };
        public event EventHandler<Device/* previousDevice */> ActiveDeviceChanged = delegate { };
        public event EventHandler<IReadOnlyList<Property>> PropertyChanged = delegate { };
        public event EventHandler<TouchProtocolType> TouchProtocolTypeChanged = delegate { };

        public Device ActiveDevice
        {
            get { return this.activeDevice; }
            set { this.ChangeActiveDevice(value); }
        }
        public IReadOnlyList<Device> ConnectedDevices { get { return this.connectedDevices.Values.ToList(); } }
        public TouchProtocolType TouchProtocolType
        {
            get => this.touchProtocolType;
            set
            {
                if(this.touchProtocolType != value)
                {
                    this.touchProtocolType = value;
                    if(this.activeDevice != null)
                    {
                        this.activeDevice.Input.TouchProtocol = value;
                    }
                    this.TouchProtocolTypeChanged(this, value);
                }
            }
        }

        Device activeDevice;
        readonly ConcurrentDictionary<string/*serial*/, Device> connectedDevices = new ConcurrentDictionary<string, Device>();
        readonly ConcurrentDictionary<string/*serial*/, int/*port*/> wirelessWaitingDevices = new ConcurrentDictionary<string, int>();
        readonly DeviceDetector detector = new DeviceDetector();
        readonly Dictionary<string/*serial*/, int> susupendRequestedCount = new Dictionary<string, int>();
        readonly Dictionary<string/*serial*/, Dictionary<Device.UpdatableProperties, string>> intervalKeys = new Dictionary<string, Dictionary<Device.UpdatableProperties, string>>();
        readonly Dictionary<Device.UpdatableProperties, int> intervalMilliseconds = new Dictionary<Device.UpdatableProperties, int>();
        TouchProtocolType touchProtocolType;

        readonly int kWirelessPortMin = 5500;
        readonly int kWirelessPortMax = 5600;
        readonly Random random = new Random();

        public DeviceManager()
        {
            this.detector.Connected += this.Detector_Connected;
            this.detector.Disconnected += this.Detector_Disconnected;

            this.LoadSettings();

            //this.intervalMilliseconds[Device.UpdatableProperties.Component] = 30 * 1000;
            this.intervalMilliseconds[Device.UpdatableProperties.ProcessInfo] = 10 * 1000;
#if DEBUG
            this.intervalMilliseconds[Device.UpdatableProperties.ProcessInfo] = 5 * 1000;
#endif
        }

        /// <summary>
        /// デバイスの接続状態監視を開始します。
        /// 接続状態の変化は下記イベントで通知します。
        /// - ConnectedDevicesChanged
        /// - DeviceConnected
        /// - DeviceDisconnecting
        /// </summary>
        public void StartDeviceDetection()
        {
            this.detector.Start();
        }

        /// <summary>
        /// デバイスの接続状態監視を停止します。
        /// </summary>
        public void StopDeviceDetection()
        {
            this.detector.Stop();
        }

        /// <summary>
        /// デバイスからの定期的な情報取得を一時的に停止します。
        /// 再開するには ResumePropertyUpdate を呼び出します。
        /// </summary>
        public void SuspendPropertyUpdate(Device device)
        {
            if (device == null) return;
            Trace.TraceInformation($"{Util.GetCurrentMethodName()} - {device.Serial} count:{this.susupendRequestedCount[device.Serial]}->{this.susupendRequestedCount[device.Serial] + 1}");
            lock (this.susupendRequestedCount)
            {
                Debug.Assert(this.susupendRequestedCount[device.Serial] >= 0);
                this.susupendRequestedCount[device.Serial]++;
            }
        }

        /// <summary>
        /// デバイスからの定期的な情報取得を再開します。
        /// </summary>
        public void ResumePropertyUpdate(Device device)
        {
            if (device == null) return;
            Trace.TraceInformation($"{Util.GetCurrentMethodName()} - {device.Serial} count:{this.susupendRequestedCount[device.Serial]}->{this.susupendRequestedCount[device.Serial] - 1}");
            lock (this.susupendRequestedCount)
            {
                if (this.susupendRequestedCount[device.Serial] > 0)
                {
                    this.susupendRequestedCount[device.Serial]--;
                }
            }
        }

        public void StartWireless(Device device)
        {
            if (!this.wirelessWaitingDevices.ContainsKey(device.Serial))
            {
                var port = this.GenerateWirelessPortNo();
                this.wirelessWaitingDevices[device.Serial] = port;
                device.RunCommandAsync($"tcpip {port}");
                // 一旦接続切れてまたつながる
            }
        }

        int GenerateWirelessPortNo()
        {
            return this.random.Next(this.kWirelessPortMin, this.kWirelessPortMax);
        }

        void ChangeActiveDevice(Device nextActiveDevice)
        {
            lock (this)
            {
                if (this.activeDevice == nextActiveDevice) return;

                var previousDevice = this.activeDevice;
                foreach (var component in (previousDevice?.Components).OrEmptyIfNull())
                {
                    component.PropertyChanged -= this.DeviceComponent_PropertyChanged;
                }
                if (previousDevice != null)
                {
                    this.StopPropertyUpdate(previousDevice);
                }

                this.activeDevice = nextActiveDevice;
                if (this.activeDevice != null)
                {
                    this.activeDevice.Input.TouchProtocol = this.touchProtocolType;
                }
                foreach (var component in (this.activeDevice?.Components).OrEmptyIfNull())
                {
                    component.PropertyChanged += this.DeviceComponent_PropertyChanged;
                }
                if (this.activeDevice != null)
                {
                    this.StartPropertyUpdate(this.activeDevice);
                }

                this.ActiveDeviceChanged(this, previousDevice);
            }
        }

        void Detector_Connected(object sender, string serial)
        {
            if(this.wirelessWaitingDevices.TryRemove(serial, out var port))
            {
                // ワイヤレス希望なので登録はせず接続試行
                this.ReconnectDeviceAsWireless(serial, port);
            }
            else
            {
                this.AddConnectedDevice(serial);
            }
        }

        void Detector_Disconnected(object sender, string serial)
        {
            this.RemoveConnectedDevice(serial);
        }

        void DeviceComponent_PropertyChanged(object sender, IReadOnlyList<Property> properties)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            foreach (var p in properties) Trace.TraceInformation($"- {p.ToString()}");

            this.PropertyChanged(sender, properties);
        }

        void StartPropertyUpdate(Device device)
        {
            this.susupendRequestedCount[device.Serial] = 0;

            this.intervalKeys[device.Serial] = new Dictionary<Device.UpdatableProperties, string>();
            foreach(var updatableProperty in this.intervalMilliseconds.Keys)
            {
                device.UpdatePropertiesAsync(updatableProperty);
                this.intervalKeys[device.Serial][updatableProperty] = Delay.SetInterval(() =>
                {
                    if (this.susupendRequestedCount[device.Serial] == 0)
                    {
                        device.UpdatePropertiesAsync(updatableProperty);
                    }
                }, this.intervalMilliseconds[updatableProperty]);
            }
        }

        void StopPropertyUpdate(Device device)
        {
            foreach (var updatableProperty in this.intervalMilliseconds.Keys)
            {
                if (this.intervalKeys[device.Serial].TryGetValue(updatableProperty, out var id))
                {
                    Delay.ClearInterval(id);
                }
            }
            this.intervalKeys.Remove(device.Serial);
        }

        void AddConnectedDevice(string serial)
        {
            if (this.connectedDevices.ContainsKey(serial)) return;

            var device = new Device(serial);
            this.connectedDevices[serial] = device;
            this.susupendRequestedCount[serial] = 0;
            this.ConnectedDevicesChanged(this, EventArgs.Empty);
            this.DeviceConnected(this, device);

            if (this.activeDevice == null || device.HasWirelessConnection)
            {
                this.ChangeActiveDevice(device);
            }

            device.UpdatePropertiesAsync();
        }

        void RemoveConnectedDevice(string serial)
        {
            if (!this.connectedDevices.TryGetValue(serial, out var device)) return;

            this.DeviceDisconnecting(this, device);
            this.connectedDevices.TryRemove(serial, out var removed);
            this.ConnectedDevicesChanged(this, EventArgs.Empty);
            if (this.activeDevice == device)
            {
                var nextActiveDevice = this.connectedDevices.Values.FirstOrDefault();
                this.ChangeActiveDevice(nextActiveDevice);
            }
            device.Dispose();
        }

        void ReconnectDeviceAsWireless(string serial, int port)
        {
            var device = new Device(serial);    // Update掛けるのやめたい
            device.UpdatePropertiesAsync(Device.UpdatableProperties.SystemComponent, () =>
            {
                var wirelessSerial = $"{device.IpAddress}:{port}";
                device.RunCommandAsync($"connect {wirelessSerial}");
                device.Dispose();
                // この後、serial=wirelessSerialでDetector_Connectedくる
            });
        }

        void LoadSettings()
        {
            this.TouchProtocolType = Properties.Settings.Default.DeviceManagerTouchProtocolType;
            this.TouchProtocolTypeChanged(this, this.TouchProtocolType);
        }

        void SaveSettings()
        {
            Properties.Settings.Default.DeviceManagerTouchProtocolType = this.TouchProtocolType;
        }

        #region IDisposable Support
        bool disposed = false;

        public virtual void Dispose()
        {
            if (this.disposed) return;

            this.SaveSettings();

            foreach (var keys in this.intervalKeys.Values)
            {
                foreach (var key in keys.Values)
                {
                    Delay.ClearInterval(key);
                }
            }
            this.StopDeviceDetection();
            foreach (var device in this.connectedDevices.Values) device.Dispose();

            this.disposed = true;
        }
        #endregion
    }
}
