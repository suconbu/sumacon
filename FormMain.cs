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
        FormCapture captureForm;
        ToolStripDropDownButton deviceDropDown;
        ToolStripItem deviceInfoLabel;

        DeviceManager deviceManager;
        CommandReceiver commandReceiver;

        readonly int observeIntervalMilliseconds = 300000;

        public FormMain()
        {
            InitializeComponent();

            this.KeyPreview = true;

            this.commandReceiver = new CommandReceiver();
            this.SetupDeviceManager();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.dockPanel = new DockPanel();
            this.dockPanel.Dock = DockStyle.Fill;
            this.dockPanel.DocumentStyle = DocumentStyle.DockingWindow;
            this.toolStripContainer1.ContentPanel.Controls.Add(this.dockPanel);

            this.deviceDropDown = new ToolStripDropDownButton(this.imageList1.Images["phone.png"]);
            this.statusStrip1.Items.Add(this.deviceDropDown);
            this.deviceInfoLabel = this.statusStrip1.Items.Add(string.Empty);

            this.consoleForm = new FormConsole(this.deviceManager, this.commandReceiver);
            this.consoleForm.Show(this.dockPanel, DockState.DockBottom);
            this.shortcutForm = new FormShortcut(this.deviceManager, this.commandReceiver);
            this.shortcutForm.Show(this.dockPanel, DockState.DockRight);
            this.propertyForm = new FormProperty(this.deviceManager);
            this.propertyForm.Show(this.dockPanel, DockState.DockRight);
            this.captureForm = new FormCapture(this.deviceManager);
            this.captureForm.Show(this.dockPanel, DockState.Document);

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

            this.shortcutForm.NotifyKeyDown(e);
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
            var devices = this.deviceManager.ConnectedDevices.ToArray();
            foreach (var device in devices)
            {
                var label = device.ToString(Properties.Resources.DeviceLabelFormat);
                var item = this.deviceDropDown.DropDownItems.Add(label);
                item.Image = this.imageList1.Images["phone.png"];
                item.Click += (s, e) => this.deviceManager.ActiveDevice = device;
            }
        }

        void UpdateStatusDeviceInfo()
        {
            var device = this.deviceManager.ActiveDevice;

            if (device != null)
            {
                this.deviceDropDown.Text = device.ToString(Properties.Resources.FormMain_StatusDeviceFormat);
                this.deviceDropDown.Image = this.imageList1.Images["phone.png"];

                this.deviceInfoLabel.Text =
                    device.ToString(Properties.Resources.FormMain_StatusScreenFormat) +
                    " " +
                    device.ToString(Properties.Resources.FormMain_StatusBatteryFormat);
            }
            else
            {
                this.deviceDropDown.Text = "-";
                this.deviceInfoLabel.Text = string.Empty;
            }
        }
    }
}
