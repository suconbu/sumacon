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
        DockPanel dockPanel;
        FormProperty propertyForm;
        FormConsole consoleForm;
        FormShortcut shortcutForm;
        ToolStripDropDownButton deviceDropDown;
        ToolStripItem deviceInfoLabel;

        DeviceManager deviceManager;

        readonly int observeIntervalMilliseconds = 3000;

        public FormMain()
        {
            InitializeComponent();

            this.KeyPreview = true;

            this.dockPanel = new DockPanel();
            this.dockPanel.Dock = DockStyle.Fill;
            this.dockPanel.DocumentStyle = DocumentStyle.DockingWindow;
            this.toolStripContainer1.ContentPanel.Controls.Add(this.dockPanel);

            this.deviceDropDown = new ToolStripDropDownButton(this.imageList1.Images["phone.png"]);
            this.statusStrip1.Items.Add(this.deviceDropDown);
            this.deviceInfoLabel = this.statusStrip1.Items.Add(string.Empty);

            this.SetupDeviceManager();

            this.consoleForm = new FormConsole();
            this.consoleForm.Show(this.dockPanel, DockState.DockBottom);
            this.shortcutForm = new FormShortcut();
            this.shortcutForm.Show(this.dockPanel, DockState.DockRight);
            this.propertyForm = new FormProperty(this.deviceManager);
            this.propertyForm.Show(this.dockPanel, DockState.DockRight);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.deviceManager.StartDeviceWatching();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            this.deviceManager.Dispose();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
        }

        void SetupDeviceManager()
        {
            this.deviceManager = new DeviceManager();
            this.deviceManager.PropertyChanged += (s, properties) =>
            {
                Trace.TraceInformation("PropertyChanged");
                Trace.Indent();
                foreach (var p in properties) Trace.TraceInformation(p.ToString());
                Trace.Unindent();

                this.SafeInvoke(() => this.UpdateStatusDeviceInfo());
            };
            this.deviceManager.ConnectedDevicesChanged += (s, ee) =>
            {
                this.SafeInvoke(() => this.UpdateDeviceList());
            };
            this.deviceManager.ActiveDeviceChanged += (s, previousActiveDevice) =>
            {
                previousActiveDevice?.StopObserve();
                var activeDevice = this.deviceManager.ActiveDevice;
                activeDevice?.StartObserve(this.observeIntervalMilliseconds);

                this.SafeInvoke(() => this.UpdateStatusDeviceInfo());
            };
        }

        void UpdateDeviceList()
        {
            this.deviceDropDown.DropDownItems.Clear();
            foreach (var device in this.deviceManager.ConnectedDevices.OrEmptyIfNull())
            {
                var t = $"{device.Model} ({device.Name}) - {device.Id}";
                var item = this.deviceDropDown.DropDownItems.Add(t);
                item.Image = this.imageList1.Images["phone.png"];
                item.Click += (s, e) => this.deviceManager.ActiveDevice = device;
            }
        }

        void UpdateStatusDeviceInfo()
        {
            var device = this.deviceManager.ActiveDevice;

            if (device != null)
            {
                this.deviceDropDown.Text = $"{device.Model} ({device.Name})";
                this.deviceDropDown.Image = this.imageList1.Images["phone.png"];

                this.deviceInfoLabel.Text = $"{device.ScreenSize.Width}x{device.ScreenSize.Height} ({device.ScreenDensity} DPI) " +
                    $"🔋 {device.ChargeLevel} % ({device.Status})";
            }
            else
            {
                this.deviceDropDown.Text = "-";
                this.deviceInfoLabel.Text = string.Empty;
            }
        }
    }
}
