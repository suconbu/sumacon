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
        SortableBindingList<ProcessViewEntry> processList = new SortableBindingList<ProcessViewEntry>();
        SortableBindingList<ThreadViewEntry> threadList = new SortableBindingList<ThreadViewEntry>();
        StatusStrip uxStatusStrip = new StatusStrip();
        ToolStripStatusLabel uxStatusLabel = new ToolStripStatusLabel();
        TopContext topContext;
        TopSnapshot lastTop;
        bool processesUpdated;

        readonly int kGridPanelColumnDefaultWidth = 70;
        readonly int kTopIntervalSeconds = 2;
        readonly int kCpuPeakRange = 5;
        readonly Color kCpuCellPaintColor;
        readonly double kCpuCellPaintValueMin = 0.0;
        readonly double kCpuCellPaintValueMax = 50.0;

        public FormPerformance(Sumacon sumacon)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.sumacon = sumacon;
            this.sumacon.DeviceManager.ActiveDeviceChanged += this.DeviceManager_ActiveDeviceChanged;

            this.kCpuCellPaintColor = this.colorSet.Accent3;
        }

        protected override void OnLoad(EventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnLoad(e);

            this.SetupProcessGridPanel();
            this.SetupThreadGridPanel();
            this.SetupToolStrip();
            this.SetupStatusStrip();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnClosing(e);
            this.topContext?.Close();
            if (this.sumacon.DeviceManager.ActiveDevice != null)
            {
                this.sumacon.DeviceManager.ActiveDevice.ProcessesChanged -= this.Device_ProcessesChanged;
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
                    device.ProcessesChanged += this.Device_ProcessesChanged;
                    this.topContext?.Close();
                    this.topContext = TopContext.Start(device, this.kTopIntervalSeconds, this.TopContext_Received);
                    this.UpdateControlState();
                });
            }
            else
            {
                this.topContext?.Close();
                this.SafeInvoke(this.UpdateControlState);
            }
        }

        void Device_ProcessesChanged(object sender, EventArgs e)
        {
            this.SafeInvoke(() => this.processesUpdated = true);
        }

        void TopContext_Received(object sender, TopSnapshot top)
        {
            this.SafeInvoke(() =>
            {
                if (this.processesUpdated)
                {
                    this.processesUpdated = false;
                    this.UpdateProcessList();
                    this.UpdateThreadList();
                    this.UpdateControlState();
                }
                this.UpdateCpuUsage(top);
            });
        }

        void SetupToolStrip()
        {
            this.uxProcessToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            this.uxProcessToolStrip.Items.Add(new ToolStripLabel("Process"));
            this.uxProcessToolStrip.Items.Add(new ToolStripSeparator());
            this.uxProcessToolStrip.Items.Add("Filter:");
            this.uxProcessFilterTextBox.TextChanged += (s, e) =>
            {
                this.UpdateProcessList();
                this.UpdateControlState();
            };
            this.uxProcessToolStrip.Items.Add(this.uxProcessFilterTextBox);
            this.uxProcessFilterClearButton.Image = this.imageList1.Images["cross.png"];
            this.uxProcessFilterClearButton.Click += (s, e) => this.uxProcessFilterTextBox.Clear();
            this.uxProcessFilterClearButton.Enabled = false;
            this.uxProcessToolStrip.Items.Add(this.uxProcessFilterClearButton);
            this.uxProcessToolStrip.Items.Add(new ToolStripSeparator());
            this.uxProcessAppsOnlyButton.Text = "Apps only";
            this.uxProcessAppsOnlyButton.Click += (s, e) => this.uxProcessFilterTextBox.Text = @"^\w{2,3}\.";
            this.uxProcessToolStrip.Items.Add(this.uxProcessAppsOnlyButton);
            this.uxProcessAndThreadSplitContainer.Panel1.Controls.Add(this.uxProcessToolStrip);

            this.uxThreadToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            this.uxThreadToolStrip.Items.Add(new ToolStripLabel("Thread"));
            this.uxThreadToolStrip.Items.Add(new ToolStripSeparator());
            this.uxThreadToolStrip.Items.Add("Filter:");
            this.uxThreadFilterTextBox.TextChanged += (s, e) =>
            {
                this.UpdateThreadList();
                this.UpdateControlState();
            };
            this.uxThreadToolStrip.Items.Add(this.uxThreadFilterTextBox);
            this.uxThreadFilterClearButton.Image = this.imageList1.Images["cross.png"];
            this.uxThreadFilterClearButton.Click += (s, e) => this.uxThreadFilterTextBox.Clear();
            this.uxThreadFilterClearButton.Enabled = false;
            this.uxThreadToolStrip.Items.Add(this.uxThreadFilterClearButton);
            this.uxProcessAndThreadSplitContainer.Panel2.Controls.Add(this.uxThreadToolStrip);
        }

        void SetupStatusStrip()
        {
            this.uxStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.uxStatusLabel.Spring = true;
            this.uxStatusStrip.Items.Add(this.uxStatusLabel);
            this.uxTsContainer.BottomToolStripPanel.Controls.Add(this.uxStatusStrip);
            this.uxTsContainer.BottomToolStripPanelVisible = true;
        }

        void SetupProcessGridPanel()
        {
            var panel = this.processGridPanel;
            panel.Dock = DockStyle.Fill;
            panel.ApplyColorSet(this.colorSet);
            panel.DataSource = this.processList;
            panel.KeyColumnName = nameof(ProcessViewEntry.Pid);

            panel.SuppressibleSelectionChanged += this.ProcessGridPanel_SuppressibleSelectionChanged;

            this.uxProcessAndThreadSplitContainer.Panel1.Controls.Add(panel);

            panel.SetAllColumnWidth(this.kGridPanelColumnDefaultWidth);
            panel.SetDefaultCellStyle();
            panel.Columns[nameof(ProcessViewEntry.Vss)].DefaultCellStyle.Format = "#,0";
            panel.Columns[nameof(ProcessViewEntry.Rss)].DefaultCellStyle.Format = "#,0";
            panel.Columns[nameof(ProcessViewEntry.CpuPeak)].ToolTipText = $"Peak CPU usage (%) for the last {this.kTopIntervalSeconds * this.kCpuPeakRange} seconds.";
            panel.Columns[nameof(ProcessViewEntry.Name)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            var cpuCellPainter = new GridPanel.NumericCellPaintData()
            {
                Type = GridPanel.CellPaintType.Fill,
                PaintColor = this.kCpuCellPaintColor,
                MinValue = this.kCpuCellPaintValueMin,
                MaxValue = this.kCpuCellPaintValueMax
            };
            panel.Columns[nameof(ProcessViewEntry.Cpu)].Tag = cpuCellPainter;
            panel.Columns[nameof(ProcessViewEntry.CpuPeak)].Tag = cpuCellPainter;

            // デフォルトはCPU使用率の降順
            panel.SortColumn(panel.Columns[nameof(ProcessViewEntry.Cpu)], ListSortDirection.Descending);
        }

        void ProcessGridPanel_SuppressibleSelectionChanged(object sender, EventArgs e)
        {
            this.UpdateThreadList();
            this.UpdateControlState();
        }

        void SetupThreadGridPanel()
        {
            var panel = this.threadGridPanel;
            panel.Dock = DockStyle.Fill;
            panel.ApplyColorSet(this.colorSet);
            panel.AutoGenerateColumns = true;

            panel.DataSource = this.threadList;
            panel.KeyColumnName = nameof(ThreadEntry.Tid);

            this.uxProcessAndThreadSplitContainer.Panel2.Controls.Add(panel);

            panel.SetAllColumnWidth(this.kGridPanelColumnDefaultWidth);
            panel.SetDefaultCellStyle();
            panel.Columns[nameof(ThreadViewEntry.CpuPeak)].ToolTipText = $"Peak CPU usage (%) for the last {this.kTopIntervalSeconds * this.kCpuPeakRange} seconds.";
            panel.Columns[nameof(ThreadViewEntry.Name)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            panel.Columns[nameof(ThreadViewEntry.ProcessName)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            var cpuCellPainter = new GridPanel.NumericCellPaintData()
            {
                Type = GridPanel.CellPaintType.Fill,
                PaintColor = this.kCpuCellPaintColor,
                MinValue = this.kCpuCellPaintValueMin,
                MaxValue = this.kCpuCellPaintValueMax
            };
            panel.Columns[nameof(ThreadViewEntry.Cpu)].Tag = cpuCellPainter;
            panel.Columns[nameof(ThreadViewEntry.CpuPeak)].Tag = cpuCellPainter;

            // デフォルトはCPU使用率の降順
            panel.SortColumn(panel.Columns[nameof(ThreadViewEntry.Cpu)], ListSortDirection.Descending);
        }

        void UpdateControlState()
        {
            var processes = this.sumacon.DeviceManager.ActiveDevice?.Processes;
            
            int pShown = this.processGridPanel.RowCount;
            int pTotal = processes?.Count() ?? 0;
            int tShown = this.threadGridPanel.RowCount;
            int tTotal = processes?.Sum(p => p.Threads.Count()) ?? 0;
            this.uxStatusLabel.Text = $"{pShown}/{pTotal} processes, {tShown}/{tTotal} threads. CPU:{this.lastTop?.TotalCpu ?? 0}%";

            this.uxProcessFilterClearButton.Enabled = !string.IsNullOrEmpty(this.uxProcessFilterTextBox.Text);
            this.uxThreadFilterClearButton.Enabled = !string.IsNullOrEmpty(this.uxThreadFilterTextBox.Text);
        }

        void UpdateProcessList()
        {
            var processes = this.sumacon.DeviceManager.ActiveDevice?.Processes;
            if (processes == null)
            {
                this.processList.Clear();
                return;
            }

            var pv = processes.Select(p => new ProcessViewEntry(p, this.kCpuPeakRange));

            var filterText = this.uxProcessFilterTextBox.Text;
            if (!string.IsNullOrEmpty(filterText))
            {
                // フィルタ適用
                try
                {
                    var inverted = filterText.StartsWith("-");
                    filterText = inverted ? filterText.Substring(1) : filterText;
                    var pattern = new Regex(filterText, RegexOptions.IgnoreCase);
                    pv = pv.Where(p =>
                        inverted != (pattern.IsMatch($"{p.Pid}") || pattern.IsMatch($"{p.User}") || pattern.IsMatch($"{p.Name}")));
                }
                catch (ArgumentException ex)
                {
                    // 正規表現の不正
                    Trace.TraceError(ex.ToString());
                }
            }

            var processViewState = this.processGridPanel.GetViewState();
            this.processGridPanel.SuppressEvent(GridPanel.SupressibleEvent.SelectedItemChanged);

            var removes = this.processList.Except(pv, new ProcessRecordEqualityComparer()).ToArray();
            foreach (var p in removes) this.processList.Remove(p);
            var adds = pv.Except(this.processList, new ProcessRecordEqualityComparer()).ToArray();
            foreach (var p in adds) this.processList.Add(p);

            this.processGridPanel.SetViewState(processViewState, GridViewState.ApplyTargets.SortedColumn | GridViewState.ApplyTargets.Selection);
            this.processGridPanel.UnsuppressEvent(GridPanel.SupressibleEvent.SelectedItemChanged);
        }

        void UpdateThreadList()
        {
            var processes = this.sumacon.DeviceManager.ActiveDevice?.Processes;
            if (processes == null)
            {
                this.threadList.Clear();
                return;
            }

            var tvList = new List<ThreadViewEntry>();
            var processRows = new List<DataGridViewRow>();
            foreach (DataGridViewRow row in this.processGridPanel.SelectedRows) processRows.Add(row);
            if (processRows.Count == 0)
            {
                // 何も選択されてなければ全部
                foreach (DataGridViewRow row in this.processGridPanel.Rows) processRows.Add(row);
            }

            foreach (DataGridViewRow row in processRows)
            {
                var process = processes[(int)row.Cells[nameof(ProcessViewEntry.Pid)].Value];
                if (process != null)
                {
                    tvList.AddRange(process.Threads.Select(t => new ThreadViewEntry(t, this.kCpuPeakRange)));
                }
            }

            var tv = (IEnumerable<ThreadViewEntry>)tvList;
            var filterText = this.uxThreadFilterTextBox.Text;
            if (!string.IsNullOrEmpty(filterText))
            {
                // フィルタ適用
                try
                {
                    var inverted = filterText.StartsWith("-");
                    filterText = inverted ? filterText.Substring(1) : filterText;
                    var pattern = new Regex(filterText, RegexOptions.IgnoreCase);
                    tv = tv.Where(t =>
                        inverted != (pattern.IsMatch($"{t.Tid}") || pattern.IsMatch($"{t.Name}") || pattern.IsMatch($"{t.ProcessName}")));
                }
                catch (ArgumentException ex)
                {
                    // 正規表現の不正
                    Trace.TraceError(ex.ToString());
                }
            }

            var threadViewState = this.threadGridPanel.GetViewState();
            this.threadGridPanel.SuppressEvent(GridPanel.SupressibleEvent.SelectedItemChanged);

            var removes = this.threadList.Except(tv, new ThreadRecordEqualityComparer()).ToArray();
            foreach (var t in removes) this.threadList.Remove(t);
            var adds = tv.Except(this.threadList, new ThreadRecordEqualityComparer()).ToArray();
            foreach (var t in adds) this.threadList.Add(t);

            this.threadGridPanel.SetViewState(threadViewState, GridViewState.ApplyTargets.SortedColumn | GridViewState.ApplyTargets.Selection);
            this.threadGridPanel.UnsuppressEvent(GridPanel.SupressibleEvent.SelectedItemChanged);
        }

        void UpdateCpuUsage(TopSnapshot top)
        {
            foreach (var p in this.processList)
            {
                p.SetCpuUsage(top.GetProcessCpu(p.Pid));
            }
            this.processGridPanel.SortColumn(this.processGridPanel.SortedColumn, this.processGridPanel.SortOrder == SortOrder.Descending ? ListSortDirection.Descending : ListSortDirection.Ascending);

            foreach (var t in this.threadList)
            {
                t.SetCpuUsage(top.GetThreadCpu(t.Tid));
            }
            this.threadGridPanel.SortColumn(this.threadGridPanel.SortedColumn, this.threadGridPanel.SortOrder == SortOrder.Descending ? ListSortDirection.Descending : ListSortDirection.Ascending);

            this.lastTop = top;
        }
    }

    class ProcessViewEntry
    {
        public int Pid { get; private set; }
        public int Priority { get; private set; }
        public float Cpu { get; private set; }
        public float CpuPeak { get { return this.cpuHistory.Max(); } }
        public uint Vss { get; private set; }
        public uint Rss { get; private set; }
        public string User { get; private set; }
        public int ThreadCount { get; private set; }
        public string Name { get; private set; }

        Queue<float> cpuHistory = new Queue<float>(new[] { 0.0f });
        readonly int cpuPeakRange;

        public ProcessViewEntry(ProcessEntry pi, int cpuPeakRange)
        {
            this.Pid = pi.Pid;
            this.Priority = pi.Priority;
            this.Cpu = 0.0f;
            this.Vss = pi.Vss;
            this.Rss = pi.Rss;
            this.User = pi.User;
            this.ThreadCount = pi.Threads.Count();
            this.Name = pi.Name;
            this.cpuPeakRange = cpuPeakRange;
        }

        public void SetCpuUsage(float cpu)
        {
            this.Cpu = cpu;
            if(this.cpuHistory.Count >= this.cpuPeakRange)
            {
                this.cpuHistory.Dequeue();
            }
            this.cpuHistory.Enqueue(cpu);
        }
    }

    class ProcessRecordEqualityComparer : IEqualityComparer<ProcessViewEntry>
    {
        public bool Equals(ProcessViewEntry a, ProcessViewEntry b) { return a?.Pid == b?.Pid; }
        public int GetHashCode(ProcessViewEntry p) { return p.Pid; }
    }

    class ThreadViewEntry
    {
        public int Tid { get; private set; }
        public int Priority { get; private set; }
        public float Cpu { get; private set; }
        public float CpuPeak { get { return this.cpuHistory.Max(); } }
        public string Name { get; private set; }
        public string ProcessName { get; private set; }

        Queue<float> cpuHistory = new Queue<float>(new[] { 0.0f });
        readonly int cpuPeakCount;

        public ThreadViewEntry(ThreadEntry ti, int cpuPeakCount)
        {
            this.Tid = ti.Tid;
            this.Priority = ti.Priority;
            this.Cpu = 0.0f;
            this.Name = ti.Name;
            this.ProcessName = $"{ti.Process.Pid}: {ti.Process.Name}";
            this.cpuPeakCount = cpuPeakCount;
        }

        public void SetCpuUsage(float cpu)
        {
            this.Cpu = cpu;
            if (this.cpuHistory.Count >= this.cpuPeakCount)
            {
                this.cpuHistory.Dequeue();
            }
            this.cpuHistory.Enqueue(cpu);
        }
    }

    class ThreadRecordEqualityComparer : IEqualityComparer<ThreadViewEntry>
    {
        public bool Equals(ThreadViewEntry a, ThreadViewEntry b) { return a?.Tid == b?.Tid; }
        public int GetHashCode(ThreadViewEntry p) { return p.Tid; }
    }
}
