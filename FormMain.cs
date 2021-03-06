﻿using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Suconbu.Sumacon
{
    public partial class FormMain : FormBase
    {
        DockPanel dockPanel;
        FormProperty propertyForm;
        FormConsole consoleForm;
        FormCapture captureForm;
        FormRecord recordForm;
        FormLog logForm;
        FormPerformance performanceForm;
        FormControl controlForm;
        FormScript scriptForm;
        ToolStripDropDownButton deviceDropDown;
        ToolStripLabel deviceInfoLabel = new ToolStripLabel();
        ToolStripButton airplaneModeButton = new ToolStripButton();
        ToolStripButton showTouchesButton = new ToolStripButton();
        ToolStripButton wirelessAdbButton = new ToolStripButton();
        //ToolStripLabel memoryInfoLabel = new ToolStripLabel();
        Timer memoryTimer = new Timer() { Interval = 1000 };
        FormWindowState previousWindowState;
        List<IDockContent> previousVisibleContents = new List<IDockContent>();

        Sumacon sumacon = new Sumacon();

        public FormMain()
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.Text = $"{Util.GetApplicationName()} - ver{Util.GetVersionString(3)}";
            this.Icon = Properties.Resources.sumacon;

            this.KeyPreview = true;
            this.AllowDrop = true;

            this.SetupDeviceManager();
        }

        protected override void OnLoad(EventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnLoad(e);

            this.dockPanel = new DockPanel();
            this.dockPanel.Dock = DockStyle.Fill;
            this.dockPanel.DocumentStyle = DocumentStyle.DockingWindow;
            this.toolStripContainer1.ContentPanel.Controls.Add(this.dockPanel);

            this.deviceDropDown = new ToolStripDropDownButton();
            this.statusStrip1.Items.Add(this.deviceDropDown);
            this.statusStrip1.Items.Add(this.deviceInfoLabel);
            this.statusStrip1.Items.Add(this.airplaneModeButton);
            this.statusStrip1.Items.Add(this.showTouchesButton);
            this.statusStrip1.Items.Add(this.wirelessAdbButton);
            this.statusStrip1.Items.Add(new ToolStripStatusLabel() { Spring = true });
//#if DEBUG
//            this.statusStrip1.Items.Add(this.memoryInfoLabel);
//#endif

            this.consoleForm = new FormConsole(this.sumacon);
            this.consoleForm.Text = "Console";
            this.consoleForm.Show(this.dockPanel, DockState.DockBottom);
            this.propertyForm = new FormProperty(this.sumacon);
            this.propertyForm.Text = "Property";
            this.propertyForm.Show(this.dockPanel, DockState.DockRight);
            this.scriptForm = new FormScript(this.sumacon);
            this.scriptForm.Text = "Script";
            this.scriptForm.Show(this.dockPanel, DockState.DockRight);
            this.captureForm = new FormCapture(this.sumacon);
            this.captureForm.Text = "Capture";
            this.captureForm.Show(this.dockPanel, DockState.Document);
            this.recordForm = new FormRecord(this.sumacon);
            this.recordForm.Text = "Record";
            this.recordForm.Show(this.dockPanel, DockState.Document);
            this.logForm = new FormLog(this.sumacon);
            this.logForm.Text = "Log";
            this.logForm.Show(this.dockPanel, DockState.Document);
            this.performanceForm = new FormPerformance(this.sumacon);
            this.performanceForm.Text = "Performace";
            this.performanceForm.Show(this.dockPanel, DockState.Document);
            this.controlForm = new FormControl(this.sumacon);
            this.controlForm.Text = "Control";
            this.controlForm.Show(this.dockPanel, DockState.Document);

            //this.propertyForm.Activate();
            this.controlForm.Activate();

            foreach (var form in this.dockPanel.Contents)
            {
                form.DockHandler.CloseButtonVisible = false;
            }

            this.airplaneModeButton.Click += this.AirplaneModeButton_Click;
            this.showTouchesButton.Click += this.ShowTouchesButton_Click;
            this.wirelessAdbButton.Click += this.WirelessAdbButton_Click;

            this.LoadSettings();

            this.sumacon.SaveCapturedImageRequested += (image) => this.captureForm.SaveCapturedImage(image);
            this.sumacon.DeviceManager.StartDeviceDetection();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnClosing(e);

            var device = this.sumacon.DeviceManager.ActiveDevice;
            if(device != null)
            {
                this.sumacon.DeviceManager.SuspendPropertyUpdate(device);
                foreach (var component in device.Components)
                {
                    component.ResetAsync();
                }
            }

            // 片付けているところが見えないように
            this.Visible = false;
            var forms = this.dockPanel.Contents.ToArray();
            foreach (var form in forms)
            {
                form.DockHandler.Close();
            }

            this.SaveSettings();

            this.sumacon.Dispose();
        }

        protected override void OnClosed(EventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnClosed(e);

            Properties.Settings.Default.Save();
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);
            if (this.sumacon.DeviceManager.ActiveDevice != null)
            {
                var path = (e.Data.GetData(DataFormats.FileDrop, false) as string[]).FirstOrDefault();
                if (path != null && Path.GetExtension(path).ToLower() == ".apk")
                {
                    e.Effect = DragDropEffects.All;
                }
            }
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnDragDrop(e);
            var path = (e.Data.GetData(DataFormats.FileDrop, false) as string[]).FirstOrDefault();
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device != null)
            {
                var command = $"install -r {path}";
                this.sumacon.WriteConsole(command);
                device?.RunCommandAsync(command, output => this.sumacon.WriteConsole(output), error => this.sumacon.WriteConsole(error));
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if(e.KeyCode == Keys.F11)
            {
                e.Handled = true;
                this.ToggleFullscreen();
            }
        }

        void AirplaneModeButton_Click(object sender, EventArgs e)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device != null)
            {
                device.AirplaneMode = !device.AirplaneMode;
            }
        }

        void ShowTouchesButton_Click(object sender, EventArgs e)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device != null)
            {
                device.ShowTouches = !device.ShowTouches;
            }
        }

        void WirelessAdbButton_Click(object sender, EventArgs e)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device != null)
            {
                if (!this.wirelessAdbButton.Checked)
                {
                    this.sumacon.DeviceManager.StartWireless(device);
                }
                else
                {
                    device.Dispose();
                }
            }
        }

        void SetupDeviceManager()
        {
            this.sumacon.DeviceManager.PropertyChanged += (s, properties) =>
            {
                this.SafeInvoke(() => this.UpdateControlState());
            };
            this.sumacon.DeviceManager.ConnectedDevicesChanged += (s, ee) =>
            {
                this.SafeInvoke(() => this.UpdateDeviceList());
            };
            this.sumacon.DeviceManager.ActiveDeviceChanged += (s, previousActiveDevice) =>
            {
                this.SafeInvoke(() => this.UpdateControlState());
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
                item.Image = this.imageList1.Images[device.HasWirelessConnection ? "phone_denpa.png" : "phone.png"];
                item.Click += (s, e) => this.sumacon.DeviceManager.ActiveDevice = device;
            }
        }

        void ToggleFullscreen()
        {
            if (this.FormBorderStyle == FormBorderStyle.None)
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = this.previousWindowState;

                this.dockPanel.DocumentStyle = DocumentStyle.DockingWindow;
                var activeContent = this.dockPanel.ActiveContent;
                foreach (var content in this.dockPanel.Contents)
                {
                    content.DockHandler.Show();
                }
                foreach(var content in this.previousVisibleContents)
                {
                    content.DockHandler.Activate();
                }
                activeContent.DockHandler.Show();
            }
            else
            {
                foreach (var document in this.dockPanel.Documents)
                {
                    if (document.DockHandler.Form.Visible)
                    {
                        document.DockHandler.Activate();
                    }
                }

                this.previousVisibleContents.Clear();
                foreach (var content in this.dockPanel.Contents)
                {
                    if (content.DockHandler.Form.Visible)
                    {
                        this.previousVisibleContents.Add(content);
                    }
                }

                foreach (var content in this.dockPanel.Contents)
                {
                    if (content != this.dockPanel.ActiveContent)
                    {
                        content.DockHandler.Hide();
                    }
                }

                this.dockPanel.DocumentStyle = DocumentStyle.DockingSdi;    // タブを消すため

                this.previousWindowState = this.WindowState;
                if (this.WindowState == FormWindowState.Maximized)
                {
                    this.WindowState = FormWindowState.Normal;
                }
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
            }
        }

        void UpdateControlState()
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;

            if (device != null)
            {
                this.deviceDropDown.Text = device.ToString(Properties.Resources.FormMain_StatusDeviceFormat);
                this.deviceDropDown.Image = this.imageList1.Images[device.HasWirelessConnection ? "phone_denpa.png" : "phone.png"];
                var color = Util.ColorFromHsv(Math.Abs(device.Model.GetHashCode()) % 360, 1.0f, 1.0f);
                this.deviceDropDown.BackColor = color;
                this.deviceDropDown.ForeColor = color.GetLuminance() >= 0.5f ? Color.Black : Color.White;

                this.deviceInfoLabel.Text =
                    device.ToString(Properties.Resources.FormMain_StatusScreenFormat) +
                    " " +
                    device.ToString(Properties.Resources.FormMain_StatusBatteryFormat);
                this.deviceInfoLabel.Visible = true;

                this.airplaneModeButton.Text = device.AirplaneMode ? "✈ ON" : "✈ OFF";
                this.airplaneModeButton.Checked = device.AirplaneMode;
                this.airplaneModeButton.Visible = true;
                this.showTouchesButton.Text = device.ShowTouches ? "👆 ON" : "👆 OFF";
                this.showTouchesButton.Checked = device.ShowTouches;
                this.showTouchesButton.Visible = true;
                var wireless = device.HasWirelessConnection;
                this.wirelessAdbButton.Text = wireless ? $"WirelessConnect ON ({device.WirelessPort})" : "WirelessConnect OFF";
                this.wirelessAdbButton.Checked = wireless;
                this.wirelessAdbButton.Visible = true;
            }
            else
            {
                this.deviceDropDown.BackColor = SystemColors.Control;
                this.deviceDropDown.ForeColor = SystemColors.ControlText;
                this.deviceDropDown.Image = this.imageList1.Images["phone.png"];
                this.deviceDropDown.Text = "-";
                this.deviceInfoLabel.Visible = false;
                this.airplaneModeButton.Visible = false;
                this.showTouchesButton.Visible = false;
                this.wirelessAdbButton.Visible = false;
            }
        }

        void LoadSettings()
        {
            this.WindowState = Properties.Settings.Default.MainMaximized ? FormWindowState.Maximized : FormWindowState.Normal;
            this.Size = Properties.Settings.Default.MainSize;
        }

        void SaveSettings()
        {
            Properties.Settings.Default.MainMaximized = (this.WindowState == FormWindowState.Maximized);
            Properties.Settings.Default.MainSize = this.Size;
        }

        //void MemoryTimer_Tick(object sender, EventArgs e)
        //{
        //    this.memoryInfoLabel.Text = $"{GC.GetTotalMemory(false):#,##0} byte";
        //}
    }
}
