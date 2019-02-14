﻿using Suconbu.Mobile;
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

        public FormProperty(Sumacon sumacon)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

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
                var category = this.propertyGrid1.SelectedGridItem.PropertyDescriptor.Category;
                device.ComponentsByCategory[category]?.ResetAsync();
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
                //if(resetPropertyMenuItem.Enabled)
                //{
                //    resetPropertyMenuItem.Text += $" (={property.OriginalValue.ToString()})";
                //}

                resetCategoryMenuItem.Enabled = (component != null);
                resetCategoryMenuItem.Text = string.Format(
                    Properties.Resources.FormProperty_MenuItemLabel_ResetGroup, category);

                resetAllMenuItem.Text = Properties.Resources.FormProperty_MenuItemLabel_ResetAll;
            };

            this.propertyGrid1.ContextMenuStrip = this.menu;
        }

        void DeviceManager_ActiveDeviceChanged(object sender, Device device)
        {
            this.SafeInvoke(() =>
            {
                this.propertyGrid1.SelectedObject = device;
                this.SetupContextMenu();
            });
        }

        void DeviceManager_PropertyChanged(object sender, IReadOnlyList<Property> properties)
        {
            this.SafeInvoke(() => this.propertyGrid1.SelectedObject = this.sumacon.DeviceManager.ActiveDevice);
        }

        bool GetSelectedItemProperty(out string category, out DeviceComponentBase component, out string label, out Property property)
        {
            component = null;
            label = null;
            property = null;
            var device = this.sumacon.DeviceManager.ActiveDevice;
            category = this.propertyGrid1.SelectedGridItem.PropertyDescriptor?.Category;
            if (device == null || category == null) return false;
            component = device.ComponentsByCategory[category];
            label = this.propertyGrid1.SelectedGridItem.Label;
            // 先頭のコンポーネント名は外して探す(例：ScreenSize->Size)
            var findLabel = label.StartsWith(component.Name) ? label.Substring(component.Name.Length) : label;
            property = component?.Find(findLabel);
            return true;
        }
    }
}
