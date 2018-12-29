using Suconbu.Mobile;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Suconbu.Sumacon
{
    public partial class FormMain : Form
    {
        DockPanel dockPanel = new DockPanel();
        ToolStripDropDownButton deviceInfoDropDown;
        ToolStripItem displayInfoLabel;
        ToolStripItem batteryInfoLabel;
        bool closed = false;

        MobileDeviceWatcher watcher = new MobileDeviceWatcher();
        List<MobileDevice> devices = new List<MobileDevice>();
        MobileDevice selectedDevice;

        public FormMain()
        {
            InitializeComponent();

            this.KeyPreview = true;

            this.dockPanel.Dock = DockStyle.Fill;
            this.dockPanel.DocumentStyle = DocumentStyle.DockingWindow;

            this.deviceInfoDropDown = new ToolStripDropDownButton(this.imageList1.Images["phone.png"]);
            this.statusStrip1.Items.Add(this.deviceInfoDropDown);
            this.statusStrip1.Items.Add(new ToolStripSeparator());
            this.displayInfoLabel = this.statusStrip1.Items.Add(string.Empty);
            this.statusStrip1.Items.Add(new ToolStripSeparator());
            this.batteryInfoLabel = this.statusStrip1.Items.Add(string.Empty);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            this.closed = true;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.watcher.Connected += this.Watcher_Connected;
            this.watcher.Disconnected += this.Watcher_Disconnected;
            this.watcher.Start();
        }

        private void Watcher_Connected(object sender, string deviceId)
        {
            this.SafeInvoke(() =>
            {
                if (this.devices.Find(d => d.Id == deviceId) != null) return;

                var device = new MobileDevice(deviceId, 1000);
                device.Battery.PropertyChanged += (s, names) =>
                {
                    Trace.TraceInformation("PropertyChanged");
                    Trace.Indent();
                    foreach (var name in names) Trace.TraceInformation(name);
                    Trace.Unindent();
                };
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

        void SafeInvoke(MethodInvoker action)
        {
            if(!this.closed) this.Invoke(action);
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

        void ChangeSelectedDevice(MobileDevice device)
        {
            this.selectedDevice = device;

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

            if (e.KeyCode == Keys.P)
            {
                this.selectedDevice?.GetScreenCaptureAsync(image =>
                {
                    this.Invoke((MethodInvoker)(() => { this.pictureBox1.Image = image; }));
                });
            }
        }
    }
}
