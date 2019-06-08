using SharpAdbClient;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;

namespace Suconbu.Mobile
{
    public class Device : IDisposable
    {
        public struct ComponentCategory
        {
            public const string System = "01.System";
            public const string Setting = "02.Setting";
            public const string Battery = "03.Battery";
            public const string Screen = "04.Screen";
            public const string Input = "05.Input";
        }

        public enum StatusCode { Unknown = 1, Charging = 2, Discharging = 3, NotCharging = 4, Full = 5 }
        public enum HealthCode { Unknown = 1, Good = 2, OverHeat = 3, Dead = 4, OverVoltage = 5, UnspecifiedFailrue, Cold = 7 }
        [Flags]
        public enum UpdatableProperties
        {
            ProcessInfo = 0x1,
            SystemComponent = 0x2, SettingComponent = 0x4, BatteryComponent = 0x08, ScreenComponent = 0x10, InputComponent = 0x20
            //Components = SystemComponent | SettingComponent | BatteryComponent | ScreenComponent,
            //All = Components | ProcessInfo
        }
        public const UpdatableProperties AllUpdatableProperties = UpdatableProperties.ProcessInfo | UpdatableProperties.SystemComponent | UpdatableProperties.SettingComponent | UpdatableProperties.BatteryComponent | UpdatableProperties.ScreenComponent | UpdatableProperties.InputComponent;

        public event EventHandler ProcessesChanged = delegate { };

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
        // e.g. 8
        [Category(ComponentCategory.System)]
        public int CpuCount { get { return (int)this.system[nameof(this.CpuCount)].Value; } }
        // e.g. 1508
        [Category(ComponentCategory.System), Description("(MHz)")]
        public int CpuClockMax { get { return (int)this.system[nameof(this.CpuClockMax)].Value / 1000; } }
        // e.g. 338
        [Category(ComponentCategory.System), Description("(MHz)")]
        public int CpuClockMin { get { return (int)this.system[nameof(this.CpuClockMin)].Value / 1000; } }
        // e.g. 1836
        [Category(ComponentCategory.System), Description("(MB)")]
        public int RAM { get { return (int)this.system[nameof(this.RAM)].Value / 1024; } }
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
        // e.g. 2019-01-01 12:34:56
        [Category(ComponentCategory.System)]
        public DateTime Date { get { return DateTime.TryParse((string)this.system[nameof(this.Date)].Value, out var date) ? date : DateTime.MinValue; } }
        // e.g. Asia/Tokyo
        [Category(ComponentCategory.System)]
        public string TimeZone { get { return (string)this.system[nameof(this.TimeZone)].Value; } }
        // e.g. 192.168.0.1
        [Category(ComponentCategory.System)]
        public string IpAddress { get { return (string)this.system[nameof(this.IpAddress)].Value; } }

        [Category(ComponentCategory.Setting)]
        public bool AirplaneMode { get { return (bool)this.setting[nameof(this.AirplaneMode)].Value; } set { this.setting.SetAndPushValue(nameof(this.AirplaneMode), value); } }
        [Category(ComponentCategory.Setting)]
        public bool ShowTouches { get { return (bool)this.setting[nameof(this.ShowTouches)].Value; } set { this.setting.SetAndPushValue(nameof(this.ShowTouches), value); } }
        [Category(ComponentCategory.Setting)]
        public float FontScale { get { return (float)this.setting[nameof(this.FontScale)].Value; } set { this.setting.SetAndPushValue(nameof(this.FontScale), value); } }

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

        [Category(ComponentCategory.Input)]
        public string TouchDevice { get => this.Input.TouchDevice; }
        [Category(ComponentCategory.Input)]
        public Point TouchMin { get => this.Input.TouchMin; }
        [Category(ComponentCategory.Input)]
        public Point TouchMax { get => this.Input.TouchMax; }

        [Browsable(false)]
        public IEnumerable<DeviceComponent> Components { get { return this.components.Values; } }
        [Browsable(false)]
        public Battery Battery { get; private set; }
        [Browsable(false)]
        public Screen Screen { get; private set; }
        [Browsable(false)]
        public Input Input { get; private set; }
        [Browsable(false)]
        public EntryCollection<int, ProcessEntry> Processes { get; private set; } = new EntryCollection<int, ProcessEntry>();
        [Browsable(false)]
        public bool HasWirelessConnection { get => (this.WirelessPort > 0); }
        [Browsable(false)]
        public int WirelessPort { get; private set; }
        [Browsable(false)]
        public bool ScreenIsUpright { get { return this.CurrentRotation == Screen.RotationCode.Protrait || this.CurrentRotation == Screen.RotationCode.ProtraitReversed; } }

        readonly DeviceData deviceData;
        CommandContext.NewLineMode newLineMode = CommandContext.NewLineMode.CrLf;
        readonly DeviceComponent system;
        readonly DeviceComponent setting;
        readonly Dictionary<UpdatableProperties, DeviceComponent> components = new Dictionary<UpdatableProperties, DeviceComponent>();
        readonly Dictionary<UpdatableProperties, List<Action>> propertyReadyChanged = new Dictionary<UpdatableProperties, List<Action>>();
        readonly Dictionary<UpdatableProperties, bool> propertyIsReady = new Dictionary<UpdatableProperties, bool>();

