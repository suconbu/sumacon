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

        DeviceManager deviceManager;
        Dictionary<Log.PriorityCode, ToolStripButton> uxPriorityFilterButtons = new Dictionary<Log.PriorityCode, ToolStripButton>();
        ToolStripTextBox uxPidFilterTextBox = new ToolStripTextBox();
        ToolStripTextBox uxTidFilterTextBox = new ToolStripTextBox();
        ToolStripTextBox uxTagFilterTextBox = new ToolStripTextBox();
        ToolStripTextBox uxMessageFilterTextBox = new ToolStripTextBox();
        ToolStripButton uxClearFilterButton = new ToolStripButton();
        ToolStripButton uxAutoScrollButton = new ToolStripButton();
        ToolStripButton uxMarkedListButton = new ToolStripButton();
        GridPanel uxLogGridPanel = new GridPanel();
        //GridPanel uxMarkedListGridPanel = new GridPanel();
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
        FilterSetting filterSetting = new FilterSetting();
        BindingList<Log> bookmarkedLogs = new BindingList<Log>();
        //Dictionary<Log, int> bookmarkedLogIndices = new Dictionary<Log, int>();
        //bool markedListOperated = false;

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

            this.deviceManager = deviceManager;
            this.deviceManager.ActiveDeviceChanged += (s, previousActiveDevice) =>
            {
                this.SafeInvoke(() =>
                {
                    this.ReopenLogContext();
                    this.UpdateControlState();
                });
            };

            this.uxSplitContainer.Orientation = Orientation.Horizontal;

            this.SetupToolStrip();
            this.SetupLogGridPanel();
            //this.SetupMarkedListPanel();
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

            //this.uxMarkedListButton.Text = "Marked list";
            //this.uxMarkedListButton.CheckOnClick = true;
            //this.uxMarkedListButton.Checked = false;
            //this.uxMarkedListButton.Image = this.imageList1.Images["flag_blue.png"];
            //this.uxMarkedListButton.CheckedChanged += (s, e) =>
            //{
            //    this.markedListOperated = true;
            //    this.uxSplitContainer.Panel2Collapsed = !this.uxMarkedListButton.Checked;
            //};
            //this.uxToolStrip.Items.Add(this.uxMarkedListButton);
        }

        void SetupLogGridPanel()
        {
            this.uxLogGridPanel.Dock = DockStyle.Fill;
            var imageColumn = new DataGridViewImageColumn();
            imageColumn.DefaultCellStyle.NullValue = null;
            imageColumn.Width = 20;
            this.uxLogGridPanel.Columns.Add(imageColumn);
            var noColumn = this.uxLogGridPanel.AddColumn("No");
            noColumn.Width = 40;
            var timestampColumn = this.uxLogGridPanel.AddColumn("Timestamp");
            timestampColumn.Width = 120;
            timestampColumn.DefaultCellStyle.Format = "MM/dd HH:mm:ss.fff";
            var levelColumn = this.uxLogGridPanel.AddColumn("Level");
            levelColumn.Width = 20;
            var pidColumn = this.uxLogGridPanel.AddColumn("PID");
            pidColumn.Width = 120;// 40;
            var tidColumn = this.uxLogGridPanel.AddColumn("TID");
            tidColumn.Width = 40;
            var tagColumn = this.uxLogGridPanel.AddColumn("Tag");
            var messageColumn = this.uxLogGridPanel.AddColumn("Message");
            messageColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
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
                    int cacheCount = this.uxLogGridPanel.DisplayedRowCount(true) * 2;
                    int startIndex = e.RowIndex - (cacheCount / 4);
                    this.logCache = this.GetLog(startIndex, cacheCount);
                    this.logCacheStartIndex = Math.Max(0, startIndex);
                }
                var log = (this.logCache.Count > 0) ? this.logCache[e.RowIndex - this.logCacheStartIndex] : null;
                if (log == null) return;
                // プロパティ名でアクセスしたい・・・
                e.Value =
                    (e.ColumnIndex == 1) ? (object)log.No :
                    (e.ColumnIndex == 2) ? (object)log.Timestamp :
                    (e.ColumnIndex == 3) ? (object)log.Priority :
                    (e.ColumnIndex == 4) ? (object)$"{log.Pid}:{log.ProcessName}" :
                    (e.ColumnIndex == 5) ? (object)log.Tid :
                    (e.ColumnIndex == 6) ? (object)log.Tag :
                    (e.ColumnIndex == 7) ? (object)log.Message :
                    null;
                if (e.ColumnIndex == 0)
                {
                    e.Value = this.bookmarkedLogs.Contains(log) ? this.imageList1.Images["flag_blue.png"] : null;
                }
            };
            //this.logGridPanel.CellPainting += (s, e) =>
            this.uxLogGridPanel.RowPrePaint += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    var log = this.GetLog(e.RowIndex, 1).FirstOrDefault();
                    if (log != null)
                    {
                        var row = this.uxLogGridPanel.Rows[e.RowIndex];
                        if (colorByPriorities.TryGetValue(log.Priority, out var color))
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
            this.uxLogGridPanel.ApplyColorSet(this.colorSet);
            this.uxLogGridPanel.CellDoubleClick += (s, e) => this.ToggleBookmark(this.GetLog(e.RowIndex));

            this.uxSplitContainer.Panel1.Controls.Add(this.uxLogGridPanel);
            this.uxSplitContainer.Panel2Collapsed = true;
        }

        //void SetupMarkedListPanel()
        //{
        //    this.uxMarkedListGridPanel.Dock = DockStyle.Fill;
        //    this.uxMarkedListGridPanel.AutoGenerateColumns = true;
        //    this.uxMarkedListGridPanel.DataSource = this.bookmarkedLogs;
        //    this.uxMarkedListGridPanel.SelectionChanged += (s, e) =>
        //    {
        //        int count = this.uxMarkedListGridPanel.SelectedRows.Count;
        //        if(count > 0)
        //        {
        //            var log = this.bookmarkedLogs[this.uxMarkedListGridPanel.SelectedRows[count - 1].Index];
        //            if (this.bookmarkedLogIndices.TryGetValue(log, out var index))
        //            {
        //                this.SetDisplayedLogIndex(index);
        //            }
        //        }
        //    };
        //    this.uxSplitContainer.Panel2.Controls.Add(this.uxMarkedListGridPanel);
        //}

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
                    this.filterSetting.FilterInverteds[field] = this.filterSetting.Filters[field].StartsWith("-");
                    if(this.filterSetting.FilterInverteds[field])
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
                    this.GetFilteredLogs(this.logContext.GetRange()).ToList() :
                    null;
                // いったん0にしてから設定すると速い
                this.uxLogGridPanel.RowCount = 0;
                this.uxLogGridPanel.RowCount = this.GetLogCount();

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
                this.uxLogGridPanel.RowCount = this.GetLogCount();
                if (this.AutoScrollEnabled && this.uxLogGridPanel.RowCount > 0)
                {
                    this.uxLogGridPanel.FirstDisplayedScrollingRowIndex = this.uxLogGridPanel.RowCount - 1;
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

            var pidFilter = setting.Filters[FilterSetting.FilterField.Pid];
            if (!string.IsNullOrEmpty(pidFilter))
            {
                logs = setting.FilterInverteds[FilterSetting.FilterField.Pid] ?
                    logs.Where(log => !Regex.IsMatch($"{log.Pid}:{log.ProcessName}", pidFilter, RegexOptions.IgnoreCase)) :
                    logs.Where(log => Regex.IsMatch($"{log.Pid}:{log.ProcessName}", pidFilter, RegexOptions.IgnoreCase));
            }

            var tidFilter = setting.Filters[FilterSetting.FilterField.Tid];
            if (!string.IsNullOrEmpty(tidFilter))
            {
                logs = setting.FilterInverteds[FilterSetting.FilterField.Tid] ?
                    logs.Where(log => !Regex.IsMatch($"{log.Tid}", tidFilter, RegexOptions.IgnoreCase)) :
                    logs.Where(log => Regex.IsMatch($"{log.Tid}", tidFilter, RegexOptions.IgnoreCase));
            }

            var tagFilter = setting.Filters[FilterSetting.FilterField.Tag];
            if (!string.IsNullOrEmpty(tagFilter))
            {
                logs = setting.FilterInverteds[FilterSetting.FilterField.Tag] ?
                    logs.Where(log => !Regex.IsMatch(log.Tag, tagFilter, RegexOptions.IgnoreCase)) :
                    logs.Where(log => Regex.IsMatch(log.Tag, tagFilter, RegexOptions.IgnoreCase));
            }

            var messageFilter = setting.Filters[FilterSetting.FilterField.Message];
            if (!string.IsNullOrEmpty(messageFilter))
            {
                logs = setting.FilterInverteds[FilterSetting.FilterField.Message] ?
                    logs.Where(log => !Regex.IsMatch(log.Message, messageFilter, RegexOptions.IgnoreCase)) :
                    logs.Where(log => Regex.IsMatch(log.Message, messageFilter, RegexOptions.IgnoreCase));
            }

            return logs;
        }

        void ReopenLogContext()
        {
            this.logCache.Clear();
            this.uxLogGridPanel.RowCount = 0;
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

        //List<uint> GetSelectedLogNos()
        //{
        //    var logNos = new List<uint>();
        //    foreach (DataGridViewRow row in this.uxLogGridPanel.SelectedRows)
        //    {
        //        var log = this.GetLog(row.Index);
        //        if (log != null)
        //        {
        //            logNos.Add(log.No);
        //        }
        //    }
        //    return logNos.OrderBy(no => no).ToList();
        //}

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

            //if (!this.markedListOperated)
            //{
            //    this.uxMarkedListButton.Checked = true;
            //}
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
            //this.ViewLog(-1);
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
            //this.bookmarkedLogIndices.Clear();
        }

        void SetDisplayedLog(Log log)
        {
            //if(index < 0)
            //{
            //    index = this.uxLogGridPanel.RowCount + index;
            //}
            var index = this.GetLogIndex(log);
            if (index < 0) return;
            this.uxLogGridPanel.CenterDisplayedRowIndex = index;
            this.uxLogGridPanel.ClearSelection();
            this.uxLogGridPanel.Rows[index].Selected = true;
            this.uxLogGridPanel.CurrentCell = this.uxLogGridPanel.Rows[index].Cells[0];
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
