using Sgry.Azuki.WinForms;
using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Azuki = Sgry.Azuki;
using Memezo = Suconbu.Scripting.Memezo;

namespace Suconbu.Sumacon
{
    public partial class FormScript : FormBase
    {
        enum RunState { Ready, Running, Paused }

        readonly SplitContainer uxSplitContainer = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        readonly ToolStrip uxToolStrip = new ToolStrip() { GripStyle = ToolStripGripStyle.Hidden };
        readonly AzukiControl uxScriptTextBox = new AzukiControl() { Dock = DockStyle.Fill };
        int markStartIndex;
        int markEndIndex;
        readonly Azuki.Highlighter.KeywordHighlighter highlighter = new Azuki.Highlighter.KeywordHighlighter();
        readonly GridPanel uxWatchPanel = new GridPanel() { Dock = DockStyle.Fill };
        readonly Sumacon sumacon;
        readonly ToolStripButton uxStopButton;
        readonly ToolStripButton uxRunButton;
        readonly ToolStripButton uxPauseButton;
        readonly ToolStripButton uxStepButton;
        Memezo.Interpreter interpreter = new Memezo.Interpreter();
        int currentSourceIndex;
        RunState runState = RunState.Ready;
        string stepTimeoutKey;
        string updateControlStateTimeoutKey;
        int defaultStepIntervalMilliseconds;
        int activeStepIntervalMilliseconds;
        SortableBindingList<VarEntry> watchedVars = new SortableBindingList<VarEntry>();
        readonly int kCurrentLineMarkId = 1;

        public FormScript(Sumacon sumacon)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.uxStopButton = this.uxToolStrip.Items.Add("Stop", null, (s, ee) => this.OnStop()) as ToolStripButton;
            this.uxStopButton.Image = this.imageList1.Images["control_stop_blue.png"];
            this.uxRunButton = this.uxToolStrip.Items.Add("Run", null, (s, ee) => this.OnRun()) as ToolStripButton;
            this.uxRunButton.Image = this.imageList1.Images["control_play_blue.png"];
            this.uxPauseButton = this.uxToolStrip.Items.Add("Pause", null, (s, ee) => this.OnPause()) as ToolStripButton;
            this.uxPauseButton.Image = this.imageList1.Images["control_pause_blue.png"];
            this.uxStepButton = this.uxToolStrip.Items.Add("Step", null, (s, ee) => this.OnStep()) as ToolStripButton;
            this.uxStepButton.Image = this.imageList1.Images["control_step_blue.png"];

            this.sumacon = sumacon;
        }