        public Device(string serial)
        {
            this.deviceData = AdbClient.Instance.GetDevices().Find(d => d.Serial == serial);

            var match = Regex.Match(serial, @"(?:\d+\.){3}\d+:(\d+)");
            if (match.Success)
            {
                this.WirelessPort = int.TryParse(match.Groups[1].Value, out var p) ? p : 0;
            }

            this.system = new DeviceComponent(this, "properties_system.xml");
            this.components.Add(UpdatableProperties.SystemComponent, this.system);
            this.setting = new DeviceComponent(this, "properties_setting.xml");
            this.components.Add(UpdatableProperties.SettingComponent, this.setting);
            this.Battery = new Battery(this, "properties_battery.xml");
            this.components.Add(UpdatableProperties.BatteryComponent, this.Battery);
            this.Screen = new Screen(this, "properties_screen.xml");
            this.components.Add(UpdatableProperties.ScreenComponent, this.Screen);
            this.Input = new Input(this, "properties_input.xml");
            this.components.Add(UpdatableProperties.InputComponent, this.Input);

            this.RunCommandOutputBinaryAsync("shell echo \\\\r", stream =>
            {
                this.newLineMode = (stream.Length == 3) ? CommandContext.NewLineMode.CrCrLf : CommandContext.NewLineMode.CrLf;
            });

            foreach (UpdatableProperties p in Enum.GetValues(typeof(UpdatableProperties)))
            {
                this.propertyIsReady[p] = false;
                this.propertyReadyChanged[p] = new List<Action>();
            }
        }

        public DeviceComponent GetComponent(string category)
        {
            foreach(var component in this.components.Values)
            {
                if (component.Name == category) return component;
            }
            return null;
        }

        public CommandContext UpdatePropertiesAsync(UpdatableProperties properties = Device.AllUpdatableProperties, Action onFinished = null)
        {
            CommandContext processInfoContext = null;
            if (properties.HasFlag(UpdatableProperties.ProcessInfo))
            {
                processInfoContext = ProcessEntry.GetAsync(this, p =>
                {
                    this.Processes = p;
                    this.InvokeReadyHandlers(UpdatableProperties.ProcessInfo);
                    this.ProcessesChanged(this, EventArgs.Empty);
                });
            }

            var componentContexts = new List<CommandContext>();
            foreach (var component in this.components)
            {
                if (properties.HasFlag(component.Key))
                {
                    componentContexts.Add(component.Value.PullAsync());
                }
            }

            return CommandContext.StartNew(() =>
            {
                componentContexts.ForEach(c => c.Wait());
                foreach (var component in this.components)
                {
                    if(properties.HasFlag(component.Key))
                    {
                        this.InvokeReadyHandlers(component.Key);
                    }
                }
                //if (properties.HasFlag(UpdatableProperties.Components))
                //{
                //    this.InvokeReadyHandlers(UpdatableProperties.Components);
                //}
                processInfoContext?.Wait();
                onFinished?.Invoke();
            });
        }

        public void InvokeIfPropertyIsReady(UpdatableProperties properties, Action onReady)
        {
            if (onReady == null) throw new ArgumentNullException(nameof(onReady));
            this.PushReadyHandler(properties, onReady);
        }
        //public void InvokeIfProcessInfosIsReady(Action onReady)
        //{
        //    this.PushReadyHandler(UpdatableProperties.ProcessInfo, onReady);
        //}

        //public void InvokeIfComponentsIsReady(Action onReady)
        //{
        //    this.PushReadyHandler(UpdatableProperties.Components, onReady);
        //}

        public CommandContext RunCommandAsync(string command, Action<string> onOutputReceived = null, Action<string> onErrorReceived = null)
        {
            return CommandContext.StartNew("adb", $"-s {this.Serial} {command}", onOutputReceived, onErrorReceived);
        }

        public CommandContext RunCommandOutputTextAsync(string command, Action<string, string> onFinished)
        {
            return CommandContext.StartNewText("adb", $"-s {this.Serial} {command}", (output, error) => onFinished?.Invoke(output, error));
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

        void PushReadyHandler(UpdatableProperties updatableProperty, Action onReady)
        {
            Debug.Assert(onReady != null);
            bool deferred = false;
            lock (this.propertyReadyChanged)
            {
                //if (!this.propertyReadyChanged.TryGetValue(updatableProperty, out var handlers))
                //{
                //    handlers = new List<Action>();
                //    this.propertyReadyChanged[updatableProperty] = handlers;
                //}
                foreach(UpdatableProperties p in Enum.GetValues(typeof(UpdatableProperties)))
                {
                    if(!this.propertyIsReady[p])
                    {
                        // まだ準備中なので整ってから呼ぶ
                        this.propertyReadyChanged[p].Add(onReady);
                        deferred = true;
                    }
                }
            }
            if (!deferred) onReady.Invoke();
        }

        void InvokeReadyHandlers(UpdatableProperties updatableProperty)
        {
            if (!this.propertyIsReady[updatableProperty])
            {
                List<Action> copiedHandlers = new List<Action>();
                lock (this.propertyReadyChanged)
                {
                    this.propertyIsReady[updatableProperty] = true;
                    if (this.propertyReadyChanged.TryGetValue(updatableProperty, out var handlers))
                    {
                        copiedHandlers.AddRange(handlers);
                        handlers.Clear();
                    }
                }
                copiedHandlers.ForEach(handler => handler());
            }
        }

        #region IDisposable Support
        bool disposed = false;

        public virtual void Dispose()
        {
            if (this.disposed) return;
            if(this.HasWirelessConnection)
            {
                // Disconnect wireless connection for close listening_port.
                this.RunCommandAsync("usb");
            }
            this.disposed = true;
        }
        #endregion
    }
}
