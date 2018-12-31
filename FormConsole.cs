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
        CommandReceiver commandReceiver;

        public FormConsole(CommandReceiver commandReceiver)
        {
            InitializeComponent();

            this.commandReceiver = commandReceiver;
            this.commandReceiver.OutputReceived += (s, output) =>
            {
                this.SafeInvoke(() => this.textBox1.AppendText(output + Environment.NewLine));
            };
        }
    }
}
