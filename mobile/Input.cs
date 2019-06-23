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

    public class Input : DeviceComponent, IDisposable
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

        public int TouchOn(float x, float y)
        {
            return this.TouchOn(Input.InvalidTouchNo, x, y);
        }

        public int TouchOn(int no, float x, float y)
        {
            lock (this)
            {
                no = (no == Input.InvalidTouchNo) ? this.touchPoints.Count : no;
                this.touchPoints[no] = new TouchPoint(no, x, y);
                var e = this.touchProtocol.On(no, this.GetActiveTouchPoints(), this.NormalizedToTouchPadPoint);
                if (this.touchPadContext == null || this.touchPadContext.Finished)
                {
                    this.touchPadContext = this.device.RunCommandAsync($"shell cat - > {this.TouchDevice}");
                }
                this.touchPadContext.PushInputBinary(e.ToArray(true));

                return no;
            }
        }

        public void TouchMove(int no, float x, float y)
        {
            lock (this)
            {
                if (!this.touchPoints.TryGetValue(no, out var touchPoint)) return;
                touchPoint.X = x;
                touchPoint.Y = y;
                var e = this.touchProtocol.Move(no, this.GetActiveTouchPoints(), this.NormalizedToTouchPadPoint);
                this.touchPadContext?.PushInputBinary(e.ToArray(true));
            }
        }

        public void TouchOff()
        {
            lock (this)
            {
                foreach (var touchPoint in this.touchPoints.Values.ToArray())
                {
                    var e = this.touchProtocol.Off(touchPoint.No, this.GetActiveTouchPoints());
                    this.touchPadContext?.PushInputBinary(e.ToArray(true));
                    this.touchPoints.Remove(touchPoint.No);
                }
            }
        }

        public void TouchOff(int no)
        {
            lock (this)
            {
                var e = this.touchProtocol.Off(no, this.GetActiveTouchPoints());
                this.touchPadContext?.PushInputBinary(e.ToArray(true));
                this.touchPoints.Remove(no);
            }
        }

        public int Tap(float x, float y, int durationMilliseconds)
        {
            var no = this.TouchOn(x, y);
            this.TouchMove(no, x, y);
            Delay.SetTimeout(() => this.TouchOff(no), durationMilliseconds);
            return no;
        }

        public void Swipe(float x1, float y1, float x2, float y2, int durationMilliseconds)
        {
            var px1 = (int)(x1 * this.device.Screen.Size.Width);
            var py1 = (int)(y1 * this.device.Screen.Size.Height);
            var px2 = (int)(x2 * this.device.Screen.Size.Width);
            var py2 = (int)(y2 * this.device.Screen.Size.Height);
            this.device.RunCommandAsync($"shell input {x1} {y1} {x2} {y2} {durationMilliseconds}");
        }

        TouchPoint[] GetActiveTouchPoints()
        {
            return this.touchPoints.Values.OrderBy(p => p.No).ToArray();
        }

        Point NormalizedToTouchPadPoint(PointF normalizedPoint)
        {
            var nx = normalizedPoint.X;
            var ny = normalizedPoint.Y;
            var rotation = this.device.CurrentRotation;
            var w = this.TouchMax.X - this.TouchMin.X;
            var h = this.TouchMax.Y - this.TouchMin.Y;
            var point =
                (rotation == Screen.Rotation.Protrait) ? new PointF(nx * w, ny * h) :
                (rotation == Screen.Rotation.ProtraitReversed) ? new PointF((1.0f - nx) * w, (1.0f - ny) * h) :
                (rotation == Screen.Rotation.Landscape) ? new PointF((1.0f - ny) * w, nx * h) :
                (rotation == Screen.Rotation.LandscapeReversed) ? new PointF(ny * w, (1.0f - nx) * h) :
                throw new NotSupportedException();
            point.X += this.TouchMin.X;
            point.Y += this.TouchMin.Y;
            return Point.Truncate(point);
        }

        #region IDisposable Support
        bool disposed = false;

        public virtual void Dispose()
        {
            if (this.disposed) return;
            this?.touchPadContext.Cancel();
            this.disposed = true;
        }
        #endregion
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

        InputEvent On(int no, TouchPoint[] touchPoints, Func<PointF, Point> normalizedToTouchPad);
        InputEvent Move(int no, TouchPoint[] touchPoints, Func<PointF, Point> normalizedToTouchPad);
        InputEvent Off(int no, TouchPoint[] touchPoints);
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
        public TouchProtocolType Type { get => TouchProtocolType.A; }

        int nextSequenceNo = 1;

        public InputEvent On(int no, TouchPoint[] touchPoints, Func<PointF, Point> normalizedToTouchPad)
        {
            int sequenceNo;
            lock(this) sequenceNo = this.nextSequenceNo++;
            var p = normalizedToTouchPad(touchPoints[no].Location);
            var e = new InputEvent();
            e.Add(0x0003, 0x002F, no);
            e.Add(0x0003, 0x0039, sequenceNo);
            e.Add(0x0003, 0x0035, p.X);
            e.Add(0x0003, 0x0036, p.Y);
            e.Add(0x0000, 0x0000, 0);
            return e;
        }

        public InputEvent Move(int no, TouchPoint[] touchPoints, Func<PointF, Point> normalizedToTouchPad)
        {
            var p = normalizedToTouchPad(touchPoints[no].Location);
            var e = new InputEvent();
            e.Add(0x0003, 0x002F, no);
            e.Add(0x0003, 0x0035, p.X);
            e.Add(0x0003, 0x0036, p.Y);
            e.Add(0x0000, 0x0000, 0);
            return e;
        }

        public InputEvent Off(int no, TouchPoint[] touchPoints)
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

        public InputEvent On(int no, TouchPoint[] touchPoints, Func<PointF, Point> normalizedToTouchPad)
        {
            var e = new InputEvent();
            if (touchPoints.Length == 1)
            {
                e.Add(0x0001, 0x014A, 1);
            }
            foreach (var touchPoint in touchPoints)
            {
                var p = normalizedToTouchPad(touchPoint.Location);
                e.Add(0x0003, 0x0039, touchPoint.No);
                e.Add(0x0003, 0x0035, p.X);
                e.Add(0x0003, 0x0036, p.Y);
                e.Add(0x0000, 0x0002, 0);
            }
            e.Add(0x0000, 0x0000, 0);
            return e;
        }

        public InputEvent Move(int no, TouchPoint[] touchPoints, Func<PointF, Point> normalizedToTouchPad)
        {
            var e = new InputEvent();
            foreach (var touchPoint in touchPoints)
            {
                var p = normalizedToTouchPad(touchPoint.Location);
                e.Add(0x0003, 0x0039, touchPoint.No);
                e.Add(0x0003, 0x0035, p.X);
                e.Add(0x0003, 0x0036, p.Y);
                e.Add(0x0000, 0x0002, 0);
            }
            e.Add(0x0000, 0x0000, 0);
            return e;
        }

        public InputEvent Off(int no, TouchPoint[] touchPoints)
        {
            var e = new InputEvent();
            if (touchPoints.Length == 1)
            {
                e.Add(0x0001, 0x014A, 0);
                e.Add(0x0000, 0x0002, 0);
                e.Add(0x0000, 0x0000, 0);
            }
            return e;
        }
    }

    class TouchProtocolC : ITouchProtocol
    {
        public TouchProtocolType Type { get => TouchProtocolType.C; }

        readonly int kPressure = 50;

        public InputEvent On(int no, TouchPoint[] touchPoints, Func<PointF, Point> normalizedToTouchPad)
        {
            var p = normalizedToTouchPad(touchPoints[no].Location);
            var e = new InputEvent();
            e.Add(0x0003, 0x0039, no);
            e.Add(0x0003, 0x0037, 0);
            e.Add(0x0003, 0x0035, p.X);
            e.Add(0x0003, 0x0036, p.Y);
            e.Add(0x0003, 0x003A, this.kPressure);
            e.Add(0x0000, 0x0002, 0);
            e.Add(0x0000, 0x0000, 0);
            return e;
        }

        public InputEvent Move(int no, TouchPoint[] touchPoints, Func<PointF, Point> normalizedToTouchPad)
        {
            var p = normalizedToTouchPad(touchPoints[no].Location);
            var e = new InputEvent();
            e.Add(0x0003, 0x0039, no);
            e.Add(0x0003, 0x0037, 0);
            e.Add(0x0003, 0x0035, p.X);
            e.Add(0x0003, 0x0036, p.Y);
            e.Add(0x0003, 0x003A, this.kPressure);
            e.Add(0x0000, 0x0002, 0);
            e.Add(0x0000, 0x0000, 0);
            return e;
        }

        public InputEvent Off(int no, TouchPoint[] touchPoints)
        {
            var e = new InputEvent();
            e.Add(0x0000, 0x0002, 0);
            e.Add(0x0000, 0x0000, 0);
            return e;
        }
    }

    class InputEvent
    {
        List<byte> data = new List<byte>();

        public void Add(ushort d1, ushort d2, int d3)
        {
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
