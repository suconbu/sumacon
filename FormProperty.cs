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
        DeviceManager deviceManager;
        ContextMenuStrip menu = new ContextMenuStrip();

        public FormProperty(DeviceManager deviceManager)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.deviceManager = deviceManager;
            this.deviceManager.ActiveDeviceChanged += (s, previousActiveDevice) =>
            {
                this.SafeInvoke(() =>
                {
                    this.propertyGrid1.SelectedObject = this.deviceManager.ActiveDevice;
                    this.SetupContextMenu();
                });
            };
            this.deviceManager.PropertyChanged += this.DeviceManager_PropertyChanged; ;
        }

        void SetupContextMenu()
        {
            var resetPropertyMenuItem = this.menu.Items.Add(string.Empty, null, (s, e) =>
            {
                var device = this.deviceManager.ActiveDevice;
                if (device == null) return;
                var category = this.propertyGrid1.SelectedGridItem.PropertyDescriptor.Category;
                var component = device.ComponentsByCategory[category];
                var property = component?.Find(this.propertyGrid1.SelectedGridItem.Label);
                property?.ResetAsync(device);
            });
            var resetCategoryMenuItem = this.menu.Items.Add(string.Empty, null, (s, e) =>
            {
                var device = this.deviceManager.ActiveDevice;
                if (device == null) return;
                var category = this.propertyGrid1.SelectedGridItem.PropertyDescriptor.Category;
                device.ComponentsByCategory[category]?.ResetAsync();
            });
            this.menu.Items.Add("Reset all properties", null, (s, e) =>
            {
                var device = this.deviceManager.ActiveDevice;
                if (device == null) return;
                foreach (var component in device.Components)
                {
                    component.ResetAsync();
                }
            });

            this.menu.Opening += (s, e) =>
            {
                var device = this.deviceManager.ActiveDevice;
                var category = this.propertyGrid1.SelectedGridItem.PropertyDescriptor?.Category;
                if (device == null || category == null)
                {
                    e.Cancel = true;
                    return;
                }
                var component = device.ComponentsByCategory[category];
                var label = this.propertyGrid1.SelectedGridItem.Label;
                var property = component?.Find(label);

                resetPropertyMenuItem.Enabled = (property != null && property.PushCommand != null);
                resetPropertyMenuItem.Text = $"Reset '{label}'";
                resetCategoryMenuItem.Enabled = (component != null);
                resetCategoryMenuItem.Text = $"Reset '{category}'";
            };

            this.propertyGrid1.ContextMenuStrip = this.menu;
        }


        void DeviceManager_PropertyChanged(object sender, IReadOnlyList<Property> properties)
        {
            //Console.Beep(1000, 100);
            this.SafeInvoke(() => this.propertyGrid1.SelectedObject = this.deviceManager.ActiveDevice);
        }
    }
}
