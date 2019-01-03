using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
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
        List<Device> connectedDevices = new List<Device>();
        DeviceDetector detector = new DeviceDetector();

        public DeviceManager()
        {
            this.detector.Connected += this.Detector_Connected;
            this.detector.Disconnected += this.Detector_Disconnected;
        }

        public void StartDeviceDetection()
        {
            this.detector.Start();
        }

        public void StopDeviceDetection()
        {
            this.detector.Stop();
        }

        void ChangeActiveDevice(Device nextActiveDevice)
        {
            if (this.activeDevice == nextActiveDevice) return;

            var previousDevice = this.activeDevice;
            foreach (var component in (previousDevice?.Components).OrEmptyIfNull())
            {
                component.PropertyChanged -= this.DeviceComponent_PropertyChanged;
            }

            this.activeDevice = nextActiveDevice;
            foreach (var component in (this.activeDevice?.Components).OrEmptyIfNull())
            {
                component.PropertyChanged += this.DeviceComponent_PropertyChanged;
            }

            this.ActiveDeviceChanged(this, previousDevice);
        }

        private void Detector_Connected(object sender, string deviceId)
        {
            if (this.connectedDevices.Find(d => d.Id == deviceId) != null) return;

            var device = new Device(deviceId);
            this.connectedDevices.Add(device);
            this.ConnectedDevicesChanged(this, EventArgs.Empty);
            this.DeviceConnected(this, device);

            if (this.activeDevice == null)
            {
                this.ChangeActiveDevice(device);
            }
        }

        private void Detector_Disconnected(object sender, string deviceId)
        {
            var device = this.connectedDevices.Find(d => d.Id == deviceId);
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

        void DeviceComponent_PropertyChanged(object sender, IReadOnlyList<Property> properties)
        {
            this.PropertyChanged(sender, properties);
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
