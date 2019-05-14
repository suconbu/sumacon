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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Suconbu.Sumacon
{
    public partial class FormPerformance : FormBase
    {
        Sumacon sumacon;
        ToolStrip uxProcessToolStrip = new ToolStrip();
        ToolStrip uxThreadToolStrip = new ToolStrip();
        ToolStripTextBox uxProcessFilterTextBox = new ToolStripTextBox();
        ToolStripButton uxProcessFilterClearButton = new ToolStripButton();
        ToolStripButton uxProcessAppsOnlyButton = new ToolStripButton();
        ToolStripTextBox uxThreadFilterTextBox = new ToolStripTextBox();
        ToolStripButton uxThreadFilterClearButton = new ToolStripButton();
        GridPanel processGridPanel = new GridPanel();
        GridPanel threadGridPanel = new GridPanel();
        ColorSet colorSet = ColorSet.Light;
        SortableBindingList<ProcessInfo> processList = new SortableBindingList<ProcessInfo>();
        SortableBindingList<ThreadInfo> threadList = new SortableBindingList<ThreadInfo>();
        TopContext topContext;
        TopInfo lastTopInfo;

        public FormPerformance(Sumacon sumacon)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.sumacon = sumacon;
            this.sumacon.DeviceManager.ActiveDeviceChanged += this.DeviceManager_ActiveDeviceChanged;

            //PsContext x = new PsContext();
            //BindingList<ProcessInfo> a = new BindingList<ProcessInfo>(x.ProcessInfos);
        }

        protected override void OnLoad(EventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnLoad(e);

            this.SetupProcessGridPanel();
            this.SetupThreadGridPanel();
            this.SetupToolStrip();

            this.uxProcessAndThreadSplitContainer.FixedPanel = FixedPanel.Panel1;
            this.uxProcessAndThreadSplitContainer.SplitterDistance = 200;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnClosing(e);
            if(this.sumacon.DeviceManager.ActiveDevice != null)
            {
                this.sumacon.DeviceManager.ActiveDevice.ProcessInfosChanged -= this.Device_ProcessInfosChanged;
            }
            this.sumacon.DeviceManager.ActiveDeviceChanged -= this.DeviceManager_ActiveDeviceChanged;
        }

        void DeviceManager_ActiveDeviceChanged(object sender, Device previousDevice)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device != null)
            {
                //device.InvokeIfProcessInfosIsReady(() => this.SafeInvoke(() =>
                //{
                //    this.UpdateControlState();
                //}));
                this.SafeInvoke(() =>
                {
                    device.ProcessInfosChanged += this.Device_ProcessInfosChanged;
                    this.topContext?.Close();
                    this.topContext = TopContext.Start(device, 1, this.TopContext_Received);
                });
            }
            else
            {
                this.topContext?.Close();
                this.SafeInvoke(this.UpdateControlState);
            }
        }

        void Device_ProcessInfosChanged(object sender, EventArgs e)
        {
            this.SafeInvoke(() =>
            {
                this.UpdateProcessList();
                this.UpdateThreadList();
            });
        }

        void TopContext_Received(object sender, TopInfo topInfo)
        {
            this.lastTopInfo = topInfo;
            //Debug.Print($"{topInfo.Timestamp.ToString()}");
            //foreach (var r in topInfo.Records)
            //{
            //    Debug.Print($"{r.Tid}:{r.Cpu:.0}");
            //}
            //if (this.threadDataTable == null) return;

            //foreach (DataRow row in this.threadDataTable.Rows)
            //{
            //    var tid = (int)row[nameof(ThreadInfo.Tid)];
            //    row["Cpu"] = topInfo[tid];
            //}
        }

        void SetupToolStrip()
        {
            this.uxProcessToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            this.uxProcessToolStrip.Items.Add("Filter:");
            this.uxProcessFilterTextBox.TextChanged += (s, e) =>
            {
                //this.selectAll = true;
                this.UpdateProcessList();
                this.UpdateControlState();
            };
            this.uxProcessToolStrip.Items.Add(this.uxProcessFilterTextBox);
            this.uxProcessFilterClearButton.Image = this.imageList1.Images["cross.png"];
            this.uxProcessFilterClearButton.Click += (s,e) => this.uxProcessFilterTextBox.Clear();
            this.uxProcessFilterClearButton.Enabled = false;
            this.uxProcessToolStrip.Items.Add(this.uxProcessFilterClearButton);
            this.uxProcessToolStrip.Items.Add(new ToolStripSeparator());
            this.uxProcessAppsOnlyButton.Text = "Apps only";
            //this.uxProcessAppsOnlyButton.CheckOnClick = true;
            this.uxProcessAppsOnlyButton.Click += (s, e) => this.uxProcessFilterTextBox.Text = @"^\w{2,3}\.";
            this.uxProcessToolStrip.Items.Add(this.uxProcessAppsOnlyButton);
            this.uxProcessAndThreadSplitContainer.Panel1.Controls.Add(this.uxProcessToolStrip);

            this.uxThreadToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            this.uxThreadToolStrip.Items.Add("Filter:");
            this.uxThreadFilterTextBox.TextChanged += (s, e) => this.UpdateControlState();
            this.uxThreadToolStrip.Items.Add(this.uxThreadFilterTextBox);
            this.uxThreadFilterClearButton.Image = this.imageList1.Images["cross.png"];
            this.uxThreadFilterClearButton.Click += (s, e) => this.uxThreadFilterTextBox.Clear();
            this.uxThreadFilterClearButton.Enabled = false;
            this.uxThreadToolStrip.Items.Add(this.uxThreadFilterClearButton);
            this.uxProcessAndThreadSplitContainer.Panel2.Controls.Add(this.uxThreadToolStrip);
        }

        void SetupProcessGridPanel()
        {
            var panel = this.processGridPanel;
            panel.Dock = DockStyle.Fill;
            panel.ApplyColorSet(this.colorSet);
            panel.DataSource = this.processList;
            panel.KeyColumnName = nameof(ProcessInfo.Pid);

            panel.SuppressibleSelectionChanged += this.ProcessGridPanel_SuppressibleSelectionChanged;

            this.uxProcessAndThreadSplitContainer.Panel1.Controls.Add(panel);

            panel.SetAllColumnWidth(70);
            panel.SetDefaultCellStyle();
            panel.Columns[nameof(ProcessInfo.Threads)].Visible = false;
            panel.Columns[nameof(ProcessInfo.Name)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        void ProcessGridPanel_SuppressibleSelectionChanged(object sender, EventArgs e)
        {
            this.UpdateThreadList();
        }

        void SetupThreadGridPanel()
        {
            var panel = this.threadGridPanel;
            panel.Dock = DockStyle.Fill;
            panel.ApplyColorSet(this.colorSet);
            panel.AutoGenerateColumns = true;

            panel.DataSource = this.threadList;
            panel.KeyColumnName = nameof(ThreadInfo.Tid);

            this.uxProcessAndThreadSplitContainer.Panel2.Controls.Add(panel);

            panel.SetAllColumnWidth(70);
            panel.SetDefaultCellStyle();
            panel.Columns[nameof(ThreadInfo.Process)].Visible = false;
            panel.Columns[nameof(ThreadInfo.Name)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            panel.Columns[nameof(ThreadInfo.ProcessName)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        void UpdateControlState()
        {
            this.uxProcessFilterClearButton.Enabled = !string.IsNullOrEmpty(this.uxProcessFilterTextBox.Text);
            this.uxThreadFilterClearButton.Enabled = !string.IsNullOrEmpty(this.uxThreadFilterTextBox.Text);
        }

        void UpdateProcessList()
        {
            var processInfos = this.sumacon.DeviceManager.ActiveDevice?.ProcessInfos?.ProcessInfos;
            if (processInfos == null)
            {
                this.processList.Clear();
                return;
            }

            var filterText = this.uxProcessFilterTextBox.Text;
            if (!string.IsNullOrEmpty(filterText))
            {
                // フィルタ適用
                try
                {
                    var inverted = filterText.StartsWith("-");
                    filterText = inverted ? filterText.Substring(1) : filterText;
                    var pattern = new Regex(filterText, RegexOptions.IgnoreCase);
                    processInfos = processInfos.Where(p =>
                        inverted != (pattern.IsMatch($"{p.Pid}") || pattern.IsMatch($"{p.User}") || pattern.IsMatch($"{p.Name}")));
                }
                catch(ArgumentException ex)
                {
                    ;// 正規表現の不正
                }
            }

            var processViewState = this.processGridPanel.GetViewState();
            this.processGridPanel.SuppressEvent(GridPanel.SupressibleEvent.SelectedItemChanged);

            var removes = this.processList.Except(processInfos, new ProcessInfoEqualityComparer()).ToArray();
            foreach (var p in removes) this.processList.Remove(p);
            var adds = processInfos.Except(this.processList, new ProcessInfoEqualityComparer()).ToArray();
            foreach (var p in adds) this.processList.Add(p);

            this.processGridPanel.SetViewState(processViewState, GridViewState.ApplyTargets.SortedColumn | GridViewState.ApplyTargets.Selection);
            this.processGridPanel.UnsuppressEvent(GridPanel.SupressibleEvent.SelectedItemChanged);
        }

        void UpdateThreadList()
        {
            var processInfos = this.sumacon.DeviceManager.ActiveDevice?.ProcessInfos;
            if (processInfos == null)
            {
                this.threadList.Clear();
                return;
            }

            var threadInfos = new List<ThreadInfo>();
            if (processInfos != null)
            {
                var rows = new List<DataGridViewRow>();
                foreach (DataGridViewRow row in this.processGridPanel.SelectedRows) rows.Add(row);
                if (rows.Count == 0)
                {
                    // 何も選択されてなければ全部
                    foreach (DataGridViewRow row in this.processGridPanel.Rows) rows.Add(row);
                }

                foreach (DataGridViewRow row in rows)
                {
                    var p = processInfos[(int)row.Cells["PID"].Value];
                    if (p != null)
                    {
                        threadInfos.AddRange(p.Threads.Values);
                    }
                }
            }

            var threadViewState = this.threadGridPanel.GetViewState();
            this.threadGridPanel.SuppressEvent(GridPanel.SupressibleEvent.SelectedItemChanged);

            var removes = this.threadList.Except(threadInfos, new ThreadInfoEqualityComparer()).ToArray();
            foreach (var t in removes) this.threadList.Remove(t);
            var adds = threadInfos.Except(this.threadList, new ThreadInfoEqualityComparer()).ToArray();
            foreach (var t in adds) this.threadList.Add(t);

            this.threadGridPanel.SetViewState(threadViewState, GridViewState.ApplyTargets.SortedColumn | GridViewState.ApplyTargets.Selection);
            this.threadGridPanel.UnsuppressEvent(GridPanel.SupressibleEvent.SelectedItemChanged);
        }
    }
}
