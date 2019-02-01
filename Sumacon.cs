using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Suconbu.Sumacon
{
    public class Sumacon : IDisposable
    {
        public DeviceManager DeviceManager = new DeviceManager();
        public CommandReceiver CommandReceiver = new CommandReceiver();

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
}
