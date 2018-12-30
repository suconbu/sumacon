using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Suconbu.Sumacon
{
    public partial class FormMain : FormBase
    {
        DockPanel dockPanel = new DockPanel();
        FormProperty propertyForm = new FormProperty();
        ToolStripDropDownButton deviceInfoDropDown;
        ToolStripItem displayInfoLabel;
        ToolStripItem batteryInfoLabel;

        DeviceWatcher watcher = new DeviceWatcher();
        List<Device> devices = new List<Device>();
        Device selectedDevice;

        public FormMain()
        {
            InitializeComponent();

            this.KeyPreview = true;

            this.dockPanel.Dock = DockStyle.Fill;
            this.dockPanel.DocumentStyle = DocumentStyle.DockingWindow;
            this.toolStripContainer1.ContentPanel.Controls.Add(this.dockPanel);

            this.propertyForm.Show(this.dockPanel, DockState.DockRight);

            this.deviceInfoDropDown = new ToolStripDropDownButton(this.imageList1.Images["phone.png"]);
            this.statusStrip1.Items.Add(this.deviceInfoDropDown);
            this.statusStrip1.Items.Add(new ToolStripSeparator());
            this.displayInfoLabel = this.statusStrip1.Items.Add(string.Empty);
            this.statusStrip1.Items.Add(new ToolStripSeparator());
            this.batteryInfoLabel = this.statusStrip1.Items.Add(string.Empty);
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

                var device = new Device(deviceId, 1000);
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
                this.deviceInfoDropDown.DropDownItems.Clear();
                foreach (var device in this.devices)
                {
                    var t = $"{device.Model} ({device.Name}) - {device.Id}";
                    var item = this.deviceInfoDropDown.DropDownItems.Add(t);
                    item.Image = this.imageList1.Images["phone.png"];
                    item.Click += (s, e) => this.ChangeSelectedDevice(device);
                }
            }
        }

        void ChangeSelectedDevice(Device device)
        {
            this.selectedDevice = device;
            this.propertyForm.TargetDevice = device;

            if (device != null)
            {
                this.deviceInfoDropDown.Text = $"{device.Model} ({device.Name})";
                this.deviceInfoDropDown.Image = this.imageList1.Images["phone.png"];
            }
            else
            {
                this.deviceInfoDropDown.Text = "-";
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
        }
    }
}
