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

            this.interpreter.Install(new Memezo.StandardLibrary(), new Memezo.RandomLibrary());
            this.interpreter.ErrorOccurred += (s, errorInfo) => this.sumacon.WriteConsole(errorInfo.ToString());
            this.interpreter.StatementReached += (s, location) => this.UpdateScriptSelection(location.CharIndex);
            this.interpreter.Functions["print"] = (a) => { this.sumacon.WriteConsole(a.Count >= 1 ? a.First().ToString() : null); return Memezo.Value.Zero; };
            this.interpreter.Functions["tap"] = (a) => { this.sumacon.WriteConsole(a.Count >= 3 ? $"tap x:{a[0]} y:{a[1]} duration:{a[2]}" : null); return Memezo.Value.Zero; };

            this.LoadSettings();

            this.UpdateControlState();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            this.SaveSettings();
        }

        void OnStop()
        {
            this.interpreter.Source = null;
            this.currentSourceIndex = 0;
            this.uxScriptTextBox.SelectionStart = 0;
            this.uxScriptTextBox.SelectionLength = 0;

            this.runState = RunState.Ready;
            this.UpdateControlState();
        }

        void OnRun()
        {
            this.interpreter.Source = this.interpreter.Source ?? this.uxScriptTextBox.Text;
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
            this.interpreter.Source = this.interpreter.Source ?? this.uxScriptTextBox.Text;
            this.RunOneStep();

            this.runState = RunState.Stepping;
            this.UpdateControlState();
        }

        void RunAuto()
        {
            this.stepTimeoutKey = Delay.SetTimeout(() =>
            {
                if (this.runState == RunState.Running)
                {
                    if (this.RunOneStep())
                    {
                        this.RunAuto();
                    }
                }
            }, 100, this, this.stepTimeoutKey, true);
        }

        bool RunOneStep()
        {
            var result = this.interpreter.Step(this.currentSourceIndex, out var nextIndex);
            if (result && nextIndex >= 0)
            {
                this.currentSourceIndex = nextIndex;
            }
            return result;
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
        }

        void SaveSettings()
        {
            Properties.Settings.Default.ScriptText = this.uxScriptTextBox.Text;
        }

        void UpdateControlState()
        {
            if (this.runState == RunState.Ready)
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
