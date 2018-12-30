using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Suconbu.Mobile
{
    public class Battery
    {
        public enum StatusCode { Unknown = 1, Charging = 2, Discharging = 3, NotCharging = 4, Full = 5 }
        public enum HealthCode { Unknown = 1, Good = 2, OverHeat = 3, Dead = 4, OverVoltage = 5, UnspecifiedFailrue, Cold = 7 }

        public event EventHandler<SortedSet<string>> PropertyChanged = delegate { };

        public bool ACPowered
        {
            get { return (bool)this.propertyGroup[nameof(this.ACPowered)].Value; }
            set { this.SetAndPushValue(nameof(this.ACPowered), value); }
        }
        public bool UsbPowered
        {
            get { return (bool)this.propertyGroup[nameof(this.UsbPowered)].Value; }
            set { this.SetAndPushValue(nameof(this.UsbPowered), value); }
        }
        public bool WirelessPowered
        {
            get { return (bool)this.propertyGroup[nameof(this.WirelessPowered)].Value; }
            set { this.SetAndPushValue(nameof(this.WirelessPowered), value); }
        }
        // Numerator for charge level
        public int Level
        {
            get { return (int)this.propertyGroup[nameof(this.Level)].Value; }
            set { this.SetAndPushValue(nameof(this.Level), value); }
        }
        // Denominator for charge level
        public int Scale
        {
            get { return (int)this.propertyGroup[nameof(this.Scale)].Value; }
        }
        public StatusCode Status
        {
            get { return (StatusCode)this.propertyGroup[nameof(this.Status)].Value; }
            set { this.SetAndPushValue(nameof(this.Status), (int)value); }
        }
        public HealthCode Health
        {
            get { return (HealthCode)this.propertyGroup[nameof(this.Health)].Value; }
        }
        // mV
        public int Voltage
        {
            get { return (int)this.propertyGroup[nameof(this.Voltage)].Value; }
        }
        // Celsius x10
        public int Temperature
        {
            get { return (int)this.propertyGroup[nameof(this.Temperature)].Value; }
        }
        // Remaining battery capacity [nAh]
        public int ChargeCounter
        {
            get { return (int)this.propertyGroup[nameof(this.ChargeCounter)].Value; }
        }
        // e.g. Li-ion
        public string Technology
        {
            get { return (string)this.propertyGroup[nameof(this.Technology)].Value; }
        }

        readonly MobileDevice device;
        readonly PropertyGroup propertyGroup;

        public Battery(MobileDevice device, string xmlPath)
        {
            this.device = device;
            this.propertyGroup = PropertyGroup.FromXml(xmlPath);
        }

        /// <summary>
        /// すべてのプロパティを最新の値に更新します。
        /// </summary>
        public CommandContext PullAsync()
        {
            var changedPropertyNames = new SortedSet<string>();
            return this.device.RunCommandAsync("shell dumpsys battery", output =>
            {
                if (output == null)
                {
                    if (changedPropertyNames.Count > 0) this.PropertyChanged(this, changedPropertyNames);
                    return;
                }

                this.propertyGroup.Properties.ForEach(p =>
                {
                    var latest = p.Value?.ToString();
                    if(p.TrySetValueFromString(output.Trim()))
                    {
                        if (latest != p.Value?.ToString()) changedPropertyNames.Add(p.Name);
                    }
                });
            });
        }

        /// <summary>
        /// 指定されたプロパティを本来の値に戻します。
        /// プロパティ名を省略した時はすべてのプロパティが対象となります。
        /// </summary>
        public CommandContext ResetAsync(string propertyName = null)
        {
            if(string.IsNullOrEmpty(propertyName))
            {
                var property = this.propertyGroup.Properties.Find(p => p.Name == propertyName);
                if (property == null) return null;
                return property.ResetAsync(this.device);
            }
            else
            {
                return CommandContext.StartNew(() => this.propertyGroup.Properties.ForEach(p => p.ResetAsync(this.device).Wait()));
            }
        }

        void SetAndPushValue(string name, object value)
        {
            var property = this.propertyGroup[name];
            if(property != null && property?.Value?.ToString() != value?.ToString())
            {
                property.Value = value;
                property.PushAsync(this.device);
                this.PropertyChanged(this, new SortedSet<string>(new[] { property.Name }));
            }
        }
    }
}
