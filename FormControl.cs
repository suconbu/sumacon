using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using WeifenLuo.WinFormsUI.Docking;

namespace Suconbu.Sumacon
{
    public partial class FormControl : FormBase
    {
        enum ProcAction { PressPower, PressVolumeUp, PressVolumeDown, RotateScreenCw, RotateScreenCcw }

        Sumacon sumacon;
        StatusStrip uxScreenStatusStrip = new StatusStrip();
        SplitContainer uxBaseSplitContaier = new SplitContainer() { Dock = DockStyle.Fill };
        SplitContainer uxUpperSplitContaier = new SplitContainer() { Dock = DockStyle.Fill };
        PictureBox uxScreenPictureBox = new PictureBox() { Dock = DockStyle.Fill };
        GridPanel uxActionsGridPanel = new GridPanel() { Dock = DockStyle.Fill };
        GridPanel uxLogGridPanel = new GridPanel() { Dock = DockStyle.Fill };
        ControlActionGroup actionGroup;
        string updateScreenTimeoutId;
        bool beepEnabled = true;

        readonly int kActionsGridPanelWidth = 150;
        readonly int kUpdateScreenDelayedMilliseconds = 1000;

        public FormControl(Sumacon sumacon)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.sumacon = sumacon;
            this.sumacon.DeviceManager.ActiveDeviceChanged += this.DeviceManager_ActiveDeviceChanged;
        }