        protected override void OnLoad(EventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnLoad(e);

            this.uxScriptTextBox.Font = new Font(Properties.Resources.MonospaceFontName, this.uxScriptTextBox.Font.Size);
            this.uxScriptTextBox.ColorScheme.BackColor = this.sumacon.ColorSet.Back;
            this.uxScriptTextBox.ColorScheme.ForeColor = this.sumacon.ColorSet.Text;
            this.uxScriptTextBox.ColorScheme.LineNumberBack = this.sumacon.ColorSet.SelectionBack;
            this.uxScriptTextBox.ColorScheme.LineNumberFore = this.sumacon.ColorSet.GrayedText;
            this.uxScriptTextBox.ColorScheme.SelectionBack = this.sumacon.ColorSet.SelectionBack;
            this.uxScriptTextBox.ColorScheme.SelectionFore =
                this.uxScriptTextBox.ColorScheme.SelectionBack.GetLuminance() >= 0.5f ? Color.Black : Color.White;
            this.uxScriptTextBox.ColorScheme.CleanedLineBar = Color.Transparent;
            this.uxScriptTextBox.ColorScheme.DirtyLineBar = this.sumacon.ColorSet.Accent2;
            this.uxScriptTextBox.ColorScheme.MatchedBracketBack = this.sumacon.ColorSet.SelectionBack;
            this.uxScriptTextBox.ColorScheme.MatchedBracketFore = this.sumacon.ColorSet.SelectionText;
            this.uxScriptTextBox.ColorScheme.RightEdgeColor = Color.Transparent;
            this.uxScriptTextBox.ColorScheme.HighlightColor = this.sumacon.ColorSet.GridLine;
            this.uxScriptTextBox.ColorScheme.WhiteSpaceColor = this.sumacon.ColorSet.GrayedText;
            this.uxScriptTextBox.ColorScheme.EolColor = this.sumacon.ColorSet.GrayedText;
            this.uxScriptTextBox.ColorScheme.EofColor = this.sumacon.ColorSet.GrayedText;
            this.uxScriptTextBox.ColorScheme.SetColor(Azuki.CharClass.Comment, this.sumacon.ColorSet.GrayedText, Color.Transparent);
            Azuki.Marking.Register(new Azuki.MarkingInfo(this.kCurrentLineMarkId, "CurrentLine"));
            this.uxScriptTextBox.ColorScheme.SetMarkingDecoration(this.kCurrentLineMarkId, new Azuki.BgColorTextDecoration(this.uxScriptTextBox.ColorScheme.SelectionBack));
            this.uxScriptTextBox.TabWidth = 4;
            this.uxScriptTextBox.LeftMargin = 4;
            this.uxScriptTextBox.DrawsSpace = true;
            this.uxScriptTextBox.DrawsFullWidthSpace = true;
            this.uxScriptTextBox.DrawsEolCode = false;
            this.uxScriptTextBox.DrawsEofMark = false;
            this.uxScriptTextBox.Document.Highlighter = this.highlighter;

            this.uxWatchPanel.ApplyColorSet(this.sumacon.ColorSet);
            this.uxWatchPanel.DataSource = this.watchedVars;
            this.uxWatchPanel.KeyColumnName = nameof(VarEntry.Name);

            this.uxSplitContainer.Panel1.Controls.Add(this.uxScriptTextBox);
            this.uxSplitContainer.Panel2.Controls.Add(this.uxWatchPanel);
            this.uxSplitContainer.SplitterDistance = this.uxSplitContainer.Height * 70 / 100;

            this.Controls.Add(this.uxSplitContainer);
            this.Controls.Add(this.uxToolStrip);

            this.uxWatchPanel.Columns[nameof(VarEntry.Name)].Width = 150;
            this.uxWatchPanel.Columns[nameof(VarEntry.Value)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.uxWatchPanel.CellMouseDoubleClick += this.UxWatchPanel_CellMouseDoubleClick;

            this.SetupInterpreter();

            this.sumacon.DeviceManager.ActiveDeviceChanged += this.DeviceManager_ActiveDeviceChanged;
            this.sumacon.DeviceManager.PropertyChanged += this.DeviceManager_PropertyChanged;

            this.LoadSettings();

            this.UpdateControlState();
        }

        private void UxWatchPanel_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if(e.RowIndex < this.watchedVars.Count)
            {
                this.uxScriptTextBox.GetSelection(out var start, out var end);
                var index = Math.Min(start, end);
                var length = Math.Abs(end - start);
                this.uxScriptTextBox.Text = this.uxScriptTextBox.Text
                    .Remove(index, length)
                    .Insert(index, this.watchedVars[e.RowIndex].Name);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            this.sumacon.DeviceManager.ActiveDeviceChanged -= this.DeviceManager_ActiveDeviceChanged;
            this.sumacon.DeviceManager.PropertyChanged -= this.DeviceManager_PropertyChanged;
            this.SaveSettings();
        }

        void DeviceManager_ActiveDeviceChanged(object sender, Mobile.Device e)
        {
            this.SafeInvoke(() =>
            {
                if (this.sumacon.DeviceManager.ActiveDevice == null)
                {
                    this.OnStop();
                }
                this.UpdateControlState();
            });
        }

        void DeviceManager_PropertyChanged(object sender, IReadOnlyList<Mobile.Property> properties)
        {
            this.updateControlStateTimeoutKey = Delay.SetTimeout(
                () => this.SafeInvoke(this.UpdateControlState), 100, this, this.updateControlStateTimeoutKey, true);
        }

        void OnStop()
        {
            this.runState = RunState.Ready;
            this.uxScriptTextBox.Document.Unmark(this.markStartIndex, this.markEndIndex, this.kCurrentLineMarkId);
            this.markStartIndex = 0;
            this.markEndIndex = 0;
            this.UpdateControlState();
        }

        void OnRun()
        {
            if (this.runState == RunState.Ready) this.PrepareToRun();

            this.RunAuto();

            this.runState = RunState.Running;
            this.UpdateControlState();
        }

        void OnPause()
        {
            Delay.ClearTimeout(this.stepTimeoutKey);

            this.runState = RunState.Paused;
            this.UpdateControlState();
        }

        void OnStep()
        {
            if (this.runState == RunState.Ready)
            {
                this.PrepareToRun();
                this.runState = RunState.Paused;
            }
            else
            {
                if (this.RunOneStep())
                {
                    this.runState = RunState.Paused;
                }
                else
                {
                    this.OnStop();
                }
            }
            this.UpdateControlState();
        }

        void RunAuto()
        {
            this.stepTimeoutKey = Delay.SetTimeout(() =>
            {
                if (this.runState == RunState.Running)
                {
                    if (this.RunOneStep() && this.sumacon.DeviceManager.ActiveDevice != null)
                    {
                        this.RunAuto();
                    }
                    else
                    {
                        this.OnStop();
                    }
                }
            }, this.activeStepIntervalMilliseconds, this, this.stepTimeoutKey, true);
        }

        bool RunOneStep()
        {
            var result = this.interpreter.Step(out var nextIndex);
            if (result && nextIndex >= 0)
            {
                this.PullSpecialVars();
                this.UpdateWatchedVars();
                this.interpreter.ForwardToNextStatement(out this.currentSourceIndex);
                this.UpdateScriptSelection(this.currentSourceIndex);
                return true;
            }
            return false;
        }

        void SetupInterpreter()
        {
            this.interpreter.Install(new Memezo.StandardLibrary(), new Memezo.RandomLibrary());
            this.interpreter.ErrorOccurred += this.Interpreter_ErrorOccurred;
            this.interpreter.Functions["print"] = this.Interpreter_Print;
            this.interpreter.Functions["wait"] = this.Interpreter_Wait;
            this.interpreter.Functions["beep"] = this.Interpreter_Beep;
            this.interpreter.Functions["tap"] = this.Interpreter_Tap;
            this.interpreter.Functions["touch_on"] = this.Interpreter_TouchOn;
            this.interpreter.Functions["touch_move"] = this.Interpreter_TouchMove;
            this.interpreter.Functions["touch_off"] = this.Interpreter_TouchOff;

            var keywords = Memezo.Interpreter.Keywords;
            this.highlighter.AddKeywordSet(keywords.OrderBy(s => s).ToArray(), Azuki.CharClass.Keyword);

            var functionNames = this.interpreter.Functions.Keys;
            this.highlighter.AddKeywordSet(functionNames.OrderBy(s => s).ToArray(), Azuki.CharClass.Function);

            foreach (var quoteMarker in Memezo.Interpreter.StringQuoteMarkers)
            {
                this.highlighter.AddEnclosure(quoteMarker.ToString(), quoteMarker.ToString(), Azuki.CharClass.String, false, '\\');
            }

            this.highlighter.AddLineHighlight(Memezo.Interpreter.LineCommentMarker, Azuki.CharClass.Comment);
        }

        void PrepareToRun()
        {
            this.interpreter.Source = this.uxScriptTextBox.Text;
            this.interpreter.ForwardToNextStatement(out this.currentSourceIndex);
            this.UpdateScriptSelection(this.currentSourceIndex);

            this.PushSpecialVars();
            this.UpdateWatchedVars();
        }

        void Interpreter_ErrorOccurred(object sender, Memezo.ErrorInfo errorInfo)
        {
            this.sumacon.WriteConsole($"ERROR: {errorInfo}");
        }

        Memezo.Value Interpreter_Print(List<Memezo.Value> args)
        {
            this.sumacon.WriteConsole(string.Join(" ", args.Select(arg => arg.String)));
            return Memezo.Value.Zero;
        }

        Memezo.Value Interpreter_Wait(List<Memezo.Value> args)
        {
            if (args.Count < 1) throw new ArgumentException("Too few arguments");

            var duration = (int)Math.Max(1.0, args[0].Number);
            this.sumacon.WriteConsole($"wait({duration})");
            Task.Delay(duration).Wait();
            return Memezo.Value.Zero;
        }

        Memezo.Value Interpreter_Beep(List<Memezo.Value> args)
        {
            var frequency = (args.Count > 0) ? (int)args[0].Number : 1000;
            var duration = (args.Count > 1) ? (int)args[1].Number : 100;
            this.sumacon.WriteConsole($"beep({frequency}, {duration})");
            Console.Beep(frequency, duration);
            return Memezo.Value.Zero;
        }

        Memezo.Value Interpreter_Tap(List<Memezo.Value> args)
        {
            if (args.Count < 2) throw new ArgumentException("Too few arguments");

            var x = (args[0].Type == Memezo.DataType.Number) ? (float)args[0].Number : throw new ArgumentException("Argument type mismatch", "x");
            var y = (args[1].Type == Memezo.DataType.Number) ? (float)args[1].Number : throw new ArgumentException("Argument type mismatch", "y");
            var duration = 100;
            if (args.Count > 2)
            {
                duration = (args[2].Type == Memezo.DataType.Number) ? (int)args[2].Number : throw new ArgumentException("Argument type mismatch", "duration");
            }

            this.Tap(x, y, duration);

            return Memezo.Value.Zero;
        }

        // touch_on(no, x, y)
        Memezo.Value Interpreter_TouchOn(List<Memezo.Value> args)
        {
            if (args.Count < 3) throw new ArgumentException("Too few arguments");

            var no = (args[0].Type == Memezo.DataType.Number) ? (int)args[0].Number : throw new ArgumentException("Argument type mismatch", "no");
            var x = (args[1].Type == Memezo.DataType.Number) ? (float)args[1].Number : throw new ArgumentException("Argument type mismatch", "x");
            var y = (args[2].Type == Memezo.DataType.Number) ? (float)args[2].Number : throw new ArgumentException("Argument type mismatch", "y");

            this.TouchOn(no, x, y);

            return Memezo.Value.Zero;
        }

        // touch_move(no, x, y, duration)
        Memezo.Value Interpreter_TouchMove(List<Memezo.Value> args)
        {
            if (args.Count < 3) throw new ArgumentException("Too few arguments");

            var no = (args[0].Type == Memezo.DataType.Number) ? (int)args[0].Number : throw new ArgumentException("Argument type mismatch", "no");
            var x = (args[1].Type == Memezo.DataType.Number) ? (float)args[1].Number : throw new ArgumentException("Argument type mismatch", "x");
            var y = (args[2].Type == Memezo.DataType.Number) ? (float)args[2].Number : throw new ArgumentException("Argument type mismatch", "y");
            var duration = 0;
            if (args.Count > 3)
            {
                duration = (args[3].Type == Memezo.DataType.Number) ? (int)args[3].Number : throw new ArgumentException("Argument type mismatch", "duration");
            }

            this.TouchMove(no, x, y, duration);

            return Memezo.Value.Zero;
        }

        // touch_off(no)
        Memezo.Value Interpreter_TouchOff(List<Memezo.Value> args)
        {
            var no = Input.InvalidTouchNo;
            if (args.Count > 0)
            {
                no = (args[0].Type == Memezo.DataType.Number) ? (int)args[0].Number : throw new ArgumentException("Argument type mismatch", "no");
            }

            this.TouchOff(no);

            return Memezo.Value.Zero;
        }

        void Tap(float x, float y, int duration)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device == null) throw new InvalidOperationException("Device not available");

