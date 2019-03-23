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

namespace Suconbu.Sumacon
{
    public partial class FormPerformance : FormBase
    {
        Sumacon sumacon;
        GridPanel processGridPanel = new GridPanel();
        GridPanel threadGridPanel = new GridPanel();
        ColorSet colorSet = ColorSet.Light;
        DataTable processInfoDataTable;
        DataTable threadInfoDataTable;

        public FormPerformance(Sumacon sumacon)
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

            this.SetupProcessGridPanel();
            this.SetupThreadGridPanel();

            this.uxProcessAndThreadSplitContainer.FixedPanel = FixedPanel.Panel1;
            this.uxProcessAndThreadSplitContainer.SplitterDistance = 500;
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
                device.ProcessInfosChanged += this.Device_ProcessInfosChanged;
                device.InvokeIfProcessInfosIsReady(() => this.SafeInvoke(() =>
                {
                    this.UpdateProcessInfoGridPanel(device);
                    this.UpdateControlState();
                }));
            }
            else
            {
                this.SafeInvoke(() =>
                {
                    device.ProcessInfosChanged -= this.Device_ProcessInfosChanged;
                    this.UpdateProcessInfoGridPanel(null);
                    this.UpdateControlState();
                });
            }
        }

        void Device_ProcessInfosChanged(object sender, EventArgs e)
        {
            this.SafeInvoke(() =>
            {
                this.processInfoDataTable = null;
                this.threadInfoDataTable = null;
                this.UpdateProcessInfoGridPanel(this.sumacon.DeviceManager.ActiveDevice);
            });
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
                new DataGridViewColumn() { Name = nameof(ProcessInfo.Vsize), HeaderText = "VSS", Width = 60, CellTemplate = new DataGridViewTextBoxCell() },
                new DataGridViewColumn() { Name = nameof(ProcessInfo.Rsize), HeaderText = "RSS", Width = 60, CellTemplate = new DataGridViewTextBoxCell() },
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
            this.UpdateThreadInfoGridPanel(this.sumacon.DeviceManager.ActiveDevice);
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
                new DataGridViewColumn() { Name = "Process", HeaderText = "Process", Width = 200, CellTemplate = new DataGridViewTextBoxCell() },
                new DataGridViewColumn() { Name = nameof(ThreadInfo.Name), HeaderText = "Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, CellTemplate = new DataGridViewTextBoxCell() },
            };
            foreach (var column in columns)
            {
                column.DataPropertyName = column.Name;
                this.threadGridPanel.Columns.Add(column);
            }
            this.threadGridPanel.Columns[nameof(ThreadInfo.Tid)].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            this.threadGridPanel.Columns[nameof(ThreadInfo.Priority)].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            this.threadGridPanel.KeyColumnName = nameof(ThreadInfo.Tid);
            this.threadGridPanel.SortColumn(this.threadGridPanel.Columns[nameof(ThreadInfo.Tid)], ListSortDirection.Ascending);

            this.uxProcessAndThreadSplitContainer.Panel2.Controls.Add(this.threadGridPanel);
        }

        void UpdateProcessInfoGridPanel(Device device)
        {
            bool selectAll = this.processGridPanel.DataSource == null; // 初回だけ全選択
            var processInfos = device?.ProcessInfos;
            if (processInfos != null && this.processInfoDataTable == null)
            {
                this.processInfoDataTable = this.CreateProcessInfoDataTable(processInfos.ProcessInfos);
            }

            this.processGridPanel.SuppressEvent(GridPanel.SupressibleEvent.SelectedItemChanged);
            var state = this.processGridPanel.GetViewState();
            this.processGridPanel.DataSource = this.processInfoDataTable;
            this.processGridPanel.SetViewState(state);
            if (selectAll)
            {
                this.processGridPanel.SelectAll();
            }
            this.processGridPanel.UnsuppressEvent(GridPanel.SupressibleEvent.SelectedItemChanged);

            this.UpdateThreadInfoGridPanel(device);
        }

        void UpdateThreadInfoGridPanel(Device device)
        {
            var processInfos = device?.ProcessInfos;

            var threadInfos = new List<ThreadInfo>();
            if (processInfos != null)
            {
                foreach (DataGridViewRow row in this.processGridPanel.SelectedRows)
                {
                    threadInfos.AddRange(processInfos[(int)row.Cells["PID"].Value].ThreadInfos);
                }
            }

            this.threadInfoDataTable = this.CreateThreadInfoDataTable(threadInfos);
            var state = this.threadGridPanel.GetViewState();
            this.threadGridPanel.DataSource = this.threadInfoDataTable;
            this.threadGridPanel.SetViewState(state);
        }

        void UpdateControlState()
        {
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
            dataTable.Columns.Add(nameof(ThreadInfo.Name), typeof(string));
            dataTable.Columns.Add("Process", typeof(string));

            foreach (var t in threadInfos)
            {
                var row = dataTable.NewRow();
                row[nameof(ThreadInfo.Tid)] = t.Tid;
                row[nameof(ThreadInfo.Priority)] = t.Priority;
                row[nameof(ThreadInfo.Name)] = t.Name;
                row["Process"] = $"{t.Process.Pid}:{t.Process.Name}";
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }
    }
}
