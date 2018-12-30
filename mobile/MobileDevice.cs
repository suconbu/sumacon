using SharpAdbClient;
using Suconbu.Toolbox;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Timers;

namespace Suconbu.Mobile
{
    public class MobileDevice : IDisposable
    {
        // e.g. HXC8KSKL24PZB
        [Category("Device")]
        public string Id { get { return this.deviceData.Serial; } }
        // e.g. Nexus_9
        [Category("Device")]
        public string Model { get { return this.deviceData.Model; } }
        // e.g. MyTablet
        [Category("Device")]
        public string Name { get { return this.deviceData.Name; } }

        [Category("Battery")]
        public bool ACPowered { get { return this.Battery.ACPowered; } set { this.Battery.ACPowered = value; } }
        [Category("Battery")]
        public bool UsbPowered { get { return this.Battery.UsbPowered; } set { this.Battery.UsbPowered = value; } }
        [Category("Battery")]
        public bool WirelessPowered { get { return this.Battery.WirelessPowered; } set { this.Battery.WirelessPowered = value; } }
        [Category("Battery")]
        public float Level { get { return this.Battery.Level / this.Battery.Scale * 100.0f; } set { this.Battery.Level = (int)(value / 100.0f * this.Battery.Scale); } }
        [Category("Battery")]
        public Battery.StatusCode Status { get { return this.Battery.Status; } set { this.Battery.Status = value; } }
        [Category("Battery")]
        public Battery.HealthCode Health { get { return this.Battery.Health; } }
        [Category("Battery")]
        public float Voltage { get { return this.Battery.Voltage / 1000.0f; } }
        [Category("Battery")]
        public float Temperature { get { return this.Battery.Temperature / 10.0f; } }
        [Category("Battery")]
        public int ChargeCounter { get { return this.Battery.ChargeCounter; } }
        [Category("Battery")]
        public string Technology { get { return this.Battery.Technology; } }

        [Browsable(false)]
        public Battery Battery { get; private set; }

        DeviceData deviceData;
        Timer observeTimer = new Timer();

        public MobileDevice(string id, int observeIntervalMilliseconds = 0)
        {
            this.deviceData = AdbClient.Instance.GetDevices().Find(d => d.Serial == id);
            this.Battery = new Battery(this, "properties_battery.xml");
            this.observeTimer.Interval = 1;
            this.observeTimer.Elapsed += (s, e) =>
            {
                this.observeTimer.Interval = observeIntervalMilliseconds;
                this.Battery.PullAsync();
            };
            if (observeIntervalMilliseconds > 0) this.observeTimer.Start();
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

            this.observeTimer.Stop();
            this.Battery.ResetAsync();

            this.disposed = true;
        }
        #endregion
    }
}