            //var rotatedSize = device.RotatedScreenSize;
            //x = (int)Math.Max(0.0, Math.Min(x, rotatedSize.Width));
            //y = (int)Math.Max(0.0, Math.Min(y, rotatedSize.Height));

            //this.sumacon.WriteConsole($"tap({x}, {y}, {duration})");
            //this.sumacon.ShowTouchMarkers(new PointF(x, y));
            //this.sumacon.ShowTouchMarkers(new PointF((float)x / rotatedSize.Width, (float)y / rotatedSize.Height));

            var no = device.Input.Tap(x, y, duration);
            //device.Input.Tap((float)x / rotatedSize.Width, (float)y / rotatedSize.Height, duration);

            this.UpdateTouchMarkers(device);

            Task.Delay(duration).Wait();
            while (device.Input.TouchPoints.ContainsKey(no)) Task.Delay(10).Wait();

            this.UpdateTouchMarkers(device);
        }

        void TouchOn(int no, float x, float y)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device == null) throw new InvalidOperationException("Device not available");

            device.Input.OnTouch(no, x, y);

            this.UpdateTouchMarkers(device);
        }

        void TouchMove(int no, float x, float y, int duration)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device == null) throw new InvalidOperationException("Device not available");

            var previousPoint = device.Input.TouchPoints[no].Location;
            var step = 10;
            var remain = duration;
            var startedAt = DateTime.Now;
            for (int i = step; i < duration; i += step)
            {
                while((DateTime.Now - startedAt).TotalMilliseconds < i) Task.Delay(10).Wait();

                remain -= step;
                var ix = previousPoint.X + (x - previousPoint.X) * i / duration;
                var iy = previousPoint.Y + (y - previousPoint.Y) * i / duration;
                device.Input.MoveTouch(no, ix, iy);
                this.UpdateTouchMarkers(device);
            }
            while ((DateTime.Now - startedAt).TotalMilliseconds < duration) Task.Delay(10).Wait();
            device.Input.MoveTouch(no, x, y);
            this.UpdateTouchMarkers(device);
        }

        void TouchOff(int no)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device == null) throw new InvalidOperationException("Device not available");

            device.Input.OffTouch(no);

            this.UpdateTouchMarkers(device);
        }

        void UpdateTouchMarkers(Device device)
        {
            var points = device.Input.TouchPoints.Select(p => p.Value.Location).ToArray();
            this.sumacon.ShowTouchMarkers(points);
        }

        void PushSpecialVars()
        {
            this.interpreter.Vars["sumacon_step_interval"] = new Memezo.Value(this.activeStepIntervalMilliseconds);
            var device = this.sumacon.DeviceManager.ActiveDevice;
            var rotatedSize = device?.RotatedScreenSize ?? Size.Empty;
            this.interpreter.Vars["sumacon_screen_width"] = new Memezo.Value(rotatedSize.Width);
            this.interpreter.Vars["sumacon_screen_height"] = new Memezo.Value(rotatedSize.Height);
            this.interpreter.Vars["sumacon_touch_protocol"] = new Memezo.Value((device?.Input.TouchProtocol ?? Mobile.TouchProtocolType.A).ToString());
        }

        void PullSpecialVars()
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;

            this.activeStepIntervalMilliseconds = (int)(this.interpreter.Vars.GetValue("sumacon_step_interval", new Memezo.Value(this.defaultStepIntervalMilliseconds)).Number);
            this.activeStepIntervalMilliseconds = Math.Max(1, this.activeStepIntervalMilliseconds);

            if (device != null)
            {
                var size = new Size(
                    (int)this.interpreter.Vars["sumacon_screen_width"].Number,
                    (int)this.interpreter.Vars["sumacon_screen_height"].Number);
                size = device.ScreenIsUpright ? size : size.Swapped();
                if (device?.ScreenSize != size && !size.IsEmpty)
                {
                    device.ScreenSize = size;
                }
            }

            if (Enum.TryParse(this.interpreter.Vars.GetValue("sumacon_touch_protocol", Memezo.Value.Zero).String, out Mobile.TouchProtocolType p))
            {
                this.sumacon.DeviceManager.TouchProtocolType = p;
            }
        }

        void UpdateWatchedVars()
        {
            if (this.interpreter == null) return;

            var vars = this.interpreter.Vars.Select(var => new VarEntry(var.Key, var.Value));

            var threadViewState = this.uxWatchPanel.GetViewState();
            this.uxWatchPanel.SuppressEvent(GridPanel.SupressibleEvent.SelectedItemChanged);

            // 変数消えることはないけど...
            var removes = this.watchedVars.Except(vars, new VarEntryComparer()).ToArray();
            foreach (var t in removes) this.watchedVars.Remove(t);
            var adds = vars.Except(this.watchedVars, new VarEntryComparer()).ToArray();
            foreach (var t in adds) this.watchedVars.Add(t);

            // 値が変わってたら更新
            for(int i = 0; i < this.watchedVars.Count; i++)
            {
                var var = this.watchedVars[i];
                var latestValue = this.interpreter.Vars[var.Name];
                if (var.Value.ToString() != latestValue.ToString())
                {
                    this.watchedVars[i] = new VarEntry(var.Name, latestValue);
                }
            }

            this.uxWatchPanel.SetViewState(threadViewState, GridViewState.ApplyTargets.SortedColumn | GridViewState.ApplyTargets.Selection);
            this.uxWatchPanel.UnsuppressEvent(GridPanel.SupressibleEvent.SelectedItemChanged);
        }

        void UpdateScriptSelection(int index)
        {
            var startIndex = this.interpreter.Source.LastIndexOf(Environment.NewLine, index);
            startIndex = (startIndex >= 0) ? (startIndex + Environment.NewLine.Length) : 0;

            var endIndex = this.interpreter.Source.IndexOf(Environment.NewLine, index);
            endIndex = (endIndex >= 0) ? endIndex : this.interpreter.Source.Length;

            this.uxScriptTextBox.Document.Unmark(this.markStartIndex, this.markEndIndex, this.kCurrentLineMarkId);
            this.uxScriptTextBox.Document.Mark(startIndex, endIndex, this.kCurrentLineMarkId);
            this.uxScriptTextBox.SetSelection(startIndex, endIndex);
            this.markStartIndex = startIndex;
            this.markEndIndex = endIndex;
        }

        void LoadSettings()
        {
            this.uxScriptTextBox.Text = Properties.Settings.Default.ScriptText;
            this.uxScriptTextBox.ClearHistory();
            this.defaultStepIntervalMilliseconds = Properties.Settings.Default.ScriptStepIntervalMilliseconds;
            this.activeStepIntervalMilliseconds = this.defaultStepIntervalMilliseconds;
        }

        void SaveSettings()
        {
            Properties.Settings.Default.ScriptText = this.uxScriptTextBox.Text;
        }

        void UpdateControlState()
        {
            var readOnly = false;
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if(device == null)
            {
                this.uxStopButton.Enabled = false;
                this.uxRunButton.Visible = true;
                this.uxPauseButton.Visible = false;
                this.uxStepButton.Enabled = false;
                readOnly = false;
            }
            else if (this.runState == RunState.Ready)
            {
                this.uxStopButton.Enabled = false;
                this.uxRunButton.Visible = true;
                this.uxPauseButton.Visible = false;
                this.uxStepButton.Enabled = true;
                readOnly = false;
            }
            else if (this.runState == RunState.Running)
            {
                this.uxStopButton.Enabled = true;
                this.uxRunButton.Visible = false;
                this.uxPauseButton.Visible = true;
                this.uxStepButton.Enabled = false;
                readOnly = true;
            }
            else if (this.runState == RunState.Paused)
            {
                this.uxStopButton.Enabled = true;
                this.uxRunButton.Visible = true;
                this.uxPauseButton.Visible = false;
                this.uxStepButton.Enabled = true;
                readOnly = true;
            }
            else
            {
                Debug.Assert(false);
            }

            if (this.uxScriptTextBox.IsReadOnly != readOnly)
            {
                this.uxScriptTextBox.IsReadOnly = readOnly;
                if (this.uxScriptTextBox.IsReadOnly)
                {
                    this.uxScriptTextBox.ColorScheme.ForeColor = this.sumacon.ColorSet.GrayedText;
                    this.uxScriptTextBox.Document.Highlighter = null;
                }
                else
                {
                    this.uxScriptTextBox.ColorScheme.ForeColor = this.sumacon.ColorSet.Text;
                    this.uxScriptTextBox.Document.Highlighter = this.highlighter;
                }
                this.uxScriptTextBox.Invalidate();
            }

            this.PushSpecialVars();
            this.UpdateWatchedVars();
        }

        class VarEntry
        {
            public string Name { get; private set; }
            public Memezo.Value Value { get; set; }
            public VarEntry(string name, Memezo.Value value)
            {
                this.Name = name;
                this.Value = value;
            }
        }
        class VarEntryComparer : IEqualityComparer<VarEntry>
        {
            public bool Equals(VarEntry a, VarEntry b) => a?.Name == b?.Name;
            public int GetHashCode(VarEntry p) => p.Name.GetHashCode();
        }
    }
}
