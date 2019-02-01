﻿using Suconbu.Mobile;
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
        FormRecord recordForm;
        FormLog logForm;
        ToolStripDropDownButton deviceDropDown;
        ToolStripItem deviceInfoLabel;
        ToolStripItem memoryInfoLabel;
        Timer memoryTimer = new Timer() { Interval = 1000 };

        Sumacon sumacon = new Sumacon();
        //DeviceManager deviceManager;
        //CommandReceiver commandReceiver;
        //LogReceiverManager logReceiverManager;

        public FormMain()
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.Icon = Properties.Resources.sumacon;

            this.KeyPreview = true;

            this.SetupDeviceManager();
            //this.logReceiverManager = new LogReceiverManager();
        }

        protected override void OnLoad(EventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnLoad(e);

            this.dockPanel = new DockPanel();
            this.dockPanel.Dock = DockStyle.Fill;
            this.dockPanel.DocumentStyle = DocumentStyle.DockingWindow;
            this.toolStripContainer1.ContentPanel.Controls.Add(this.dockPanel);

            this.deviceDropDown = new ToolStripDropDownButton(this.imageList1.Images["phone.png"]);
            this.statusStrip1.Items.Add(this.deviceDropDown);
            this.deviceInfoLabel = this.statusStrip1.Items.Add(string.Empty);
            this.statusStrip1.Items.Add(new ToolStripStatusLabel() { Spring = true });
            this.memoryInfoLabel = this.statusStrip1.Items.Add(string.Empty);

            this.consoleForm = new FormConsole(this.sumacon);
            this.consoleForm.Show(this.dockPanel, DockState.DockBottom);
            this.shortcutForm = new FormShortcut(this.sumacon);
            this.shortcutForm.Show(this.dockPanel, DockState.DockRight);
            this.propertyForm = new FormProperty(this.sumacon);
            this.propertyForm.Show(this.dockPanel, DockState.DockRight);
            this.captureForm = new FormCapture(this.sumacon);
            this.captureForm.Show(this.dockPanel, DockState.Document);
            this.recordForm = new FormRecord(this.sumacon);
            this.recordForm.Show(this.dockPanel, DockState.Document);
            this.logForm = new FormLog(this.sumacon);
            this.logForm.Show(this.dockPanel, DockState.Document);

            this.sumacon.DeviceManager.StartDeviceDetection();

            this.memoryTimer.Tick += this.MemoryTimer_Tick;
            this.memoryTimer.Start();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnClosing(e);
            this.sumacon.Dispose();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            this.shortcutForm.NotifyKeyDown(e);
        }

        void SetupDeviceManager()
        {
            this.sumacon.DeviceManager.PropertyChanged += (s, properties) =>
            {
                this.SafeInvoke(() => this.UpdateStatusDeviceInfo());
            };
            this.sumacon.DeviceManager.ConnectedDevicesChanged += (s, ee) =>
            {
                this.SafeInvoke(() => this.UpdateDeviceList());
            };
            this.sumacon.DeviceManager.ActiveDeviceChanged += (s, previousActiveDevice) =>
            {
                this.SafeInvoke(() => this.UpdateStatusDeviceInfo());
            };
        }

        void UpdateDeviceList()
        {
            this.deviceDropDown.DropDownItems.Clear();
            var devices = this.sumacon.DeviceManager.ConnectedDevices.ToArray();
            foreach (var device in devices)
            {
                var label = device.ToString(Properties.Resources.DeviceLabelFormat);
                var item = this.deviceDropDown.DropDownItems.Add(label);
                item.Image = this.imageList1.Images["phone.png"];
                item.Click += (s, e) => this.sumacon.DeviceManager.ActiveDevice = device;
            }
        }

        void UpdateStatusDeviceInfo()
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;

            if (device != null)
            {
                this.deviceDropDown.Text = device.ToString(Properties.Resources.FormMain_StatusDeviceFormat);
                this.deviceDropDown.Image = this.imageList1.Images["phone.png"];
                var color = Util.ColorFromHsv(Math.Abs(device.Model.GetHashCode()) % 360, 1.0f, 1.0f);
                this.deviceDropDown.BackColor = color;
                this.deviceDropDown.ForeColor = color.GetLuminance() >= 0.5f ? Color.Black : Color.White;

                this.deviceInfoLabel.Text =
                    device.ToString(Properties.Resources.FormMain_StatusScreenFormat) +
                    " " +
                    device.ToString(Properties.Resources.FormMain_StatusBatteryFormat);
            }
            else
            {
                this.deviceDropDown.Text = "-";
                this.deviceInfoLabel.Text = string.Empty;
                this.deviceDropDown.BackColor = SystemColors.Control;
                this.deviceDropDown.ForeColor = SystemColors.ControlText;
            }
        }

        void MemoryTimer_Tick(object sender, EventArgs e)
        {
            this.memoryInfoLabel.Text = $"{GC.GetTotalMemory(false):#,##0} byte";
        }
    }
}
