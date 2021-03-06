﻿using Sgry.Azuki.WinForms;
using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
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
        string updateControlStateTimeoutKey;
        int defaultStepTimeoutMilliseconds;
        SortableBindingList<VarEntry> watchedVars = new SortableBindingList<VarEntry>();
        Task scriptTask;
        CancellationTokenSource scriptTaskCanceller;
        List<TouchCommand> deferredTaps = new List<TouchCommand>();
        List<TouchCommand> deferredSwipes = new List<TouchCommand>();
        List<TouchCommand> deferredTouchMoves = new List<TouchCommand>();
        string previousInvokedFunctionName;

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

            this.uxWatchPanel.ApplyColorSet(this.sumacon.ColorSet);
            this.uxWatchPanel.DataSource = this.watchedVars;
            this.uxWatchPanel.KeyColumnName = nameof(VarEntry.Name);

            this.SetupScriptTextBox();

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
                () => this.SafeInvoke(this.UpdateControlState), 100, this.updateControlStateTimeoutKey, true);
        }

        void UxWatchPanel_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < this.watchedVars.Count)
            {
                this.uxScriptTextBox.GetSelection(out var start, out var end);
                var index = Math.Min(start, end);
                var length = Math.Abs(end - start);
                this.uxScriptTextBox.Text = this.uxScriptTextBox.Text
                    .Remove(index, length)
                    .Insert(index, this.watchedVars[e.RowIndex].Name);
            }
        }

        void SetupScriptTextBox()
        {
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
        }

        void OnStop()
        {
            this.ClearDeferredCommands();
            this.scriptTaskCanceller?.Cancel();
            this.runState = RunState.Ready;
            this.uxScriptTextBox.Document.Unmark(this.markStartIndex, this.markEndIndex, this.kCurrentLineMarkId);
            this.markStartIndex = 0;
            this.markEndIndex = 0;
            this.UpdateControlState();
        }

        void OnRun()
        {
            if (this.runState == RunState.Ready) this.PrepareToRun();

            this.scriptTaskCanceller?.Cancel();
            this.scriptTaskCanceller = new CancellationTokenSource();
            this.scriptTask = Task.Run(() => this.RunLoop(), this.scriptTaskCanceller.Token);

            this.runState = RunState.Running;
            this.UpdateControlState();
        }

        void OnPause()
        {
            this.scriptTaskCanceller?.Cancel();

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
            else if(this.runState == RunState.Paused)
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
            else
            {
                Trace.TraceError($"Unexpected runState:{this.runState}");
            }
            this.UpdateControlState();
        }

        void RunLoop()
        {
            while(this.runState == RunState.Running)
            {
                if (!this.RunOneStep() || this.sumacon.DeviceManager.ActiveDevice == null)
                {
                    this.SafeInvoke(this.OnStop);
                    break;
                }
            }
        }

        bool RunOneStep()
        {
            var result = this.interpreter.Step(out var nextIndex);
            if (result && nextIndex >= 0)
            {
                this.interpreter.ForwardToNextStatement(out this.currentSourceIndex);
                this.SafeInvoke(() =>
                {
                    this.PullSpecialVars();
                    this.UpdateWatchedVars();
                    this.UpdateScriptSelection(this.currentSourceIndex);
                });
                return true;
            }
            else
            {
                // Finish to run
                var device = this.sumacon.DeviceManager.ActiveDevice;
                this.FlushDeferredTouchCommands(device);
                return false;
            }
        }

        static class ScriptCommandNames
        {
            public static readonly string Print = "print";
            public static readonly string Wait = "wait";
            public static readonly string Beep = "beep";
            public static readonly string Tap = "tap";
            public static readonly string Swipe = "swipe";
            public static readonly string TouchOn = "touch_on";
            public static readonly string TouchMove = "touch_move";
            public static readonly string TouchOff = "touch_off";
            public static readonly string Adb = "adb";
            public static readonly string SaveCapture = "save_capture";
            public static readonly string Rotate = "rotate";
            public static readonly string RotateTo = "rotate_to";
        }

        void SetupInterpreter()
        {
            this.interpreter.Install(new Memezo.StandardLibrary(), new Memezo.RandomLibrary());
            this.interpreter.ErrorOccurred += this.Interpreter_ErrorOccurred;
            this.interpreter.FunctionInvoking += this.Interpreter_FunctionInvoking;
            this.interpreter.Functions[ScriptCommandNames.Print] = this.Interpreter_Print;
            this.interpreter.Functions[ScriptCommandNames.Wait] = this.Interpreter_Wait;
            this.interpreter.Functions[ScriptCommandNames.Beep] = this.Interpreter_Beep;
            this.interpreter.Functions[ScriptCommandNames.Tap] = this.Interpreter_Tap;
            this.interpreter.Functions[ScriptCommandNames.Swipe] = this.Interpreter_Swipe;
            this.interpreter.Functions[ScriptCommandNames.TouchOn] = this.Interpreter_TouchOn;
            this.interpreter.Functions[ScriptCommandNames.TouchMove] = this.Interpreter_TouchMove;
            this.interpreter.Functions[ScriptCommandNames.TouchOff] = this.Interpreter_TouchOff;
            this.interpreter.Functions[ScriptCommandNames.Adb] = this.Interpreter_Adb;
            this.interpreter.Functions[ScriptCommandNames.SaveCapture] = this.Interpreter_SaveCapture;
            this.interpreter.Functions[ScriptCommandNames.Rotate] = this.Interpreter_Rotate;
            this.interpreter.Functions[ScriptCommandNames.RotateTo] = this.Interpreter_RotateTo;

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

        private void Interpreter_FunctionInvoking(object sender, string name)
        {
            if (this.previousInvokedFunctionName != name &&
                this.IsDeferrableFunction(this.previousInvokedFunctionName))
            {
                this.FlushDeferredTouchCommands(this.sumacon.DeviceManager.ActiveDevice);
            }
            this.previousInvokedFunctionName = name;
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
            Task.Delay(duration).Wait();
            return Memezo.Value.Zero;
        }

        Memezo.Value Interpreter_Beep(List<Memezo.Value> args)
        {
            var frequency = (args.Count > 0) ? (int)args[0].Number : 1000;
            var duration = (args.Count > 1) ? (int)args[1].Number : 100;
            Console.Beep(frequency, duration);
            return Memezo.Value.Zero;
        }

        // tap(x, y, duration)
        // tap(no, x, y, duration)
        Memezo.Value Interpreter_Tap(List<Memezo.Value> args)
        {
            if (args.Count < 3) throw new ArgumentException("Too few arguments");

            var argsIndex = 0;
            var no = Input.InvalidTouchNo;
            if (args.Count >= 4)
            {
                no = (args[argsIndex].Type == Memezo.DataType.Number) ? (int)args[argsIndex].Number : throw new ArgumentException("Argument type mismatch", "no"); argsIndex++;
            }
            var x = (args[argsIndex].Type == Memezo.DataType.Number) ? (float)args[argsIndex].Number : throw new ArgumentException("Argument type mismatch", "x"); argsIndex++;
            var y = (args[argsIndex].Type == Memezo.DataType.Number) ? (float)args[argsIndex].Number : throw new ArgumentException("Argument type mismatch", "y"); argsIndex++;
            var duration = (args[argsIndex].Type == Memezo.DataType.Number) ? (int)args[argsIndex].Number : throw new ArgumentException("Argument type mismatch", "duration"); argsIndex++;

            this.Tap(no, x, y, duration);

            return Memezo.Value.Zero;
        }

        // swipe(x, y, direction, distance, duration)
        Memezo.Value Interpreter_Swipe(List<Memezo.Value> args)
        {
            if (args.Count < 5) throw new ArgumentException("Too few arguments");

            var argsIndex = 0;
            var no = Input.InvalidTouchNo;
            if (args.Count >= 6)
            {
                no = (args[argsIndex].Type == Memezo.DataType.Number) ? (int)args[argsIndex].Number : throw new ArgumentException("Argument type mismatch", "no"); argsIndex++;
            }
            var x = (args[argsIndex].Type == Memezo.DataType.Number) ? (float)args[argsIndex].Number : throw new ArgumentException("Argument type mismatch", "x"); argsIndex++;
            var y = (args[argsIndex].Type == Memezo.DataType.Number) ? (float)args[argsIndex].Number : throw new ArgumentException("Argument type mismatch", "y"); argsIndex++;
            var direction = (args[argsIndex].Type == Memezo.DataType.Number) ? (float)args[argsIndex].Number : throw new ArgumentException("Argument type mismatch", "duration"); argsIndex++;
            var distance = (args[argsIndex].Type == Memezo.DataType.Number) ? (float)args[argsIndex].Number : throw new ArgumentException("Argument type mismatch", "duration"); argsIndex++;
            var duration = (args[argsIndex].Type == Memezo.DataType.Number) ? (int)args[argsIndex].Number : throw new ArgumentException("Argument type mismatch", "duration"); argsIndex++;

            this.Swipe(no, x, y, direction, distance, duration);

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
            if (args.Count < 4) throw new ArgumentException("Too few arguments");

            var no = (args[0].Type == Memezo.DataType.Number) ? (int)args[0].Number : throw new ArgumentException("Argument type mismatch", "no");
            var x = (args[1].Type == Memezo.DataType.Number) ? (float)args[1].Number : throw new ArgumentException("Argument type mismatch", "x");
            var y = (args[2].Type == Memezo.DataType.Number) ? (float)args[2].Number : throw new ArgumentException("Argument type mismatch", "y");
            var duration = (args[3].Type == Memezo.DataType.Number) ? (int)args[3].Number : throw new ArgumentException("Argument type mismatch", "duration");

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

        Memezo.Value Interpreter_Adb(List<Memezo.Value> args)
        {
            if (args.Count < 1) throw new ArgumentException("Too few arguments");

            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device == null) throw new InvalidOperationException("Device not available");

            var command = (args[0].Type == Memezo.DataType.String) ? args[0].String : throw new ArgumentException("Argument type mismatch", "command");
            var timeoutMilliseconds = this.defaultStepTimeoutMilliseconds;
            if (args.Count > 1)
            {
                timeoutMilliseconds = (args[1].Type == Memezo.DataType.Number) ? (int)args[1].Number : throw new ArgumentException("Argument type mismatch", "timeout");
            }

            var sb = new StringBuilder();
            device.RunCommandAsync(command, output => sb.AppendLine(output), error => sb.AppendLine(error))
                .Wait(timeoutMilliseconds);

            return new Memezo.Value(sb.ToString());
        }

        Memezo.Value Interpreter_SaveCapture(List<Memezo.Value> args)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device == null) throw new InvalidOperationException("Device not available");

            var filePath = string.Empty;
            device.Screen.CaptureAsync(bitmap =>
            {
                filePath = this.sumacon.SaveCapturedImage(bitmap);
                bitmap.Dispose();
            }).Wait(this.defaultStepTimeoutMilliseconds);

            return new Memezo.Value(filePath);
        }

        Memezo.Value Interpreter_Rotate(List<Memezo.Value> args)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device == null) throw new InvalidOperationException("Device not available");

            var degrees = (args[0].Type == Memezo.DataType.Number) ? (int)args[0].Number : throw new ArgumentException("Argument type mismatch", "degrees");

            device.Screen.Rotate(degrees);

            return new Memezo.Value((int)device.Screen.UserRotation);
        }

        Memezo.Value Interpreter_RotateTo(List<Memezo.Value> args)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device == null) throw new InvalidOperationException("Device not available");

            var degrees = (args[0].Type == Memezo.DataType.Number) ? (int)args[0].Number : throw new ArgumentException("Argument type mismatch", "degrees");
            if (degrees < 0) degrees += 360;
            if (360 <= degrees) degrees -= 360;
            var rotationValue = (int)Math.Round(degrees / 90.0f);

            device.Screen.UserRotation = (Mobile.Screen.Rotation)Enum.Parse(typeof(Mobile.Screen.Rotation), rotationValue.ToString());

            return new Memezo.Value((int)device.Screen.UserRotation);
        }

        void Tap(int no, float x, float y, int duration)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device == null) throw new InvalidOperationException("Device not available");

            var actualTouchNo = device.Input.TouchOn(no, x, y);

            if (this.deferredTaps.Count > 0 &&
               this.deferredTaps.Exists(c => c.TouchNo == actualTouchNo))
            {
                this.FlushDeferredTouchCommands(device);
            }
            this.deferredTaps.Add(new TouchCommand(actualTouchNo, x, y, duration));
            if(no == Input.InvalidTouchNo)
            {
                this.FlushDeferredTouchCommands(device);
            }
        }

        void Swipe(int no, float x, float y, float direction, float distance, int duration)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device == null) throw new InvalidOperationException("Device not available");

            var actualTouchNo = device.Input.TouchOn(no, x, y);

            if (this.deferredSwipes.Count > 0 &&
               this.deferredSwipes.Exists(c => c.TouchNo == actualTouchNo))
            {
                this.FlushDeferredTouchCommands(device);
            }
            var degrees = Math.PI * direction / 180.0;
            var dx = (float)(x + distance * Math.Cos(degrees));
            var dy = (float)(y - distance * Math.Sin(degrees));
            this.deferredSwipes.Add(new TouchCommand(actualTouchNo, dx, dy, duration));
            if (no == Input.InvalidTouchNo)
            {
                this.FlushDeferredTouchCommands(device);
            }
        }

        void TouchOn(int no, float x, float y)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device == null) throw new InvalidOperationException("Device not available");

            device.Input.TouchOn(no, x, y);

            this.UpdateTouchMarkers(device);
        }

        void TouchMove(int no, float x, float y, int duration)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device == null) throw new InvalidOperationException("Device not available");

            if(this.deferredTouchMoves.Count > 0 &&
               this.deferredTouchMoves.Exists(c => c.TouchNo == no))
            {
                this.FlushDeferredTouchCommands(device);
            }
            this.deferredTouchMoves.Add(new TouchCommand(no, x, y, duration));
        }

        void TouchOff(int no)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device == null) throw new InvalidOperationException("Device not available");

            device.Input.TouchOff(no);

            this.UpdateTouchMarkers(device);
        }

        bool IsDeferrableFunction(string name)
        {
            return
                name == ScriptCommandNames.Tap ||
                name == ScriptCommandNames.Swipe ||
                name == ScriptCommandNames.TouchMove;
        }

        void FlushDeferredTouchCommands(Device device)
        {
            this.ExecuteTouchCommands(device, this.deferredTaps, false, true);
            this.ExecuteTouchCommands(device, this.deferredSwipes, true, true);
            this.ExecuteTouchCommands(device, this.deferredTouchMoves, true, false);
        }

        void ClearDeferredCommands()
        {
            this.deferredTaps.Clear();
            this.deferredSwipes.Clear();
            this.deferredTouchMoves.Clear();
        }

        void ExecuteTouchCommands(Device device, List<TouchCommand> commands, bool move, bool off)
        {
            if (device == null) return;
            if (commands.Count == 0) return;

            var previousPoints = device.Input.TouchPoints.ToDictionary(p => p.Key, p => new TouchPoint(p.Value));
            var durations = commands.ToDictionary(c => c.TouchNo, c => c.Duration);
            var maxDuration = commands.Max(c => c.Duration);
            var startedAt = DateTime.Now;
            while (true)
            {
                var elaspseMilliseconds = (float)(DateTime.Now - startedAt).TotalMilliseconds;
                foreach (var command in commands.ToArray())
                {
                    var no = command.TouchNo;
                    if (move)
                    {
                        if (previousPoints.TryGetValue(no, out var previousPoint))
                        {
                            var progress = Math.Min(1.0f, elaspseMilliseconds / durations[no]);
                            var ix = previousPoint.X + (command.Point.X - previousPoint.X) * progress;
                            var iy = previousPoint.Y + (command.Point.Y - previousPoint.Y) * progress;
                            device.Input.TouchMove(no, ix, iy);
                        }
                    }
                    if(command.Duration < elaspseMilliseconds)
                    {
                        if(off)
                        {
                            device.Input.TouchOff(command.TouchNo);
                        }
                        commands.Remove(command);
                    }
                }
                this.UpdateTouchMarkers(device);

                commands.RemoveAll(c => c.Duration < elaspseMilliseconds);
                if (elaspseMilliseconds > maxDuration) break;

                Task.Delay(10).Wait();
            }
            commands.Clear();
        }

        void UpdateTouchMarkers(Device device)
        {
            this.sumacon.ShowTouchMarkers(device.Input.TouchPoints.Values.ToArray());
        }

        void PushSpecialVars()
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            var rotatedSize = device?.RotatedScreenSize ?? Size.Empty;
            this.interpreter.Vars["sumacon_screen_width"] = new Memezo.Value(rotatedSize.Width);
            this.interpreter.Vars["sumacon_screen_height"] = new Memezo.Value(rotatedSize.Height);
            this.interpreter.Vars["sumacon_touch_protocol"] = new Memezo.Value((device?.Input.TouchProtocol ?? Mobile.TouchProtocolType.A).ToString());
        }

        void PullSpecialVars()
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;

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

            this.uxScriptTextBox.ScrollToCaret();
        }

        void LoadSettings()
        {
            this.uxScriptTextBox.Text = Properties.Settings.Default.ScriptText;
            this.uxScriptTextBox.ClearHistory();
            this.defaultStepTimeoutMilliseconds = Properties.Settings.Default.ScriptStepTimeoutMilliseconds;
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
                this.uxRunButton.Enabled = false;
                this.uxPauseButton.Visible = false;
                this.uxStepButton.Enabled = false;
                readOnly = false;
            }
            else if (this.runState == RunState.Ready)
            {
                this.uxStopButton.Enabled = false;
                this.uxRunButton.Visible = true;
                this.uxRunButton.Enabled = true;
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

        struct TouchCommand
        {
            public int TouchNo { get; private set; }
            public PointF Point { get; private set; }
            public int Duration { get; private set; }

            public TouchCommand(int no, float x, float y, int duration)
            {
                this.TouchNo = no;
                this.Point = new PointF(x, y);
                this.Duration = duration;
            }
        }
    }
}
