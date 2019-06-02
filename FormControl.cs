using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        ToolStripButton uxZoomButton = new ToolStripButton();
        ToolStripButton uxHoldButton = new ToolStripButton();
        BindingDropDownButton<TouchProtocolType> uxTouchProtocolDropDown = new BindingDropDownButton<TouchProtocolType>();
        SplitContainer uxMainSplitContaier = new SplitContainer() { Dock = DockStyle.Fill };
        PictureBox uxScreenPictureBox = new PictureBox() { Dock = DockStyle.Fill };
        GridPanel uxActionsGridPanel = new GridPanel() { Dock = DockStyle.Fill };
        ControlActionGroup actionGroup;
        bool beepEnabled { get => this.uxBeepButton.Checked; set => this.uxBeepButton.Checked = value; }
        bool zoomEnabled { get => this.uxZoomButton.Checked; set => this.uxZoomButton.Checked = value; }
        bool holdEnabled { get => this.uxHoldButton.Checked; set => this.uxHoldButton.Checked = value; }
        TouchProtocolType touchProtocolType { get => this.uxTouchProtocolDropDown.Value; set => this.uxTouchProtocolDropDown.Value = value; }
        int activeTouchNo = -1;
        Point screenPointedPosition = new Point(-1, -1);
        Panel uxTouchMarker = new Panel();
        ZoomBox uxZoomBox = new ZoomBox();
        int zoomRatioIndex;
        Point lastMousePosition;
        string delayedUpdateTimeoutId;

        readonly int kActionsGridPanelWidth = 150;
        readonly int kUpdateScreenIntervalMilliseconds = 500;
        readonly float kZoomPanelHeightRatio = 0.4f;
        readonly int kZoomPanelRelocateMarginPixels = 10;
        readonly int[] kZoomRatios = { 2, 5, 10, 20, 50, 100 };

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

            this.KeyPreview = true;

            this.actionGroup = ControlActionGroup.FromXml("control_actions.xml");
            this.uxActionsGridPanel.ApplyColorSet(ColorSet.Light);
            this.uxActionsGridPanel.AutoGenerateColumns = true;
            this.uxActionsGridPanel.MultiSelect = false;
            this.uxActionsGridPanel.ColumnHeadersVisible = false;
            this.uxActionsGridPanel.DataSource = this.actionGroup.Actions;
            this.uxActionsGridPanel.SetDefaultCellStyle();
            this.uxActionsGridPanel.MouseDown += this.UxActionsGridPanel_MouseDown;
            this.uxActionsGridPanel.MouseMove += this.UxActionsGridPanel_MouseMove;
            this.uxActionsGridPanel.ShowCellToolTips = true;
            this.uxActionsGridPanel.CellToolTipTextNeeded += this.UxActionsGridPanel_CellToolTipTextNeeded;

            this.uxBeepButton.CheckOnClick = true;
            this.uxBeepButton.AutoToolTip = false;
            this.uxBeepButton.CheckedChanged += (s, ee) => this.UpdateControlState();

            this.uxZoomButton.Text = "🔍 Zoom";
            this.uxZoomButton.ToolTipText = "Press Control key to show the zoom view.";
            this.uxScreenStatusStrip.ShowItemToolTips = true;

            this.uxHoldButton.CheckOnClick = true;
            this.uxHoldButton.AutoToolTip = false;
            this.uxHoldButton.CheckedChanged += (s, ee) => this.UpdateControlState();

            this.uxTouchProtocolDropDown.AutoToolTip = false;
            this.uxTouchProtocolDropDown.DataSource = new Dictionary<TouchProtocolType, ToolStripDropDownItem>()
            {
                { TouchProtocolType.A, new ToolStripMenuItem("Touch protocol A") },
                { TouchProtocolType.B, new ToolStripMenuItem("Touch protocol B") },
            };
            this.uxTouchProtocolDropDown.DropDownItemClicked += (s, ee) => this.UpdateControlState();

            this.uxColorLabel.Alignment = ToolStripItemAlignment.Right;
            this.uxColorLabel.AutoSize = false;
            this.uxColorLabel.Width = 100;
            this.uxColorLabel.TextAlign = ContentAlignment.MiddleLeft;

            this.uxTouchPositionLabel.Alignment = ToolStripItemAlignment.Right;
            this.uxTouchPositionLabel.AutoSize = false;
            this.uxTouchPositionLabel.Width = 70;
            this.uxTouchPositionLabel.TextAlign = ContentAlignment.MiddleLeft;

            this.uxScreenStatusStrip.Items.Add(this.uxBeepButton);
            this.uxScreenStatusStrip.Items.Add(this.uxZoomButton);
            this.uxScreenStatusStrip.Items.Add(this.uxTouchProtocolDropDown);
            this.uxScreenStatusStrip.Items.Add(this.uxHoldButton);
            this.uxScreenStatusStrip.Items.Add(new ToolStripStatusLabel() { Spring = true });
            this.uxScreenStatusStrip.Items.Add(this.uxColorLabel);
            this.uxScreenStatusStrip.Items.Add(this.uxTouchPositionLabel);
            this.uxScreenStatusStrip.SizingGrip = false;

            this.uxZoomBox.Visible = false;
            this.uxZoomBox.MouseMove += this.UxScreenPictureBox_MouseMove; // Redirect

            this.uxScreenPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            this.uxScreenPictureBox.MouseDown += this.UxScreenPictureBox_MouseDown;
            this.uxScreenPictureBox.MouseMove += this.UxScreenPictureBox_MouseMove;
            this.uxScreenPictureBox.MouseUp += this.UxScreenPictureBox_MouseUp;
            this.uxScreenPictureBox.MouseLeave += (s, ee) => this.UpdateControlState();
            this.uxScreenPictureBox.Controls.Add(this.uxZoomBox);

            this.uxTouchMarker.Visible = false;
            this.uxTouchMarker.BackColor = Color.OrangeRed;
            this.uxTouchMarker.Size = new Size(10, 10);
            this.uxScreenPictureBox.Controls.Add(this.uxTouchMarker);

            this.uxMainSplitContaier.Orientation = Orientation.Vertical;
            this.uxMainSplitContaier.Panel1.Controls.Add(this.uxScreenPictureBox);
            this.uxMainSplitContaier.Panel2.Controls.Add(this.uxActionsGridPanel);
            this.uxMainSplitContaier.FixedPanel = FixedPanel.Panel2;
            this.Controls.Add(this.uxMainSplitContaier);

            this.uxMainSplitContaier.Panel1.Controls.Add(this.uxScreenStatusStrip);

            this.uxMainSplitContaier.SplitterDistance = this.uxMainSplitContaier.Width - this.kActionsGridPanelWidth;

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
            if (this.GetScreenNormalizedPoint(e.Location, out var point))
            {
                if(e.Button.HasFlag(MouseButtons.Left))
                {
                    if (this.zoomEnabled)
                    {
                        this.PickColor();
                    }
                    else
                    {
                        this.activeTouchNo = device.Input.OnTouch(point.X, point.Y);
                        if (this.beepEnabled) Beep.Play(Beep.Note.Po);
                        this.uxTouchMarker.Visible = true;
                        this.uxTouchMarker.Location = new Point(e.X - this.uxTouchMarker.Width / 2, e.Y - this.uxTouchMarker.Height / 2);
                    }
                }
            }
            this.UpdateControlState();
        }

        void UxScreenPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.lastMousePosition == e.Location) return;
            this.lastMousePosition = e.Location;

            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device != null && this.GetScreenNormalizedPoint(e.Location, out var point))
            {
                this.screenPointedPosition = new Point(
                    (int)Math.Floor(point.X * this.uxScreenPictureBox.Image.Width),
                    (int)Math.Floor(point.Y * this.uxScreenPictureBox.Image.Height));

                if (this.activeTouchNo != -1 && e.Button.HasFlag(MouseButtons.Left))
                {
                    device.Input.MoveTouch(this.activeTouchNo, point.X, point.Y);
                    this.uxTouchMarker.Location = new Point(e.X - this.uxTouchMarker.Width / 2, e.Y - this.uxTouchMarker.Height / 2);
                }
            }
            else
            {
                this.screenPointedPosition = new Point(-1, -1);
            }

            // ここいっぱい呼ばれるからちょびっと遅延させて負荷抑制
            this.delayedUpdateTimeoutId = Delay.SetTimeout(() => this.UpdateControlState(), 1, this, this.delayedUpdateTimeoutId, true);
        }

        void UxScreenPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            this.uxTouchMarker.Visible = false;
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device != null && this.activeTouchNo != -1)
            {
                device.Input.OffTouch();
                this.activeTouchNo = -1;
                if (this.beepEnabled) Beep.Play(Beep.Note.Pe);
            }
            this.UpdateControlState();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            var image = this.uxScreenPictureBox.Image;
            if (image != null)
            {
                if (e.KeyCode == Keys.P)
                {
                    this.sumacon.SaveCapturedImage(image as Bitmap);
                    this.uxScreenPictureBox.Visible = false;
                    Delay.SetTimeout(() => this.uxScreenPictureBox.Visible = true, 100, this);
                }
                else if (e.KeyCode == Keys.Left) this.screenPointedPosition.X = Math.Max(0, this.screenPointedPosition.X - 1);
                else if (e.KeyCode == Keys.Right) this.screenPointedPosition.X = Math.Min(this.screenPointedPosition.X + 1, image.Width - 1);
                else if (e.KeyCode == Keys.Up) this.screenPointedPosition.Y = Math.Max(0, this.screenPointedPosition.Y - 1);
                else if (e.KeyCode == Keys.Down) this.screenPointedPosition.Y = Math.Min(this.screenPointedPosition.Y + 1, image.Height - 1);
                else if (e.Control && !this.zoomEnabled) this.zoomEnabled = true;
                else return;
                this.UpdateControlState();
                e.Handled = true;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (!e.Control) this.zoomEnabled = false;
            else return;
            this.UpdateControlState();
            e.Handled = true;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            var index = this.zoomRatioIndex;
            if (e.Delta > 0) index++;
            if (e.Delta < 0) index--;
            this.zoomRatioIndex = Math.Max(0, Math.Min(index, this.kZoomRatios.Length - 1));
            this.UpdateControlState();
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
                if (!this.holdEnabled)
                {
                    device.Screen.CaptureAsync(bitmap => this.SafeInvoke(() =>
                    {
                        if (!this.holdEnabled)
                        {
                            this.uxScreenPictureBox.Image = bitmap;
                        }
                        Delay.SetTimeout(() => this.StartScreenPictureUpdate(), this.kUpdateScreenIntervalMilliseconds);
                    }));
                }
                else
                {
                    Delay.SetTimeout(() => this.StartScreenPictureUpdate(), this.kUpdateScreenIntervalMilliseconds);
                }
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

        void PickColor()
        {
            var bitmap = this.uxScreenPictureBox.Image as Bitmap;
            if (bitmap != null && new Rectangle(new Point(0, 0), bitmap.Size).Contains(this.screenPointedPosition))
            {
                var color = bitmap.GetPixel(this.screenPointedPosition.X, this.screenPointedPosition.Y);
                var sb = new StringBuilder();
                sb.Append($"{color.ToRgbString(true)} {color.ToHslString(true)} {color.ToHex6String()}");
                sb.Append($" at ({this.screenPointedPosition.X,4}px, {this.screenPointedPosition.Y,4}px)");
                this.sumacon.CommandReceiver.WriteOutput(sb.ToString());
            }
            if (this.beepEnabled) Beep.Play(Beep.Note.Pi);
        }

        bool GetScreenNormalizedPoint(Point point, out PointF normalizedPoint)
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

        void UpdateZoomBox()
        {
            this.uxZoomBox.Visible = false;

            if (!this.zoomEnabled) return;

            var mousePosition = this.uxScreenPictureBox.PointToClient(MousePosition);
            if (!this.GetScreenNormalizedPoint(mousePosition, out var dummy)) return;

            var upperLimit = this.uxZoomBox.Height + this.kZoomPanelRelocateMarginPixels;
            var lowerLimit = this.uxScreenPictureBox.Height - this.uxZoomBox.Height - this.kZoomPanelRelocateMarginPixels;
            this.uxZoomBox.Location =
                (mousePosition.Y <= upperLimit) ? new Point(0, this.uxScreenPictureBox.Height - this.uxZoomBox.Height) :
                (lowerLimit <= mousePosition.Y) ? new Point(0, 0) :
                this.uxZoomBox.Location;
            this.uxZoomBox.Width = this.uxScreenPictureBox.Width;
            this.uxZoomBox.Height = (int)(this.uxScreenPictureBox.Height * this.kZoomPanelHeightRatio);
            var zoomRatio = this.kZoomRatios[this.zoomRatioIndex];
            this.uxZoomBox.UpdateContent(this.uxScreenPictureBox.Image, this.uxScreenPictureBox.BackColor, this.screenPointedPosition, zoomRatio);
            this.uxZoomBox.Visible = true;
        }

        void UpdateControlState()
        {
            var bitmap = this.uxScreenPictureBox.Image as Bitmap;
            if (bitmap != null && new Rectangle(new Point(0, 0), bitmap.Size).Contains(this.screenPointedPosition))
            {
                this.uxTouchPositionLabel.Text = $"👆 {this.screenPointedPosition.X}, {this.screenPointedPosition.Y}";
                var color = bitmap.GetPixel(this.screenPointedPosition.X, this.screenPointedPosition.Y);
                this.uxColorLabel.Text = color.ToRgbString(true);
                this.uxColorLabel.BackColor = color;
                this.uxColorLabel.ForeColor = color.GetLuminance() >= 0.5f ? Color.Black : Color.White;
            }
            else
            {
                this.uxTouchPositionLabel.Text = "👆 -, -";
                this.uxColorLabel.BackColor = SystemColors.Control;
                this.uxColorLabel.Text = "-";
            }

            this.uxBeepButton.Text = this.beepEnabled ? "Beep ON" : "Beep OFF";
            this.uxHoldButton.Text = this.holdEnabled ? "Screen hold ON" : "Screen hold OFF";

            this.uxScreenPictureBox.BackColor = this.holdEnabled ? Color.OrangeRed : SystemColors.Control;

            this.UpdateZoomBox();
        }

        void LoadSettings()
        {
            this.beepEnabled = Properties.Settings.Default.ControlBeep;
            this.zoomRatioIndex = Properties.Settings.Default.ControlZoomRatioIndex;
            this.touchProtocolType = Properties.Settings.Default.ControlTouchProtocol;
        }

        void SaveSettings()
        {
            Properties.Settings.Default.ControlBeep = this.beepEnabled;
            Properties.Settings.Default.ControlZoomRatioIndex = this.zoomRatioIndex;
            Properties.Settings.Default.ControlTouchProtocol = this.touchProtocolType;
        }
    }

    class ZoomBox : PictureBox
    {
        Bitmap buffer = new Bitmap(1, 1);
        readonly Brush textBrush = new SolidBrush(Color.Black);
        readonly Brush textBackBrush = new SolidBrush(Color.FromArgb(128, Color.White));
        readonly Pen linePen = new Pen(Color.OrangeRed, 1.0f);
        readonly Font zoomFont = new Font(SystemFonts.MessageBoxFont.FontFamily, 16.0f);
        readonly Font noteFont = new Font(SystemFonts.MessageBoxFont.FontFamily, 12.0f);
        readonly int kNoteMargin = 5;

        public void UpdateContent(Image image, Color imageBackColor, Point lookPoint, float ratio)
        {
            if (this.buffer.Size != this.Size)
            {
                this.buffer = new Bitmap(this.buffer, this.Size);
            }

            var g = Graphics.FromImage(this.buffer);
            var dstRectange = new Rectangle(new Point(0, 0), this.buffer.Size);
            // 背景ぬり
            g.FillRectangle(new SolidBrush(imageBackColor), dstRectange);

            if (image == null) return;

            var zh = (float)Math.Floor(this.buffer.Height / ratio);
            // 中心を出すためかならず奇数に
            zh = (float)Math.Floor(zh / 2) * 2 + 1;
            var zw = this.buffer.Width / (this.buffer.Height / zh);
            var srcRectangle = new RectangleF(lookPoint.X - zw / 2.0f, lookPoint.Y - zh / 2.0f, zw, zh);

            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.DrawImage(image, dstRectange, srcRectangle, GraphicsUnit.Pixel);

            // 補助線
            var cw = this.buffer.Width / zw / 2;
            var ch = this.buffer.Height / zh / 2;
            var x1 = (dstRectange.Width / 2) - cw;
            var x2 = (dstRectange.Width / 2) + cw;
            var y1 = (dstRectange.Height / 2) - ch;
            var y2 = (dstRectange.Height / 2) + ch;
            g.DrawLine(this.linePen, x1, 0, x1, dstRectange.Height);
            g.DrawLine(this.linePen, x2, 0, x2, dstRectange.Height);
            g.DrawLine(this.linePen, 0, y1, dstRectange.Width, y1);
            g.DrawLine(this.linePen, 0, y2, dstRectange.Width, y2);

            // 倍率
            this.DrawString($"x{ratio}", g, this.zoomFont, this.textBrush, this.textBackBrush, new PointF(this.buffer.Size.Width, this.buffer.Height), ContentAlignment.BottomRight);

            // カーソル位置
            var cursorPoint = new PointF(x2 + this.kNoteMargin, y2 + this.kNoteMargin);
            this.DrawString($"X:{lookPoint.X} Y:{lookPoint.Y}", g, this.noteFont, this.textBrush, this.textBackBrush, cursorPoint, ContentAlignment.TopLeft);

            // 色情報
            var imageBitmap = image as Bitmap;
            if (imageBitmap != null && new Rectangle(new Point(0, 0), imageBitmap.Size).Contains(lookPoint))
            {
                var color = imageBitmap.GetPixel(lookPoint.X, lookPoint.Y);
                var h = color.GetHue();
                var s = color.GetSaturation() * 100;
                var v = color.GetLuminance() * 100;
                var colorText = $"{color.ToRgbString(true)} {color.ToHslString(true)} {color.ToHex6String()}";
                var colorPoint = new PointF(x2 + this.kNoteMargin, y1 - this.kNoteMargin);
                this.DrawString(colorText, g, this.noteFont, this.textBrush, this.textBackBrush, colorPoint, ContentAlignment.BottomLeft);
            }

            this.Image = this.buffer;
        }

        void DrawString(string s, Graphics g, Font font, Brush foreBrush, Brush backBrush, PointF point, ContentAlignment anchor)
        {
            var size = g.MeasureString(s, font);
            var leftTop = point;

            leftTop.Y -=
                (anchor == ContentAlignment.MiddleLeft || anchor == ContentAlignment.MiddleCenter || anchor == ContentAlignment.MiddleRight) ? size.Height / 2.0f :
                (anchor == ContentAlignment.BottomLeft || anchor == ContentAlignment.BottomCenter || anchor == ContentAlignment.BottomRight) ? size.Height :
                0;

            leftTop.X -=
                (anchor == ContentAlignment.TopCenter || anchor == ContentAlignment.MiddleCenter || anchor == ContentAlignment.BottomCenter) ? size.Width / 2.0f :
                (anchor == ContentAlignment.TopRight || anchor == ContentAlignment.MiddleRight || anchor == ContentAlignment.BottomRight) ? size.Width :
                0;

            var rectangle = new RectangleF(leftTop, size);
            if (backBrush != null)
            {
                g.FillRectangle(backBrush, rectangle);
            }
            g.DrawString(s, font, foreBrush, rectangle);
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
