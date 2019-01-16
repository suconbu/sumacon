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
        public int ObserveIntervalMilliseconds { get; set; } = 10000;

        Device activeDevice;
        List<Device> connectedDevices = new List<Device>();
        DeviceDetector detector = new DeviceDetector();
        Dictionary<string, int> susupendRequestedCount = new Dictionary<string, int>();

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

        public void SuspendObserve(Device device)
        {
            if (device == null) return;
            int count = this.susupendRequestedCount[device.Id]++;
            if (count == 0)
            {
                device.StartObserve(int.MaxValue);
            }
        }

        public void ResumeObserve(Device device)
        {
            if (device == null) return;
            int count = --this.susupendRequestedCount[device.Id];
            if(count <= 0 && device == this.activeDevice && device.ObserveActivated)
            {
                device.StartObserve(this.ObserveIntervalMilliseconds);
                this.susupendRequestedCount[device.Id] = 0;
            }
        }

        void ChangeActiveDevice(Device nextActiveDevice)
        {
            if (this.activeDevice == nextActiveDevice) return;

            var previousDevice = this.activeDevice;
            foreach (var component in (previousDevice?.Components).OrEmptyIfNull())
            {
                component.PropertyChanged -= this.DeviceComponent_PropertyChanged;
            }
            previousDevice?.StopObserve();

            this.activeDevice = nextActiveDevice;
            foreach (var component in (this.activeDevice?.Components).OrEmptyIfNull())
            {
                component.PropertyChanged += this.DeviceComponent_PropertyChanged;
            }
            activeDevice?.StartObserve(this.ObserveIntervalMilliseconds);

            this.ActiveDeviceChanged(this, previousDevice);
        }

        void Detector_Connected(object sender, string deviceId)
        {
            if (this.connectedDevices.Find(d => d.Id == deviceId) != null) return;

            var device = new Device(deviceId);
            this.connectedDevices.Add(device);
            this.susupendRequestedCount[deviceId] = 0;
            this.ConnectedDevicesChanged(this, EventArgs.Empty);
            this.DeviceConnected(this, device);

            if (this.activeDevice == null)
            {
                this.ChangeActiveDevice(device);
            }
        }

        void Detector_Disconnected(object sender, string deviceId)
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
            Trace.TraceInformation(Util.GetCurrentMethodName());
            foreach (var p in properties) Trace.TraceInformation($"- {p.ToString()}");

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
