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
        class FilterSetting
        {
            public enum FilterField { Pid, Tid, Tag, Message }

            public Dictionary<Log.PriorityCode, bool> EnabledByPriority { get; set; } = new Dictionary<Log.PriorityCode, bool>();
            public Dictionary<FilterField, string> Filters { get; set; } = new Dictionary<FilterField, string>();
            public Dictionary<FilterField, bool> FilterInverteds { get; set; } = new Dictionary<FilterField, bool>();

            public bool IsFilterEnabled()
            {
                return
                    this.EnabledByPriority.Any(pair => !pair.Value) ||
                    this.Filters.Any(pair => !string.IsNullOrEmpty(pair.Value));
            }
        }

        struct LogColumnNames
        {
            public static string Bookmark = nameof(Bookmark);
            public static string No = nameof(No);
            public static string Timestamp = nameof(Timestamp);
            public static string Priority = nameof(Priority);
            public static string Pid = nameof(Pid);
            public static string Tid = nameof(Tid);
            public static string Tag = nameof(Tag);
            public static string Message = nameof(Message);
        }

        DataGridViewColumn[] logColumns = new DataGridViewColumn[]
        {
            new DataGridViewImageColumn() { Name = LogColumnNames.Bookmark, Width = 20 },
            new DataGridViewColumn() { Name = LogColumnNames.No, HeaderText = "No", Width = 40, CellTemplate = new DataGridViewTextBoxCell() },
            new DataGridViewColumn() { Name = LogColumnNames.Timestamp, HeaderText = "Timestamp", Width = 120, CellTemplate = new DataGridViewTextBoxCell() },
            new DataGridViewColumn() { Name = LogColumnNames.Priority, HeaderText = "Priority", Width = 20, CellTemplate = new DataGridViewTextBoxCell() },
            new DataGridViewColumn() { Name = LogColumnNames.Pid, HeaderText = "PID", Width = 100, CellTemplate = new DataGridViewTextBoxCell() },
            new DataGridViewColumn() { Name = LogColumnNames.Tid, HeaderText = "TID", Width = 100, CellTemplate = new DataGridViewTextBoxCell() },
            new DataGridViewColumn() { Name = LogColumnNames.Tag, HeaderText = "Tag", Width = 100, CellTemplate = new DataGridViewTextBoxCell() },
            new DataGridViewColumn() { Name = LogColumnNames.Message, HeaderText = "Message", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, CellTemplate = new DataGridViewTextBoxCell() },
        };

        Sumacon sumacon;
        Dictionary<Log.PriorityCode, ToolStripButton> priorityFilterButtons = new Dictionary<Log.PriorityCode, ToolStripButton>();
        ToolStripTextBox[] filterTextBoxes;
        ToolStripTextBox uxPidFilterTextBox = new ToolStripTextBox();
        ToolStripTextBox uxTidFilterTextBox = new ToolStripTextBox();
        ToolStripTextBox uxTagFilterTextBox = new ToolStripTextBox();
        ToolStripTextBox uxMessageFilterTextBox = new ToolStripTextBox();
        ToolStripButton uxAutoScrollButton = new ToolStripButton();
        ToolStripButton uxMarkedListButton = new ToolStripButton();
        GridPanel uxLogGridPanel = new GridPanel();
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
        //ProcessInfo selectedProcessInfo = null;
        FilterSetting filterSetting = new FilterSetting();
        BindingList<Log> bookmarkedLogs = new BindingList<Log>();

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

        public FormLog(Sumacon sumacon)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.sumacon = sumacon;
            this.sumacon.DeviceManager.ActiveDeviceChanged += this.DeviceManager_ActiveDeviceChanged;

            this.SetupToolStrip();
            this.SetupLogGridPanel();
            this.SetupStatusStrip();
        }

        protected override void OnLoad(EventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnLoad(e);

            this.LoadSettings();
            this.ApplyFilterSetting();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnClosing(e);
            this.CloseLogContext();
            this.sumacon.DeviceManager.ActiveDeviceChanged -= this.DeviceManager_ActiveDeviceChanged;

            this.SaveSettings();
        }

        void DeviceManager_ActiveDeviceChanged(object sender, Device previousDevice)
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
            if (device != null)
            {
                device.InvokeIfPropertyIsReady(Device.UpdatableProperties.ProcessInfo, () => this.SafeInvoke(() =>
                {
                    this.OpenLogContext(device);
                    this.UpdateControlState();
                }));
            }
            else
            {
                this.SafeInvoke(() =>
                {
                    this.CloseLogContext();
                    this.UpdateControlState();
                });
            }
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
                this.priorityFilterButtons[pair.Key] = button;
                this.uxToolStrip.Items.Add(button);
            }

            this.uxToolStrip.Items.Add(new ToolStripSeparator());

            this.filterTextBoxes = new[] { this.uxPidFilterTextBox, this.uxTidFilterTextBox, this.uxTagFilterTextBox, this.uxMessageFilterTextBox };
            foreach (var item in this.filterTextBoxes)
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

            var uxClearFilterButton = new ToolStripButton("Clear");
            uxClearFilterButton.Click += (s, e) => this.ClearFilter();
            this.uxToolStrip.Items.Add(uxClearFilterButton);

            this.uxToolStrip.Items.Add(new ToolStripSeparator());

            this.uxAutoScrollButton.Text = "Auto scroll";
            this.uxAutoScrollButton.CheckOnClick = true;
            this.uxAutoScrollButton.Checked = true;
            this.uxAutoScrollButton.Image = this.imageList1.Images["arrow_down.png"];
            this.uxAutoScrollButton.CheckedChanged += (s, e) => this.AutoScrollEnabled = this.uxAutoScrollButton.Checked;
            this.uxToolStrip.Items.Add(this.uxAutoScrollButton);

            this.uxToolStrip.Items.Add(new ToolStripSeparator());

            var uxToggleBookmark = new ToolStripButton("", this.imageList1.Images["flag_blue.png"], (s, e) => this.ToggleBookmark(this.GetSelectedLogs().FirstOrDefault()));
            uxToggleBookmark.ToolTipText = "Add/Remove bookmark a selected log (Space)";
            this.uxToolStrip.Items.Add(uxToggleBookmark);
            var uxJumpPrevBookmark = new ToolStripButton("", this.imageList1.Images["flag_blue_back.png"], (s, e) => this.JumpPrevBookmark());
            uxJumpPrevBookmark.ToolTipText = "Move previous bookmark (Ctrl + Up)";
            this.uxToolStrip.Items.Add(uxJumpPrevBookmark);
            var uxJumpNextBookmark = new ToolStripButton("", this.imageList1.Images["flag_blue_go.png"], (s, e) => this.JumpNextBookmark());
            uxJumpNextBookmark.ToolTipText = "Move next bookmark (Ctrl + Down)";
            this.uxToolStrip.Items.Add(uxJumpNextBookmark);
            var uxClearBookmark = new ToolStripButton("", this.imageList1.Images["flag_blue_delete.png"], (s, e) => this.ClearBookmark());
            uxClearBookmark.ToolTipText = "Clear all bookmarks";
            this.uxToolStrip.Items.Add(uxClearBookmark);
        }

        void SetupLogGridPanel()
        {
            this.uxLogGridPanel.Dock = DockStyle.Fill;
            foreach(var column in this.logColumns)
            {
                this.uxLogGridPanel.Columns.Add(column);
            }
            this.uxLogGridPanel.Columns[LogColumnNames.Bookmark].DefaultCellStyle.NullValue = null;
            this.uxLogGridPanel.Columns[LogColumnNames.Timestamp].DefaultCellStyle.Format = "MM/dd HH:mm:ss.fff";
            this.uxLogGridPanel.ApplyColorSet(this.colorSet);
            this.uxLogGridPanel.Click += (s, e) => this.AutoScrollEnabled = this.LastRowIsVisible();
            this.uxLogGridPanel.MouseWheel += (s, e) => this.AutoScrollEnabled = this.LastRowIsVisible();
            this.uxLogGridPanel.KeyDown += (s, e) => this.AutoScrollEnabled = this.LastRowIsVisible();
            this.uxLogGridPanel.KeyDown += (s, e) =>
            {
                if(ModifierKeys == Keys.Control && e.KeyCode == Keys.Up)
                {
                    this.JumpPrevBookmark();
                    e.SuppressKeyPress = true;
                }
                else if (ModifierKeys == Keys.Control && e.KeyCode == Keys.Down)
                {
                    this.JumpNextBookmark();
                    e.SuppressKeyPress = true;
                }
                else if(e.KeyCode == Keys.Space)
                {
                    this.ToggleBookmark(this.GetSelectedLogs().FirstOrDefault());
                }
                else
                {
                    ;
                }
            };
            this.uxLogGridPanel.CellValueNeeded += (s, e) =>
            {
                if (this.filteredLogs == null && this.logContext == null) return;

                if (e.RowIndex < this.logCacheStartIndex ||
                    (this.logCacheStartIndex + this.logCache.Count) <= e.RowIndex)
                {
                    // 表示領域の上下に50%ずつの余裕
                    int cacheCount = this.uxLogGridPanel.DisplayedRowCount(true) * 4;
                    int startIndex = e.RowIndex - (cacheCount / 2);
                    this.logCache = this.GetLog(startIndex, cacheCount);
                    this.logCacheStartIndex = Math.Max(0, startIndex);
                }
                var cacheIndex = e.RowIndex - this.logCacheStartIndex;
                var log = this.logCache.ElementAtOrDefault(e.RowIndex - this.logCacheStartIndex);
                if (log == null) return;

                var name = this.uxLogGridPanel.Columns[e.ColumnIndex].Name;
                e.Value =
                    (name == LogColumnNames.Bookmark) ? (this.bookmarkedLogs.Contains(log) ? this.imageList1.Images["flag_blue.png"] : null) :
                    (name == LogColumnNames.No) ? (object)log.No :
                    (name == LogColumnNames.Timestamp) ? (object)log.Timestamp :
                    (name == LogColumnNames.Priority) ? (object)log.Priority :
                    (name == LogColumnNames.Pid) ? (object)$"{log.Pid}:{log.ProcessName}" :
                    (name == LogColumnNames.Tid) ? (object)$"{log.Tid}:{log.ThreadName}":
                    (name == LogColumnNames.Tag) ? (object)log.Tag :
                    (name == LogColumnNames.Message) ? (object)log.Message :
                    null;
            };
            this.uxLogGridPanel.RowPrePaint += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    var log = this.GetLog(e.RowIndex);
                    if (log != null)
                    {
                        var row = this.uxLogGridPanel.Rows[e.RowIndex];
                        if (this.colorByPriorities.TryGetValue(log.Priority, out var color))
                        {
                            row.DefaultCellStyle.ForeColor = color;
                            row.DefaultCellStyle.SelectionForeColor = color;
                        }
                    }
                }
            };
            this.uxLogGridPanel.SuppressibleSelectionChanged += (s, e) => this.UpdateControlState();
            this.uxLogGridPanel.Scroll += (s, e) => this.AutoScrollEnabled = this.LastRowIsVisible();
            this.uxLogGridPanel.VirtualMode = true;
            this.uxLogGridPanel.CellDoubleClick += (s, e) => this.ToggleBookmark(this.GetLog(e.RowIndex));

            this.uxToolStripContainer.ContentPanel.Controls.Add(this.uxLogGridPanel);
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
                this.ApplyFilterSetting();
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

        //TODO: フィルタしてないときでもFormLogでログ持つほうがいい
        void OnLogReceived(object sender, Log log)
        {
            this.filteredLogs?.AddRange(this.GetFilteredLogs(new[] { log }));

            this.logUpdateTimeoutId = Delay.SetTimeout(() =>
            {
                this.uxLogGridPanel.RowCount = this.GetLogCount();
                if (this.AutoScrollEnabled && this.uxLogGridPanel.RowCount > 0)
                {
                    this.uxLogGridPanel.FirstDisplayedScrollingRowIndex = this.uxLogGridPanel.RowCount - 1;
                }
                this.UpdateControlState();
            }, this.logUpdateIntervalMilliseconds, this, this.logUpdateTimeoutId);
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

        void ApplyFilterSetting()
        {
            foreach (var pair in this.priorityFilterButtons)
            {
                this.filterSetting.EnabledByPriority[pair.Key] = pair.Value.Checked;
            }

            if (this.IsValidFilter(this.uxPidFilterTextBox.Text))
            {
                this.filterSetting.Filters[FilterSetting.FilterField.Pid] = this.uxPidFilterTextBox.Text;
            }
            if (this.IsValidFilter(this.uxTidFilterTextBox.Text))
            {
                this.filterSetting.Filters[FilterSetting.FilterField.Tid] = this.uxTidFilterTextBox.Text;
            }
            if (this.IsValidFilter(this.uxTagFilterTextBox.Text))
            {
                this.filterSetting.Filters[FilterSetting.FilterField.Tag] = this.uxTagFilterTextBox.Text;
            }
            if (this.IsValidFilter(this.uxMessageFilterTextBox.Text))
            {
                this.filterSetting.Filters[FilterSetting.FilterField.Message] = this.uxMessageFilterTextBox.Text;
            }

            foreach (FilterSetting.FilterField field in Enum.GetValues(typeof(FilterSetting.FilterField)))
            {
                // 先頭に'-'がついていたら除外フィルタ
                this.filterSetting.FilterInverteds[field] = this.filterSetting.Filters[field].StartsWith("-");
                if (this.filterSetting.FilterInverteds[field])
                {
                    this.filterSetting.Filters[field] = this.filterSetting.Filters[field].Substring(1);
                }
                else
                {
                    this.filterSetting.Filters[field] = this.filterSetting.Filters[field].TrimStart();
                }
            }

            this.logCache.Clear();
            this.filteredLogs = this.filterSetting.IsFilterEnabled() ?
                this.GetFilteredLogs((this.logContext?.GetRange()).OrEmptyIfNull()).ToList() :
                null;
            // いったん0にしてから設定すると速い
            this.uxLogGridPanel.RowCount = 0;
            this.uxLogGridPanel.RowCount = this.GetLogCount();

            this.UpdateControlState();
        }

        IEnumerable<Log> GetFilteredLogs(IEnumerable<Log> input)
        {
            var setting = this.filterSetting;

            var logs = input.Where(log => setting.EnabledByPriority[log.Priority]);

            var pidFilter = setting.Filters[FilterSetting.FilterField.Pid];
            if (!string.IsNullOrEmpty(pidFilter))
            {
                logs = logs.Where(log => setting.FilterInverteds[FilterSetting.FilterField.Pid] != Regex.IsMatch($"{log.Pid}:{log.ProcessName}", pidFilter, RegexOptions.IgnoreCase));
            }

            var tidFilter = setting.Filters[FilterSetting.FilterField.Tid];
            if (!string.IsNullOrEmpty(tidFilter))
            {
                logs = logs.Where(log => setting.FilterInverteds[FilterSetting.FilterField.Tid] != Regex.IsMatch($"{log.Tid}:{log.ThreadName}", tidFilter, RegexOptions.IgnoreCase));
            }

            var tagFilter = setting.Filters[FilterSetting.FilterField.Tag];
            if (!string.IsNullOrEmpty(tagFilter))
            {
                logs = logs.Where(log => setting.FilterInverteds[FilterSetting.FilterField.Tag] != Regex.IsMatch(log.Tag, tagFilter, RegexOptions.IgnoreCase));
            }

            var messageFilter = setting.Filters[FilterSetting.FilterField.Message];
            if (!string.IsNullOrEmpty(messageFilter))
            {
                logs = logs.Where(log => setting.FilterInverteds[FilterSetting.FilterField.Message] != Regex.IsMatch(log.Message, messageFilter, RegexOptions.IgnoreCase));
            }

            return logs;
        }

        void OpenLogContext(Device device)
        {
            this.logCache.Clear();
            this.uxLogGridPanel.RowCount = 0;
            this.CloseLogContext();
            if (this.sumacon.DeviceManager.ActiveDevice != null)
            {
                this.logContext = LogContext.Open(device, this.logSetting);
                this.logContext.Received += this.OnLogReceived;
            }
            this.FilterSettingChanged(this, EventArgs.Empty);

            this.UpdateControlState();
        }

        void CloseLogContext()
        {
            if (this.logContext == null) return;
            this.logContext.Received -= this.OnLogReceived;
            this.logContext.Close();
            this.logContext = null;
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
                this.logContext?.GetRange(safeIndex, safeCount) ?? new List<Log>();
        }

        Log GetLog(int index)
        {
            return this.GetLog(index, 1).FirstOrDefault();
        }

        int GetLogIndex(Log log)
        {
            return (this.filteredLogs != null) ? this.filteredLogs.FindIndex(l => l == log) : log.No;
        }

        bool AutoScrollEnabled
        {
            get { return this.uxAutoScrollButton.Checked; }
            set
            {
                if (this.uxAutoScrollButton.Checked != value)
                {
                    this.uxAutoScrollButton.Checked = value;
                    if (this.uxAutoScrollButton.Checked && this.uxLogGridPanel.Rows.Count > 0)
                    {
                        this.uxLogGridPanel.FirstDisplayedScrollingRowIndex = this.uxLogGridPanel.Rows.Count - 1;
                    }
                }
            }
        }

        bool LastRowIsVisible()
        {
            return (this.uxLogGridPanel.RowCount - this.uxLogGridPanel.FirstDisplayedScrollingRowIndex) <= this.uxLogGridPanel.DisplayedRowCount(true);
        }

        List<Log> GetSelectedLogs()
        {
            var logs = new List<Log>();
            foreach(DataGridViewRow row in this.uxLogGridPanel.SelectedRows)
            {
                logs.AddRange(this.GetLog(row.Index, 1));
            }
            return logs;
        }

        void ToggleBookmark(Log log)
        {
            if (log == null) return;

            if (!this.bookmarkedLogs.Remove(log))
            {
                this.bookmarkedLogs.Add(log);
            }
            var index = this.GetLogIndex(log);
            if (index >= 0)
            {
                this.uxLogGridPanel.InvalidateRow(index);
            }
        }

        void JumpPrevBookmark()
        {
            var selectedLog = this.GetSelectedLogs().FirstOrDefault();
            if (selectedLog == null) return;

            foreach (var log in this.bookmarkedLogs.OrderByDescending(log => log.No))
            {
                if (selectedLog.No > log.No && this.GetLogIndex(log) >= 0)
                {
                    this.SetDisplayedLog(log);
                    break;
                }
            }
        }

        void JumpNextBookmark()
        {
            var selectedLog = this.GetSelectedLogs().FirstOrDefault();
            if (selectedLog == null) return;

            foreach (var log in this.bookmarkedLogs.OrderBy(log => log.No))
            {
                if (selectedLog.No < log.No && this.GetLogIndex(log) >= 0)
                {
                    this.SetDisplayedLog(log);
                    break;
                }
            }
        }

        void ClearBookmark()
        {
            foreach(var log in this.bookmarkedLogs)
            {
                var index = this.GetLogIndex(log);
                if (index >= 0)
                {
                    this.uxLogGridPanel.InvalidateRow(index);
                }
            }
            this.bookmarkedLogs.Clear();
        }

        void SetDisplayedLog(Log log)
        {
            var index = this.GetLogIndex(log);
            if (index < 0) return;
            this.uxLogGridPanel.CenterDisplayedRowIndex = index;
            this.uxLogGridPanel.ClearSelection();
            this.uxLogGridPanel.Rows[index].Selected = true;
            this.uxLogGridPanel.CurrentCell = this.uxLogGridPanel.Rows[index].Cells[0];
        }

        void ClearFilter()
        {
            foreach (var button in this.priorityFilterButtons.Values)
            {
                button.Checked = true;
            }
            foreach (var text in this.filterTextBoxes)
            {
                text.Clear();
            }
        }

        void UpdateControlState()
        {
            var device = this.sumacon.DeviceManager.ActiveDevice;
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

        void LoadSettings()
        {
            this.priorityFilterButtons[Log.PriorityCode.F].Checked = Properties.Settings.Default.LogFilterPriorityF;
            this.priorityFilterButtons[Log.PriorityCode.E].Checked = Properties.Settings.Default.LogFilterPriorityE;
            this.priorityFilterButtons[Log.PriorityCode.W].Checked = Properties.Settings.Default.LogFilterPriorityW;
            this.priorityFilterButtons[Log.PriorityCode.I].Checked = Properties.Settings.Default.LogFilterPriorityI;
            this.priorityFilterButtons[Log.PriorityCode.D].Checked = Properties.Settings.Default.LogFilterPriorityD;
            this.priorityFilterButtons[Log.PriorityCode.V].Checked = Properties.Settings.Default.LogFilterPriorityV;
        }

        void SaveSettings()
        {
            Properties.Settings.Default.LogFilterPriorityF = this.priorityFilterButtons[Log.PriorityCode.F].Checked;
            Properties.Settings.Default.LogFilterPriorityE = this.priorityFilterButtons[Log.PriorityCode.E].Checked;
            Properties.Settings.Default.LogFilterPriorityW = this.priorityFilterButtons[Log.PriorityCode.W].Checked;
            Properties.Settings.Default.LogFilterPriorityI = this.priorityFilterButtons[Log.PriorityCode.I].Checked;
            Properties.Settings.Default.LogFilterPriorityD = this.priorityFilterButtons[Log.PriorityCode.D].Checked;
            Properties.Settings.Default.LogFilterPriorityV = this.priorityFilterButtons[Log.PriorityCode.V].Checked;
        }
    }
}
