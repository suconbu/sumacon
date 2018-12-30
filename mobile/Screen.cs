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
            get { return (Size)(this.propertyGroup[nameof(this.Size)].Value ?? Size.Empty); }
            set { this.SetAndPushValue(nameof(this.Size), value); }
        }
        public int Density
        {
            get { return (int)(this.propertyGroup[nameof(this.Density)].Value ?? 0); }
            set { this.SetAndPushValue(nameof(this.Density), value); }
        }

        public Screen(Device device, string xmlPath) : base(device, xmlPath) { }
    }
}
