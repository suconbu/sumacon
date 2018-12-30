using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace Suconbu.Mobile
{
    public class Screen : DeviceComponentBase
    {
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
