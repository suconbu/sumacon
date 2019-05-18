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
        ToolStripButton uxProcessSelectionClearButton;
        ToolStripTextBox uxThreadFilterTextBox = new ToolStripTextBox();
        ToolStripButton uxThreadFilterClearButton = new ToolStripButton();
        GridPanel uxProcessGridPanel = new GridPanel();
        GridPanel uxThreadGridPanel = new GridPanel();
        GridPanel uxMeminfoGridPanel = new GridPanel();
        ColorSet colorSet = ColorSet.Light;
        SortableBindingList<ProcessViewData> processList = new SortableBindingList<ProcessViewData>();
        SortableBindingList<ThreadViewData> threadList = new SortableBindingList<ThreadViewData>();
        SortableBindingList<MeminfoViewData> meminfoList = new SortableBindingList<MeminfoViewData>();
        StatusStrip uxStatusStrip = new StatusStrip();
        ToolStripStatusLabel uxStatusLabel = new ToolStripStatusLabel();
        TopContext topContext;
        TopSnapshot lastTop;
        bool processesUpdated;

        readonly int kGridPanelDefaultColumnWidth = 70;
        readonly int kGridPanelNarrowColumnWidth = 55;
        readonly int kTopIntervalSeconds = 2;
        readonly int kCpuPeakRange = 5;
        readonly Color kCpuCellPaintColor;
        readonly double kCpuCellPaintValueMin = 0.0;
        readonly double kCpuCellPaintValueMax = 50.0;
        readonly int kProcessMeminfoMax = 10;

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
            this.SetupProcessToolStrip();
            this.SetupThreadGridPanel();
            this.SetupThreadToolStrip();
            this.SetupMeminfoGridPanel();
            this.uxLowerSplitContainer.Panel2Collapsed = true; // チャートは後ほど……
            this.SetupStatusStrip();

            this.uxBaseSplitContainer.SplitterDistance = this.uxBaseSplitContainer.Height * 80 / 100;
            this.uxUpperSplitContainer.SplitterDistance = this.uxUpperSplitContainer.Width * 60 / 100;
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

        protected override void OnVisibleChanged(EventArgs e)
        {
            this.ChangeDevice(this.Visible ? this.sumacon.DeviceManager.ActiveDevice : null);
        }

        void DeviceManager_ActiveDeviceChanged(object sender, Device previousDevice)
        {
            this.SafeInvoke(() => this.ChangeDevice(this.sumacon.DeviceManager.ActiveDevice));
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

        void ProcessGridPanel_SuppressibleSelectionChanged(object sender, EventArgs e)
        {
            this.UpdateThreadList();
            this.UpdateProcessMeminfo();
            this.UpdateControlState();
        }

        void ChangeDevice(Device device)
        {
            if (device != null)
            {
                device.ProcessesChanged += this.Device_ProcessesChanged;
                this.topContext?.Close();
                this.topContext = TopContext.Start(device, this.kTopIntervalSeconds, this.TopContext_Received);
            }
            else
            {
                this.topContext?.Close();
            }
            this.UpdateControlState();
        }

        void SetupStatusStrip()
        {
            this.uxStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.uxStatusLabel.Spring = true;
            this.uxStatusStrip.Items.Add(this.uxStatusLabel);
            this.uxTsContainer.BottomToolStripPanel.Controls.Add(this.uxStatusStrip);
            this.uxTsContainer.BottomToolStripPanelVisible = true;
        }

        void SetupProcessToolStrip()
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
            this.uxProcessToolStrip.Items.Add("Apps only", null, (s, e) => this.uxProcessFilterTextBox.Text = @"^\w{2,3}\.");
            this.uxProcessSelectionClearButton = new ToolStripButton("Clear selection", null, (s, e) => this.uxProcessGridPanel.ClearSelection());
            this.uxProcessToolStrip.Items.Add(this.uxProcessSelectionClearButton);
            this.uxUpperSplitContainer.Panel1.Controls.Add(this.uxProcessToolStrip);
        }

        void SetupProcessGridPanel()
        {
            var panel = this.uxProcessGridPanel;
            panel.Dock = DockStyle.Fill;
            panel.ApplyColorSet(this.colorSet);
            panel.DataSource = this.processList;
            panel.KeyColumnName = nameof(ProcessViewData.Pid);

            panel.SuppressibleSelectionChanged += this.ProcessGridPanel_SuppressibleSelectionChanged;

            this.uxUpperSplitContainer.Panel1.Controls.Add(panel);

            panel.SetAllColumnWidth(this.kGridPanelDefaultColumnWidth);
            panel.SetDefaultCellStyle();
            //panel.Columns[nameof(ProcessViewData.Vss)].DefaultCellStyle.Format = "#,0";
            //panel.Columns[nameof(ProcessViewData.Rss)].DefaultCellStyle.Format = "#,0";
            panel.Columns[nameof(ProcessViewData.Priority)].Width = this.kGridPanelNarrowColumnWidth;
            panel.Columns[nameof(ProcessViewData.Cpu)].Width = this.kGridPanelNarrowColumnWidth;
            panel.Columns[nameof(ProcessViewData.CpuPeak)].Width = this.kGridPanelNarrowColumnWidth;
            panel.Columns[nameof(ProcessViewData.ThreadCount)].Width = this.kGridPanelNarrowColumnWidth;
            panel.Columns[nameof(ProcessViewData.CpuPeak)].ToolTipText = $"Peak CPU usage (%) for the last {this.kTopIntervalSeconds * this.kCpuPeakRange} seconds.";
            panel.Columns[nameof(ProcessViewData.Name)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            var cpuCellPainter = new GridPanel.NumericCellPaintData()
            {
                Type = GridPanel.CellPaintType.Fill,
                PaintColor = this.kCpuCellPaintColor,
                MinValue = this.kCpuCellPaintValueMin,
                MaxValue = this.kCpuCellPaintValueMax
            };
            panel.Columns[nameof(ProcessViewData.Cpu)].Tag = cpuCellPainter;
            panel.Columns[nameof(ProcessViewData.CpuPeak)].Tag = cpuCellPainter;

            // デフォルトはCPU使用率の降順
            panel.SortColumn(panel.Columns[nameof(ProcessViewData.Cpu)], ListSortDirection.Descending);
        }

        void SetupThreadToolStrip()
        {
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
            this.uxUpperSplitContainer.Panel2.Controls.Add(this.uxThreadToolStrip);
        }

        void SetupThreadGridPanel()
        {
            var panel = this.uxThreadGridPanel;
            panel.Dock = DockStyle.Fill;
            panel.ApplyColorSet(this.colorSet);
            panel.AutoGenerateColumns = true;

            panel.DataSource = this.threadList;
            panel.KeyColumnName = nameof(ThreadEntry.Tid);

            this.uxUpperSplitContainer.Panel2.Controls.Add(panel);

            panel.SetAllColumnWidth(this.kGridPanelDefaultColumnWidth);
            panel.SetDefaultCellStyle();
            panel.Columns[nameof(ThreadViewData.Priority)].Width = this.kGridPanelNarrowColumnWidth;
            panel.Columns[nameof(ThreadViewData.Cpu)].Width = this.kGridPanelNarrowColumnWidth;
            panel.Columns[nameof(ThreadViewData.CpuPeak)].Width = this.kGridPanelNarrowColumnWidth;
            panel.Columns[nameof(ThreadViewData.CpuPeak)].ToolTipText = $"Peak CPU usage (%) for the last {this.kTopIntervalSeconds * this.kCpuPeakRange} seconds.";
            panel.Columns[nameof(ThreadViewData.Name)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            var cpuCellPainter = new GridPanel.NumericCellPaintData()
            {
                Type = GridPanel.CellPaintType.Fill,
                PaintColor = this.kCpuCellPaintColor,
                MinValue = this.kCpuCellPaintValueMin,
                MaxValue = this.kCpuCellPaintValueMax
            };
            panel.Columns[nameof(ThreadViewData.Cpu)].Tag = cpuCellPainter;
            panel.Columns[nameof(ThreadViewData.CpuPeak)].Tag = cpuCellPainter;

            // デフォルトはCPU使用率の降順
            panel.SortColumn(panel.Columns[nameof(ThreadViewData.Cpu)], ListSortDirection.Descending);
        }

        void SetupMeminfoGridPanel()
        {
            var panel = this.uxMeminfoGridPanel;
            panel.Dock = DockStyle.Fill;
            panel.ApplyColorSet(this.colorSet);
            panel.AutoGenerateColumns = true;
            panel.DataSource = this.meminfoList;
            panel.SetAllColumnWidth(this.kGridPanelDefaultColumnWidth);
            panel.SetDefaultCellStyle();
            panel.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            this.uxLowerSplitContainer.Panel1.Controls.Add(panel);

            panel.DefaultCellStyle.Format = "#,0";
            panel.Columns[nameof(MeminfoViewData.Pid)].Visible = false;
            panel.Columns[nameof(MeminfoViewData.Process)].Width = 200;
        }

        void UpdateProcessList()
        {
            var filtered = this.GetFilteredProcessViewData();

            var processViewState = this.uxProcessGridPanel.GetViewState();
            this.uxProcessGridPanel.SuppressEvent(GridPanel.SupressibleEvent.SelectedItemChanged);

            var removes = this.processList.Except(filtered, new ProcessViewDataEqualityComparer()).ToArray();
            foreach (var p in removes) this.processList.Remove(p);
            var adds = filtered.Except(this.processList, new ProcessViewDataEqualityComparer()).ToArray();
            foreach (var p in adds) this.processList.Add(p);

            this.uxProcessGridPanel.SetViewState(processViewState, GridViewState.ApplyTargets.SortedColumn | GridViewState.ApplyTargets.Selection);
            this.uxProcessGridPanel.UnsuppressEvent(GridPanel.SupressibleEvent.SelectedItemChanged);
        }

        IEnumerable<ProcessViewData> GetFilteredProcessViewData()
        {
            var processes = this.sumacon.DeviceManager.ActiveDevice?.Processes;
            var output = (processes?.Select(p => new ProcessViewData(p, this.kCpuPeakRange))).OrEmptyIfNull();

            var filterText = this.uxProcessFilterTextBox.Text;
            if (!string.IsNullOrEmpty(filterText))
            {
                // フィルタ適用
                try
                {
                    var inverted = filterText.StartsWith("-");
                    filterText = inverted ? filterText.Substring(1) : filterText;
                    var pattern = new Regex(filterText, RegexOptions.IgnoreCase);
                    output = output.Where(p =>
                        inverted != (pattern.IsMatch($"{p.Pid}") || pattern.IsMatch($"{p.User}") || pattern.IsMatch($"{p.Name}")));
                }
                catch (ArgumentException ex)
                {
                    // 正規表現の不正
                    Trace.TraceError(ex.ToString());
                }
            }
            return output;
        }

        void UpdateThreadList()
        {
            var filtered = this.GetFilteredThreadViewData();

            var threadViewState = this.uxThreadGridPanel.GetViewState();
            this.uxThreadGridPanel.SuppressEvent(GridPanel.SupressibleEvent.SelectedItemChanged);

            var removes = this.threadList.Except(filtered, new ThreadViewDataEqualityComparer()).ToArray();
            foreach (var t in removes) this.threadList.Remove(t);
            var adds = filtered.Except(this.threadList, new ThreadViewDataEqualityComparer()).ToArray();
            foreach (var t in adds) this.threadList.Add(t);

            this.uxThreadGridPanel.SetViewState(threadViewState, GridViewState.ApplyTargets.SortedColumn | GridViewState.ApplyTargets.Selection);
            this.uxThreadGridPanel.UnsuppressEvent(GridPanel.SupressibleEvent.SelectedItemChanged);
        }

        IEnumerable<ThreadViewData> GetFilteredThreadViewData()
        {
            var output = this.GetTargetThreadViewData().OrEmptyIfNull();
            var filterText = this.uxThreadFilterTextBox.Text;
            if (!string.IsNullOrEmpty(filterText))
            {
                // フィルタ適用
                try
                {
                    var inverted = filterText.StartsWith("-");
                    filterText = inverted ? filterText.Substring(1) : filterText;
                    var pattern = new Regex(filterText, RegexOptions.IgnoreCase);
                    output = output.Where(t =>
                        inverted != (pattern.IsMatch($"{t.Tid}") || pattern.IsMatch($"{t.Name}")));
                }
                catch (ArgumentException ex)
                {
                    // 正規表現の不正
                    Trace.TraceError(ex.ToString());
                }
            }
            return output;
        }

        IEnumerable<ThreadViewData> GetTargetThreadViewData()
        {
            var output = new List<ThreadViewData>();
            var selectedProcesses = this.GetSelectedProcesses();
            if (selectedProcesses != null)
            {
                foreach (var p in selectedProcesses)
                {
                    output.AddRange(p.Threads.Select(t => new ThreadViewData(t, this.kCpuPeakRange)));
                }
            }
            else
            {
                // 何も選択されてなければ全部
                var device = this.sumacon.DeviceManager.ActiveDevice;
                if (device != null)
                {
                    foreach (var p in device.Processes)
                    {
                        output.AddRange(p.Threads.Select(t => new ThreadViewData(t, this.kCpuPeakRange)));
                    }
                }
            }
            return output;
        }

        void UpdateCpuUsage(TopSnapshot top)
        {
            foreach (var p in this.processList)
            {
                p.SetCpuUsage(top.GetProcessCpu(p.Pid));
            }
            this.uxProcessGridPanel.SortColumn(this.uxProcessGridPanel.SortedColumn, this.uxProcessGridPanel.SortOrder == SortOrder.Descending ? ListSortDirection.Descending : ListSortDirection.Ascending);

            foreach (var t in this.threadList)
            {
                t.SetCpuUsage(top.GetThreadCpu(t.Tid));
            }
            this.uxThreadGridPanel.SortColumn(this.uxThreadGridPanel.SortedColumn, this.uxThreadGridPanel.SortOrder == SortOrder.Descending ? ListSortDirection.Descending : ListSortDirection.Ascending);

            this.lastTop = top;
        }

        void UpdateProcessMeminfo()
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            var selectedProcesses = this.GetSelectedProcesses();
            if (device != null && selectedProcesses != null)
            {
                selectedProcesses = selectedProcesses.OrderBy(p=>p.Pid).Take(this.kProcessMeminfoMax);
                // 今回なくなったものは消す、増えたものはとりあえず0で出しておく(後ほど値設定)
                var newMeminfoList = selectedProcesses.Select(p => new MeminfoViewData(device.Processes[p.Pid]));
                var removes = this.meminfoList.Except(newMeminfoList, new MeminfoViewDataEqualityComparer()).ToArray();
                foreach (var m in removes) this.meminfoList.Remove(m);
                var adds = newMeminfoList.Except(this.meminfoList, new MeminfoViewDataEqualityComparer()).ToArray();
                foreach (var m in adds) this.meminfoList.Add(new MeminfoViewData(device.Processes[m.Pid]));

                foreach (var p in selectedProcesses)
                {
                    ProcessMeminfo.GetAsync(device, p, meminfo =>
                    {
                        this.SafeInvoke(() =>
                        {
                            // あれば上書き、もうなくなってたら何もしない
                            var viewData = new MeminfoViewData(meminfo, device.Processes[meminfo.Pid]);
                            for (int i = 0; i < this.meminfoList.Count; i++)
                            {
                                if(this.meminfoList[i].Pid == meminfo.Pid)
                                {
                                    this.meminfoList[i] = viewData;
                                    return;
                                }
                            }
                        });
                    });
                }
            }
            else
            {
                this.meminfoList.Clear();
            }
        }

        IEnumerable<ProcessEntry> GetSelectedProcesses()
        {
            var output = new List<ProcessEntry>();
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device != null)
            {
                foreach (DataGridViewRow row in this.uxProcessGridPanel.SelectedRows)
                {
                    var pid = (int)row.Cells[nameof(ProcessViewData.Pid)].Value;
                    var process = device.Processes[pid];
                    if (process != null)
                    {
                        output.Add(process);
                    }
                }
            }
            return output;
        }

        void UpdateControlState()
        {
            var processes = this.sumacon.DeviceManager.ActiveDevice?.Processes;

            int pShown = this.uxProcessGridPanel.RowCount;
            int pTotal = processes?.Count() ?? 0;
            int tShown = this.uxThreadGridPanel.RowCount;
            int tTotal = processes?.Sum(p => p.Threads.Count()) ?? 0;
            this.uxStatusLabel.Text = $"{pShown}/{pTotal} processes, {tShown}/{tTotal} threads. CPU:{this.lastTop?.TotalCpu ?? 0}%";

            this.uxProcessFilterClearButton.Enabled = !string.IsNullOrEmpty(this.uxProcessFilterTextBox.Text);
            this.uxThreadFilterClearButton.Enabled = !string.IsNullOrEmpty(this.uxThreadFilterTextBox.Text);
            this.uxProcessSelectionClearButton.Enabled = this.uxProcessGridPanel.SelectedRows.Count > 0;
        }
    }

    class ProcessViewData
    {
        public int Pid { get; private set; }
        public int Priority { get; private set; }
        public float Cpu { get; private set; }
        public float CpuPeak { get { return this.cpuHistory.Max(); } }
        //public uint Vss { get; private set; }
        //public uint Rss { get; private set; }
        public string User { get; private set; }
        public int ThreadCount { get; private set; }
        public string Name { get; private set; }

        Queue<float> cpuHistory = new Queue<float>(new[] { 0.0f });
        readonly int cpuPeakRange;

        public ProcessViewData(ProcessEntry pi, int cpuPeakRange)
        {
            this.Pid = pi.Pid;
            this.Priority = pi.Priority;
            this.Cpu = 0.0f;
            //this.Vss = pi.Vss;
            //this.Rss = pi.Rss;
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

    class ProcessViewDataEqualityComparer : IEqualityComparer<ProcessViewData>
    {
        public bool Equals(ProcessViewData a, ProcessViewData b) { return a?.Pid == b?.Pid; }
        public int GetHashCode(ProcessViewData p) { return p.Pid; }
    }

    class ThreadViewData
    {
        public int Tid { get; private set; }
        public int Priority { get; private set; }
        public float Cpu { get; private set; }
        public float CpuPeak { get { return this.cpuHistory.Max(); } }
        public string Name { get; private set; }

        Queue<float> cpuHistory = new Queue<float>(new[] { 0.0f });
        readonly int cpuPeakCount;

        public ThreadViewData(ThreadEntry ti, int cpuPeakCount)
        {
            this.Tid = ti.Tid;
            this.Priority = ti.Priority;
            this.Cpu = 0.0f;
            this.Name = ti.Name;
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

    class ThreadViewDataEqualityComparer : IEqualityComparer<ThreadViewData>
    {
        public bool Equals(ThreadViewData a, ThreadViewData b) { return a?.Tid == b?.Tid; }
        public int GetHashCode(ThreadViewData p) { return p.Tid; }
    }

    class MeminfoViewData
    {
        public int Pid { get; private set; }
        public string Process { get { return $"{this.processName} ({this.Pid})"; } }
        public int PssTotal { get; private set; }
        public int PssNativeHeap { get; private set; }
        public int PssDalvikHeap { get; private set; }
        public int UssTotal { get; private set; }
        public int UssEGL { get; private set; }
        public int UssGL { get; private set; }

        string processName;

        public MeminfoViewData(ProcessEntry process)
        {
            this.Pid = process.Pid;
            this.processName = process?.Name ?? "-";
        }

        public MeminfoViewData(ProcessMeminfo meminfo, ProcessEntry process)
        {
            this.Pid = meminfo.Pid;
            this.processName = process?.Name ?? "-";
            this.PssTotal = meminfo.Total.PssTotal;
            this.PssNativeHeap = meminfo.NativeHeap.PssTotal;
            this.PssDalvikHeap = meminfo.DalvikHeap.PssTotal;
            this.UssTotal = meminfo.Total.PrivateDirty + meminfo.Total.PrivateClean;
            this.UssEGL = meminfo.EglMtrack.PrivateDirty + meminfo.EglMtrack.PrivateClean;
            this.UssGL = meminfo.GlMtrack.PrivateDirty + meminfo.GlMtrack.PrivateClean;
        }
    }

    class MeminfoViewDataEqualityComparer : IEqualityComparer<MeminfoViewData>
    {
        public bool Equals(MeminfoViewData a, MeminfoViewData b) { return a?.Pid == b?.Pid; }
        public int GetHashCode(MeminfoViewData p) { return p.Pid; }
    }
}
