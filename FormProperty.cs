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
                foreach (var component in this.device?.Components ?? Enumerable.Empty<DeviceComponentBase>())
                {
                    component.PropertyChanged -= this.DeviceComponent_PropertyChanged;
                }
                this.device = value;
                this.propertyGrid1.SelectedObject = this.device;
                foreach (var component in this.device?.Components ?? Enumerable.Empty<DeviceComponentBase>())
                {
                    component.PropertyChanged += this.DeviceComponent_PropertyChanged;
                }
            }
        }

        Device device;
        ContextMenuStrip menu = new ContextMenuStrip();

        public FormProperty()
        {
            InitializeComponent();

            var resetPropertyMenuItem = this.menu.Items.Add(string.Empty, null, (s, e) =>
            {
                var category = this.propertyGrid1.SelectedGridItem.PropertyDescriptor.Category;
                var component = this.device.ComponentsByCategory[category];
                var property = component?.Find(this.propertyGrid1.SelectedGridItem.Label);
                property?.ResetAsync(this.device);
            });
            var resetCategoryMenuItem = this.menu.Items.Add(string.Empty, null, (s, e) =>
            {
                var category = this.propertyGrid1.SelectedGridItem.PropertyDescriptor.Category;
                this.device.ComponentsByCategory[category]?.ResetAsync();
            });
            this.menu.Items.Add("Reset all properties", null, (s, e) =>
            {
                foreach (var component in this.device.Components)
                {
                    component.ResetAsync();
                }
            });

            this.menu.Opening += (s, e) =>
            {
                var category = this.propertyGrid1.SelectedGridItem.PropertyDescriptor.Category;
                var component = this.device.ComponentsByCategory[category];
                var label = this.propertyGrid1.SelectedGridItem.Label;
                var property = component?.Find(label);

                resetPropertyMenuItem.Enabled = (property != null && property.PushCommand != null);
                resetPropertyMenuItem.Text = $"Reset '{label}'";
                resetCategoryMenuItem.Enabled = (component != null);
                resetCategoryMenuItem.Text = $"Reset '{category}'";
            };

            this.propertyGrid1.ContextMenuStrip = this.menu;
        }

        void DeviceComponent_PropertyChanged(object sender, IReadOnlyList<Property> properties)
        {
            //Console.Beep(1000, 100);
            this.SafeInvoke(() => this.propertyGrid1.SelectedObject = this.device);
        }
    }
}
