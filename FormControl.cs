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
        ToolStripButton uxHoldButton = new ToolStripButton();
        ToolStripButton uxRecordingButton = new ToolStripButton();
        BindingDropDownButton<TouchProtocolType> uxTouchProtocolDropDown = new BindingDropDownButton<TouchProtocolType>();
        SplitContainer uxMainSplitContaier = new SplitContainer() { Dock = DockStyle.Fill };
        PictureBox uxScreenPictureBox = new PictureBox() { Dock = DockStyle.Fill };
        GridPanel uxActionsGridPanel = new GridPanel() { Dock = DockStyle.Fill };
        ContextMenuStrip uxScreenContextMenu = new ContextMenuStrip();
        ControlActionGroup actionGroup;
        bool beepEnabled { get => this.uxBeepButton.Checked; set => this.uxBeepButton.Checked = value; }
        bool zoomEnabled;
        bool holdEnabled { get => this.uxHoldButton.Checked; set => this.uxHoldButton.Checked = value; }
        bool recordingEnabled { get => this.uxRecordingButton.Checked; set => this.uxRecordingButton.Checked = value; }
        TouchProtocolType touchProtocolType { get => this.uxTouchProtocolDropDown.Value; set => this.uxTouchProtocolDropDown.Value = value; }
        int activeTouchNo = Input.InvalidTouchNo;
        Point screenPoint;
        List<Control> uxTouchMarkers = new List<Control>();
        ZoomBox uxZoomBox = new ZoomBox();
        int zoomRatioIndex;
        Point lastMousePosition;
        string delayedUpdateTimeoutId;
        DateTime lastMouseDownOrMoveAt = DateTime.MinValue;
        DateTime lastOutputControlLogAt = DateTime.MinValue;
        bool swiping;

        readonly int kActionsGridPanelWidth = 150;
        readonly int kUpdateScreenIntervalMilliseconds = 500;
        readonly float kZoomPanelHeightRatio = 0.4f;
        readonly int kZoomPanelRelocateMarginPixels = 10;
        readonly int[] kZoomRatios = { 1, 2, 5, 10, 20, 50, 100 };
        readonly int[] kZoomGridUnits = { 0, 5, 5, 5, 5, 5, 5 };
        readonly int[] kZoomGridAlphas = { 0, 16, 32, 64, 64, 64, 64 };
        readonly string[] kZoomNotes = { "E4", "A4", "E5", "A5", "E6", "A6", "E7" };
        readonly int kLogDurationUnitMilliseconds = 10;
        readonly int kLogTouchMoveThresholdMilliseconds = 100;
        readonly int kTouchMarkerCountMax = 10;

        public FormControl(Sumacon sumacon)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.sumacon = sumacon;
            this.sumacon.DeviceManager.ActiveDeviceChanged += this.DeviceManager_ActiveDeviceChanged;
            this.sumacon.DeviceManager.TouchProtocolTypeChanged += this.DeviceManager_TouchProtocolTypeChanged;
            this.sumacon.ShowTouchMarkersRequested += (touchPoints) => this.SafeInvoke(() => this.Sumacon_ShowTouchMarkersRequested(touchPoints));
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

            this.uxHoldButton.CheckOnClick = true;
            this.uxHoldButton.AutoToolTip = false;
            this.uxHoldButton.Image = this.imageList1.Images["lock.png"];
            this.uxHoldButton.CheckedChanged += (s, ee) =>
            {
                if (this.holdEnabled)
                {
                    Beep.Play(Beep.Note.Pi, Beep.Note.Pi);
                }
                else
                {
                    Beep.Play(Beep.Note.Pi, Beep.Note.Po);
                }
                this.UpdateControlState();
            };

            this.uxRecordingButton.CheckOnClick = true;
            this.uxRecordingButton.ToolTipText = "Recording your operations to console.";
            this.uxRecordingButton.Image = this.imageList1.Images["script_edit.png"];
            this.uxRecordingButton.CheckedChanged += (s, ee) =>
            {
                if (this.recordingEnabled)
                {
                    this.sumacon.WriteConsole("");
                    this.sumacon.WriteConsole("# Start recording");
                    Beep.Play(Beep.Note.Po, Beep.Note.Pi);
                }
                else
                {
                    this.sumacon.WriteConsole("# End recording");
                    Beep.Play(Beep.Note.Pi, Beep.Note.Po);
                    this.lastOutputControlLogAt = DateTime.MinValue;
                }
                this.UpdateControlState();
            };

            this.uxTouchProtocolDropDown.AutoToolTip = false;
            var protocols = new Dictionary<TouchProtocolType, ToolStripDropDownItem>();
            foreach(TouchProtocolType type in Enum.GetValues(typeof(TouchProtocolType)))
            {
                protocols.Add(type, new ToolStripMenuItem($"Touch protocol {type}"));
            }
            this.uxTouchProtocolDropDown.DataSource = protocols;
            this.uxTouchProtocolDropDown.DropDownItemClicked += (s, ee) => this.sumacon.DeviceManager.TouchProtocolType = this.touchProtocolType;

            this.uxColorLabel.Alignment = ToolStripItemAlignment.Right;
            this.uxColorLabel.AutoSize = false;
            this.uxColorLabel.Width = 100;
            this.uxColorLabel.TextAlign = ContentAlignment.MiddleLeft;

            this.uxTouchPositionLabel.Alignment = ToolStripItemAlignment.Right;
            this.uxTouchPositionLabel.AutoSize = false;
            this.uxTouchPositionLabel.Width = 70;
            this.uxTouchPositionLabel.TextAlign = ContentAlignment.MiddleLeft;

            this.uxScreenStatusStrip.Items.Add(this.uxBeepButton);
            this.uxScreenStatusStrip.Items.Add(this.uxTouchProtocolDropDown);
            this.uxScreenStatusStrip.Items.Add(this.uxHoldButton);
            this.uxScreenStatusStrip.Items.Add(this.uxRecordingButton);
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
            this.uxScreenPictureBox.MouseWheel += this.UxScreenPictureBox_MouseWheel;
            this.uxScreenPictureBox.MouseLeave += (s, ee) => this.UpdateControlState();
            this.uxScreenPictureBox.Controls.Add(this.uxZoomBox);

            this.uxScreenContextMenu.Items.Add("Pick color (Left click (on zoom enabled))", null, (s, ee) => this.PickColor(this.screenPoint));
            this.uxScreenContextMenu.Items.Add(new ToolStripSeparator());
            this.uxScreenContextMenu.Items.Add("Save screen capture (P)", null, (s, ee) => this.SaveCapturedImage());
            this.uxScreenContextMenu.Items.Add("Copy screen capture (Control + C)", null, (s, ee) => this.SaveCapturedImage());
            this.uxScreenContextMenu.Opening += (s, ee) => ee.Cancel = this.uxScreenPictureBox.Image == null;
            this.uxScreenPictureBox.ContextMenuStrip = this.uxScreenContextMenu;

            for (int i = 0; i < this.kTouchMarkerCountMax; i++)
            {
                var touchMarker = new Panel()
                {
                    Visible = false,
                    BackColor = Color.Lime,
                    Size = new Size(10, 10)
                };
                this.uxScreenPictureBox.Controls.Add(touchMarker);
                this.uxTouchMarkers.Add(touchMarker);
            }

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
            this.sumacon.ShowTouchMarkersRequested -= this.Sumacon_ShowTouchMarkersRequested;
            this.SaveSettings();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            if(this.Visible) this.StartScreenPictureUpdate();
        }

        void DeviceManager_ActiveDeviceChanged(object sender, Device previousDevice)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if(device != null)
            {
                this.SafeInvoke(() => this.touchProtocolType = device.Input.TouchProtocol);
            }
            this.StartScreenPictureUpdate();
        }

        void DeviceManager_TouchProtocolTypeChanged(object sender, TouchProtocolType e)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device != null)
            {
                this.SafeInvoke(() => this.touchProtocolType = device.Input.TouchProtocol);
            }
        }

        void Sumacon_ShowTouchMarkersRequested(Mobile.TouchPoint[] touchPoints)
        {
            for(int i = 0; i < this.kTouchMarkerCountMax; i++)
            {
                var touchPoint = touchPoints.FirstOrDefault(p => p.No == i);
                if (touchPoint != null)
                {
                    this.uxTouchMarkers[i].Location = this.NormalizedPointToPictureBoxPoint(touchPoint.Location);
                    this.uxTouchMarkers[i].Visible = true;
                }
                else
                {
                    this.uxTouchMarkers[i].Visible = false;
                }
            }
        }

        void UxScreenPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.TryPictureBoxPointToScreenPoint(e.Location, out var point))
            {
                this.screenPoint = point;

                if (e.Button.HasFlag(MouseButtons.Left))
                {
                    if (this.zoomEnabled)
                    {
                        this.PickColor(this.screenPoint);
                    }
                    else
                    {
                        var device = this.sumacon.DeviceManager.ActiveDevice;
                        if (device != null)
                        {
                            var rotatedSize = device.RotatedScreenSize;
                            var nx = (float)this.screenPoint.X / rotatedSize.Width;
                            var ny = (float)this.screenPoint.Y / rotatedSize.Height;
                            this.activeTouchNo = device.Input.OnTouch(nx, ny);
                            this.PlayBeepIfEnabled(Beep.Note.Po);
                            this.lastMouseDownOrMoveAt = DateTime.Now;
                            this.uxTouchMarkers[0].Visible = true;
                            this.uxTouchMarkers[0].Location = new Point(e.X - this.uxTouchMarkers[0].Width / 2, e.Y - this.uxTouchMarkers[0].Height / 2);
                        }
                    }
                }
            }

            this.UpdateControlState();
        }

        void UxScreenPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.lastMousePosition == e.Location) return;
            this.lastMousePosition = e.Location;

            var previousScreenPoint = this.screenPoint;
            if (this.TryPictureBoxPointToScreenPoint(e.Location, out var point))
            {
                this.screenPoint = point;

                if (e.Button.HasFlag(MouseButtons.Left))
                {
                    var device = this.sumacon.DeviceManager.ActiveDevice;
                    if (device != null && this.activeTouchNo != Input.InvalidTouchNo)
                    {
                        var rotatedSize = device.RotatedScreenSize;
                        var nx = (float)this.screenPoint.X / rotatedSize.Width;
                        var ny = (float)this.screenPoint.Y / rotatedSize.Height;
                        device.Input.MoveTouch(this.activeTouchNo, nx, ny);
                        this.uxTouchMarkers[0].Location = new Point(e.X - this.uxTouchMarkers[0].Width / 2, e.Y - this.uxTouchMarkers[0].Height / 2);
                        if (!this.swiping)
                        {
                            this.swiping = true;
                            var pnx = (float)previousScreenPoint.X / rotatedSize.Width;
                            var pny = (float)previousScreenPoint.Y / rotatedSize.Height;
                            this.OutputControlLogIfEnabled($"touch_on({this.activeTouchNo}, {pnx:F4}, {pny:F4})");
                        }

                        var elapsedMilliseconds = this.GetTruncatedElapseMilliseconds(this.lastMouseDownOrMoveAt, DateTime.Now, this.kLogDurationUnitMilliseconds);
                        if (elapsedMilliseconds >= this.kLogTouchMoveThresholdMilliseconds)
                        {
                            this.OutputControlLogIfEnabled($"touch_move({this.activeTouchNo}, {nx:F4}, {ny:F4}, {elapsedMilliseconds})", false);
                            this.lastMouseDownOrMoveAt = DateTime.Now;
                        }
                    }
                }
            }

            // ここいっぱい呼ばれるからちょびっと遅延させて負荷抑制
            this.delayedUpdateTimeoutId = Delay.SetTimeout(() => this.UpdateControlState(), 1, this, this.delayedUpdateTimeoutId, true);
        }

        void UxScreenPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            this.uxTouchMarkers[0].Visible = false;

            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device != null && this.activeTouchNo != Input.InvalidTouchNo)
            {
                device.Input.OffTouch();
                this.PlayBeepIfEnabled(Beep.Note.Pe);
                if (this.swiping)
                {
                    this.OutputControlLogIfEnabled($"touch_off({this.activeTouchNo})", false);
                }
                else
                {
                    var rotatedSize = device.RotatedScreenSize;
                    var nx = (float)this.screenPoint.X / rotatedSize.Width;
                    var ny = (float)this.screenPoint.Y / rotatedSize.Height;
                    var elapsedMilliseconds = this.GetTruncatedElapseMilliseconds(this.lastMouseDownOrMoveAt, DateTime.Now, this.kLogDurationUnitMilliseconds);
                    elapsedMilliseconds = Math.Max(elapsedMilliseconds, this.kLogDurationUnitMilliseconds);
                    this.OutputControlLogIfEnabled($"tap({nx:F4}, {ny:F4}, {elapsedMilliseconds})");
                }
                this.activeTouchNo = Input.InvalidTouchNo;
            }

            this.lastMouseDownOrMoveAt = DateTime.MinValue;
            this.swiping = false;
            this.UpdateControlState();
        }

        void UxScreenPictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            var imageRect = this.GetImageRectInPictureBox();
            if (!imageRect.Contains(e.Location)) return;

            var nextIndex =
                (e.Delta > 0) ? this.zoomRatioIndex + 1 :
                (e.Delta < 0) ? this.zoomRatioIndex - 1 :
                this.zoomRatioIndex;
            nextIndex = Math.Max(0, Math.Min(nextIndex, this.kZoomRatios.Length - 1));
            if (nextIndex != this.zoomRatioIndex) this.PlayBeepIfEnabled(this.kZoomNotes[nextIndex]);
            this.zoomRatioIndex = nextIndex;
            this.zoomEnabled = (this.zoomRatioIndex > 0);
            this.UpdateControlState();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            var image = this.uxScreenPictureBox.Image;
            if (image != null)
            {
                if (e.KeyCode == Keys.P) this.SaveCapturedImage();
                else if (e.KeyCode == Keys.C && ModifierKeys.HasFlag(Keys.Control)) this.CopyCapturedImage();
                else if (e.KeyCode == Keys.Left) this.screenPoint.X = Math.Max(0, this.screenPoint.X - 1);
                else if (e.KeyCode == Keys.Right) this.screenPoint.X = Math.Min(this.screenPoint.X + 1, image.Width - 1);
                else if (e.KeyCode == Keys.Up) this.screenPoint.Y = Math.Max(0, this.screenPoint.Y - 1);
                else if (e.KeyCode == Keys.Down) this.screenPoint.Y = Math.Min(this.screenPoint.Y + 1, image.Height - 1);
                else return;
                this.UpdateControlState();
                e.Handled = true;
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
                if (!this.holdEnabled)
                {
                    device.Screen.CaptureAsync(bitmap => this.SafeInvoke(() =>
                    {
                        if (!this.holdEnabled && bitmap != null)
                        {
                            this.uxScreenPictureBox.Image?.Dispose();
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

            this.PlayBeepIfEnabled(Beep.Note.Po, Beep.Note.Pe);

            if (!string.IsNullOrEmpty(action.Command))
            {
                device.RunCommandAsync(action.Command);
                this.OutputControlLogIfEnabled($"adb('{action.Command}')");
            }
            if(!string.IsNullOrEmpty(action.Proc))
            {
                this.ExecuteProc(device, action.Proc);
            }
        }

        void ExecuteProc(Device device, string procName)
        {
            if (!Enum.TryParse<ProcAction>(procName, out var proc)) return;

            if (proc == ProcAction.RotateScreenCw) this.ExecuteProcRotateScreen(device, 90);
            else if (proc == ProcAction.RotateScreenCcw) this.ExecuteProcRotateScreen(device, -90);
        }

        void ExecuteProcRotateScreen(Device device, int degrees)
        {
            device.Screen.Rotate(degrees);
            this.OutputControlLogIfEnabled($"rotate_to({(int)device.Screen.CurrentRotation * 90})");
        }

        ControlAction GetSelectedAction()
        {
            return (this.uxActionsGridPanel.SelectedRows.Count > 0) ?
                this.actionGroup.Actions[this.uxActionsGridPanel.SelectedRows[0].Index] : null;
        }

        void PickColor(Point screenPoint)
        {
            var bitmap = this.uxScreenPictureBox.Image as Bitmap;
            if (bitmap != null && new Rectangle(new Point(0, 0), bitmap.Size).Contains(screenPoint))
            {
                var color = bitmap.GetPixel(screenPoint.X, screenPoint.Y);
                var sb = new StringBuilder();
                sb.Append($"# {color.ToRgbString(true)} {color.ToHslString(true)} {color.ToHex6String()}");
                sb.Append($" at ({screenPoint.X,4}px, {screenPoint.Y,4}px)");
                this.sumacon.WriteConsole(sb.ToString());
                this.PlayBeepIfEnabled(Beep.Note.Pi);
            }
        }

        void SaveCapturedImage()
        {
            if (this.recordingEnabled)
            {
                this.OutputControlLogIfEnabled("save_capture()");
            }
            else
            {
                var path = this.sumacon.SaveCapturedImage(this.uxScreenPictureBox.Image as Bitmap);
                this.sumacon.WriteConsole($"# Save screen capture to {path}.");
            }
            this.uxScreenPictureBox.Visible = false;
            Delay.SetTimeout(() => this.uxScreenPictureBox.Visible = true, 100, this);
            this.PlayBeepIfEnabled(Beep.Note.Po, Beep.Note.Pe);
        }

        void CopyCapturedImage()
        {
            if (this.uxScreenPictureBox.Image == null) return;
            Clipboard.SetImage(this.uxScreenPictureBox.Image);
            this.uxScreenPictureBox.Visible = false;
            Delay.SetTimeout(() => this.uxScreenPictureBox.Visible = true, 100, this);
            this.PlayBeepIfEnabled(Beep.Note.Po, Beep.Note.Pe);
            this.sumacon.WriteConsole("# Copy screen capture to clipboard.");
        }

        bool TryPictureBoxPointToScreenPoint(Point pictureBoxPoint, out Point screenPoint)
        {
            screenPoint = Point.Empty;
            var imageRect = this.GetImageRectInPictureBox();
            if (!imageRect.IsEmpty && imageRect.Contains(pictureBoxPoint))
            {
                var x = (int)Math.Round((double)(pictureBoxPoint.X - imageRect.X) / imageRect.Width * this.uxScreenPictureBox.Image.Width);
                var y = (int)Math.Round((double)(pictureBoxPoint.Y - imageRect.Y) / imageRect.Height * this.uxScreenPictureBox.Image.Height);
                screenPoint = new Point(x, y);
                return true;
            }
            return false;
        }

        //PointF ScreenPointToNormalizedPoint(Point screenPoint)
        //{
        //    var imageRect = this.GetImageRectInPictureBox();
        //    return new PointF(
        //        (float)Math.Round((double)screenPoint.X / imageRect.Width),
        //        (float)Math.Round((double)screenPoint.Y / imageRect.Height));
        //}

        //bool TryPictureBoxPointToNormalizedPoint(Point point, out PointF normalizedPoint)
        //{
        //    // PictureBox相対座標からディスプレイ正規化座標へ
        //    normalizedPoint = PointF.Empty;
        //    var imageRect = this.GetImageRectInPictureBox();
        //    if (!imageRect.IsEmpty && imageRect.Contains(point))
        //    {
        //        normalizedPoint = new PointF((float)(point.X - imageRect.X) / imageRect.Width, (float)(point.Y - imageRect.Y) / imageRect.Height);
        //        return true;
        //    }
        //    return false;
        //}

        Point NormalizedPointToPictureBoxPoint(PointF normalizedPoint)
        {
            var imageRect = this.GetImageRectInPictureBox();
            return new Point(
                (int)(normalizedPoint.X * imageRect.Width + imageRect.X),
                (int)(normalizedPoint.Y * imageRect.Height + imageRect.Y));
        }

        Rectangle GetImageRectInPictureBox()
        {
            var image = this.uxScreenPictureBox.Image;
            if (image == null) return Rectangle.Empty;
            var imageRatio = (float)image.Width / image.Height;
            var boxRect = this.uxScreenPictureBox.ClientRectangle;
            var boxRatio = (float)boxRect.Width / boxRect.Height;
            var imageRect = boxRect;
            if (imageRatio > boxRatio) // 絵の方が横長
            {
                imageRect.Height = (int)(boxRect.Width / imageRatio);
                imageRect.Y = (boxRect.Height - imageRect.Height) / 2;
            }
            else
            {
                imageRect.Width = (int)(boxRect.Height * imageRatio);
                imageRect.X = (boxRect.Width - imageRect.Width) / 2;
            }
            return imageRect;
        }

        void UpdateZoomBox()
        {
            this.uxZoomBox.Visible = false;

            if (!this.zoomEnabled) return;
            if (this.uxScreenPictureBox.Image == null) return;

            var mousePosition = this.uxScreenPictureBox.PointToClient(Control.MousePosition);
            if (!this.GetImageRectInPictureBox().Contains(mousePosition)) return;
            //if (!new Rectangle(new Point(0, 0), this.uxScreenPictureBox.Image.Size).Contains(this.screenPoint)) return;
            //if (!this.TryPictureBoxPointToScreenPoint(mousePosition, out var dummy)) return;

            var upperLimit = this.uxZoomBox.Height + this.kZoomPanelRelocateMarginPixels;
            var lowerLimit = this.uxScreenPictureBox.Height - this.uxZoomBox.Height - this.kZoomPanelRelocateMarginPixels;
            this.uxZoomBox.Location =
                (mousePosition.Y <= upperLimit) ? new Point(0, this.uxScreenPictureBox.Height - this.uxZoomBox.Height) :
                (lowerLimit <= mousePosition.Y) ? new Point(0, 0) :
                this.uxZoomBox.Location;
            this.uxZoomBox.Width = this.uxScreenPictureBox.Width;
            this.uxZoomBox.Height = (int)(this.uxScreenPictureBox.Height * this.kZoomPanelHeightRatio);
            var zoomRatio = this.kZoomRatios[this.zoomRatioIndex];
            var gridUnit = this.kZoomGridUnits[this.zoomRatioIndex];
            var gridAlpha = this.kZoomGridAlphas[this.zoomRatioIndex];
            this.uxZoomBox.UpdateContent(this.uxScreenPictureBox.Image, this.uxScreenPictureBox.BackColor, this.screenPoint, zoomRatio, gridUnit, gridAlpha);
            this.uxZoomBox.Visible = true;
        }

        void PlayBeepIfEnabled(params Beep.Note[] notes)
        {
            if (this.beepEnabled) Beep.Play(notes);
        }

        void PlayBeepIfEnabled(params string[] notes)
        {
            if (this.beepEnabled) Beep.Play(notes);
        }

        void OutputControlLogIfEnabled(string s, bool insertWait = true)
        {
            if (!this.recordingEnabled) return;

            var now = DateTime.Now;
            if (insertWait && this.lastOutputControlLogAt != DateTime.MinValue)
            {
                var elapsed = this.GetTruncatedElapseMilliseconds(this.lastOutputControlLogAt, now, this.kLogDurationUnitMilliseconds);
                this.sumacon.WriteConsole($"wait({elapsed})");
            }
            this.sumacon.WriteConsole(s);
            this.lastOutputControlLogAt = now;
        }

        int GetTruncatedElapseMilliseconds(DateTime from, DateTime to, int multiply = 1)
        {
            return (int)Math.Truncate((to - from).TotalMilliseconds / multiply) * multiply;
        }

        void UpdateControlState()
        {
            var bitmap = this.uxScreenPictureBox.Image as Bitmap;
            if (bitmap != null && new Rectangle(new Point(0, 0), bitmap.Size).Contains(this.screenPoint))
            {
                this.uxTouchPositionLabel.Text = $"👆 {this.screenPoint.X}, {this.screenPoint.Y}";
                var color = bitmap.GetPixel(this.screenPoint.X, this.screenPoint.Y);
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
            this.uxRecordingButton.Text = this.recordingEnabled ? "Recording ON" : "Recording OFF";

            if (this.recordingEnabled)
            {
                this.uxScreenPictureBox.BackColor = Color.FromArgb(128, Color.OrangeRed);
                this.uxRecordingButton.ForeColor = Color.OrangeRed;
            }
            else
            {
                this.uxScreenPictureBox.BackColor = this.holdEnabled ? SystemColors.ControlDark : SystemColors.Control;
                this.uxRecordingButton.ForeColor = SystemColors.ControlText;
            }

            this.UpdateZoomBox();
        }

        void LoadSettings()
        {
            this.beepEnabled = Properties.Settings.Default.ControlBeep;
        }

        void SaveSettings()
        {
            Properties.Settings.Default.ControlBeep = this.beepEnabled;
        }
    }

    class ZoomBox : PictureBox
    {
        Bitmap buffer = new Bitmap(1, 1);
        readonly Brush textBrush = new SolidBrush(Color.Black);
        readonly Brush textBackBrush = new SolidBrush(Color.FromArgb(128, Color.White));
        readonly Pen mainLinePen = new Pen(Color.OrangeRed, 1.0f);
        readonly Pen subLinePen = new Pen(Color.OrangeRed, 1.0f);
        readonly Font zoomFont = new Font(SystemFonts.MessageBoxFont.FontFamily, 16.0f);
        readonly Font noteFont = new Font(SystemFonts.MessageBoxFont.FontFamily, 12.0f);
        readonly int kNoteMargin = 5;

        public void UpdateContent(Image image, Color imageBackColor, Point lookPoint, float ratio, int gridUnit, int gridAlpha)
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
            var cw = this.buffer.Width / zw;
            var ch = this.buffer.Height / zh;
            var x1 = (dstRectange.Width / 2) - (cw / 2);
            var x2 = (dstRectange.Width / 2) + (cw / 2);
            var y1 = (dstRectange.Height / 2) - (ch / 2);
            var y2 = (dstRectange.Height / 2) + (ch / 2);
            this.subLinePen.Color = Color.FromArgb(gridAlpha, this.subLinePen.Color);
            for (int ox = 0; ox < ((int)(this.buffer.Width - cw) / 2); ox += (int)(cw * gridUnit))
            {
                var pen = (ox == 0) ? this.mainLinePen : this.subLinePen;
                g.DrawLine(pen, x1 - ox, 0, x1 - ox, dstRectange.Height);
                g.DrawLine(pen, x2 + ox, 0, x2 + ox, dstRectange.Height);
            }
            for (int oy = 0; oy < ((int)(this.buffer.Height - ch) / 2); oy += (int)(ch * gridUnit))
            {
                var pen = (oy == 0) ? this.mainLinePen : this.subLinePen;
                g.DrawLine(pen, 0, y1 - oy, dstRectange.Width, y1 - oy);
                g.DrawLine(pen, 0, y2 + oy, dstRectange.Width, y2 + oy);
            }

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

            var rectangle = new RectangleF(leftTop.X, leftTop.Y, (float)Math.Ceiling(size.Width), (float)Math.Ceiling(size.Height));
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
