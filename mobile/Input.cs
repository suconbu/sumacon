using Suconbu.Mobile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Suconbu.Mobile
{
    public struct InputEvent
    {
        public int Code0;
        public int Code1;
        public int Code2;

        public InputEvent(int code0, int code1, int code2)
        {
            this.Code0 = code0;
            this.Code1 = code1;
            this.Code2 = code2;
        }
    }

    public enum InputDevice { TouchScreen, HardSwitch }

    public class Input
    {
        readonly Device device;
        readonly Dictionary<InputDevice, string> inputDevices = new Dictionary<InputDevice, string>()
        {
            { InputDevice.TouchScreen, "/dev/input/event0" },
            { InputDevice.HardSwitch, "/dev/input/event3" },
        };

        public Input(Device device)
        {
            this.device = device;
        }

        public void Send(InputDevice inputDevice, params InputEvent[] events )
        {
            var sb = new StringBuilder();
            sb.Append("shell ");
            foreach(var e in events)
            {
                sb.Append($"sendevent {this.inputDevices[inputDevice]} {e.Code0} {e.Code1} {e.Code2}; ");
            }
            sb.Append($"sendevent {this.inputDevices[inputDevice]} 0 0 0");
            this.device.RunCommandAsync(sb.ToString());
        }
    }
}
