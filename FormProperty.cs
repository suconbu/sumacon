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
        Sumacon sumacon;
        ContextMenuStrip menu = new ContextMenuStrip();
        ToolStripLabel uxTimestampLabel = new ToolStripLabel();

        public FormProperty(Sumacon sumacon)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.uxToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            this.uxToolStrip.Items.Add("Refresh", this.imageList1.Images["arrow_refresh.png"], (s, e) => RefreshProperties());
            this.uxToolStrip.Items.Add(new ToolStripSeparator());
            this.uxToolStrip.Items.Add(this.uxTimestampLabel);

            this.sumacon = sumacon;
            this.sumacon.DeviceManager.ActiveDeviceChanged += this.DeviceManager_ActiveDeviceChanged;
            this.sumacon.DeviceManager.PropertyChanged += this.DeviceManager_PropertyChanged;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            this.sumacon.DeviceManager.ActiveDeviceChanged -= this.DeviceManager_ActiveDeviceChanged;
            this.sumacon.DeviceManager.PropertyChanged -= this.DeviceManager_PropertyChanged;
        }

        void SetupContextMenu()
        {
            this.menu.Items.Clear();
            var resetPropertyMenuItem = this.menu.Items.Add(string.Empty, null, (s, e) =>
            {
                var device = this.sumacon.DeviceManager.ActiveDevice;
                if (device == null) return;
                if (!this.GetSelectedItemProperty(out var category, out var component, out var label, out var property)) return;
                property?.ResetAsync(device);
            });
            var resetCategoryMenuItem = this.menu.Items.Add(string.Empty, null, (s, e) =>
            {
                var device = this.sumacon.DeviceManager.ActiveDevice;
                if (device == null) return;
                var category = this.uxPropertyGrid.SelectedGridItem.PropertyDescriptor.Category;
                device.GetComponent(category)?.ResetAsync();
            });
            var resetAllMenuItem = this.menu.Items.Add(string.Empty, null, (s, e) =>
            {
                var device = this.sumacon.DeviceManager.ActiveDevice;
                foreach (var component in device.Components.OrEmptyIfNull())
                {
                    component.ResetAsync();
                }
            });

            this.menu.Opening += (s, e) =>
            {
                if(!this.GetSelectedItemProperty(out var category, out var component, out var label, out var property))
                {
                    e.Cancel = true;
                    return;
                }

                resetPropertyMenuItem.Enabled = (property != null && property.PushCommand != null);
                resetPropertyMenuItem.Text = string.Format(Properties.Resources.FormProperty_MenuItemLabel_ResetOne, label);

                resetCategoryMenuItem.Enabled = (component != null);
                resetCategoryMenuItem.Text = string.Format(
                    Properties.Resources.FormProperty_MenuItemLabel_ResetGroup, category);

                resetAllMenuItem.Text = Properties.Resources.FormProperty_MenuItemLabel_ResetAll;
            };

            this.uxPropertyGrid.ContextMenuStrip = this.menu;
        }

        void DeviceManager_ActiveDeviceChanged(object sender, Device previousDevice)
        {
            this.SafeInvoke(() =>
            {
                this.uxPropertyGrid.SelectedObject = this.sumacon.DeviceManager.ActiveDevice;
                this.SetupContextMenu();
            });
        }

        void DeviceManager_PropertyChanged(object sender, IReadOnlyList<Property> properties)
        {
            this.SafeInvoke(() =>
            {
                this.uxPropertyGrid.SelectedObject = this.sumacon.DeviceManager.ActiveDevice;
                this.UpdateTimestamp();
            });
        }

        bool GetSelectedItemProperty(out string category, out DeviceComponent component, out string label, out Property property)
        {
            component = null;
            label = null;
            property = null;
            var device = this.sumacon.DeviceManager.ActiveDevice;
            category = this.uxPropertyGrid.SelectedGridItem.PropertyDescriptor?.Category;
            component = device?.GetComponent(category);
            if (component == null) return false;
            label = this.uxPropertyGrid.SelectedGridItem.Label;
            // 先頭のコンポーネント名は外して探す(例：ScreenSize->Size)
            var findLabel = label.StartsWith(component.Name) ? label.Substring(component.Name.Length) : label;
            property = component?.Find(findLabel);
            return true;
        }

        void RefreshProperties()
        {
            var contexts = new List<CommandContext>();
            var device = this.sumacon.DeviceManager.ActiveDevice;
            foreach (var component in device.Components.OrEmptyIfNull())
            {
                contexts.Add(component.PullAsync());
            }
            this.Enabled = false;
            this.uxTimestampLabel.Text = "-";
            CommandContext.StartNew(() =>
            {
                contexts.ForEach(c => c?.Wait());
                this.SafeInvoke(() =>
                {
                    this.Enabled = true;
                    this.UpdateTimestamp();
                });
            });
            //Delay.SetTimeout(() => this.uxPropertyGrid.Enabled = true, 1000, this);
        }

        void UpdateTimestamp()
        {
            this.uxTimestampLabel.Text = $"Updated at {DateTime.Now.ToLongTimeString()}";
        }
    }
}
