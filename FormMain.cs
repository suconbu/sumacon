using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
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
        FormShortcut shortcutForm;
        FormCapture captureForm;
        FormRecord recordForm;
        FormLog logForm;
        FormPerformance performanceForm;
        ToolStripDropDownButton deviceDropDown;
        ToolStripLabel deviceInfoLabel = new ToolStripLabel();
        ToolStripButton airplaneModeButton = new ToolStripButton() { CheckOnClick = true };
        ToolStripButton showTouchesButton = new ToolStripButton() { CheckOnClick = true };
        //ToolStripLabel memoryInfoLabel = new ToolStripLabel();
        Timer memoryTimer = new Timer() { Interval = 1000 };

        Sumacon sumacon = new Sumacon();

        public FormMain()
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.Text = $"{Util.GetApplicationName()} - ver{Util.GetVersionString(3)}";
            this.Icon = Properties.Resources.sumacon;

            this.KeyPreview = true;

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

            this.deviceDropDown = new ToolStripDropDownButton(this.imageList1.Images["phone.png"]);
            this.statusStrip1.Items.Add(this.deviceDropDown);
            this.statusStrip1.Items.Add(this.deviceInfoLabel);
            this.statusStrip1.Items.Add(this.airplaneModeButton);
            this.statusStrip1.Items.Add(this.showTouchesButton);
            this.statusStrip1.Items.Add(new ToolStripStatusLabel() { Spring = true });
//#if DEBUG
//            this.statusStrip1.Items.Add(this.memoryInfoLabel);
//#endif

            this.consoleForm = new FormConsole(this.sumacon);
            this.consoleForm.Text = "Console";
            this.consoleForm.Show(this.dockPanel, DockState.DockBottom);
            this.shortcutForm = new FormShortcut(this.sumacon);
            this.shortcutForm.Text = "Command";
            this.shortcutForm.Show(this.dockPanel, DockState.DockRight);
            this.propertyForm = new FormProperty(this.sumacon);
            this.propertyForm.Text = "Property";
            this.propertyForm.Show(this.dockPanel, DockState.DockRight);
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

            foreach (var form in this.dockPanel.Contents)
            {
                form.DockHandler.CloseButtonVisible = false;
            }

            this.airplaneModeButton.Click += this.AirplaneModeButton_Click;
            this.showTouchesButton.Click += this.ShowTouchesButton_Click;

            //this.memoryTimer.Tick += this.MemoryTimer_Tick;
            //this.memoryTimer.Start();

            this.sumacon.DeviceManager.StartDeviceDetection();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnClosing(e);

            foreach (var component in (this.sumacon.DeviceManager.ActiveDevice?.Components).OrEmptyIfNull())
            {
                component.ResetAsync();
            }

            // 片付けているところが見えないように
            this.Visible = false;
            var forms = this.dockPanel.Contents.ToArray();
            foreach (var form in forms)
            {
                form.DockHandler.Close();
            }
            Properties.Settings.Default.Save();
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
                item.Image = this.imageList1.Images["phone.png"];
                item.Click += (s, e) => this.sumacon.DeviceManager.ActiveDevice = device;
            }
        }

        void UpdateControlState()
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
                this.deviceInfoLabel.Visible = true;

                this.airplaneModeButton.Text = device.AirplaneMode ? "✈ ON" : "✈ OFF";
                this.airplaneModeButton.Checked = device.AirplaneMode;
                this.airplaneModeButton.Visible = true;
                this.showTouchesButton.Text = device.ShowTouches ? "👆 ON" : "👆 OFF";
                this.showTouchesButton.Checked = device.ShowTouches;
                this.showTouchesButton.Visible = true;
            }
            else
            {
                this.deviceDropDown.BackColor = SystemColors.Control;
                this.deviceDropDown.ForeColor = SystemColors.ControlText;
                this.deviceDropDown.Text = "-";
                this.deviceInfoLabel.Visible = false;
                this.airplaneModeButton.Visible = false;
                this.showTouchesButton.Visible = false;
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

        //void MemoryTimer_Tick(object sender, EventArgs e)
        //{
        //    this.memoryInfoLabel.Text = $"{GC.GetTotalMemory(false):#,##0} byte";
        //}
    }
}
