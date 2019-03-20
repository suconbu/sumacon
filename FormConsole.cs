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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Suconbu.Sumacon
{
    public partial class FormConsole : FormBase
    {
        Sumacon sumacon;
        Dictionary<string, CommandContext> contexts = new Dictionary<string, CommandContext>();
        LruCache<string, string> commandHistory = new LruCache<string, string>(10);

        public FormConsole(Sumacon sumacon)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.sumacon = sumacon;
            this.sumacon.CommandReceiver.InputReceived += this.CommandReceiver_InputReceived;
            this.sumacon.CommandReceiver.OutputReceived += this.CommandReceiver_OutputReceived;
            this.sumacon.DeviceManager.DeviceConnected += this.DeviceManager_DeviceConnected;
            this.sumacon.DeviceManager.DeviceDisconnecting += this.DeviceManager_DeviceDisconnecting;

            this.uxInputCombo.KeyDown += this.UxInputText_KeyDown;
            this.uxInputCombo.PreviewKeyDown += this.UxInputCombo_PreviewKeyDown;
            this.uxInputCombo.AutoCompleteMode = AutoCompleteMode.Append;
        }

        protected override void OnLoad(EventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnLoad(e);

            this.uxInputCombo.Font = new Font(Properties.Resources.MonospaceFontName, this.uxInputCombo.Font.Size);
            this.uxInputCombo.BackColor = Color.Black;
            this.uxInputCombo.ForeColor = Color.White;
            this.uxOutputText.Font = new Font(Properties.Resources.MonospaceFontName, this.uxOutputText.Font.Size);
            this.uxOutputText.BackColor = Color.Black;
            this.uxOutputText.ForeColor = Color.White;

            this.uxOutputText.AppendText($"{Util.GetApplicationName()} version {Util.GetVersionString(3)}" + Environment.NewLine);
            CommandContext.StartNewText("adb", "version", output => this.SafeInvoke(() => this.sumacon.CommandReceiver.WriteOutput(output))).Wait(1000);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            foreach(var context in this.contexts.Values)
            {
                context.Cancel();
            }

            this.sumacon.CommandReceiver.InputReceived -= this.CommandReceiver_InputReceived;
            this.sumacon.CommandReceiver.OutputReceived -= this.CommandReceiver_OutputReceived;
            this.sumacon.DeviceManager.DeviceConnected -= this.DeviceManager_DeviceConnected;
            this.sumacon.DeviceManager.DeviceDisconnecting -= this.DeviceManager_DeviceDisconnecting;
        }

        void CommandReceiver_InputReceived(object sender, string input)
        {
            this.SafeInvoke(() => this.uxOutputText.AppendText(Environment.NewLine + input));
        }

        void CommandReceiver_OutputReceived(object sender, string output)
        {
            this.SafeInvoke(() => this.uxOutputText.AppendText(Environment.NewLine + output));
        }

        void DeviceManager_DeviceConnected(object sender, Device device)
        {
            this.sumacon.CommandReceiver.WriteOutput($"Connected: '{device.ToString(Properties.Resources.DeviceLabelFormat)}'");
        }

        void DeviceManager_DeviceDisconnecting(object sender, Device device)
        {
            this.CancelCommandRun(device.Serial);
            this.sumacon.CommandReceiver.WriteOutput($"Disconnected: '{device.ToString(Properties.Resources.DeviceLabelFormat)}'");
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
            var device = this.sumacon.DeviceManager.ActiveDevice;

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
                this.CancelCommandRun(device?.Serial);
            }
        }

        bool StartCommandRun(Device device, string command)
        {
            if (device == null) return false;
            if (!this.contexts.TryGetValue(device.Serial, out var context))
            {
                // まだshellを開いてなかったら開く
                context = device.RunCommandAsync("shell", output =>
                {
                    if (output != null)
                    {
                        this.sumacon.CommandReceiver.WriteOutput(output);
                    }
                    else
                    {
                        // 終了
                        this.SafeInvoke(() => this.contexts.Remove(device.Serial));
                    }
                });
                this.contexts.Add(device.Serial, context);
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
