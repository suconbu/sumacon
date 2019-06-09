using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Suconbu.Sumacon
{
    public class Sumacon : IDisposable
    {
        public DeviceManager DeviceManager = new DeviceManager();
        public event Action<string> WriteConsoleRequested = delegate { };
        public event Func<Bitmap, string> SaveCapturedImageRequested = delegate { return null; };
        public event Action<PointF[]> ShowTouchMarkersRequested = delegate { };
        public ColorSet ColorSet = ColorSet.Light;

        public void WriteConsole(string s)
        {
            this.WriteConsoleRequested(s);
        }

        public string SaveCapturedImage(Bitmap bitmap)
        {
            return this.SaveCapturedImageRequested(bitmap);
        }

        public void ShowTouchMarkers(params PointF[] normalizedPoints)
        {
            this.ShowTouchMarkersRequested(normalizedPoints);
        }

        #region IDisposable Support
        bool disposed = false;

        public virtual void Dispose()
        {
            if (this.disposed) return;

            this.DeviceManager.Dispose();

            this.disposed = true;
        }
        #endregion
    }

    public static class Beep
    {
        public enum Note { Un, Pu = 440, Po = 880, Pe = 1760, Pi = 3520 }

        readonly static Dictionary<char, float> freq = new Dictionary<char, float>()
        {
            { 'C', 32.703f }, { 'D', 36.708f }, { 'E', 41.203f }, { 'F', 43.654f }, { 'G', 48.999f }, { 'A', 55.0f }, { 'B', 61.735f }
        };

        public static CommandContext Play(params Note[] notes)
        {
            return CommandContext.StartNew(() =>
            {
                foreach (var note in notes)
                {
                    if (note == Note.Un) Thread.Sleep(100);
                    else Console.Beep((int)note, 100);
                }
            });
        }

        public static CommandContext Play(params string[] notes)
        {
            return CommandContext.StartNew(() =>
            {
                foreach (var note in notes)
                {
                    if (string.IsNullOrEmpty(note)) Thread.Sleep(100);
                    else Console.Beep((int)(freq[char.ToUpper(note[0])] * Math.Pow(2, int.Parse(note[1].ToString())) - 1), 100);
                }
            });
        }
    }
}
