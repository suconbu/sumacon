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
        ToolStripButton uxProcessApplicationOnlyButton = new ToolStripButton();
        ToolStripTextBox uxThreadFilterTextBox = new ToolStripTextBox();
        ToolStripButton uxThreadFilterClearButton = new ToolStripButton();
        GridPanel processGridPanel = new GridPanel();
        GridPanel threadGridPanel = new GridPanel();
        ColorSet colorSet = ColorSet.Light;
        DataTable processDataTable;
        DataTable threadDataTable;
        bool selectAll = true;
        TopContext topContext;
        IReadOnlyList<ThreadInfo> testReadOnlyList;
        BindingList<ThreadInfo> testBindingList = new BindingList<ThreadInfo>();
        GridPanel testGridPanel = new GridPanel();

        public FormPerformance(Sumacon sumacon)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.sumacon = sumacon;
            this.sumacon.DeviceManager.ActiveDeviceChanged += this.DeviceManager_ActiveDeviceChanged;

            PsContext x = new PsContext();
            //BindingList<ProcessInfo> a = new BindingList<ProcessInfo>(x.ProcessInfos);

            this.testGridPanel.Dock = DockStyle.Fill;
            this.uxBaseSplitContainer.Panel2.Controls.Add(this.testGridPanel);

            this.testBindingList = new BindingList<ThreadInfo>();
            this.testReadOnlyList = this.testBindingList;
            this.testGridPanel.DataSource = this.testReadOnlyList;

            this.testGridPanel.Columns["Priority"].Visible = false;
            var c = this.testGridPanel.Columns.Add("a", "a");

            Delay.SetInterval(() => this.SafeInvoke(() =>this.testBindingList.Add(new ThreadInfo(10, 0, "aaa", null))), 500);
        }

        protected override void OnLoad(EventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnLoad(e);

            this.SetupProcessGridPanel();
            this.SetupThreadGridPanel();
            this.SetupToolStrip();

            this.uxProcessAndThreadSplitContainer.FixedPanel = FixedPanel.Panel1;
            this.uxProcessAndThreadSplitContainer.SplitterDistance = 500;
            this.testBindingList.Add(new ThreadInfo(10, 0, "aaa", null));
            this.testBindingList.Add(new ThreadInfo(20, 0, "bbb", null));
            this.testBindingList.Add(new ThreadInfo(30, 0, "ccc", null));
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
                device.InvokeIfProcessInfosIsReady(() => this.SafeInvoke(() =>
                {
                    this.UpdateControlState();
                }));
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
                this.SafeInvoke(() =>
                {
                    device.ProcessInfosChanged -= this.Device_ProcessInfosChanged;
                    this.UpdateControlState();
                });
            }
        }

        void Device_ProcessInfosChanged(object sender, EventArgs e)
        {
            this.SafeInvoke(() =>
            {
                this.processDataTable = null;
                this.threadDataTable = null;
                this.UpdateControlState();
            });
        }

        void TopContext_Received(object sender, TopInfo topInfo)
        {
            //Debug.Print($"{topInfo.Timestamp.ToString()}");
            //foreach (var r in topInfo.Records)
            //{
            //    Debug.Print($"{r.Tid}:{r.Cpu:.0}");
            //}
            if (this.threadDataTable == null) return;

            foreach (DataRow row in this.threadDataTable.Rows)
            {
                var tid = (int)row[nameof(ThreadInfo.Tid)];
                row["Cpu"] = topInfo[tid];
            }
        }

        void SetupToolStrip()
        {
            this.uxProcessToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            this.uxProcessToolStrip.Items.Add("Filter:");
            this.uxProcessFilterTextBox.TextChanged += (s, e) =>
            {
                this.selectAll = true;
                this.UpdateControlState();
            };
            this.uxProcessToolStrip.Items.Add(this.uxProcessFilterTextBox);
            this.uxProcessFilterClearButton.Image = this.imageList1.Images["cross.png"];
            this.uxProcessFilterClearButton.Click += (s,e) => this.uxProcessFilterTextBox.Clear();
            this.uxProcessFilterClearButton.Enabled = false;
            this.uxProcessToolStrip.Items.Add(this.uxProcessFilterClearButton);
            this.uxProcessToolStrip.Items.Add(new ToolStripSeparator());
            this.uxProcessApplicationOnlyButton.Text = "App. only";
            this.uxProcessApplicationOnlyButton.CheckOnClick = true;
            this.uxProcessApplicationOnlyButton.Click += (s, e) => this.UpdateControlState();
            this.uxProcessToolStrip.Items.Add(this.uxProcessApplicationOnlyButton);
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
            this.processGridPanel.Dock = DockStyle.Fill;
            this.processGridPanel.ApplyColorSet(this.colorSet);
            this.processGridPanel.AutoGenerateColumns = false;

            DataGridViewColumn[] columns = new DataGridViewColumn[]
            {
                new DataGridViewColumn() { Name = nameof(ProcessInfo.Pid), HeaderText = "PID", Width = 40, CellTemplate = new DataGridViewTextBoxCell() },
                new DataGridViewColumn() { Name = nameof(ProcessInfo.User), HeaderText = "User", Width = 60, CellTemplate = new DataGridViewTextBoxCell() },
                new DataGridViewColumn() { Name = nameof(ProcessInfo.Priority), HeaderText = "Pri", Width = 40, CellTemplate = new DataGridViewTextBoxCell() },
                new DataGridViewColumn() { Name = nameof(ProcessInfo.Vsize), HeaderText = "VSS", Width = 70, CellTemplate = new DataGridViewTextBoxCell() },
                new DataGridViewColumn() { Name = nameof(ProcessInfo.Rsize), HeaderText = "RSS", Width = 70, CellTemplate = new DataGridViewTextBoxCell() },
                new DataGridViewColumn() { Name = "Threads", HeaderText = "Threads", Width = 40, CellTemplate = new DataGridViewTextBoxCell() },
                new DataGridViewColumn() { Name = nameof(ProcessInfo.Name), HeaderText = "Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, CellTemplate = new DataGridViewTextBoxCell() },
            };
            foreach (var column in columns)
            {
                column.DataPropertyName = column.Name;
                this.processGridPanel.Columns.Add(column);
            }
            this.processGridPanel.Columns[nameof(ProcessInfo.Pid)].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            this.processGridPanel.Columns[nameof(ProcessInfo.Priority)].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            this.processGridPanel.Columns[nameof(ProcessInfo.Vsize)].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            this.processGridPanel.Columns[nameof(ProcessInfo.Vsize)].DefaultCellStyle.Format = "#,0K";
            this.processGridPanel.Columns[nameof(ProcessInfo.Rsize)].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            this.processGridPanel.Columns[nameof(ProcessInfo.Rsize)].DefaultCellStyle.Format = "#,0K";
            this.processGridPanel.Columns["Threads"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            this.processGridPanel.KeyColumnName = nameof(ProcessInfo.Pid);
            this.processGridPanel.SortColumn(this.processGridPanel.Columns[nameof(ProcessInfo.Pid)], ListSortDirection.Ascending);

            this.processGridPanel.SuppressibleSelectionChanged += this.ProcessGridPanel_SuppressibleSelectionChanged;

            this.uxProcessAndThreadSplitContainer.Panel1.Controls.Add(this.processGridPanel);
        }

        void ProcessGridPanel_SuppressibleSelectionChanged(object sender, EventArgs e)
        {
            this.selectAll = this.processGridPanel.SelectedRows.Count == this.processGridPanel.Rows.Count;
            this.UpdateThreadGridPanel(this.sumacon.DeviceManager.ActiveDevice);
        }

        void SetupThreadGridPanel()
        {
            this.threadGridPanel.Dock = DockStyle.Fill;
            this.threadGridPanel.ApplyColorSet(this.colorSet);
            this.threadGridPanel.AutoGenerateColumns = false;

            DataGridViewColumn[] columns = new DataGridViewColumn[]
            {
                new DataGridViewColumn() { Name = nameof(ThreadInfo.Tid), HeaderText = "TID", Width = 40, CellTemplate = new DataGridViewTextBoxCell() },
                new DataGridViewColumn() { Name = nameof(ThreadInfo.Priority), HeaderText = "Pri", Width = 40, CellTemplate = new DataGridViewTextBoxCell() },
                new DataGridViewColumn() { Name = nameof(ThreadInfo.Process.Pid), HeaderText = "PID", Width = 80, CellTemplate = new DataGridViewTextBoxCell() },
                new DataGridViewColumn() { Name = "Cpu", HeaderText = "CPU%", Width = 40, CellTemplate = new DataGridViewTextBoxCell() },
                new DataGridViewColumn() { Name = nameof(ThreadInfo.Name), HeaderText = "Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, CellTemplate = new DataGridViewTextBoxCell() },
            };
            foreach (var column in columns)
            {
                column.DataPropertyName = column.Name;
                this.threadGridPanel.Columns.Add(column);
            }
            this.threadGridPanel.Columns[nameof(ThreadInfo.Tid)].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            this.threadGridPanel.Columns[nameof(ThreadInfo.Priority)].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            this.threadGridPanel.Columns["Cpu"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            this.threadGridPanel.KeyColumnName = nameof(ThreadInfo.Tid);
            this.threadGridPanel.SortColumn(this.threadGridPanel.Columns[nameof(ThreadInfo.Tid)], ListSortDirection.Ascending);

            this.uxProcessAndThreadSplitContainer.Panel2.Controls.Add(this.threadGridPanel);
        }

        void UpdateControlState()
        {
            this.uxProcessFilterClearButton.Enabled = !string.IsNullOrEmpty(this.uxProcessFilterTextBox.Text);
            this.uxThreadFilterClearButton.Enabled = !string.IsNullOrEmpty(this.uxThreadFilterTextBox.Text);
            this.UpdateProcessGridPanel(this.sumacon.DeviceManager.ActiveDevice);
        }

        void UpdateProcessGridPanel(Device device)
        {
            var processInfos = device?.ProcessInfos;
            if (processInfos != null && this.processDataTable == null)
            {
                this.processDataTable = this.CreateProcessInfoDataTable(processInfos.ProcessInfos);
            }

            var rows = this.processDataTable.AsEnumerable();
            var filterText = this.uxProcessFilterTextBox.Text;
            if (!string.IsNullOrEmpty(filterText))
            {
                // フィルタ適用
                var inverted = filterText.StartsWith("-");
                filterText = inverted ? filterText.Substring(1) : filterText;
                rows = rows.Where(row =>
                    inverted != Regex.IsMatch(
                        $"{row[nameof(ProcessInfo.Pid)]}{row[nameof(ProcessInfo.User)]}{row[nameof(ProcessInfo.Name)]}",
                        filterText, RegexOptions.IgnoreCase));
            }

            this.processGridPanel.SuppressEvent(GridPanel.SupressibleEvent.SelectedItemChanged);
            var state = this.processGridPanel.GetViewState();
            this.processGridPanel.DataSource = rows.AsDataView();
            this.processGridPanel.SetViewState(state);
            if (this.selectAll)
            {
                this.processGridPanel.SelectAll();
            }
            this.processGridPanel.UnsuppressEvent(GridPanel.SupressibleEvent.SelectedItemChanged);

            this.UpdateThreadGridPanel(device);
        }

        void UpdateThreadGridPanel(Device device)
        {
            var processInfos = device?.ProcessInfos;

            var threadInfos = new List<ThreadInfo>();
            if (processInfos != null)
            {
                foreach (DataGridViewRow row in this.processGridPanel.SelectedRows)
                {
                    threadInfos.AddRange((processInfos[(int)row.Cells["PID"].Value]?.ThreadInfos).OrEmptyIfNull());
                }
            }

            //TODO: 前回と同じか判断して使いまわしたいなあ
            this.threadDataTable = this.CreateThreadInfoDataTable(threadInfos);
            var rows = this.threadDataTable.AsEnumerable();
            var filterText = this.uxThreadFilterTextBox.Text;
            if (!string.IsNullOrEmpty(filterText))
            {
                // フィルタ適用
                var inverted = filterText.StartsWith("-");
                filterText = inverted ? filterText.Substring(1) : filterText;
                rows = rows.Where(row =>
                    inverted != Regex.IsMatch(
                        $"{row[nameof(ThreadInfo.Tid)]}{row[nameof(ThreadInfo.Process.Pid)]}{row[nameof(ThreadInfo.Name)]}",
                        filterText, RegexOptions.IgnoreCase));
            }

            this.threadGridPanel.SuppressEvent(GridPanel.SupressibleEvent.SelectedItemChanged);
            var state = this.threadGridPanel.GetViewState();
            this.threadGridPanel.DataSource = rows.AsDataView();
            this.threadGridPanel.SetViewState(state);
            this.threadGridPanel.UnsuppressEvent(GridPanel.SupressibleEvent.SelectedItemChanged);
        }

        DataTable CreateProcessInfoDataTable(IEnumerable<ProcessInfo> processInfos)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add(nameof(ProcessInfo.User), typeof(string));
            dataTable.Columns.Add(nameof(ProcessInfo.Pid), typeof(int));
            dataTable.Columns.Add(nameof(ProcessInfo.Ppid), typeof(int));
            dataTable.Columns.Add(nameof(ProcessInfo.Vsize), typeof(uint));
            dataTable.Columns.Add(nameof(ProcessInfo.Rsize), typeof(uint));
            dataTable.Columns.Add(nameof(ProcessInfo.Priority), typeof(int));
            dataTable.Columns.Add(nameof(ProcessInfo.Name), typeof(string));
            dataTable.Columns.Add("Threads", typeof(int));

            foreach (var p in processInfos)
            {
                var row = dataTable.NewRow();
                row[nameof(ProcessInfo.User)] = p.User;
                row[nameof(ProcessInfo.Pid)] = p.Pid;
                row[nameof(ProcessInfo.Ppid)] = p.Ppid;
                row[nameof(ProcessInfo.Vsize)] = p.Vsize;
                row[nameof(ProcessInfo.Rsize)] = p.Rsize;
                row[nameof(ProcessInfo.Priority)] = p.Priority;
                row[nameof(ProcessInfo.Name)] = p.Name;
                row["Threads"] = p.ThreadInfos.Count();
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        DataTable CreateThreadInfoDataTable(IEnumerable<ThreadInfo> threadInfos)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add(nameof(ThreadInfo.Tid), typeof(int));
            dataTable.Columns.Add(nameof(ThreadInfo.Priority), typeof(int));
            dataTable.Columns.Add(nameof(ThreadInfo.Process.Pid), typeof(string));
            dataTable.Columns.Add("Cpu", typeof(float));
            dataTable.Columns.Add(nameof(ThreadInfo.Name), typeof(string));

            foreach (var t in threadInfos)
            {
                var row = dataTable.NewRow();
                row[nameof(ThreadInfo.Tid)] = t.Tid;
                row[nameof(ThreadInfo.Priority)] = t.Priority;
                row[nameof(ThreadInfo.Process.Pid)] = $"{t.Process.Pid}:{t.Process.Name}";
                row["Cpu"] = 0.0f;
                row[nameof(ThreadInfo.Name)] = t.Name;
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }
    }
}
