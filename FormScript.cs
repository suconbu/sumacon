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
        readonly ToolStrip uxToolStrip = new ToolStrip() { GripStyle = ToolStripGripStyle.Hidden };
        readonly TextBox uxTextBox = new TextBox() { Dock = DockStyle.Fill, Multiline = true, HideSelection = false };
        readonly Sumacon sumacon;
        Memezo.Interpreter interpreter = new Memezo.Interpreter();
        int currentSourceIndex;

        public FormScript(Sumacon sumacon)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.sumacon = sumacon;
        }

        protected override void OnLoad(EventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnLoad(e);

            this.uxToolStrip.Items.Add("Run", null, (s, ee) => this.Run());

            this.Controls.Add(this.uxTextBox);
            this.Controls.Add(this.uxToolStrip);
            this.uxTextBox.Font = new Font(Properties.Resources.MonospaceFontName, this.uxTextBox.Font.Size);

            this.interpreter.Install(new Memezo.StandardLibrary(), new Memezo.RandomLibrary());
            this.interpreter.ErrorOccurred += (s, errorInfo) => this.sumacon.WriteConsole(errorInfo.ToString());
            this.interpreter.StatementReached += (s, location) =>
            {
                var startIndex = this.interpreter.Source.LastIndexOf(Environment.NewLine, location.CharIndex);
                startIndex = (startIndex >= 0) ? (startIndex + Environment.NewLine.Length) : 0;
                var endIndex = this.interpreter.Source.IndexOf(Environment.NewLine, location.CharIndex);
                endIndex = (endIndex >= 0) ? endIndex : this.interpreter.Source.Length;
                this.uxTextBox.SelectionStart = startIndex;
                this.uxTextBox.SelectionLength = endIndex - startIndex;
            };
            this.interpreter.Functions["print"] = (a) => { this.sumacon.WriteConsole(a.Count >= 1 ? a.First().ToString() : null); return Memezo.Value.Zero; };
            this.interpreter.Functions["tap"] = (a) => { this.sumacon.WriteConsole(a.Count >= 3 ? $"tap x:{a[0]} y:{a[1]} duration:{a[2]}" : null); return Memezo.Value.Zero; };

            this.LoadSettings();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            this.SaveSettings();
        }

        void Run()
        {
            this.interpreter.Source = this.uxTextBox.Text;
            //this.uxTextBox.ReadOnly = true;

            this.currentSourceIndex = 0;
            this.AutoStep();
        }

        void AutoStep()
        {
            Delay.SetTimeout(() =>
            {
                var result = this.interpreter.Step(this.currentSourceIndex, out var nextIndex);
                if (result && nextIndex >= 0)
                {
                    this.currentSourceIndex = nextIndex;
                    this.AutoStep();
                }
            }, 100, this);
        }

        void LoadSettings()
        {
            this.uxTextBox.Text = Properties.Settings.Default.ScriptText;
        }

        void SaveSettings()
        {
            Properties.Settings.Default.ScriptText = this.uxTextBox.Text;
        }
    }
}
