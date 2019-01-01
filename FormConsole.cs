using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Suconbu.Sumacon
{
    public partial class FormConsole : FormBase
    {
        DeviceManager deviceManager;
        CommandReceiver commandReceiver;
        Dictionary<string, CommandContext> contexts = new Dictionary<string, CommandContext>();

        public FormConsole(DeviceManager deviceManager, CommandReceiver commandReceiver)
        {
            InitializeComponent();

            this.deviceManager = deviceManager;
            this.commandReceiver = commandReceiver;
            this.commandReceiver.InputReceived += (s, input) =>
            {
                this.SafeInvoke(() => this.uxOutputText.AppendText(input + Environment.NewLine));
            };
            this.commandReceiver.OutputReceived += (s, output) =>
            {
                this.SafeInvoke(() => this.uxOutputText.AppendText(output + Environment.NewLine));
            };

            this.uxInputText.KeyDown += this.UxInputText_KeyDown;
            //this.uxInputText.PreviewKeyDown += this.UxInputText_PreviewKeyDown;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.uxInputText.Font = new Font(Properties.Resources.MonospaceFontName, SystemFonts.MessageBoxFont.Size);
            this.uxInputText.BackColor = Color.Black;
            this.uxInputText.ForeColor = Color.White;
            this.uxOutputText.Font = new Font(Properties.Resources.MonospaceFontName, SystemFonts.MessageBoxFont.Size);
            this.uxOutputText.BackColor = Color.Black;
            this.uxOutputText.ForeColor = Color.White;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            foreach(var context in this.contexts.Values)
            {
                context.Cancel();
            }
        }

        void UxInputText_KeyDown(object sender, KeyEventArgs e)
        {
            var device = this.deviceManager.ActiveDevice;

            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (device == null) return;

                var command = this.uxInputText.Text;
                if(!string.IsNullOrEmpty(command))
                {
                    if (!this.contexts.TryGetValue(device.Id, out var context))
                    {
                        context = device.RunCommandAsync("shell", output =>
                        {
                            if (output != null)
                            {
                               this.commandReceiver?.WriteOutput(output);
                            }
                            else
                            {
                                this.SafeInvoke(() => this.contexts.Remove(device.Id));
                            }
                        });
                        this.contexts.Add(device.Id, context);
                    }
                    context.PushInput($"echo '> {command}'");
                    context.PushInput(command);
                    this.uxInputText.Text = string.Empty;
                }
            }
            else if(e.KeyCode == Keys.C && e.Modifiers.HasFlag(Keys.Control))
            {
                e.SuppressKeyPress = true;
                if (device == null) return;

                if (this.contexts.TryGetValue(device.Id, out var context))
                {
                    this.contexts.Remove(device.Id);
                    context.Cancel();
                }
            }
        }
    }
}
