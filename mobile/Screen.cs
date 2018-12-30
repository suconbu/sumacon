using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace Suconbu.Mobile
{
    public class Screen : DeviceComponentBase
    {
        public enum UserRotationCode { Protrait = 0, Landscape = 1, ProtraitReversed = 2, LandscapeReversed = 3 };

        public Size Size
        {
            get { return (Size)this.propertyGroup[nameof(this.Size)].Value; }
            set { this.SetAndPushValue(nameof(this.Size), value); }
        }
        public int Density
        {
            get { return (int)this.propertyGroup[nameof(this.Density)].Value; }
            set { this.SetAndPushValue(nameof(this.Density), value); }
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
        public UserRotationCode UserRotation
        {
            get { return (UserRotationCode)this.propertyGroup[nameof(this.UserRotation)].Value; }
            set { this.SetAndPushValue(nameof(this.UserRotation), (int)value); }
        }
        public int OffTimeout
        {
            get { return (int)this.propertyGroup[nameof(this.OffTimeout)].Value; }
            set { this.SetAndPushValue(nameof(this.OffTimeout), value); }
        }

        public Screen(Device device, string xmlPath) : base(device, xmlPath) { }

        public CommandContext CaptureAsync(Action<Image> onCaptured)
        {
            return this.device.RunCommandOutputBinaryAsync("shell screencap -p", stream =>
            {
                var image = Bitmap.FromStream(stream);
                onCaptured?.Invoke(image);
            });
        }
    }
}
