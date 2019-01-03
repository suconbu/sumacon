using SharpAdbClient;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace Suconbu.Mobile
{
    public class Device : IDisposable
    {
        public struct ComponentCategory
        {
            public const string Device = "01.Device";
            public const string Battery = "02.Battery";
            public const string Screen = "03.Screen";
        }

        // e.g. HXC8KSKL24PZB
        [Category(ComponentCategory.Device)]
        public string Id { get { return this.deviceData.Serial; } }
        // e.g. Nexus_9
        [Category(ComponentCategory.Device)]
        public string Model { get { return this.deviceData.Model; } }
        // e.g. MyTablet
        [Category(ComponentCategory.Device)]
        public string Name { get { return this.deviceData.Name; } }

        [Category(ComponentCategory.Battery)]
        public bool ACPowered { get { return this.Battery.ACPowered; } set { this.Battery.ACPowered = value; } }
        [Category(ComponentCategory.Battery)]
        public bool UsbPowered { get { return this.Battery.UsbPowered; } set { this.Battery.UsbPowered = value; } }
        [Category(ComponentCategory.Battery)]
        public bool WirelessPowered { get { return this.Battery.WirelessPowered; } set { this.Battery.WirelessPowered = value; } }
        [Category(ComponentCategory.Battery), Description("0-100")]
        public float ChargeLevel { get { return 100.0f * this.Battery.Level / this.Battery.Scale; } set { this.Battery.Level = (int)(value / 100.0f * this.Battery.Scale); } }
        [Category(ComponentCategory.Battery)]
        public Battery.StatusCode Status { get { return this.Battery.Status; } set { this.Battery.Status = value; } }
        [Category(ComponentCategory.Battery)]
        public Battery.HealthCode Health { get { return this.Battery.Health; } }
        [Category(ComponentCategory.Battery), Description("[V]")]
        public float Voltage { get { return this.Battery.Voltage / 1000.0f; } }
        [Category(ComponentCategory.Battery), Description("[℃]")]
        public float Temperature { get { return this.Battery.Temperature / 10.0f; } }
        [Category(ComponentCategory.Battery)]
        public int ChargeCounter { get { return this.Battery.ChargeCounter; } }
        [Category(ComponentCategory.Battery)]
        public string Technology { get { return this.Battery.Technology; } }

        [Category(ComponentCategory.Screen)]
        public Size ScreenSize { get { return this.Screen.Size; } set { this.Screen.Size = value; } }
        [Category(ComponentCategory.Screen)]
        public int ScreenDensity { get { return this.Screen.Density; } set { this.Screen.Density = value; } }
        [Category(ComponentCategory.Screen), Description("0-255")]
        public int Brightness { get { return this.Screen.Brightness; } set { this.Screen.Brightness = value; } }
        [Category(ComponentCategory.Screen)]
        public bool AutoRotate { get { return this.Screen.AutoRotate; } set { this.Screen.AutoRotate = value; } }
        [Category(ComponentCategory.Screen)]
        public Screen.RotationCode UserRotation { get { return this.Screen.UserRotation; } set { this.Screen.UserRotation = value; } }
        [Category(ComponentCategory.Screen)]
        public Screen.RotationCode CurrentRotation { get { return this.Screen.CurrentRotation; } }
        [Category(ComponentCategory.Screen), Description("[s]")]
        public int OffTimeout { get { return this.Screen.OffTimeout / 1000; } set { this.Screen.OffTimeout = value * 1000; } }

        [Browsable(false)]
        public Battery Battery { get; private set; }
        [Browsable(false)]
        public Screen Screen { get; private set; }
        [Browsable(false)]
        public IReadOnlyList<DeviceComponentBase> Components;
        [Browsable(false)]
        public Dictionary<string, DeviceComponentBase> ComponentsByCategory = new Dictionary<string, DeviceComponentBase>();
        [Browsable(false)]
        public bool ObserveActivated { get; private set; }

        DeviceData deviceData;
        int observeIntervalMilliseconds = 1000;
        string timeoutId;

        public Device(string id)
        {
            this.deviceData = AdbClient.Instance.GetDevices().Find(d => d.Serial == id);
            this.Battery = new Battery(this, "properties_battery.xml");
            this.Screen = new Screen(this, "properties_screen.xml");
            this.Components = new List<DeviceComponentBase>() { this.Battery, this.Screen };
            this.ComponentsByCategory[ComponentCategory.Device] = null;
            this.ComponentsByCategory[ComponentCategory.Battery] = this.Battery;
            this.ComponentsByCategory[ComponentCategory.Screen] = this.Screen;
        }

        public void StartObserve(int intervalMilliseconds, bool imidiate = true)
        {
            this.observeIntervalMilliseconds = intervalMilliseconds;
            if (this.ObserveActivated)
            {
                this.StopObserve();
            }
            this.ObserveActivated = true;
            this.timeoutId = Delay.SetTimeout(this.TimerElapsed, imidiate ? 1 : this.observeIntervalMilliseconds);
        }

        public void StopObserve()
        {
            this.ObserveActivated = false;
            Delay.ClearTimeout(this.timeoutId);
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

        public string ToString(string format)
        {
            var replacer = new Dictionary<string, string>()
            {
                { "device-id", this.Id},
                { "device-model", this.Model},
                { "device-name", this.Name},
                { "screen-width", this.ScreenSize.Width.ToString()},
                { "screen-height", this.ScreenSize.Height.ToString()},
                { "screen-density", this.ScreenDensity.ToString()},
                { "battery-level", ((int)this.ChargeLevel).ToString()},
                { "battery-status", this.Status.ToString() }
            };
            return format.Replace(replacer);
        }

        void TimerElapsed()
        {
            Parallel.ForEach(this.Components, component => component.PullAsync().Wait());
            if (!this.ObserveActivated) return;
            this.timeoutId = Delay.SetTimeout(this.TimerElapsed, this.observeIntervalMilliseconds);
        }

        #region IDisposable Support
        bool disposed = false;

        public virtual void Dispose()
        {
            if (this.disposed) return;

            foreach (var component in this.Components)
            {
                component.ResetAsync();
            }

            this.disposed = true;
        }
        #endregion
    }
}
