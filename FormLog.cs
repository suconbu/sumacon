using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Concurrent;
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
    public partial class FormLog : FormBase
    {
        struct FilterSetting
        {
            public Dictionary<Log.PriorityCode, bool> EnabledByPriority;
            public string PidFilter;
            public string TidFilter;
            public string TagFilter;
            public string MessageFilter;

            public bool IsFilterEnabled()
            {
                return
                    this.EnabledByPriority.Any(pair => !pair.Value) ||
                    !string.IsNullOrEmpty(this.PidFilter) ||
                    !string.IsNullOrEmpty(this.TagFilter) ||
                    !string.IsNullOrEmpty(this.MessageFilter);
            }
        }

        DeviceManager deviceManager;
        Dictionary<Log.PriorityCode, ToolStripButton> uxPriorityFilterButtons = new Dictionary<Log.PriorityCode, ToolStripButton>();
        ToolStripTextBox uxPidFilterTextBox = new ToolStripTextBox();
        ToolStripTextBox uxTidFilterTextBox = new ToolStripTextBox();
        ToolStripTextBox uxTagFilterTextBox = new ToolStripTextBox();
        ToolStripTextBox uxMessageFilterTextBox = new ToolStripTextBox();
        ToolStripButton uxClearFilterButton = new ToolStripButton();
        ToolStripButton uxAutoScrollButton = new ToolStripButton();
        GridPanel logGridPanel = new GridPanel();
        GridPanel countGridPanel = new GridPanel();
        ToolStripStatusLabel uxSelectedInfoLabel = new ToolStripStatusLabel();
        ToolStripStatusLabel uxSummaryInfoLabel = new ToolStripStatusLabel();
        string logUpdateTimeoutId;
        string filterSettingChangedTimeoutId;
        List<Log> logCache = new List<Log>();
        string filterText = string.Empty;
        List<Log> filteredLogs;
        int logCacheStartIndex;
        LogContext logContext;
        LogSetting logSetting = new LogSetting() { StartAt = DateTime.Now };
        ColorSet colorSet = ColorSet.Light;
        ProcessInfo selectedProcessInfo = ProcessInfo.Empty;
        FilterSetting filterSetting;

        readonly int logUpdateIntervalMilliseconds = 100;
        readonly int filterSettingChangedDelayMilliseconds = 100;
        readonly Dictionary<Log.PriorityCode, Color> colorByPriorities = new Dictionary<Log.PriorityCode, Color>()
        {
            { Log.PriorityCode.F, Color.Red },
            { Log.PriorityCode.E, Color.Red },
            { Log.PriorityCode.W, Color.DarkOrange },
            { Log.PriorityCode.I, Color.Green },
            { Log.PriorityCode.D, Color.DarkBlue },
            { Log.PriorityCode.V, Color.Black }
        };

        public FormLog(DeviceManager deviceManager)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.filterSetting.EnabledByPriority = new Dictionary<Log.PriorityCode, bool>();

            this.deviceManager = deviceManager;
            this.deviceManager.ActiveDeviceChanged += (s, previousActiveDevice) =>
            {
                this.SafeInvoke(() =>
                {
                    this.ReopenLogContext();
                    this.UpdateControlState();
                });
            };

            this.SetupToolStrip();
            this.SetupLogGridPanel();
            this.SetupStatusStrip();
        }

        void SetupToolStrip()
        {
            foreach (var pair in this.colorByPriorities)
            {
                var button = new ToolStripButton(pair.Key.ToString());
                button.CheckOnClick = true;
                button.Checked = true;
                button.ForeColor = pair.Value;
                button.CheckedChanged += this.FilterSettingChanged;
                this.uxPriorityFilterButtons[pair.Key] = button;
                this.uxToolStrip.Items.Add(button);
            }

            this.uxToolStrip.Items.Add(new ToolStripSeparator());

            foreach (var item in new[] { this.uxPidFilterTextBox, this.uxTidFilterTextBox, this.uxTagFilterTextBox, this.uxMessageFilterTextBox })
            {
                item.AutoSize = false;
                item.TextChanged += this.FilterSettingChanged;
                item.Click += (s, e) => ((ToolStripTextBox)s).SelectAll();
                item.GotFocus += (s, e) => ((ToolStripTextBox)s).SelectAll();
            }

            this.uxToolStrip.Items.Add(new ToolStripLabel("PID:"));
            this.uxPidFilterTextBox.Width = 60;
            this.uxToolStrip.Items.Add(this.uxPidFilterTextBox);

            this.uxToolStrip.Items.Add(new ToolStripLabel("TID:"));
            this.uxTidFilterTextBox.Width = 60;
            this.uxToolStrip.Items.Add(this.uxTidFilterTextBox);

            this.uxToolStrip.Items.Add(new ToolStripLabel("Tag:"));
            this.uxTagFilterTextBox.Width = 60;
            this.uxToolStrip.Items.Add(this.uxTagFilterTextBox);

            this.uxToolStrip.Items.Add(new ToolStripLabel("Message:"));
            this.uxMessageFilterTextBox.Width = 120;
            this.uxToolStrip.Items.Add(this.uxMessageFilterTextBox);

            this.uxToolStrip.Items.Add(new ToolStripSeparator());

            this.uxAutoScrollButton.Text = "Auto scroll";
            this.uxAutoScrollButton.CheckOnClick = true;
            this.uxAutoScrollButton.Checked = true;
            this.uxAutoScrollButton.Image = this.imageList1.Images["arrow_down.png"];
            this.uxAutoScrollButton.CheckedChanged += (s, e) => this.AutoScrollEnabled = this.uxAutoScrollButton.Checked;
            this.uxToolStrip.Items.Add(this.uxAutoScrollButton);
        }

        void SetupLogGridPanel()
        {
            this.logGridPanel.Dock = DockStyle.Fill;
            var timestampColumn = this.logGridPanel.AddColumn("Timestamp");
            timestampColumn.Width = 120;
            timestampColumn.DefaultCellStyle.Format = "MM/dd HH:mm:ss.fff";
            var levelColumn = this.logGridPanel.AddColumn("Level");
            levelColumn.Width = 20;
            var pidColumn = this.logGridPanel.AddColumn("PID");
            pidColumn.Width = 120;// 40;
            var tidColumn = this.logGridPanel.AddColumn("TID");
            tidColumn.Width = 40;
            var tagColumn = this.logGridPanel.AddColumn("Tag");
            var messageColumn = this.logGridPanel.AddColumn("Message");
            messageColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.logGridPanel.Click += (s, e) => this.AutoScrollEnabled = this.LastRowIsVisible();
            this.logGridPanel.MouseWheel += (s, e) => this.AutoScrollEnabled = this.LastRowIsVisible();
            this.logGridPanel.KeyDown += (s, e) => this.AutoScrollEnabled = this.LastRowIsVisible();
            this.logGridPanel.CellValueNeeded += (s, e) =>
            {
                if (this.filteredLogs == null && this.logContext == null) return;

                if (e.RowIndex < this.logCacheStartIndex ||
                    (this.logCacheStartIndex + this.logCache.Count) <= e.RowIndex)
                {
                    // 表示領域の上下に50%ずつの余裕
                    int cacheCount = this.logGridPanel.DisplayedRowCount(true) * 2;
                    int startIndex = e.RowIndex - (cacheCount / 4);
                    this.logCache = this.GetLog(startIndex, cacheCount);
                    this.logCacheStartIndex = Math.Max(0, startIndex);
                }
                var log = (this.logCache.Count > 0) ? this.logCache[e.RowIndex - this.logCacheStartIndex] : null;
                if (log == null) return;
                // プロパティ名でアクセスしたい・・・
                e.Value =
                    (e.ColumnIndex == 0) ? (object)log.Timestamp :
                    (e.ColumnIndex == 1) ? (object)log.Priority :
                    (e.ColumnIndex == 2) ? (object)$"{log.Pid}:{log.ProcessName}" :
                    (e.ColumnIndex == 3) ? (object)log.Tid :
                    (e.ColumnIndex == 4) ? (object)log.Tag :
                    (e.ColumnIndex == 5) ? (object)log.Message :
                    null;
            };
            //this.logGridPanel.CellPainting += (s, e) =>
            this.logGridPanel.RowPrePaint += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    var log = this.GetLog(e.RowIndex, 1).FirstOrDefault();
                    if (log != null)
                    {
                        if (colorByPriorities.TryGetValue(log.Priority, out var color))
                        {
                            this.logGridPanel.Rows[e.RowIndex].DefaultCellStyle.ForeColor = color;
                            this.logGridPanel.Rows[e.RowIndex].DefaultCellStyle.SelectionForeColor = color;
                        }
                    }
                }
            };
            this.logGridPanel.SuppressibleSelectionChanged += (s, e) => this.UpdateControlState();
            this.logGridPanel.Scroll += (s, e) => this.AutoScrollEnabled = this.LastRowIsVisible();
            this.logGridPanel.VirtualMode = true;
            this.logGridPanel.ApplyColorSet(this.colorSet);

            this.uxSplitContainer.Panel1.Controls.Add(this.logGridPanel);
            this.uxSplitContainer.Panel2Collapsed = true;
        }

        void SetupStatusStrip()
        {
            this.uxSelectedInfoLabel.Spring = true;
            this.uxSelectedInfoLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.statusStrip1.Items.Add(this.uxSelectedInfoLabel);
            this.uxSummaryInfoLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.statusStrip1.Items.Add(this.uxSummaryInfoLabel);
        }

        void FilterSettingChanged(object sender, EventArgs e)
        {
            this.filterSettingChangedTimeoutId = Delay.SetTimeout(() =>
            {
                foreach (var pair in this.uxPriorityFilterButtons)
                {
                    this.filterSetting.EnabledByPriority[pair.Key] = pair.Value.Checked;
                }

                this.filterSetting.PidFilter = this.IsValidFilter(this.uxPidFilterTextBox.Text) ? this.uxPidFilterTextBox.Text : this.filterSetting.PidFilter;
                this.filterSetting.TidFilter = this.IsValidFilter(this.uxTidFilterTextBox.Text) ? this.uxTidFilterTextBox.Text : this.filterSetting.TidFilter;
                this.filterSetting.TagFilter = this.IsValidFilter(this.uxTagFilterTextBox.Text) ? this.uxTagFilterTextBox.Text : this.filterSetting.TagFilter;
                this.filterSetting.MessageFilter = this.IsValidFilter(this.uxMessageFilterTextBox.Text) ? this.uxMessageFilterTextBox.Text : this.filterSetting.MessageFilter;

                this.logCache.Clear();
                this.filteredLogs =
                    this.filterSetting.IsFilterEnabled() ?
                    this.GetFilteredLogs(this.logContext.GetRange()).ToList() :
                    null;
                // 直接設定すると時間掛かるので一旦0に
                this.logGridPanel.RowCount = 0;
                this.logGridPanel.RowCount = this.GetLogCount();

                this.UpdateControlState();
            }, this.filterSettingChangedDelayMilliseconds, this, this.filterSettingChangedTimeoutId, true);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.logContext != null)
            {
                this.logContext.Suspended = !this.Visible;
            }
        }

        void OnLogReceived(object sender, Log log)
        {
            this.filteredLogs?.AddRange(this.GetFilteredLogs(new[] { log }));

            this.logUpdateTimeoutId = Delay.SetTimeout(() =>
            {
                this.logGridPanel.RowCount = this.GetLogCount();
                if (this.AutoScrollEnabled && this.logGridPanel.RowCount > 0)
                {
                    this.logGridPanel.FirstDisplayedScrollingRowIndex = this.logGridPanel.RowCount - 1;
                }
                this.UpdateControlState();
            }, this.logUpdateIntervalMilliseconds, this, this.logUpdateTimeoutId);
        }

        protected override void OnLoad(EventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnLoad(e);
            this.UpdateControlState();
        }

        bool IsValidFilter(string filter)
        {
            try
            {
                Regex.IsMatch(string.Empty, filter);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        IEnumerable<Log> GetFilteredLogs(IEnumerable<Log> input)
        {
            var setting = this.filterSetting;

            var logs = input.Where(log => setting.EnabledByPriority[log.Priority]);

            if (!string.IsNullOrEmpty(setting.PidFilter))
            {
                logs = logs.Where(log =>
                    Regex.IsMatch($"{log.Pid}:{log.ProcessName}", setting.PidFilter, RegexOptions.IgnoreCase));
            }

            if (!string.IsNullOrEmpty(setting.TidFilter))
            {
                logs = logs.Where(log =>
                    Regex.IsMatch($"{log.Tid}", setting.TidFilter, RegexOptions.IgnoreCase));
            }

            if (!string.IsNullOrEmpty(setting.TagFilter))
            {
                logs = logs.Where(log => Regex.IsMatch(log.Tag, setting.TagFilter, RegexOptions.IgnoreCase));
            }

            if (!string.IsNullOrEmpty(setting.MessageFilter))
            {
                logs = logs.Where(log => Regex.IsMatch(log.Message, setting.MessageFilter, RegexOptions.IgnoreCase));
            }

            return logs;
        }

        void ReopenLogContext()
        {
            this.logCache.Clear();
            this.logGridPanel.RowCount = 0;
            if (this.logContext != null)
            {
                this.logContext.Received -= this.OnLogReceived;
                this.logContext.Close();
            }
            this.logContext = LogContext.Open(this.deviceManager.ActiveDevice, this.logSetting);
            this.logContext.Received += this.OnLogReceived;
            this.UpdateControlState();
        }

        int GetLogCount()
        {
            return (this.filteredLogs != null) ? this.filteredLogs.Count : (this.logContext?.Count ?? 0);
        }

        List<Log> GetLog(int index, int count)
        {
            int logCount = (this.filteredLogs != null) ? this.filteredLogs.Count : this.logContext?.Count ?? 0;
            int safeIndex = Math.Max(0, Math.Min(index, logCount - 1));
            int safeCount = Math.Min(index + count, logCount) - safeIndex;
            return (this.filteredLogs != null) ?
                this.filteredLogs.GetRange(safeIndex, safeCount) :
                this.logContext?.GetRange(safeIndex, safeCount);
        }

        bool AutoScrollEnabled
        {
            get { return this.uxAutoScrollButton.Checked; }
            set
            {
                if (this.uxAutoScrollButton.Checked != value)
                {
                    this.uxAutoScrollButton.Checked = value;
                    if (this.uxAutoScrollButton.Checked && this.logGridPanel.Rows.Count > 0)
                    {
                        this.logGridPanel.FirstDisplayedScrollingRowIndex = this.logGridPanel.Rows.Count - 1;
                    }
                }
            }
        }

        bool LastRowIsVisible()
        {
            return (this.logGridPanel.RowCount - this.logGridPanel.FirstDisplayedScrollingRowIndex) <= this.logGridPanel.DisplayedRowCount(true);
        }

        List<Log> GetSelectedLogs()
        {
            var logs = new List<Log>();
            foreach(DataGridViewRow row in this.logGridPanel.SelectedRows)
            {
                logs.AddRange(this.GetLog(row.Index, 1));
            }
            return logs;
        }

        void UpdateControlState()
        {
            var device = this.deviceManager.ActiveDevice;
            this.uxToolStrip.Enabled = (device != null);

            var totalLogCount = this.logContext?.Count ?? 0;
            if (this.filteredLogs != null)
            {
                this.uxSummaryInfoLabel.Text = $"{this.filteredLogs.Count:#,##0} / {totalLogCount:#,##0} logs";
            }
            else
            {
                this.uxSummaryInfoLabel.Text = $"{totalLogCount:#,##0} logs";
            }

            var selectedLogs = this.GetSelectedLogs();
            if (selectedLogs.Count > 0)
            {
                var ordered = selectedLogs.OrderBy(log => log.Timestamp);
                double duration = Math.Abs((ordered.Last().Timestamp - ordered.First().Timestamp).TotalMilliseconds);
                this.uxSelectedInfoLabel.Text = $"{selectedLogs.Count:#,##0} logs selected ({duration:#,###0} ms)";
            }
            else
            {
                this.uxSelectedInfoLabel.Text = string.Empty;
            }
        }
    }
}
