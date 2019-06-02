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
        public event EventHandler<string> WriteConsoleRequested = delegate { };
        public event EventHandler<Bitmap> SaveCapturedImageRequested = delegate { };

        public void WriteConsole(string s)
        {
            this.WriteConsoleRequested(this, s);
        }

        public void SaveCapturedImage(Bitmap bitmap)
        {
            this.SaveCapturedImageRequested(this, bitmap);
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
    }
}
