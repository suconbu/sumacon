using Memezo = Suconbu.Scripting.Memezo;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Suconbu.Sumacon
{
    public partial class FormScript : FormBase
    {
        enum RunState { Ready, Running, Paused, Stepping }

        readonly ToolStrip uxToolStrip = new ToolStrip() { GripStyle = ToolStripGripStyle.Hidden };
        readonly TextBox uxScriptTextBox = new TextBox() { Dock = DockStyle.Fill, Multiline = true, HideSelection = false };
        readonly Sumacon sumacon;
        readonly ToolStripButton uxStopButton;
        readonly ToolStripButton uxRunButton;
        readonly ToolStripButton uxPauseButton;
        readonly ToolStripButton uxStepButton;
        Memezo.Interpreter interpreter = new Memezo.Interpreter();
        int currentSourceIndex;
        RunState runState = RunState.Ready;
        string stepTimeoutKey;
        int defaultStepIntervalMilliseconds;
        int activeStepIntervalMilliseconds;

        public FormScript(Sumacon sumacon)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.uxStopButton = this.uxToolStrip.Items.Add("Stop", null, (s, ee) => this.OnStop()) as ToolStripButton;
            this.uxRunButton = this.uxToolStrip.Items.Add("Run", null, (s, ee) => this.OnRun()) as ToolStripButton;
            this.uxPauseButton = this.uxToolStrip.Items.Add("Pause", null, (s, ee) => this.OnPause()) as ToolStripButton;
            this.uxStepButton = this.uxToolStrip.Items.Add("Step", null, (s, ee) => this.OnStep()) as ToolStripButton;

            this.sumacon = sumacon;
        }

        protected override void OnLoad(EventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnLoad(e);

            this.Controls.Add(this.uxScriptTextBox);
            this.Controls.Add(this.uxToolStrip);
            this.uxScriptTextBox.Font = new Font(Properties.Resources.MonospaceFontName, this.uxScriptTextBox.Font.Size);

            this.SetupInterpreter();

            this.sumacon.DeviceManager.ActiveDeviceChanged += this.DeviceManager_ActiveDeviceChanged;

            this.LoadSettings();

            this.UpdateControlState();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            this.sumacon.DeviceManager.ActiveDeviceChanged -= this.DeviceManager_ActiveDeviceChanged;
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

        void OnStop()
        {
            this.uxScriptTextBox.SelectionLength = 0;

            this.runState = RunState.Ready;
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
            if (this.runState == RunState.Ready) this.PrepareToRun();

            if (this.RunOneStep())
            {
                this.runState = RunState.Stepping;
            }
            else
            {
                this.OnStop();
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
            var result = this.interpreter.Step(this.currentSourceIndex, out var nextIndex);
            if (result && nextIndex >= 0)
            {
                this.currentSourceIndex = nextIndex;
                return true;
            }
            return false;
        }

        void SetupInterpreter()
        {
            this.interpreter.Install(new Memezo.StandardLibrary(), new Memezo.RandomLibrary());
            this.interpreter.ErrorOccurred += (s, errorInfo) => this.sumacon.WriteConsole(errorInfo.ToString());
            this.interpreter.StatementReached += this.Interpreter_StatementReached;
            this.interpreter.Functions["print"] = this.Interpreter_Print;
            this.interpreter.Functions["tap"] = this.Interpreter_Tap;
        }

        void Interpreter_StatementReached(object sender, Memezo.SourceLocation location)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;

            this.activeStepIntervalMilliseconds = (int)(this.interpreter.Vars.GetValue("sumacon_step_interval", new Memezo.Value(this.defaultStepIntervalMilliseconds)).Number);

            var size = new Size(
                (int)this.interpreter.Vars["sumacon_screen_width"].Number,
                (int)this.interpreter.Vars["sumacon_screen_height"].Number);
            size = device.ScreenIsUpright ? size : size.Swapped();
            if (device?.ScreenSize != size && !size.IsEmpty)
            {
                device.ScreenSize = size;
            }

            var touchProtocolString = this.interpreter.Vars.GetValue("sumacon_touch_protocol", Memezo.Value.Zero).String;
            var touchProtocol = Enum.TryParse(touchProtocolString, out Mobile.TouchProtocolType p) ? p : device.Input.TouchProtocol;
            if (device.Input.TouchProtocol != touchProtocol)
            {
                device.Input.TouchProtocol = touchProtocol;
            }

            this.UpdateScriptSelection(location.CharIndex);
        }

        Memezo.Value Interpreter_Print(List<Memezo.Value> args)
        {
            foreach (var arg in args)
            {
                this.sumacon.WriteConsole(arg.ToString());
            }
            return Memezo.Value.Zero;
        }

        Memezo.Value Interpreter_Tap(List<Memezo.Value> args)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (args.Count >= 3 && device != null)
            {
                var rotatedSize = device.ScreenIsUpright ? device.ScreenSize : device.ScreenSize.Swapped();
                var x = (float)Math.Max(0.0, Math.Min(args[0].Number, rotatedSize.Width));
                var y = (float)Math.Max(0.0, Math.Min(args[1].Number, rotatedSize.Height));
                var duration = (int)Math.Max(1.0, args[2].Number);
                device.Input.Tap(x / rotatedSize.Width, y / rotatedSize.Height, duration);
                Task.Delay(duration).Wait();
                this.sumacon.WriteConsole($"tap x:{x} y:{y} duration:{duration}");
            }
            return Memezo.Value.Zero;
        }

        void PrepareToRun()
        {
            this.interpreter.Source = this.uxScriptTextBox.Text;
            this.currentSourceIndex = 0;
            this.activeStepIntervalMilliseconds = this.defaultStepIntervalMilliseconds;
            this.interpreter.Vars["sumacon_step_interval"] = new Memezo.Value(this.activeStepIntervalMilliseconds);
            var device = this.sumacon.DeviceManager.ActiveDevice;
            var rotatedSize = (device != null) ? (device.ScreenIsUpright ? device.ScreenSize : device.ScreenSize.Swapped()) : Size.Empty;
            this.interpreter.Vars["sumacon_screen_width"] = new Memezo.Value(rotatedSize.Width);
            this.interpreter.Vars["sumacon_screen_height"] = new Memezo.Value(rotatedSize.Height);
            this.interpreter.Vars["sumacon_touch_protocol"] = new Memezo.Value((device?.Input.TouchProtocol ?? Mobile.TouchProtocolType.A).ToString());
        }

        void UpdateScriptSelection(int index)
        {
            var startIndex = this.interpreter.Source.LastIndexOf(Environment.NewLine, index);
            startIndex = (startIndex >= 0) ? (startIndex + Environment.NewLine.Length) : 0;

            var endIndex = this.interpreter.Source.IndexOf(Environment.NewLine, index);
            endIndex = (endIndex >= 0) ? endIndex : this.interpreter.Source.Length;

            this.uxScriptTextBox.SelectionStart = startIndex;
            this.uxScriptTextBox.SelectionLength = endIndex - startIndex;
        }

        void LoadSettings()
        {
            this.uxScriptTextBox.Text = Properties.Settings.Default.ScriptText;
            this.defaultStepIntervalMilliseconds = Properties.Settings.Default.ScriptStepIntervalMilliseconds;
        }

        void SaveSettings()
        {
            Properties.Settings.Default.ScriptText = this.uxScriptTextBox.Text;
        }

        void UpdateControlState()
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if(device == null)
            {
                this.uxStopButton.Enabled = false;
                this.uxRunButton.Enabled = false;
                this.uxPauseButton.Enabled = false;
                this.uxStepButton.Enabled = false;
                this.uxScriptTextBox.ReadOnly = false;
            }
            else if (this.runState == RunState.Ready)
            {
                this.uxStopButton.Enabled = false;
                this.uxRunButton.Enabled = true;
                this.uxPauseButton.Enabled = false;
                this.uxStepButton.Enabled = true;
                this.uxScriptTextBox.ReadOnly = false;
            }
            else if (this.runState == RunState.Running)
            {
                this.uxStopButton.Enabled = true;
                this.uxRunButton.Enabled = false;
                this.uxPauseButton.Enabled = true;
                this.uxStepButton.Enabled = false;
                this.uxScriptTextBox.ReadOnly = true;
            }
            else if (this.runState == RunState.Stepping)
            {
                this.uxStopButton.Enabled = true;
                this.uxRunButton.Enabled = true;
                this.uxPauseButton.Enabled = false;
                this.uxStepButton.Enabled = true;
                this.uxScriptTextBox.ReadOnly = true;
            }
            else if (this.runState == RunState.Paused)
            {
                this.uxStopButton.Enabled = true;
                this.uxRunButton.Enabled = true;
                this.uxPauseButton.Enabled = false;
                this.uxStepButton.Enabled = true;
                this.uxScriptTextBox.ReadOnly = true;
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }
}