        protected override void OnLoad(EventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnLoad(e);

            this.actionGroup = ControlActionGroup.FromXml("control_actions.xml");
            this.uxActionsGridPanel.ApplyColorSet(ColorSet.Light);
            this.uxActionsGridPanel.AutoGenerateColumns = true;
            this.uxActionsGridPanel.MultiSelect = false;
            this.uxActionsGridPanel.ColumnHeadersVisible = false;
            this.uxActionsGridPanel.DataSource = this.actionGroup.Actions;
            this.uxActionsGridPanel.SetDefaultCellStyle();
            this.uxActionsGridPanel.MouseDown += this.UxActionsGridPanel_MouseDown;
            this.uxActionsGridPanel.MouseMove += this.UxActionsGridPanel_MouseMove;
            this.uxActionsGridPanel.KeyDown += this.UxActionsGridPanel_KeyDown;
            this.uxActionsGridPanel.ShowCellToolTips = true;
            this.uxActionsGridPanel.CellToolTipTextNeeded += this.UxActionsGridPanel_CellToolTipTextNeeded;

            this.uxScreenStatusStrip.Items.Add("Position");
            this.uxScreenStatusStrip.Items.Add("Auto update");
            this.uxScreenStatusStrip.Items.Add("Update");
            this.uxScreenStatusStrip.Items.Add("Beep");
            this.uxScreenStatusStrip.SizingGrip = false;

            this.uxScreenPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            this.uxScreenPictureBox.MouseDown += this.UxScreenPictureBox_MouseDown;
            this.uxScreenPictureBox.MouseMove += this.UxScreenPictureBox_MouseMove;
            this.uxScreenPictureBox.MouseUp += this.UxScreenPictureBox_MouseUp;

            this.uxUpperSplitContaier.Orientation = Orientation.Vertical;
            this.uxUpperSplitContaier.Panel1.Controls.Add(this.uxScreenPictureBox);
            this.uxUpperSplitContaier.Panel2.Controls.Add(this.uxActionsGridPanel);
            this.uxUpperSplitContaier.FixedPanel = FixedPanel.Panel2;

            this.uxBaseSplitContaier.Orientation = Orientation.Horizontal;
            this.uxBaseSplitContaier.Panel1.Controls.Add(this.uxUpperSplitContaier);
            this.uxBaseSplitContaier.Panel1.Controls.Add(this.uxScreenStatusStrip);
            this.uxBaseSplitContaier.Panel2.Controls.Add(this.uxLogGridPanel);
            this.Controls.Add(this.uxBaseSplitContaier);

            this.uxUpperSplitContaier.SplitterDistance = this.uxUpperSplitContaier.Width - this.kActionsGridPanelWidth;
            this.uxBaseSplitContaier.SplitterDistance = this.uxBaseSplitContaier.Height * 70 / 100;

            this.uxActionsGridPanel.Columns[nameof(ControlAction.Name)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.uxActionsGridPanel.Columns[nameof(ControlAction.Command)].Visible = false;
            this.uxActionsGridPanel.Columns[nameof(ControlAction.Proc)].Visible = false;

            this.UpdateScreenPicture();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnClosing(e);
            this.sumacon.DeviceManager.ActiveDeviceChanged -= this.DeviceManager_ActiveDeviceChanged;
        }

        void DeviceManager_ActiveDeviceChanged(object sender, Device previousDevice)
        {
            this.UpdateScreenPicture();
        }

        void UxScreenPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device == null) return;
            if (!this.GetNormalizedTouchPoint(e.Location, out var point)) return;
            device.Input.OnTouch(0, point.X, point.Y);
        }

        void UxScreenPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (!e.Button.HasFlag(MouseButtons.Left)) return;
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device == null) return;
            if (!this.GetNormalizedTouchPoint(e.Location, out var point)) return;
            device.Input.MoveTouch(0, point.X, point.Y);
        }

        void UxScreenPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device == null) return;
            device.Input.OffTouch(0);
        }

        private void UxActionsGridPanel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.ExecuteAction(this.GetSelectedAction());
                e.SuppressKeyPress = true;
            }
        }

        void UxActionsGridPanel_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                e.ToolTipText = this.actionGroup.Actions[e.RowIndex].Command;
            }
        }

        void UxActionsGridPanel_MouseDown(object sender, MouseEventArgs e)
        {
            this.ExecuteAction(this.GetSelectedAction());
        }

        void UxActionsGridPanel_MouseMove(object sender, MouseEventArgs e)
        {
            var p = this.uxActionsGridPanel.PointToClient(Control.MousePosition);
            var hit = this.uxActionsGridPanel.HitTest(p.X, p.Y);
            if (hit.Type == DataGridViewHitTestType.Cell)
            {
                this.uxActionsGridPanel.Rows[hit.RowIndex].Cells[hit.ColumnIndex].Selected = true;
            }
        }

        void UpdateScreenPicture()
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device != null)
            {
                device.Screen.CaptureAsync(bitmap => this.uxScreenPictureBox.Image = bitmap);
            }
        }

        void UpdateScreenPictureDelayed()
        {
            this.updateScreenTimeoutId = Delay.SetTimeout(
                () => this.SafeInvoke(this.UpdateScreenPicture),
                this.kUpdateScreenDelayedMilliseconds,
                this.updateScreenTimeoutId);
        }

        void ExecuteAction(ControlAction action)
        {
            if (this.beepEnabled) Beep.Play(Beep.Note.Po, Beep.Note.Pe);

            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (action == null || device == null) return;

            if (!string.IsNullOrEmpty(action.Command))
            {
                device.RunCommandAsync(action.Command).Wait(() => this.UpdateScreenPictureDelayed());
            }
            if(!string.IsNullOrEmpty(action.Proc))
            {
                this.ExecuteProc(device, action.Proc);
            }
        }

        void ExecuteProc(Device device, string procName)
        {
            if (!Enum.TryParse<ProcAction>(procName, out var proc)) return;

            if (proc == ProcAction.PressPower) device.Input.PressSwitch(Input.HardSwitch.Power);
            else if (proc == ProcAction.PressVolumeUp) device.Input.PressSwitch(Input.HardSwitch.VolumeUp);
            else if (proc == ProcAction.PressVolumeDown) device.Input.PressSwitch(Input.HardSwitch.VolumeDown);
            else if (proc == ProcAction.RotateScreenCw) this.ExecuteProcRotateScreen(device, 1);
            else if (proc == ProcAction.RotateScreenCcw) this.ExecuteProcRotateScreen(device, -1);
        }

        void ExecuteProcRotateScreen(Device device, int direction)
        {
            var current = device.UserRotation;
            if (device.AutoRotate)
            {
                current = device.CurrentRotation;
                device.AutoRotate = false;
            }
            int code = (int)current + direction;
            while (code < 0) code += 4;
            while (code >= 4) code -= 4;
            device.UserRotation = (Mobile.Screen.RotationCode)Enum.Parse(typeof(Mobile.Screen.RotationCode), code.ToString());
            device.Screen.PullAsync().Wait(() => this.UpdateScreenPictureDelayed());
        }

        ControlAction GetSelectedAction()
        {
            return (this.uxActionsGridPanel.SelectedRows.Count > 0) ?
                this.actionGroup.Actions[this.uxActionsGridPanel.SelectedRows[0].Index] : null;
        }

        bool GetNormalizedTouchPoint(Point point, out PointF normalizedPoint)
        {
            // PictureBox相対座標からディスプレイ正規化座標へ
            normalizedPoint = new PointF();
            var image = this.uxScreenPictureBox.Image;
            if (image == null) return false;
            var imageRatio = (float)image.Width / image.Height;
            var boxRect = this.uxScreenPictureBox.ClientRectangle;
            var boxRatio = (float)boxRect.Width / boxRect.Height;
            var screenRect = boxRect;
            if (imageRatio > boxRatio) // 絵の方が横長
            {
                screenRect.Height = (int)(boxRect.Width / imageRatio);
                screenRect.Y = (boxRect.Height - screenRect.Height) / 2;
            }
            else
            {
                screenRect.Width = (int)(boxRect.Height * imageRatio);
                screenRect.X = (boxRect.Width - screenRect.Width) / 2;
            }
            if (!screenRect.Contains(point)) return false;
            normalizedPoint = new PointF(
                (float)(point.X - screenRect.X) / screenRect.Width,
                (float)(point.Y - screenRect.Y) / screenRect.Height);
            return true;
        }
    }

    [XmlRoot("root")]
    public class ControlActionGroup
    {
        [XmlElement("action")]
        public List<ControlAction> Actions;

        public static ControlActionGroup FromXml(string path)
        {
            var serializer = new XmlSerializer(typeof(ControlActionGroup));
            using (var reader = new StreamReader(path))
            {
                return serializer.Deserialize(reader) as ControlActionGroup;
            }
        }
    }

    public class ControlAction
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("command")]
        public string Command { get; set; }
        [XmlAttribute("proc")]
        public string Proc { get; set; }
    }
}
