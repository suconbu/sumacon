using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Suconbu.Sumacon
{
    public partial class FormMain : FormBase
    {
        DockPanel dockPanel = new DockPanel();
        FormProperty propertyForm = new FormProperty();
        ToolStripDropDownButton deviceDropDown;
        ToolStripItem deviceInfoLabel;

        DeviceWatcher watcher = new DeviceWatcher();
        List<Device> devices = new List<Device>();
        Device selectedDevice;

        readonly int observeIntervalMilliseconds = 3000;

        public FormMain()
        {
            InitializeComponent();

            this.KeyPreview = true;

            this.dockPanel.Dock = DockStyle.Fill;
            this.dockPanel.DocumentStyle = DocumentStyle.DockingWindow;
            this.toolStripContainer1.ContentPanel.Controls.Add(this.dockPanel);

            this.propertyForm.Show(this.dockPanel, DockState.DockRight);

            this.deviceDropDown = new ToolStripDropDownButton(this.imageList1.Images["phone.png"]);
            this.statusStrip1.Items.Add(this.deviceDropDown);
            this.deviceInfoLabel = this.statusStrip1.Items.Add(string.Empty);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.watcher.Connected += this.Watcher_Connected;
            this.watcher.Disconnected += this.Watcher_Disconnected;
            this.watcher.Start();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            this.devices.ForEach(d => d.Dispose());
        }

        private void Watcher_Connected(object sender, string deviceId)
        {
            this.SafeInvoke(() =>
            {
                if (this.devices.Find(d => d.Id == deviceId) != null) return;

                var device = new Device(deviceId);
                this.devices.Add(device);
                this.UpdateDeviceList();
                if (this.selectedDevice == null)
                {
                    this.ChangeSelectedDevice(device);
                }
            });
        }

        private void Watcher_Disconnected(object sender, string deviceId)
        {
            this.SafeInvoke(() =>
            {
                var device = this.devices.Find(d => d.Id == deviceId);
                if (device == null) return;

                this.devices.Remove(device);
                this.UpdateDeviceList();
                if (this.selectedDevice == device)
                {
                    var nextDevice = (this.devices.Count > 0) ? this.devices[0] : null;
                    this.ChangeSelectedDevice(nextDevice);
                }
                device.Dispose();
            });
        }

        void UpdateDeviceList()
        {
            if (this.devices.Count > 0)
            {
                this.deviceDropDown.DropDownItems.Clear();
                foreach (var device in this.devices)
                {
                    var t = $"{device.Model} ({device.Name}) - {device.Id}";
                    var item = this.deviceDropDown.DropDownItems.Add(t);
                    item.Image = this.imageList1.Images["phone.png"];
                    item.Click += (s, e) => this.ChangeSelectedDevice(device);
                }
            }
        }

        void ChangeSelectedDevice(Device device)
        {
            if (this.selectedDevice != null)
            {
                foreach (var component in this.selectedDevice.Components)
                {
                    component.PropertyChanged -= this.DeviceComponent_PropertyChanged;
                }
                this.selectedDevice.ObserveIntervalMilliseconds = 0;
            }

            this.selectedDevice = device;
            if (this.selectedDevice != null)
            {
                foreach (var component in this.selectedDevice.Components)
                {
                    component.PropertyChanged += this.DeviceComponent_PropertyChanged;
                }
                this.selectedDevice.ObserveIntervalMilliseconds = this.observeIntervalMilliseconds;
            }

            this.propertyForm.TargetDevice = device;

            if (device != null)
            {
                this.deviceDropDown.Text = $"{device.Model} ({device.Name})";
                this.deviceDropDown.Image = this.imageList1.Images["phone.png"];
            }
            else
            {
                this.deviceDropDown.Text = "-";
            }
            this.UpdateDeviceInfo();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
        }

        void DeviceComponent_PropertyChanged(object sender, IReadOnlyList<Property> properties)
        {
            Trace.TraceInformation("PropertyChanged");
            Trace.Indent();
            foreach (var p in properties) Trace.TraceInformation(p.ToString());
            Trace.Unindent();

            this.SafeInvoke(() => this.UpdateDeviceInfo());
        }

        void UpdateDeviceInfo()
        {
            var device = this.selectedDevice;
            this.deviceInfoLabel.Text = string.Empty;
            if (device != null)
            {
                this.deviceInfoLabel.Text = $"{device.ScreenSize.Width}x{device.ScreenSize.Height} ({device.ScreenDensity} DPI) " +
                    $"🔋 {device.ChargeLevel} % ({device.Status})";
            }
        }
    }
}
