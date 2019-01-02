using Suconbu.Mobile;
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
        LruCache<string, string> commandHistory = new LruCache<string, string>(10);

        public FormConsole(DeviceManager deviceManager, CommandReceiver commandReceiver)
        {
            InitializeComponent();

            this.commandReceiver = commandReceiver;
            this.commandReceiver.InputReceived += (s, input) =>
            {
                this.SafeInvoke(() => this.uxOutputText.AppendText(Environment.NewLine + input));
            };
            this.commandReceiver.OutputReceived += (s, output) =>
            {
                this.SafeInvoke(() => this.uxOutputText.AppendText(Environment.NewLine + output));
            };

            this.deviceManager = deviceManager;
            this.deviceManager.DeviceConnected += (s, device) =>
            {
                this.commandReceiver.WriteOutput($"# '{device.ToString(Properties.Resources.DeviceLabelFormat)}' is connected.");
            };
            this.deviceManager.DeviceDisconnecting += (s, device) =>
            {
                this.CancelCommandRun(device.Id);
                this.commandReceiver.WriteOutput($"# '{device.ToString(Properties.Resources.DeviceLabelFormat)}' is disconnected.");
            };

            this.uxInputCombo.KeyDown += this.UxInputText_KeyDown;
            this.uxInputCombo.PreviewKeyDown += this.UxInputCombo_PreviewKeyDown;
            this.uxInputCombo.AutoCompleteMode = AutoCompleteMode.Append;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.uxInputCombo.Font = new Font(Properties.Resources.MonospaceFontName, this.uxInputCombo.Font.Size);
            this.uxInputCombo.BackColor = Color.Black;
            this.uxInputCombo.ForeColor = Color.White;
            this.uxOutputText.Font = new Font(Properties.Resources.MonospaceFontName, this.uxOutputText.Font.Size);
            this.uxOutputText.BackColor = Color.Black;
            this.uxOutputText.ForeColor = Color.White;

            this.uxOutputText.AppendText($"# {DateTime.Now.ToString()}");
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            foreach(var context in this.contexts.Values)
            {
                context.Cancel();
            }
        }

        private void UxInputCombo_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
            {
                foreach (var c in this.commandHistory.GetValues())
                {
                    if(c.StartsWith(this.uxInputCombo.Text))
                    {
                        this.uxInputCombo.Text = c;
                        break;
                    }
                }
            }
        }

        void UxInputText_KeyDown(object sender, KeyEventArgs e)
        {
            var device = this.deviceManager.ActiveDevice;

            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (device == null) return;

                var command = this.uxInputCombo.Text;
                if (this.StartCommandRun(device, command))
                {
                    this.PushCommandHistory(command);
                    this.uxInputCombo.Text = string.Empty;
                }
            }
            else if(e.KeyCode == Keys.C && e.Modifiers.HasFlag(Keys.Control))
            {
                e.SuppressKeyPress = true;
                this.CancelCommandRun(device?.Id);
            }
        }

        bool StartCommandRun(Device device, string command)
        {
            if (device == null) return false;
            if (!this.contexts.TryGetValue(device.Id, out var context))
            {
                // まだshellを開いてなかったら開く
                context = device.RunCommandAsync("shell", output =>
                {
                    if (output != null)
                    {
                        this.commandReceiver?.WriteOutput(output);
                    }
                    else
                    {
                        // 終了
                        this.SafeInvoke(() => this.contexts.Remove(device.Id));
                    }
                });
                this.contexts.Add(device.Id, context);
            }
            context.PushInput($"echo '> {command}'");
            context.PushInput(command);
            return true;
        }

        void CancelCommandRun(string deviceId)
        {
            if (this.contexts.TryGetValue(deviceId, out var context))
            {
                this.contexts.Remove(deviceId);
                context.Cancel();
            }
        }

        void PushCommandHistory(string command)
        {
            this.commandHistory.Add(command, command);
            this.uxInputCombo.Items.Clear();
            foreach(var c in this.commandHistory.GetValues())
            {
                this.uxInputCombo.Items.Add(c);
            }
        }
    }
}
