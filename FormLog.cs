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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Suconbu.Sumacon
{
    public partial class FormLog : FormBase
    {
        DeviceManager deviceManager;
        LogReceiverManager logReceiverManager;
        ToolStripDropDownButton uxPidDropDown = new ToolStripDropDownButton();
        GridPanel logGridPanel = new GridPanel();
        GridPanel countGridPanel = new GridPanel();
        LogReceiveContext receiver;
        string logUpdateTimeoutId;
        List<Log> logCache = new List<Log>();
        int logCacheStartIndex;
        Dictionary<string, LogSubscriber> logSubscribers = new Dictionary<string, LogSubscriber>();
        LogReceiveSetting setting = new LogReceiveSetting() { StartAt = DateTime.Now };

        readonly int logUpdateIntervalMilliseconds = 100;

        public FormLog(DeviceManager deviceManager, LogReceiverManager logReceiverManager)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            InitializeComponent();

            this.deviceManager = deviceManager;
            this.deviceManager.ActiveDeviceChanged += (s, previousActiveDevice) =>
            {
                this.SafeInvoke(() =>
                {
                    this.StopReceive(previousActiveDevice);
                    this.StartReceive(this.deviceManager.ActiveDevice);
                    this.UpdateControlState();
                });
            };

            this.logReceiverManager = logReceiverManager;
            //LogReceiverManager.Received += this.LogReceiverManager_Received;

            this.uxFilterPatternText.KeyDown += this.UxFilterPatternText_KeyDown;

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
            this.logGridPanel.Click += (s, e) => this.AutoScrollEnabled = false;
            this.logGridPanel.MouseWheel += (s, e) => this.AutoScrollEnabled = false;
            this.logGridPanel.KeyDown += (s, e) => this.AutoScrollEnabled = false;
            this.logGridPanel.CellValueNeeded += (s, e) =>
            {
                if(this.receiver != null)
                {
                    Log log;
                    if (e.RowIndex < this.logCacheStartIndex ||
                        (this.logCacheStartIndex + this.logCache.Count) <= e.RowIndex)
                    {
                        this.logCache = receiver.GetRange(e.RowIndex, 100);
                        this.logCacheStartIndex = e.RowIndex;
                    }
                    log = this.logCache[e.RowIndex - this.logCacheStartIndex];
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
                }
            };
            this.logGridPanel.VirtualMode = true;

            this.uxSplitContainer.Panel1.Controls.Add(this.logGridPanel);
        }

        private void UxFilterPatternText_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                this.FilterPattern = this.uxFilterPatternText.Text;
                e.SuppressKeyPress = true;
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible)
            {
                this.StartReceive(this.deviceManager.ActiveDevice);
            }
            else
            {
                this.StopReceive(this.deviceManager.ActiveDevice);
            }
        }

        void OnLogReceived(object sender, LogReceiveEventArgs e)
        {
            this.receiver = e.Receiver;
            this.logUpdateTimeoutId = Delay.SetTimeout(() =>
            {
                this.logGridPanel.RowCount = this.receiver?.Count ?? 0;
                if(this.AutoScrollEnabled)
                {
                    this.logGridPanel.FirstDisplayedScrollingRowIndex = this.logGridPanel.RowCount - 1;
                }
            }, this.logUpdateIntervalMilliseconds, this, this.logUpdateTimeoutId);
        }

        protected override void OnLoad(EventArgs e)
        {
            Trace.TraceInformation(Util.GetCurrentMethodName());
            base.OnLoad(e);
            this.UpdateControlState();
        }

        void StartReceive(Device device)
        {
            if (device == null) return;
            if (!this.logSubscribers.TryGetValue(device.Id, out var subscriber) || subscriber.Suspended)
            {
                this.deviceManager.SuspendObserve(device);
            }
            if (subscriber == null)
            {
                subscriber = this.logReceiverManager.NewSubscriber(device, this.setting, this.OnLogReceived);
                this.logSubscribers.Add(device.Id, subscriber);
            }
            subscriber.Suspended = false;
        }

        void StopReceive(Device device)
        {
            if (device == null) return;
            if (this.logSubscribers.TryGetValue(device.Id, out var subscriber))
            {
                if (!subscriber.Suspended)
                {
                    this.deviceManager.ResumeObserve(subscriber.Device);
                    subscriber.Suspended = true;
                    this.receiver = null;
                }
            }
        }

        void RestartReceive(Device device)
        {
            if (device == null) return;
            if (this.logSubscribers.TryGetValue(device.Id, out var subscriber))
            {
                this.logSubscribers.Remove(subscriber.Device.Id);
                subscriber.Dispose();
            }
            this.StartReceive(device);
        }

        int FilterPid
        {
            get { return this.setting.Pid; }
            set
            {
                if(this.setting.Pid != value)
                {
                    this.setting.Pid = value;
                    this.RestartReceive(this.deviceManager.ActiveDevice);
                }
            }
        }

        string FilterPattern
        {
            get { return this.setting.FilterPattern; }
            set
            {
                this.uxFilterPatternText.Text = value;
                if (this.setting.FilterPattern != value)
                {
                    this.setting.FilterPattern = value;
                    this.RestartReceive(this.deviceManager.ActiveDevice);
                }
            }
        }

        bool AutoScrollEnabled
        {
            get { return this.uxAutoScrollCheck.Checked; }
            set
            {
                this.uxAutoScrollCheck.Checked = value;
                if(this.uxAutoScrollCheck.Checked && this.logGridPanel.Rows.Count > 0)
                {
                    this.logGridPanel.FirstDisplayedScrollingRowIndex = this.logGridPanel.Rows.Count - 1;
                }
            }
        }

        void UpdateControlState()
        {
            var device = this.deviceManager.ActiveDevice;
            this.uxToolStrip.Enabled = (device != null);
        }
    }
}
