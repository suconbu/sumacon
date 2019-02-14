using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace Suconbu.Mobile
{
    public class Screen : DeviceComponentBase
    {
        public enum RotationCode { Protrait = 0, Landscape = 1, ProtraitReversed = 2, LandscapeReversed = 3 };

        public Size RealSize
        {
            get { return (Size)this.propertyGroup[nameof(this.RealSize)].Value; }
            set { this.SetAndPushValue(nameof(this.RealSize), value); }
        }
        public Size Size
        {
            get { return (Size)this.propertyGroup[nameof(this.Size)].Value; }
            set { this.SetAndPushValue(nameof(this.Size), value); }
        }
        public int RealDensity
        {
            get { return (int)this.propertyGroup[nameof(this.RealDensity)].Value; }
            set { this.SetAndPushValue(nameof(this.RealDensity), value); }
        }
        public int Density
        {
            get { return (int)this.propertyGroup[nameof(this.Density)].Value; }
            set { this.SetAndPushValue(nameof(this.Density), value); }
        }
        public string DensityClass
        {
            get { return this.GetDensityClass(this.Density); }
        }
        public Size Dpi
        {
            get { return (Size)this.propertyGroup[nameof(this.Dpi)].Value; }
            set { this.SetAndPushValue(nameof(this.Dpi), value); }
        }
        public int Brightness
        {
            get { return (int)this.propertyGroup[nameof(this.Brightness)].Value; }
            set { this.SetAndPushValue(nameof(this.Brightness), value); }
        }
        public bool AutoRotate
        {
            get { return (int)this.propertyGroup[nameof(this.AutoRotate)].Value != 0; }
            set { this.SetAndPushValue(nameof(this.AutoRotate), value ? 1 : 0); }
        }
        public RotationCode UserRotation
        {
            get { return (RotationCode)this.propertyGroup[nameof(this.UserRotation)].Value; }
            set { this.SetAndPushValue(nameof(this.UserRotation), (int)value); }
        }
        public RotationCode CurrentRotation
        {
            get { return (RotationCode)this.propertyGroup[nameof(this.CurrentRotation)].Value; }
            set { this.SetAndPushValue(nameof(this.CurrentRotation), (int)value); }
        }
        public int OffTimeout
        {
            get { return (int)this.propertyGroup[nameof(this.OffTimeout)].Value; }
            set { this.SetAndPushValue(nameof(this.OffTimeout), value); }
        }

        public Screen(Device device, string xmlPath) : base(device, xmlPath) { }

        public CommandContext CaptureAsync(Action<Bitmap> onCaptured)
        {
            return this.device.RunCommandOutputBinaryAsync("shell screencap -p", stream =>
            {
                Bitmap bitmap = null;
                if (stream != null)
                {
                    try
                    {
                        bitmap = Image.FromStream(stream) as Bitmap;
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.ToString());
                    }
                }
                onCaptured?.Invoke(bitmap);
            });
        }

        public CommandContext CaptureIntoDeviceAsync(string saveTo, Action<string> onCaptured)
        {
            return this.device.RunCommandOutputTextAsync($"shell screencap -p > {saveTo}", output => onCaptured(saveTo));
        }

        string GetDensityClass(int density)
        {
            var classes = new Dictionary<int,string>()
            {
                { 120, "ldpi" },
                { 160, "mdpi" },
                { 213, "tvdpi" },
                { 240, "hdpi" },
                { 320, "xhdpi" },
                { 480, "xxhdpi" },
                { 640, "xxxhdpi" },
            };
            if(!classes.TryGetValue(density, out var name))
            {
                name = $"{density}dpi";
            }
            return name;
        }
    }
}
