using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Suconbu.Mobile
{
    public enum TouchProtocolType { A, B, C }

    public class Input : DeviceComponent
    {
        public const int InvalidTouchNo = -1;
        public string TouchDevice { get => (string)this[nameof(this.TouchDevice)].Value; }
        public Point TouchMin { get => (Point)this[nameof(this.TouchMin)].Value; }
        public Point TouchMax { get => (Point)this[nameof(this.TouchMax)].Value; }
        public TouchProtocolType TouchProtocol
        {
            get => this.touchProtocol.Type;
            set => this.touchProtocol = (this.touchProtocol.Type != value) ? TouchProtocolFactory.New(value) : this.touchProtocol;
        }
        public IReadOnlyDictionary<int, TouchPoint> TouchPoints { get => this.touchPoints; }

        CommandContext touchPadContext;
        ITouchProtocol touchProtocol = new TouchProtocolA();
        readonly Dictionary<int, TouchPoint> touchPoints = new Dictionary<int, TouchPoint>();

        public Input(Device device, string xmlPath) : base(device, xmlPath) { }

        public int OnTouch(float x, float y)
        {
            return this.OnTouch(Input.InvalidTouchNo, x, y);
        }

        public int OnTouch(int no, float x, float y)
        {
            if (no == Input.InvalidTouchNo)
            {
                lock (this)
                {
                    no = this.touchPoints.Count;
                }
            }

            this.touchPoints[no] = new TouchPoint(no, x, y);

            var point = this.NormalizedToTouchPadPoint(x, y, this.device.CurrentRotation);

            var e = this.touchProtocol.On(no, point.X, point.Y);

            if (this.touchPadContext == null || this.touchPadContext.Finished)
            {
                this.touchPadContext = this.device.RunCommandAsync($"shell cat - > {this.TouchDevice}");
            }
            this.touchPadContext.PushInputBinary(e.ToArray(true));
            return no;
        }

        public void MoveTouch(int no, float x, float y)
        {
            if (!this.touchPoints.TryGetValue(no, out var touchPoint)) return;

            var point = this.NormalizedToTouchPadPoint(x, y, this.device.CurrentRotation);
            var e = this.touchProtocol.Move(no, point.X, point.Y);
            this.touchPadContext?.PushInputBinary(e.ToArray(true));

            touchPoint.X = x;
            touchPoint.Y = y;
        }

        public void OffTouch(int no = Input.InvalidTouchNo)
        {
            if (no != Input.InvalidTouchNo)
            {
                lock (this) this.touchPoints.Remove(no);
                var e = this.touchProtocol.Off(no);
                this.touchPadContext?.PushInputBinary(e.ToArray(true));
            }
            else
            {
                foreach(var touchPoint in this.touchPoints)
                {
                    var e = this.touchProtocol.Off(touchPoint.Key);
                    this.touchPadContext?.PushInputBinary(e.ToArray(true));
                }
                lock (this) this.touchPoints.Clear();
            }
        }

        public int Tap(float x, float y, int durationMilliseconds = 100)
        {
            var no = this.OnTouch(x, y);
            Delay.SetTimeout(() => this.OffTouch(no), durationMilliseconds);
            return no;
        }

        public void Swipe(float x1, float y1, float x2, float y2, int durationMilliseconds = 100)
        {
            var px1 = (int)(x1 * this.device.Screen.Size.Width);
            var py1 = (int)(y1 * this.device.Screen.Size.Height);
            var px2 = (int)(x2 * this.device.Screen.Size.Width);
            var py2 = (int)(y2 * this.device.Screen.Size.Height);
            this.device.RunCommandAsync($"shell input {x1} {y1} {x2} {y2} {durationMilliseconds}");
        }

        Point NormalizedToTouchPadPoint(float nx, float ny, Screen.Rotation rotation)
        {
            var w = this.TouchMax.X - this.TouchMin.X;
            var h = this.TouchMax.Y - this.TouchMin.Y;
            var point =
                (rotation == Screen.Rotation.Protrait) ? new PointF(nx * w, ny * h) :
                (rotation == Screen.Rotation.ProtraitReversed) ? new PointF((1.0f - nx) * w, (1.0f - ny) * h) :
                (rotation == Screen.Rotation.Landscape) ? new PointF((1.0f - ny) * w, nx * h) :
                (rotation == Screen.Rotation.LandscapeReversed) ? new PointF(ny * w, (1.0f - nx) * h) :
                throw new NotSupportedException();
            return Point.Truncate(point);
        }
    }

    public class TouchPoint
    {
        /// <summary>
        /// Identification no for the touch point.
        /// </summary>
        public int No { get; private set; }

        /// <summary>
        /// Normalized touch point location.
        /// (0.0, 0.0): Left top
        /// (1.0, 1.0): Right bottom
        /// </summary>
        public PointF Location { get => this.location; }

        public float X { get => this.location.X; internal set => this.location.X = value; }
        public float Y { get => this.location.Y; internal set => this.location.Y = value; }

        PointF location;

        public TouchPoint(int no, float x, float y)
        {
            this.No = no;
            this.location = new PointF(x, y);
        }

        public TouchPoint(TouchPoint from)
        {
            this.No = from.No;
            this.location = from.location;
        }

        TouchPoint() { }
    }

    interface ITouchProtocol
    {
        TouchProtocolType Type { get; }

        InputEvent On(int no, int x, int y);
        InputEvent Move(int no, int x, int y);
        InputEvent Off(int no);
    }

    static class TouchProtocolFactory
    {
        public static ITouchProtocol New(TouchProtocolType type)
        {
            if (type == TouchProtocolType.A) return new TouchProtocolA();
            if (type == TouchProtocolType.B) return new TouchProtocolB();
            if (type == TouchProtocolType.C) return new TouchProtocolC();
            throw new NotSupportedException();
        }
    }

    class TouchProtocolA : ITouchProtocol
    {
        int nextSequenceNo = 1;

        public TouchProtocolType Type { get => TouchProtocolType.A; }

        public InputEvent On(int no, int x, int y)
        {
            int sequenceNo;
            lock(this) sequenceNo = this.nextSequenceNo++;
            var e = new InputEvent();
            e.Add(0x0003, 0x002F, no);
            e.Add(0x0003, 0x0039, sequenceNo);
            e.Add(0x0003, 0x0035, x);
            e.Add(0x0003, 0x0036, y);
            e.Add(0x0000, 0x0000, 0);
            return e;
        }

        public InputEvent Move(int no, int x, int y)
        {
            var e = new InputEvent();
            e.Add(0x0003, 0x002F, no);
            e.Add(0x0003, 0x0035, x);
            e.Add(0x0003, 0x0036, y);
            e.Add(0x0000, 0x0000, 0);
            return e;
        }

        public InputEvent Off(int no)
        {
            var e = new InputEvent();
            e.Add(0x0003, 0x002F, no);
            e.Add(0x0003, 0x0039, -1);
            e.Add(0x0000, 0x0000, 0);
            return e;
        }
    }

    class TouchProtocolB : ITouchProtocol
    {
        public TouchProtocolType Type { get => TouchProtocolType.B; }

        public InputEvent On(int no, int x, int y)
        {
            var e = new InputEvent();
            e.Add(0x0001, 0x014A, 1);
            e.Add(0x0003, 0x0039, no);
            e.Add(0x0003, 0x0035, x);
            e.Add(0x0003, 0x0036, y);
            e.Add(0x0000, 0x0000, 0);
            return e;
        }

        public InputEvent Move(int no, int x, int y)
        {
            var e = new InputEvent();
            e.Add(0x0003, 0x0039, no);
            e.Add(0x0003, 0x0035, x);
            e.Add(0x0003, 0x0036, y);
            e.Add(0x0000, 0x0000, 0);
            return e;
        }

        public InputEvent Off(int no)
        {
            var e = new InputEvent();
            e.Add(0x0001, 0x014A, 0);
            e.Add(0x0000, 0x0000, 0);
            return e;
        }
    }

    class TouchProtocolC : ITouchProtocol
    {
        public TouchProtocolType Type { get => TouchProtocolType.C; }

        readonly int kPressure = 50;

        public InputEvent On(int no, int x, int y)
        {
            var e = new InputEvent();
            e.Add(0x0003, 0x0039, no);
            e.Add(0x0003, 0x0037, 0);
            e.Add(0x0003, 0x0035, x);
            e.Add(0x0003, 0x0036, y);
            e.Add(0x0003, 0x003A, this.kPressure);
            e.Add(0x0000, 0x0002, 0);
            e.Add(0x0000, 0x0000, 0);
            return e;
        }

        public InputEvent Move(int no, int x, int y)
        {
            var e = new InputEvent();
            e.Add(0x0003, 0x0039, no);
            e.Add(0x0003, 0x0037, 0);
            e.Add(0x0003, 0x0035, x);
            e.Add(0x0003, 0x0036, y);
            e.Add(0x0003, 0x003A, this.kPressure);
            e.Add(0x0000, 0x0002, 0);
            e.Add(0x0000, 0x0000, 0);
            return e;
        }

        public InputEvent Off(int no)
        {
            var e = new InputEvent();
            e.Add(0x0000, 0x0002, 0);
            e.Add(0x0000, 0x0000, 0);
            return e;
        }
    }

    struct InputEvent
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
}
