using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Suconbu.Mobile
{
    public struct InputEvent
    {
        List<byte> data;

        public void Add(ushort d1, ushort d2, int d3)
        {
            if (this.data == null) this.data = new List<byte>();
            this.data.AddRange(BitConverter.GetBytes((long)0));
            this.data.AddRange(BitConverter.GetBytes((long)0));
            this.data.AddRange(BitConverter.GetBytes(d1));
            this.data.AddRange(BitConverter.GetBytes(d2));
            this.data.AddRange(BitConverter.GetBytes(d3));
        }

        public byte[] ToArray(bool avoidEof)
        {
            return avoidEof ?
                this.data.Select(d => (d == 0x1A) ? (byte)0x19 : d).ToArray() :
                this.data.ToArray();
        }
    }

    public class Input : DeviceComponent
    {
        public enum HardSwitch { Power = 0x74, VolumeUp = 0x73, VolumeDown = 0x72 }

        enum InputDeviceType { TouchPad, HardSwitch }

        CommandContext touchPadContext;
        CommandContext hardSwitchContext;
        readonly HashSet<int> activeTrackingNos = new HashSet<int>();
        int nextTouchSequenceNo = 0x10000000;

        readonly Dictionary<InputDeviceType, string> inputDevices = new Dictionary<InputDeviceType, string>()
        {
            { InputDeviceType.TouchPad, "/dev/input/event0" },
            { InputDeviceType.HardSwitch, "/dev/input/event3" },
        };

        public Input(Device device, string xmlPath) : base(device, xmlPath) { }

        public void OnTouch(int trackingNo, float x, float y)
        {
            int sequenceNo;
            lock (this)
            {
                sequenceNo = this.nextTouchSequenceNo++;
                this.activeTrackingNos.Add(trackingNo);
            }
            this.NormalizedToTouchPadPoint(x, y, this.device.CurrentRotation, out var px, out var py);
            var e = new InputEvent();
            e.Add(0x0003, 0x002F, trackingNo);
            e.Add(0x0003, 0x0039, sequenceNo);
            e.Add(0x0003, 0x0035, px);
            e.Add(0x0003, 0x0036, py);
            e.Add(0x0000, 0x0000, 0);
            if (this.touchPadContext == null || this.touchPadContext.Finished)
            {
                this.touchPadContext = this.device.RunCommandAsync($"shell cat - > {this.inputDevices[InputDeviceType.TouchPad]}");
            }
            this.touchPadContext.PushInputBinary(e.ToArray(true));
        }

        public void MoveTouch(int trackingNo, float x, float y)
        {
            if (!this.activeTrackingNos.Contains(trackingNo)) return;

            this.NormalizedToTouchPadPoint(x, y, this.device.CurrentRotation, out var px, out var py);
            var e = new InputEvent();
            e.Add(0x0003, 0x002F, trackingNo);
            e.Add(0x0003, 0x0035, px);
            e.Add(0x0003, 0x0036, py);
            e.Add(0x0000, 0x0000, 0);
            this.touchPadContext?.PushInputBinary(e.ToArray(true));
        }

        public void OffTouch(int trackingNo)
        {
            lock (this)
            {
                this.activeTrackingNos.Remove(trackingNo);
            }

            var e = new InputEvent();
            e.Add(0x0003, 0x002F, trackingNo);
            e.Add(0x0003, 0x0039, -1);
            e.Add(0x0000, 0x0000, 0);
            this.touchPadContext?.PushInputBinary(e.ToArray(true));
        }

        public void Tap(float x, float y, int durationMilliseconds = 100)
        {
            this.OnTouch(0, x, y);
            Delay.SetTimeout(() => this.OffTouch(0), durationMilliseconds);
        }

        public void Swipe(float x1, float y1, float x2, float y2, int durationMilliseconds = 100)
        {
            var px1 = (int)(x1 * this.device.Screen.Size.Width);
            var py1 = (int)(y1 * this.device.Screen.Size.Height);
            var px2 = (int)(x2 * this.device.Screen.Size.Width);
            var py2 = (int)(y2 * this.device.Screen.Size.Height);
            this.device.RunCommandAsync($"shell input {x1} {y1} {x2} {y2} {durationMilliseconds}");
        }

        public void DownSwitch(HardSwitch sw)
        {
            var e = new InputEvent();
            e.Add(0x0001, (ushort)sw, 1);
            e.Add(0x0000, 0x0000, 0);
            if (this.hardSwitchContext == null || this.hardSwitchContext.Finished)
            {
                this.hardSwitchContext = this.device.RunCommandAsync($"shell cat - > {this.inputDevices[InputDeviceType.HardSwitch]}");
            }
            this.hardSwitchContext?.PushInputBinary(e.ToArray(true));
        }

        public void UpSwitch(HardSwitch sw)
        {
            var e = new InputEvent();
            e.Add(0x0001, (ushort)sw, 0);
            e.Add(0x0000, 0x0000, 0);
            this.hardSwitchContext?.PushInputBinary(e.ToArray(true));
        }

        public void PressSwitch(HardSwitch sw, int durationMilliseconds = 100)
        {
            this.DownSwitch(sw);
            Delay.SetTimeout(() => this.UpSwitch(sw), durationMilliseconds);
        }

        void NormalizedToTouchPadPoint(float nx, float ny, Screen.RotationCode rotation, out int tx, out int ty)
        {
            var w = 3072;
            var h = 2304;
            var x = nx;
            var y = ny;
            if (rotation == Screen.RotationCode.Protrait) { x = nx; y = ny; }// return new Point((int)(x * w), (int)(y * h));
            else if (rotation == Screen.RotationCode.ProtraitReversed) { x = 1 - nx; y = 1 - ny; }// return new Point((int)((1 - x) * w), (int)((1 - y) * h));
            else if (rotation == Screen.RotationCode.Landscape) { x = 1 - ny; y = nx; }// return new Point((int)((1 - y) * w), (int)(x * h));
            else if (rotation == Screen.RotationCode.LandscapeReversed) { x = y; y = 1 - x; }//return new Point((int)(y * w), (int)((1 - x) * h));
            else throw new NotSupportedException();
            tx = (int)(x * w);
            ty = (int)(y * h);
        }
    }
}
