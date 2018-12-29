using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Suconbu.Mobile
{
    class Battery
    {
        // https://developer.android.com/reference/android/os/BatteryManager.html
        [Flags]
        public enum PowerSources { AC = 0x1, Usb = 0x2, Wireless = 0x4 }
        public enum StatusCode { Unknown = 1, Charging = 2, Discharging = 3, NotCharging = 4, Full = 5 }
        public enum HealthCode { Unknown = 1, Good = 2, OverHeat = 3, Dead = 4, OverVoltage = 5, UnspecifiedFailrue, Cold = 7 }
        [Flags]
        public enum SettableProperties { Powered = 0x1, Level = 0x2, Status = 0x4 }

        public event EventHandler<SortedSet<string>> PropertyChanged = delegate { };

        // Setttable properties

        public bool ACPowered
        {
            get { return this.powerSources.HasFlag(PowerSources.AC); }
            set { this.ChangePowerSouceState(PowerSources.AC, value); }
        }
        public bool UsbPowered
        {
            get { return this.powerSources.HasFlag(PowerSources.Usb); }
            set { this.ChangePowerSouceState(PowerSources.Usb, value); }
        }
        public bool WirelessPowered
        {
            get { return this.powerSources.HasFlag(PowerSources.Wireless); }
            set { this.ChangePowerSouceState(PowerSources.Wireless, value); }
        }
        // 0-100
        public float Level
        {
            get { return this.level; }
            set { this.ChangeLevel(value); }
        }
        public StatusCode Status
        {
            get { return this.status; }
            set { this.ChangeStatus(value); }
        }

        // Readonly propertyes

        public HealthCode Health { get; set; } = HealthCode.Unknown;
        // V
        public float Voltage { get; set; }
        // Celsius
        public float Temperature { get; private set; }
        // Remaining battery capacity [Ah]
        public float Capacity { get; private set; }
        // Remaining battery capacity [Ah]
        public float FullCapacity { get { return this.Capacity * 100.0f / this.Level; } }
        // e.g. Li-ion
        public string Technology { get; private set; } = string.Empty;

        readonly MobileDevice device;
        readonly string commandPrefix = "shell dumpsys battery";
        PowerSources powerSources = 0;
        StatusCode status = StatusCode.Unknown;
        float level = 0.0f;

        public Battery(MobileDevice device)
        {
            this.device = device;

            this.PullAsync();
            //this.ACPowered = true;
            //this.UsbPowered = true;
            //this.WirelessPowered = true;
            //this.Level = 50;
            //this.Status = StatusCode.Discharging;
            //this.Reset();
            //this.Reset(SettableProperties.ChargedBy);
            //this.Reset(SettableProperties.Level);
            //this.Reset(SettableProperties.Status);
        }

        /// <summary>
        /// すべてのプロパティを最新の値に更新します。
        /// </summary>
        public CommandContext PullAsync()
        {
            var changedPropertyNames = new SortedSet<string>();
            int levelValue = 0;
            return this.device.RunCommandAsync(this.commandPrefix, output =>
            {
                if (output == null)
                {
                    if (changedPropertyNames.Count > 0)
                    {
                        this.PropertyChanged(this, changedPropertyNames);
                    }
                    return;
                }

                var tokens = output.Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != 2) return;

                var item = tokens[0].Trim();
                var value = tokens[1].Trim();
                bool.TryParse(value, out var boolValue);
                int.TryParse(value, out var intValue);

                try
                {
                    if (item == "AC powered")
                    {
                        if (this.ChangePowerSouceState(PowerSources.AC, boolValue, false)) changedPropertyNames.Add(nameof(SettableProperties.Powered));
                    }
                    else if (item == "USB powered")
                    {
                        if (this.ChangePowerSouceState(PowerSources.Usb, boolValue, false)) changedPropertyNames.Add(nameof(SettableProperties.Powered));
                    }
                    else if (item == "Wireless powered")
                    {
                        if (this.ChangePowerSouceState(PowerSources.Wireless, boolValue, false)) changedPropertyNames.Add(nameof(SettableProperties.Powered));
                    }
                    else if (item == "Charge counter")
                    {
                        var newCapacity = intValue / 1000.0f / 1000.0f;
                        if (this.Capacity != newCapacity) changedPropertyNames.Add(nameof(this.Capacity));
                        this.Capacity = newCapacity;
                    }
                    else if (item == "status")
                    {
                        if (this.ChangeStatus((StatusCode)Enum.ToObject(typeof(StatusCode), intValue), false)) changedPropertyNames.Add(nameof(SettableProperties.Status));
                    }
                    else if (item == "health")
                    {
                        var newHealth = (HealthCode)Enum.ToObject(typeof(HealthCode), intValue);
                        if(this.Health != newHealth) changedPropertyNames.Add(nameof(this.Health));
                        this.Health = newHealth;
                    }
                    else if (item == "level")
                    {
                        levelValue = intValue;
                    }
                    else if (item == "scale")
                    {
                        if (this.ChangeLevel(100.0f * levelValue / intValue, false)) changedPropertyNames.Add(nameof(SettableProperties.Level));
                    }
                    else if (item == "voltage")
                    {
                        var newVoltage = intValue / 1000.0f;
                        if (this.Voltage != newVoltage) changedPropertyNames.Add(nameof(this.Voltage));
                        this.Voltage = newVoltage;
                    }
                    else if (item == "temperature")
                    {
                        var newTemperature = intValue / 10.0f;
                        if (this.Temperature != newTemperature) changedPropertyNames.Add(nameof(this.Temperature));
                        this.Temperature = newTemperature;
                    }
                    else if (item == "technology")
                    {
                        var newTechnology = value;
                        if (this.Technology != value) changedPropertyNames.Add(nameof(this.Technology));
                        this.Technology = value;
                    }
                    else
                    {
                        ;
                    }
                }
                catch (InvalidCastException ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            });
        }

        /// <summary>
        /// 指定されたプロパティを本来の値に戻します。
        /// adb shell dumpsys battery reset *
        /// </summary>
        public void Reset(SettableProperties properties = 0)
        {
            if (properties == 0)
            {
                this.device.RunCommandAsync($"{this.commandPrefix} reset");
            }

            foreach (SettableProperties property in Enum.GetValues(typeof(SettableProperties)))
            {
                if(properties.HasFlag(property))
                {
                    if(property == SettableProperties.Powered)
                    {
                        foreach(var name in Enum.GetNames(typeof(PowerSources)))
                        {
                            this.device.RunCommandAsync($"{this.commandPrefix} reset {name.ToLower()}");
                        }
                    }
                    else
                    {
                        this.device.RunCommandAsync($"{this.commandPrefix} reset {property.ToString().ToLower()}");
                    }
                }
            }
        }

        /// <summary>
        /// すべての電力供給源を取り外したことにします。
        /// </summary>
        public void Unplug()
        {
            this.ChangePowerSources(0);
        }

        bool ChangePowerSouceState(PowerSources powerSources, bool plugged, bool push = true)
        {
            var newPowerSources = plugged ?
                this.powerSources | powerSources :
                this.powerSources & ~powerSources;
            return this.ChangePowerSources(newPowerSources, push);
        }

        bool ChangePowerSources(PowerSources newPowerSources, bool push = true)
        {
            if (this.powerSources == newPowerSources) return false;

            // 変化のあったものだけ更新
            foreach(PowerSources powerSource in Enum.GetValues(typeof(PowerSources)))
            {
                var newState = newPowerSources.HasFlag(powerSource);
                if (newState != this.powerSources.HasFlag(powerSource) && push)
                {
                    var name = powerSource.ToString().ToLower();
                    var value = newState ? 1 : 0;
                    this.device.RunCommandAsync($"{this.commandPrefix} set {name} {value}");
                }
            }
            this.powerSources = newPowerSources;
            if (push) this.PropertyChanged(this, new SortedSet<string>(new[] { nameof(SettableProperties.Powered) }));
            return true;
        }

        bool ChangeLevel(float newLevel, bool push = true)
        {
            if (this.level == newLevel) return false;

            if (push)
            {
                this.device.RunCommandAsync($"{this.commandPrefix} set level {(int)newLevel}");
                this.PropertyChanged(this, new SortedSet<string>(new[] { nameof(SettableProperties.Level) }));
            }
            this.level = newLevel;
            return true;
        }

        bool ChangeStatus(StatusCode newStatus, bool push = true)
        {
            if (this.status == newStatus) return false;

            if (push)
            {
                this.device.RunCommandAsync($"{this.commandPrefix} set status {(int)newStatus}");
                this.PropertyChanged(this, new SortedSet<string>(new[] { nameof(SettableProperties.Status) }));
            }
            this.status = newStatus;
            return true;
        }
    }
}
