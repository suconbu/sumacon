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
        ToolStripStatusLabel uxTouchPositionLabel = new ToolStripStatusLabel();
        ToolStripStatusLabel uxColorLabel = new ToolStripStatusLabel();
        ToolStripButton uxBeepButton = new ToolStripButton();
        BindingDropDownButton<TouchProtocolType> uxTouchProtocolDropDown = new BindingDropDownButton<TouchProtocolType>();
        SplitContainer uxSplitContaier = new SplitContainer() { Dock = DockStyle.Fill };
        PictureBox uxScreenPictureBox = new PictureBox() { Dock = DockStyle.Fill };
        GridPanel uxActionsGridPanel = new GridPanel() { Dock = DockStyle.Fill };
        ControlActionGroup actionGroup;
        bool beepEnabled { get => this.uxBeepButton.Checked; set => this.uxBeepButton.Checked = value; }
        TouchProtocolType touchProtocolType { get => this.uxTouchProtocolDropDown.Value; set => this.uxTouchProtocolDropDown.Value = value; }
        int activeTouchNo = -1;
        Point touchPosition = new Point(-1, -1);

        readonly int kActionsGridPanelWidth = 150;
        readonly int kUpdateScreenIntervalMilliseconds = 500;

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

            this.uxBeepButton.CheckOnClick = true;
            this.uxBeepButton.Checked = true;
            this.uxBeepButton.CheckedChanged += (s, ee) => this.UpdateControlState();

            this.uxTouchProtocolDropDown.DataSource = new Dictionary<TouchProtocolType, ToolStripDropDownItem>()
            {
                { TouchProtocolType.A, new ToolStripMenuItem("Touch protocol A") },
                { TouchProtocolType.B, new ToolStripMenuItem("Touch protocol B") },
            };
            this.uxTouchProtocolDropDown.DropDownItemClicked += (s, ee) => this.UpdateControlState();

            this.uxColorLabel.Alignment = ToolStripItemAlignment.Right;
            this.uxColorLabel.AutoSize = false;
            this.uxColorLabel.Width = 200;
            this.uxColorLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.uxTouchPositionLabel.Alignment = ToolStripItemAlignment.Right;
            this.uxTouchPositionLabel.AutoSize = false;
            this.uxTouchPositionLabel.Width = 70;
            this.uxTouchPositionLabel.TextAlign = ContentAlignment.MiddleLeft;

            this.uxScreenStatusStrip.Items.Add(this.uxBeepButton);
            this.uxScreenStatusStrip.Items.Add(this.uxTouchProtocolDropDown);
            this.uxScreenStatusStrip.Items.Add(new ToolStripStatusLabel() { Spring = true });
            this.uxScreenStatusStrip.Items.Add(this.uxColorLabel);
            this.uxScreenStatusStrip.Items.Add(this.uxTouchPositionLabel);
            this.uxScreenStatusStrip.SizingGrip = false;

            this.uxScreenPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            this.uxScreenPictureBox.MouseDown += this.UxScreenPictureBox_MouseDown;
            this.uxScreenPictureBox.MouseMove += this.UxScreenPictureBox_MouseMove;
            this.uxScreenPictureBox.MouseUp += this.UxScreenPictureBox_MouseUp;

            this.uxSplitContaier.Orientation = Orientation.Vertical;
            this.uxSplitContaier.Panel1.Controls.Add(this.uxScreenPictureBox);
            this.uxSplitContaier.Panel2.Controls.Add(this.uxActionsGridPanel);
            this.uxSplitContaier.FixedPanel = FixedPanel.Panel2;
            this.Controls.Add(this.uxSplitContaier);

            this.uxSplitContaier.Panel1.Controls.Add(this.uxScreenStatusStrip);

            this.uxSplitContaier.SplitterDistance = this.uxSplitContaier.Width - this.kActionsGridPanelWidth;

            this.uxActionsGridPanel.Columns[nameof(ControlAction.Name)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.uxActionsGridPanel.Columns[nameof(ControlAction.Command)].Visible = false;
            this.uxActionsGridPanel.Columns[nameof(ControlAction.Proc)].Visible = false;

            this.LoadSettings();

            this.UpdateControlState();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnClosing(e);
            this.sumacon.DeviceManager.ActiveDeviceChanged -= this.DeviceManager_ActiveDeviceChanged;
            this.SaveSettings();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            if(this.Visible) this.StartScreenPictureUpdate();
        }

        void DeviceManager_ActiveDeviceChanged(object sender, Device previousDevice)
        {
            this.StartScreenPictureUpdate();
        }

        void UxScreenPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device == null) return;
            device.Input.TouchProtocol = this.touchProtocolType;
            if (!this.GetNormalizedTouchPoint(e.Location, out var point)) return;
            this.activeTouchNo = device.Input.OnTouch(point.X, point.Y);
            if (this.beepEnabled) Beep.Play(Beep.Note.Po);
        }

        void UxScreenPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            this.touchPosition = new Point(-1, -1);
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device == null) return;
            if (!this.GetNormalizedTouchPoint(e.Location, out var point)) return;

            this.touchPosition = new Point(
                (int)Math.Floor(point.X * this.uxScreenPictureBox.Image.Width),
                (int)Math.Floor(point.Y * this.uxScreenPictureBox.Image.Height));
            this.UpdateControlState();

            if (this.activeTouchNo != -1 && e.Button.HasFlag(MouseButtons.Left))
            {
                device.Input.MoveTouch(this.activeTouchNo, point.X, point.Y);
            }
        }

        void UxScreenPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.activeTouchNo == -1) return;
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device == null) return;
            device.Input.OffTouch();
            this.activeTouchNo = -1;
            if (this.beepEnabled) Beep.Play(Beep.Note.Pe);
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
            else
            {
                this.uxActionsGridPanel.ClearSelection();
            }
        }

        void StartScreenPictureUpdate()
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device != null && this.Visible)
            {
                device.Screen.CaptureAsync(bitmap => this.uxScreenPictureBox.Image = bitmap).Wait(() =>
                {
                    //Beep.Play(Beep.Note.Pe);
                    Delay.SetTimeout(() => this.StartScreenPictureUpdate(), this.kUpdateScreenIntervalMilliseconds);
                });
            }
        }

        void ExecuteAction(ControlAction action)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (action == null || device == null) return;

            if (this.beepEnabled) Beep.Play(Beep.Note.Po, Beep.Note.Pe);

            if (!string.IsNullOrEmpty(action.Command))
            {
                device.RunCommandAsync(action.Command);
            }
            if(!string.IsNullOrEmpty(action.Proc))
            {
                this.ExecuteProc(device, action.Proc);
            }
        }

        void ExecuteProc(Device device, string procName)
        {
            if (!Enum.TryParse<ProcAction>(procName, out var proc)) return;

            if (proc == ProcAction.RotateScreenCw) this.ExecuteProcRotateScreen(device, 1);
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

        void UpdateControlState()
        {
            if (this.touchPosition.X >= 0 && this.touchPosition.Y >= 0)
            {
                this.uxTouchPositionLabel.Text = $"👆 {this.touchPosition.X}, {this.touchPosition.Y}";
                var bitmap = this.uxScreenPictureBox.Image as Bitmap;
                var color = bitmap?.GetPixel(this.touchPosition.X, this.touchPosition.Y) ?? Color.Transparent;
                var h = color.GetHue();
                var s = color.GetSaturation() * 100;
                var v = color.GetLuminance() * 100;
                this.uxColorLabel.Text = $"rgb({color.R,3:0}, {color.G,3:0}, {color.B,3:0}) hsl({h,3:0}, {s,3:0}%, {v,3:0}%)";
                this.uxColorLabel.BackColor = color;
                this.uxColorLabel.ForeColor = color.GetLuminance() >= 0.5f ? Color.Black : Color.White;
            }
            else
            {
                this.uxTouchPositionLabel.Text = "👆 -, -";
                this.uxColorLabel.Text = "-";
            }

            this.uxBeepButton.Text = this.uxBeepButton.Checked ? "Beep ON" : "Beep OFF";
        }

        void LoadSettings()
        {
            this.beepEnabled = Properties.Settings.Default.ControlBeep;
            this.touchProtocolType = Properties.Settings.Default.ControlTouchProtocol;
        }

        void SaveSettings()
        {
            Properties.Settings.Default.ControlBeep = this.beepEnabled;
            Properties.Settings.Default.ControlTouchProtocol = this.touchProtocolType;
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
