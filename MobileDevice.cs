using SharpAdbClient;
using Suconbu.Toolbox;
using System;
using System.Drawing;
using System.IO;

namespace Suconbu.Mobile
{
    class MobileDevice : IDisposable
    {
        // e.g. HXC8KSKL24PZB
        public string Id { get { return this.deviceData.Serial; } }
        // e.g. Nexus_9
        public string Model { get { return this.deviceData.Model; } }
        // e.g. MyTablet
        public string Name { get { return this.deviceData.Name; } }

        DeviceData deviceData;

        public MobileDevice(string id)
        {
            this.deviceData = AdbClient.Instance.GetDevices().Find(d => d.Serial == id);
        }

        public CommandContext GetScreenCaptureAsync(Action<Image> captured)
        {
            return this.RunCommandOutputBinaryAsync("shell screencap -p", stream =>
            {
                var image = Bitmap.FromStream(stream);
                captured?.Invoke(image);
            });
        }

        public CommandContext RunCommandAsync(string command, Action<string> onOutputReceived = null, Action<string> onErrorReceived = null)
        {
            return CommandContext.StartNew("adb", $"-s {this.Id} {command}", onOutputReceived, onErrorReceived);
        }

        public CommandContext RunCommandOutputTextAsync(string command, Action<string> onFinished)
        {
            return CommandContext.StartNewText("adb", $"-s {this.Id} {command}", output => onFinished?.Invoke(output));
        }

        public CommandContext RunCommandOutputBinaryAsync(string command, Action<Stream> onFinished)
        {
            return CommandContext.StartNewBinary("adb", $"-s {this.Id} {command}", stream => onFinished?.Invoke(stream));
        }

        #region IDisposable Support
        bool disposed = false;

        public virtual void Dispose()
        {
            if (this.disposed) return;

            //

            this.disposed = true;
        }
        #endregion
    }
}
