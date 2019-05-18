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
    public class DeviceManager : IDisposable
    {
        public event EventHandler ConnectedDevicesChanged = delegate { };
        // ConnectedDevicesChangedの後に通知します(対象はConnectedDevicesにすでに存在します)
        public event EventHandler<Device> DeviceConnected = delegate { };
        // ConnectedDevicesChangedの前に通知します(対象はConnectedDevicesにまだ存在します)
        public event EventHandler<Device> DeviceDisconnecting = delegate { };
        public event EventHandler<Device> ActiveDeviceChanged = delegate { };
        public event EventHandler<IReadOnlyList<Property>> PropertyChanged = delegate { };

        public Device ActiveDevice
        {
            get { return this.activeDevice; }
            set { this.ChangeActiveDevice(value); }
        }
        public IReadOnlyList<Device> ConnectedDevices { get { return this.connectedDevices; } }

        Device activeDevice;
        readonly List<Device> connectedDevices = new List<Device>();
        readonly DeviceDetector detector = new DeviceDetector();
        readonly Dictionary<string, int> susupendRequestedCount = new Dictionary<string, int>();
        readonly Dictionary<Device, Dictionary<Device.UpdatableProperties, string>> intervalIds = new Dictionary<Device, Dictionary<Device.UpdatableProperties, string>>();
        readonly Dictionary<Device.UpdatableProperties, int> intervalMilliseconds = new Dictionary<Device.UpdatableProperties, int>();
        readonly Dictionary<string/*serial*/, int/*port*/> wirelessWaitingDevices = new Dictionary<string, int>();

        readonly int kWirelessPortMin = 5500;
        readonly int kWirelessPortMax = 5600;
        readonly Random random = new Random();

        public DeviceManager()
        {
            this.detector.Connected += this.Detector_Connected;
            this.detector.Disconnected += this.Detector_Disconnected;

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
            if (this.activeDevice == nextActiveDevice) return;

            var previousDevice = this.activeDevice;
            foreach (var component in (previousDevice?.Components).OrEmptyIfNull())
            {
                component.PropertyChanged -= this.DeviceComponent_PropertyChanged;
            }
            if(previousDevice != null)
            {
                this.StopPropertyUpdate(previousDevice);
            }

            this.activeDevice = nextActiveDevice;
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

        void Detector_Connected(object sender, string serial)
        {
            var port = this.wirelessWaitingDevices.GetValue(serial, 0);
            if(port > 0)
            {
                // ワイヤレス希望なので登録はせず接続試行
                this.wirelessWaitingDevices.Remove(serial);
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

            this.intervalIds[device] = new Dictionary<Device.UpdatableProperties, string>();
            foreach(var updatableProperty in this.intervalMilliseconds.Keys)
            {
                device.UpdatePropertiesAsync(updatableProperty);
                this.intervalIds[device][updatableProperty] = Delay.SetInterval(() =>
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
                Delay.ClearInterval(this.intervalIds[device][updatableProperty]);
            }
            this.intervalIds.Remove(device);
        }

        void AddConnectedDevice(string serial)
        {
            if (this.connectedDevices.Find(d => d.Serial == serial) != null) return;

            var device = new Device(serial);
            this.connectedDevices.Add(device);
            this.susupendRequestedCount[serial] = 0;
            this.ConnectedDevicesChanged(this, EventArgs.Empty);
            this.DeviceConnected(this, device);

            if (this.activeDevice == null)
            {
                this.ChangeActiveDevice(device);
            }
        }

        void RemoveConnectedDevice(string serial)
        {
            var device = this.connectedDevices.Find(d => d.Serial == serial);
            if (device == null) return;

            this.DeviceDisconnecting(this, device);
            this.connectedDevices.Remove(device);
            this.ConnectedDevicesChanged(this, EventArgs.Empty);
            if (this.activeDevice == device)
            {
                var nextActiveDevice = this.connectedDevices.FirstOrDefault();
                this.ChangeActiveDevice(nextActiveDevice);
            }
            device.Dispose();
        }

        void ReconnectDeviceAsWireless(string serial, int port)
        {
            var device = new Device(serial);    // Update掛けるのやめたい
            var wirelessSerial = $"{device.IpAddress}:{port}";
            device.RunCommandAsync($"connect {wirelessSerial}");
            device.Dispose();
            // この後、serial=wirelessSerialでDetector_Connectedくる
        }

        #region IDisposable Support
        bool disposed = false;

        public virtual void Dispose()
        {
            if (this.disposed) return;

            this.StopDeviceDetection();
            this.connectedDevices.ForEach(d => d.Dispose());

            this.disposed = true;
        }
        #endregion
    }
}
