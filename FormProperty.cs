using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Suconbu.Sumacon
{
    public partial class FormProperty : FormBase
    {
        public Device TargetDevice
        {
            get { return this.device; }
            set
            {
                foreach (var component in this.device.ObserveComponents)
                {
                    component.PropertyChanged -= this.DeviceComponent_PropertyChanged;
                }
                this.device = value;
                foreach(var component in this.device.ObserveComponents)
                {
                    component.PropertyChanged += this.DeviceComponent_PropertyChanged;
                }
            }
        }

        Device device = Device.Empty;

        public FormProperty()
        {
            InitializeComponent();
        }

        void DeviceComponent_PropertyChanged(object sender, IReadOnlyList<Property> properties)
        {
            Trace.TraceInformation("PropertyChanged");
            Trace.Indent();
            foreach (var p in properties) Trace.TraceInformation(p.ToString());
            Trace.Unindent();
            this.SafeInvoke(() => this.propertyGrid1.SelectedObject = this.device);
        }
    }
}
