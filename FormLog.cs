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
        DeviceManager deviceManager;
        //LogReceiverManager logReceiverManager;
        ToolStripDropDownButton uxPidDropDown = new ToolStripDropDownButton();
        GridPanel logGridPanel = new GridPanel();
        GridPanel countGridPanel = new GridPanel();
        ToolStripStatusLabel uxSelectedInfoLabel = new ToolStripStatusLabel();
        ToolStripStatusLabel uxSummaryInfoLabel = new ToolStripStatusLabel();
        //LogContext receiver;
        string logUpdateTimeoutId;
        string filterTimeoutId;
        List<Log> logCache = new List<Log>();
        string filterText = string.Empty;
        List<Log> filteredLogs;
        int logCacheStartIndex;
        LogContext logContext;
        //Dictionary<string, LogSubscriber> logSubscribers = new Dictionary<string, LogSubscriber>();
        LogSetting logSetting = new LogSetting() { StartAt = DateTime.Now };

        readonly int logUpdateIntervalMilliseconds = 100;
        readonly int filterDelayMilliseconds = 100;

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

            this.uxFilterTextBox.KeyDown += this.UxFilterText_KeyDown;

            this.uxAutoScrollCheck.CheckOnClick = true;
            this.uxAutoScrollCheck.Checked = true;
            this.uxAutoScrollCheck.Image = this.imageList1.Images["arrow_down.png"];
            this.uxAutoScrollCheck.CheckedChanged += (s, e) => this.AutoScrollEnabled = this.uxAutoScrollCheck.Checked;

            this.uxPidDropDown.DropDownItems.Add("u0_a13 - 18846 - jp.co.yahoo.android.apps.transit");
            this.uxPidDropDown.DropDownItems.Add("u0_a13 - 22553 - com.google.process.gapps");
            this.uxPidDropDown.DropDownItems.Add("u0_a13 - 25653 - com.google.android.apps.maps");
            //this.uxToolStrip.Items.Add(this.uxPidDropDown);
            this.uxPidDropDown.Text = this.uxPidDropDown.DropDownItems[0].Text;

            this.logGridPanel.Dock = DockStyle.Fill;
            var timestampColumn = this.logGridPanel.AddColumn("Timestamp");
            timestampColumn.Width = 120;
            timestampColumn.DefaultCellStyle.Format = "MM/dd HH:mm:ss.fff";
            var levelColumn = this.logGridPanel.AddColumn("Level");
            levelColumn.Width = 20;
            var pidColumn = this.logGridPanel.AddColumn("PID");
            pidColumn.Width = 40;
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
                var log = (this.logCache.Count>0) ? this.logCache[e.RowIndex - this.logCacheStartIndex] : null;
                if (log == null) return;
                // プロパティ名でアクセスしたい・・・
                e.Value =
                    (e.ColumnIndex == 0) ? (object)log.Timestamp :
                    (e.ColumnIndex == 1) ? (object)log.Priority :
                    (e.ColumnIndex == 2) ? (object)log.Pid :
                    (e.ColumnIndex == 3) ? (object)log.Tid :
                    (e.ColumnIndex == 4) ? (object)log.Tag :
                    (e.ColumnIndex == 5) ? (object)log.Message :
                    null;
            };
            this.logGridPanel.SuppressibleSelectionChanged += (s, e) => this.UpdateControlState();
            this.logGridPanel.Scroll += (s, e) => this.AutoScrollEnabled = this.LastRowIsVisible();
            this.logGridPanel.VirtualMode = true;

            this.uxSplitContainer.Panel1.Controls.Add(this.logGridPanel);
            this.uxSplitContainer.Panel2Collapsed = true;

            this.uxSelectedInfoLabel.Spring = true;
            this.uxSelectedInfoLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.statusStrip1.Items.Add(this.uxSelectedInfoLabel);
            this.uxSummaryInfoLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.statusStrip1.Items.Add(this.uxSummaryInfoLabel);
        }

        private void UxFilterText_KeyDown(object sender, KeyEventArgs e)
        {
            this.filterTimeoutId = Delay.SetTimeout(() =>
            {
                this.FilterText = this.uxFilterTextBox.Text;
            }, this.filterDelayMilliseconds, this, this.filterTimeoutId, true);
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
            if(this.filteredLogs != null)
            {
                if (Regex.IsMatch(log.Message.Trim(), this.filterText, RegexOptions.IgnoreCase))
                {
                    this.filteredLogs.Add(log);
                }
            }

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

        int FilterPid
        {
            get { return this.logSetting.Pid; }
            set
            {
                if(this.logSetting.Pid != value)
                {
                    this.logSetting.Pid = value;
                    this.ReopenLogContext();
                }
            }
        }

        string FilterText
        {
            get { return this.filterText; }
            set
            {
                this.uxFilterTextBox.Text = value;
                if (this.filterText != value)
                {
                    this.filterText = value;
                    try
                    {
                        // もしちゃんとした正規表現じゃなかったらfilteredLogsは前回のまま
                        Regex.IsMatch(string.Empty, this.filterText);
                    }
                    catch(ArgumentException)
                    {
                        return;
                    }
                    this.logCache.Clear();
                    this.filteredLogs = !string.IsNullOrEmpty(this.filterText) ?
                        this.logContext.GetRange().Where(log =>
                            Regex.IsMatch(log.Message.Trim(), this.filterText, RegexOptions.IgnoreCase)).ToList() :
                        null;
                    this.logGridPanel.RowCount = this.GetLogCount();
                    this.UpdateControlState();
                }
            }
        }

        bool AutoScrollEnabled
        {
            get { return this.uxAutoScrollCheck.Checked; }
            set
            {
                if (this.uxAutoScrollCheck.Checked != value)
                {
                    this.uxAutoScrollCheck.Checked = value;
                    if (this.uxAutoScrollCheck.Checked && this.logGridPanel.Rows.Count > 0)
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
            var rows = this.logGridPanel.SelectedRows;
            if (rows.Count > 0)
            {
                var first = this.GetLog(rows[0].Index, 1).FirstOrDefault();
                var last = this.GetLog(rows[rows.Count - 1].Index, 1).FirstOrDefault();
                double duration = 0.0;
                if (first != null && last != null)
                {
                    duration = Math.Abs((last.Timestamp - first.Timestamp).TotalMilliseconds);
                }
                this.uxSelectedInfoLabel.Text = $"{rows.Count:#,##0} logs selected ({duration:#,###0} ms)";
            }
            else
            {
                this.uxSelectedInfoLabel.Text = string.Empty;
            }
        }
    }
}
