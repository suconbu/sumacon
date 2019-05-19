using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using WeifenLuo.WinFormsUI.Docking;

namespace Suconbu.Sumacon
{
    public partial class FormControl : FormBase
    {
        Sumacon sumacon;
        StatusStrip uxScreenStatusStrip = new StatusStrip();
        SplitContainer uxBaseSplitContaier = new SplitContainer() { Dock = DockStyle.Fill };
        SplitContainer uxUpperSplitContaier = new SplitContainer() { Dock = DockStyle.Fill };
        PictureBox uxScreenPictureBox = new PictureBox() { Dock = DockStyle.Fill };
        GridPanel uxActionsGridPanel = new GridPanel() { Dock = DockStyle.Fill };
        GridPanel uxLogGridPanel = new GridPanel() { Dock = DockStyle.Fill };
        ControlActionGroup actionGroup;

        public FormControl(Sumacon sumacon)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.sumacon = sumacon;
            this.sumacon.DeviceManager.ActiveDeviceChanged += this.DeviceManager_ActiveDeviceChanged;
        }

        protected override void OnLoad(EventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnLoad(e);

            this.actionGroup = ControlActionGroup.FromXml("control_actions.xml");
            this.uxActionsGridPanel.ApplyColorSet(ColorSet.Light);
            this.uxActionsGridPanel.AutoGenerateColumns = true;
            this.uxActionsGridPanel.MultiSelect = false;
            this.uxActionsGridPanel.ColumnHeadersVisible = false;
            this.uxActionsGridPanel.DataSource = this.actionGroup.Actions;
            this.uxActionsGridPanel.SetDefaultCellStyle();
            this.uxActionsGridPanel.MouseDoubleClick += this.UxActionsGridPanel_MouseDoubleClick; ;
            this.uxActionsGridPanel.ShowCellToolTips = true;
            this.uxActionsGridPanel.CellToolTipTextNeeded += this.UxActionsGridPanel_CellToolTipTextNeeded;

            this.uxScreenStatusStrip.Items.Add("Position");
            this.uxScreenStatusStrip.Items.Add("Auto update");
            this.uxScreenStatusStrip.Items.Add("Update");
            this.uxScreenStatusStrip.SizingGrip = false;

            this.uxScreenPictureBox.SizeMode = PictureBoxSizeMode.Zoom;

            this.uxUpperSplitContaier.Orientation = Orientation.Vertical;
            this.uxUpperSplitContaier.Panel1.Controls.Add(this.uxScreenPictureBox);
            this.uxUpperSplitContaier.Panel2.Controls.Add(this.uxActionsGridPanel);

            this.uxBaseSplitContaier.Orientation = Orientation.Horizontal;
            this.uxBaseSplitContaier.Panel1.Controls.Add(this.uxUpperSplitContaier);
            this.uxBaseSplitContaier.Panel1.Controls.Add(this.uxScreenStatusStrip);
            this.uxBaseSplitContaier.Panel2.Controls.Add(this.uxLogGridPanel);
            this.Controls.Add(this.uxBaseSplitContaier);

            this.uxUpperSplitContaier.SplitterDistance = this.uxUpperSplitContaier.Width * 70 / 100;
            this.uxBaseSplitContaier.SplitterDistance = this.uxBaseSplitContaier.Height * 70 / 100;

            this.uxActionsGridPanel.Columns[nameof(ControlAction.Name)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.uxActionsGridPanel.Columns[nameof(ControlAction.Command)].Visible = false;

            this.UpdateScreenPicture();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnClosing(e);
            this.sumacon.DeviceManager.ActiveDeviceChanged -= this.DeviceManager_ActiveDeviceChanged;
        }

        void DeviceManager_ActiveDeviceChanged(object sender, Device e)
        {
            this.UpdateScreenPicture();
        }

        void UxActionsGridPanel_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var action = this.GetSelectedAction();
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (action == null || device == null) return;
            device.RunCommandAsync(action.Command);
        }

        void UxActionsGridPanel_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                e.ToolTipText = this.actionGroup.Actions[e.RowIndex].Command;
            }
        }

        void UpdateScreenPicture()
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device != null)
            {
                device.Screen.CaptureAsync(bitmap => this.uxScreenPictureBox.Image = bitmap);
            }
        }

        ControlAction GetSelectedAction()
        {
            return (this.uxActionsGridPanel.SelectedRows.Count > 0) ?
                this.actionGroup.Actions[this.uxActionsGridPanel.SelectedRows[0].Index] : null;
        }
    }

    [XmlRoot("root")]
    public class ControlActionGroup
    {
        [XmlElement("action")]
        public List<ControlAction> Actions;

        public static ControlActionGroup FromXml(string path)
        {
            var serializer = new XmlSerializer(typeof(ControlActionGroup));
            using (var reader = new StreamReader(path))
            {
                return serializer.Deserialize(reader) as ControlActionGroup;
            }
        }
    }

    public class ControlAction
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("command")]
        public string Command { get; set; }
    }
}
