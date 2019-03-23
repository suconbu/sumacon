using SharpAdbClient;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace Suconbu.Mobile
{
    public class Device : IDisposable
    {
        public struct ComponentCategory
        {
            public const string System = "01.System";
            public const string Battery = "02.Battery";
            public const string Screen = "03.Screen";
        }

        public enum StatusCode { Unknown = 1, Charging = 2, Discharging = 3, NotCharging = 4, Full = 5 }
        public enum HealthCode { Unknown = 1, Good = 2, OverHeat = 3, Dead = 4, OverVoltage = 5, UnspecifiedFailrue, Cold = 7 }

        // e.g. HXC8KSKL24PZB
        [Category(ComponentCategory.System)]
        public string Serial { get { return this.deviceData.Serial; } }
        // e.g. Nexus_9
        [Category(ComponentCategory.System)]
        public string Model { get { return this.deviceData.Model; } }
        // e.g. MyTablet
        [Category(ComponentCategory.System)]
        public string Name { get { return this.deviceData.Name; } }
        // e.g. google
        [Category(ComponentCategory.System)]
        public string Brand { get { return (string)this.system[nameof(this.Brand)].Value; } }
        // e.g. volantis
        [Category(ComponentCategory.System)]
        public string DeviceName { get { return (string)this.system[nameof(this.DeviceName)].Value; } }
        // e.g. htc
        [Category(ComponentCategory.System)]
        public string Manufacturer { get { return (string)this.system[nameof(this.Manufacturer)].Value; } }
        // e.g. tegra132
        [Category(ComponentCategory.System)]
        public string Platform { get { return (string)this.system[nameof(this.Platform)].Value; } }
        // e.g. arm64-v8a
        [Category(ComponentCategory.System)]
        public string CpuAbi { get { return (string)this.system[nameof(this.CpuAbi)].Value; } }
        // e.g. 7.1.1
        [Category(ComponentCategory.System)]
        public string AndroidVersion { get { return (string)this.system[nameof(this.AndroidVersion)].Value; } }
        // e.g. 25
        [Category(ComponentCategory.System)]
        public int ApiLevel { get { return (int)this.system[nameof(this.ApiLevel)].Value; } }
        // e.g. 3.1
        [Category(ComponentCategory.System)]
        public string OpenGLES
        {
            get
            {
                var v = (int)this.system[nameof(this.OpenGLES)].Value;
                return $"{v / 0x10000}.{v % 0x10000}";
            }
        }
        // e.g. Asia/Tokyo
        [Category(ComponentCategory.System)]
        public string TimeZone { get { return (string)this.system[nameof(this.TimeZone)].Value; } }
        [Category(ComponentCategory.System)]
        public bool AirplaneMode { get { return (bool)this.system[nameof(this.AirplaneMode)].Value; } set { this.system.SetAndPushValue(nameof(this.AirplaneMode), value); } }
        [Category(ComponentCategory.System)]
        public bool ShowTouches { get { return (bool)this.system[nameof(this.ShowTouches)].Value; } set { this.system.SetAndPushValue(nameof(this.ShowTouches), value); } }
        [Category(ComponentCategory.System)]
        public float FontScale { get { return (float)this.system[nameof(this.FontScale)].Value; } set { this.system.SetAndPushValue(nameof(this.FontScale), value); } }

        [Category(ComponentCategory.Battery)]
        public bool ACPowered { get { return this.Battery.ACPowered; } set { this.Battery.ACPowered = value; } }
        [Category(ComponentCategory.Battery)]
        public bool UsbPowered { get { return this.Battery.UsbPowered; } set { this.Battery.UsbPowered = value; } }
        [Category(ComponentCategory.Battery)]
        public bool WirelessPowered { get { return this.Battery.WirelessPowered; } set { this.Battery.WirelessPowered = value; } }
        [Category(ComponentCategory.Battery), Description("0-100")]
        public float BatteryLevel { get { return (this.Battery.Scale != 0.0f) ? (100.0f * this.Battery.Level / this.Battery.Scale) : 0.0f; } set { this.Battery.Level = (int)(value / 100.0f * this.Battery.Scale); } }
        [Category(ComponentCategory.Battery)]
        public Battery.StatusCode Status { get { return this.Battery.Status; } set { this.Battery.Status = value; } }
        [Category(ComponentCategory.Battery)]
        public Battery.HealthCode Health { get { return this.Battery.Health; } }
        [Category(ComponentCategory.Battery), Description("(V)")]
        public float Voltage { get { return this.Battery.Voltage / 1000.0f; } }
        [Category(ComponentCategory.Battery), Description("(℃)")]
        public float Temperature { get { return this.Battery.Temperature / 10.0f; } }
        [Category(ComponentCategory.Battery)]
        public int ChargeCounter { get { return this.Battery.ChargeCounter; } }
        [Category(ComponentCategory.Battery)]
        public string Technology { get { return this.Battery.Technology; } }

        [Category(ComponentCategory.Screen)]
        public Size ScreenRealSize { get { return this.Screen.RealSize; } }
        [Category(ComponentCategory.Screen)]
        public Size ScreenSize { get { return this.Screen.Size; } set { this.Screen.Size = value; } }
        [Category(ComponentCategory.Screen)]
        public int ScreenRealDensity { get { return this.Screen.RealDensity; } }
        [Category(ComponentCategory.Screen)]
        public int ScreenDensity { get { return this.Screen.Density; } set { this.Screen.Density = value; } }
        [Category(ComponentCategory.Screen)]
        public string ScreenDensityClass { get { return this.Screen.DensityClass; } }
        [Category(ComponentCategory.Screen)]
        public Size PhysicalScreenDpi { get { return this.Screen.Dpi; } }
        [Category(ComponentCategory.Screen), Description("Width/Height (inch)")]
        public SizeF PhysicalScreenSize { get { return new SizeF((float)this.Screen.RealSize.Width / this.Screen.Dpi.Width, (float)this.Screen.RealSize.Height / this.Screen.Dpi.Height); } }
        [Category(ComponentCategory.Screen), Description("Diagonal length (inch)")]
        public float PhysicalScreenLength { get { return (float)Math.Sqrt(this.PhysicalScreenSize.Width * this.PhysicalScreenSize.Width + this.PhysicalScreenSize.Height * this.PhysicalScreenSize.Height); } }
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
        public IEnumerable<DeviceComponent> Components { get { return this.componentsByCategory.Values; } }
        [Browsable(false)]
        public Battery Battery { get; private set; }
        [Browsable(false)]
        public Screen Screen { get; private set; }
        [Browsable(false)]
        public bool ObserveActivated { get; private set; }
        [Browsable(false)]
        public ProcessInfoCollection ProcessInfos { get; private set; }

        DeviceData deviceData;
        int observeIntervalMilliseconds;
        string timeoutId;
        CommandContext.NewLineMode newLineMode = CommandContext.NewLineMode.CrLf;
        DeviceComponent system;
        Dictionary<string, DeviceComponent> componentsByCategory = new Dictionary<string, DeviceComponent>();

        public Device(string id)
        {
            this.deviceData = AdbClient.Instance.GetDevices().Find(d => d.Serial == id);
            this.system = new DeviceComponent(this, "properties_system.xml");
            this.componentsByCategory.Add(ComponentCategory.System, this.system);
            this.Battery = new Battery(this, "properties_battery.xml");
            this.componentsByCategory.Add(ComponentCategory.Battery, this.Battery);
            this.Screen = new Screen(this, "properties_screen.xml");
            this.componentsByCategory.Add(ComponentCategory.Screen, this.Screen);
            this.RunCommandOutputBinaryAsync("shell echo \\\\r", stream =>
            {
                this.newLineMode = (stream.Length == 3) ? CommandContext.NewLineMode.CrCrLf : CommandContext.NewLineMode.CrLf;
            });
            //ProcessInfoList.GetAsync(this, processes => this.Processes = processes).Wait();
        }

        public DeviceComponent GetComponent(string category)
        {
            return this.componentsByCategory[category];
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

        public CommandContext UpdatePropertiesAsync(Action onFinished)
        {
            var contexts = new List<CommandContext>();
            foreach (var component in this.Components)
            {
                contexts.Add(component.PullAsync());
            }
            return CommandContext.StartNew(() =>
            {
                contexts.ForEach(c => c.Wait());
                ProcessInfo.GetAsync(this, p => this.ProcessInfos = p).Wait();
                onFinished?.Invoke();
            });
        }

        public CommandContext RunCommandAsync(string command, Action<string> onOutputReceived = null, Action<string> onErrorReceived = null)
        {
            return CommandContext.StartNew("adb", $"-s {this.Serial} {command}", onOutputReceived, onErrorReceived);
        }

        public CommandContext RunCommandOutputTextAsync(string command, Action<string> onFinished)
        {
            return CommandContext.StartNewText("adb", $"-s {this.Serial} {command}", output => onFinished?.Invoke(output));
        }

        public CommandContext RunCommandOutputBinaryAsync(string command, Action<Stream> onFinished)
        {
            return CommandContext.StartNewBinary("adb", $"-s {this.Serial} {command}", this.newLineMode, stream => onFinished?.Invoke(stream));
        }

        public string ToString(string format)
        {
            var replacer = new Dictionary<string, string>()
            {
                { "device-serial", this.Serial},
                { "device-model", this.Model},
                { "device-name", this.Name},
                { "screen-width", this.ScreenSize.Width.ToString()},
                { "screen-height", this.ScreenSize.Height.ToString()},
                { "screen-density", this.ScreenDensity.ToString()},
                { "battery-level", ((int)this.BatteryLevel).ToString()},
                { "battery-status", this.Status.ToString() }
            };
            return format.Replace(replacer);
        }

        void TimerElapsed()
        {
            this.UpdatePropertiesAsync(() =>
            {
                if (this.ObserveActivated)
                {
                    this.timeoutId = Delay.SetTimeout(this.TimerElapsed, this.observeIntervalMilliseconds);
                }
            });
        }

        #region IDisposable Support
        bool disposed = false;

        public virtual void Dispose()
        {
            if (this.disposed) return;
            this.disposed = true;
        }
        #endregion
    }
}
