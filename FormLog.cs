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
        LogReceiver receiver;
        string logUpdateTimeoutId;
        List<Log> logCache = new List<Log>();
        int logCacheStartIndex;
        LogSubscriber logSubscriber;

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
                    this.ChangeReceiveEnabled(previousActiveDevice, false);
                    this.ChangeReceiveEnabled(this.deviceManager.ActiveDevice, true);
                    this.UpdateControlState();
                });
            };

            this.logReceiverManager = logReceiverManager;
            //LogReceiverManager.Received += this.LogReceiverManager_Received;

            this.uxPauseCheck.CheckOnClick = true;
            this.uxPauseCheck.Checked = false;
            this.uxPauseCheck.Image = this.imageList1.Images["control_pause.png"];
            this.uxPauseCheck.CheckedChanged += (s, e) => this.ReceivingPaused = this.uxPauseCheck.Checked;

            this.uxAutoScrollCheck.CheckOnClick = true;
            this.uxAutoScrollCheck.Checked = true;
            this.uxAutoScrollCheck.Image = this.imageList1.Images["arrow_down.png"];
            //this.uxFilterLabel.Image = this.imageList1.Images["drink.png"];

            this.uxPidDropDown.DropDownItems.Add("u0_a13 - 18846 - jp.co.yahoo.android.apps.transit");
            this.uxPidDropDown.DropDownItems.Add("u0_a13 - 22553 - com.google.process.gapps");
            this.uxPidDropDown.DropDownItems.Add("u0_a13 - 25653 - com.google.android.apps.maps");
            //this.uxToolStrip.Items.Add(this.uxPidDropDown);
            this.uxPidDropDown.Text = this.uxPidDropDown.DropDownItems[0].Text;

            this.logGridPanel.Dock = DockStyle.Fill;
            this.logGridPanel.Columns.Add("Timestamp", "Timestamp");
            this.logGridPanel.Columns["Timestamp"].Width = 160;
            this.logGridPanel.Columns.Add("Type", "Type");
            this.logGridPanel.Columns.Add("PID", "PID");
            this.logGridPanel.Columns.Add("TID", "TID");
            this.logGridPanel.Columns.Add("Tag", "Tag");
            this.logGridPanel.Columns.Add("Message", "Message");
            this.logGridPanel.Columns["Message"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.logGridPanel.Click += (s, e) => this.AutoScrollEnabled = false;
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

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            this.ChangeReceiveEnabled(this.deviceManager.ActiveDevice, this.Visible);
        }

        void OnLogReceived(object sender, LogReceiveEventArgs e)
        {
            this.receiver = e.Receiver;
            this.logUpdateTimeoutId = Delay.SetTimeout(() =>
            {
                this.logGridPanel.RowCount = this.receiver.Count;
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

        void ChangeReceiveEnabled(Device device, bool enabled)
        {
            if (device == null) return;
            if (enabled)
            {
                if (this.logSubscriber == null || this.logSubscriber.Suspended)
                {
                    this.deviceManager.SuspendObserve(device);
                }
                var setting = this.GetReceiveSetting();
                this.logSubscriber = this.logSubscriber ?? this.logReceiverManager.AddSubscriber(device, setting, this.OnLogReceived);
            }
            else
            {
                if (!this.logSubscriber.Suspended)
                {
                    this.deviceManager.ResumeObserve(device);
                }
            }

            if (this.logSubscriber != null)
            {
                this.logSubscriber.Suspended = !enabled;
            }
        }

        void ResumeReceive(Device device)
        {
            if (device == null) return;
            this.deviceManager.SuspendObserve(device);
            if(this.logSubscriber == null)
            {
                var setting = this.GetReceiveSetting();
                this.logSubscriber = this.logReceiverManager.AddSubscriber(device, setting, this.OnLogReceived);
            }
            this.logSubscriber.Suspended = false;
        }

        void SuspendReceive(Device device)
        {
            if (device == null) return;
            this.logSubscriber.Suspended = true;
            this.deviceManager.ResumeObserve(device);
        }

        bool ReceivingPaused
        {
            get { return this.uxPauseCheck.Checked; }
            set
            {
                this.uxPauseCheck.Checked = value;
                var device = this.deviceManager.ActiveDevice;
                if (this.uxPauseCheck.Checked)
                {
                    this.SuspendReceive(device);
                }
                else
                {
                    this.ResumeReceive(device);
                }
            }
        }

        bool AutoScrollEnabled
        {
            get { return this.uxAutoScrollCheck.Checked; }
            set { this.uxAutoScrollCheck.Checked = value; }
        }

        LogReceiveSetting GetReceiveSetting()
        {
            var setting = new LogReceiveSetting();
            //TODO: 設定取得
            return setting;
        }

        void UpdateControlState()
        {
            var device = this.deviceManager.ActiveDevice;
            this.uxToolStrip.Enabled = (device != null);
        }
    }
}
