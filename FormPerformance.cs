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
        SortableBindingList<ProcessViewInfo> processList = new SortableBindingList<ProcessViewInfo>();
        SortableBindingList<ThreadViewInfo> threadList = new SortableBindingList<ThreadViewInfo>();
        TopContext topContext;

        readonly int kGridPanelColumnDefaultWidth = 70;
        readonly int kTopIntervalSeconds = 1;
        readonly int kCpuPeakRange = 10;

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
            this.SetupToolStrip();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnClosing(e);
            this.topContext?.Close();
            if (this.sumacon.DeviceManager.ActiveDevice != null)
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
                    this.topContext = TopContext.Start(device, this.kTopIntervalSeconds, this.TopContext_Received);
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

        void TopContext_Received(object sender, TopSnapshot top)
        {
            this.SafeInvoke(() => this.UpdateCpuUsage(top));
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

        void SetupProcessGridPanel()
        {
            var panel = this.processGridPanel;
            panel.Dock = DockStyle.Fill;
            panel.ApplyColorSet(this.colorSet);
            panel.DataSource = this.processList;
            panel.KeyColumnName = nameof(ProcessViewInfo.Pid);

            panel.SuppressibleSelectionChanged += this.ProcessGridPanel_SuppressibleSelectionChanged;

            this.uxProcessAndThreadSplitContainer.Panel1.Controls.Add(panel);

            panel.SetAllColumnWidth(this.kGridPanelColumnDefaultWidth);
            panel.SetDefaultCellStyle();
            panel.Columns[nameof(ProcessViewInfo.CpuPeak)].ToolTipText = $"Peak CPU usage (%) for the last {this.kTopIntervalSeconds * this.kCpuPeakRange} seconds.";
            panel.Columns[nameof(ProcessViewInfo.Name)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            var barPainter = new GridPanel.NumericCellPaintData()
            {
                Type = GridPanel.CellPaintType.Bar,
                PaintColor = this.colorSet.Accent1,
                MinValue = 0.0,
                MaxValue = 100.0
            };
            panel.Columns[nameof(ProcessViewInfo.Cpu)].Tag = barPainter;
            panel.Columns[nameof(ProcessViewInfo.CpuPeak)].Tag = barPainter;

            // デフォルトはCPU使用率の降順
            panel.SortColumn(panel.Columns[nameof(ProcessViewInfo.Cpu)], ListSortDirection.Descending);
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

            panel.SetAllColumnWidth(this.kGridPanelColumnDefaultWidth);
            panel.SetDefaultCellStyle();
            panel.Columns[nameof(ThreadViewInfo.CpuPeak)].ToolTipText = $"Peak CPU usage (%) for the last {this.kTopIntervalSeconds * this.kCpuPeakRange} seconds.";
            panel.Columns[nameof(ThreadViewInfo.Name)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            panel.Columns[nameof(ThreadViewInfo.ProcessName)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            var barPainter = new GridPanel.NumericCellPaintData()
            {
                Type = GridPanel.CellPaintType.Bar,
                PaintColor = this.colorSet.Accent1,
                MinValue = 0.0,
                MaxValue = 100.0
            };
            panel.Columns[nameof(ThreadViewInfo.Cpu)].Tag = barPainter;
            panel.Columns[nameof(ThreadViewInfo.CpuPeak)].Tag = barPainter;

            // デフォルトはCPU使用率の降順
            panel.SortColumn(panel.Columns[nameof(ThreadViewInfo.Cpu)], ListSortDirection.Descending);
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

            var pv = processInfos.Select(p => new ProcessViewInfo(p, this.kCpuPeakRange));

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

            var removes = this.processList.Except(pv, new ProcessInfoEqualityComparer()).ToArray();
            foreach (var p in removes) this.processList.Remove(p);
            var adds = pv.Except(this.processList, new ProcessInfoEqualityComparer()).ToArray();
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

            var tvList = new List<ThreadViewInfo>();
            var processRows = new List<DataGridViewRow>();
            foreach (DataGridViewRow row in this.processGridPanel.SelectedRows) processRows.Add(row);
            if (processRows.Count == 0)
            {
                // 何も選択されてなければ全部
                foreach (DataGridViewRow row in this.processGridPanel.Rows) processRows.Add(row);
            }

            foreach (DataGridViewRow row in processRows)
            {
                var processInfo = processInfos[(int)row.Cells[nameof(ProcessViewInfo.Pid)].Value];
                if (processInfo != null)
                {
                    tvList.AddRange(processInfo.Threads.Values.Select(t => new ThreadViewInfo(t, this.kCpuPeakRange)));
                }
            }

            var tv = (IEnumerable<ThreadViewInfo>)tvList;
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

            var removes = this.threadList.Except(tv, new ThreadInfoEqualityComparer()).ToArray();
            foreach (var t in removes) this.threadList.Remove(t);
            var adds = tv.Except(this.threadList, new ThreadInfoEqualityComparer()).ToArray();
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
            foreach (var t in this.threadList)
            {
                t.SetCpuUsage(top.GetThreadCpu(t.Tid));
            }
            Console.Beep(1000, 200);
        }
    }

    class ProcessViewInfo
    {
        public int Pid { get; private set; }
        public int Priority { get; private set; }
        public float Cpu { get; private set; }
        public float CpuPeak { get { return this.cpuHistory.Max(); } }
        public uint Vsize { get; private set; }
        public uint Rsize { get; private set; }
        public string User { get; private set; }
        public int ThreadCount { get; private set; }
        public string Name { get; private set; }

        Queue<float> cpuHistory = new Queue<float>(new[] { 0.0f });
        readonly int cpuPeakRange;

        public ProcessViewInfo(ProcessInfo pi, int cpuPeakRange)
        {
            this.Pid = pi.Pid;
            this.Priority = pi.Priority;
            this.Cpu = 0.0f;
            this.Vsize = pi.Vsize;
            this.Rsize = pi.Rsize;
            this.User = pi.User;
            this.ThreadCount = pi.Threads.Count;
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

    class ProcessInfoEqualityComparer : IEqualityComparer<ProcessViewInfo>
    {
        public bool Equals(ProcessViewInfo a, ProcessViewInfo b)
        {
            if (b == null && a == null)
                return true;
            else if (a == null || b == null)
                return false;
            else if (a.Pid == b.Pid)
                return true;
            else
                return false;
        }

        public int GetHashCode(ProcessViewInfo p)
        {
            return p.Pid;
        }
    }

    class ThreadViewInfo
    {
        public int Tid { get; private set; }
        public int Priority { get; private set; }
        public float Cpu { get; private set; }
        public float CpuPeak { get { return this.cpuHistory.Max(); } }
        public string Name { get; private set; }
        public string ProcessName { get; private set; }

        Queue<float> cpuHistory = new Queue<float>(new[] { 0.0f });
        readonly int cpuPeakRange;

        public ThreadViewInfo(ThreadInfo ti, int cpuPeakRange)
        {
            this.Tid = ti.Tid;
            this.Priority = ti.Priority;
            this.Cpu = 0.0f;
            this.Name = ti.Name;
            this.ProcessName = $"{ti.Process.Pid}: {ti.Process.Name}";
            this.cpuPeakRange = cpuPeakRange;
        }

        public void SetCpuUsage(float cpu)
        {
            this.Cpu = cpu;
            if (this.cpuHistory.Count >= this.cpuPeakRange)
            {
                this.cpuHistory.Dequeue();
            }
            this.cpuHistory.Enqueue(cpu);
        }
    }

    class ThreadInfoEqualityComparer : IEqualityComparer<ThreadViewInfo>
    {
        public bool Equals(ThreadViewInfo a, ThreadViewInfo b)
        {
            if (b == null && a == null)
                return true;
            else if (a == null || b == null)
                return false;
            else if (a.Tid == b.Tid)
                return true;
            else
                return false;
        }

        public int GetHashCode(ThreadViewInfo p)
        {
            return p.Tid;
        }
    }
}
