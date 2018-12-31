using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Suconbu.Sumacon
{
    public class CommandReceiver
    {
        public event EventHandler<string> InputReceived = delegate { };
        public event EventHandler<string> OutputReceived = delegate { };

        public CommandReceiver() { }

        public void WriteInput(string input)
        {
            this.InputReceived(this, input);
        }

        public void WriteOutput(string output)
        {
            this.OutputReceived(this, output);
        }
    }
}
