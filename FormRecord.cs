using Suconbu.Mobile;
using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WMPLib;

namespace Suconbu.Sumacon
{
    public partial class FormRecord : FormBase
    {
        RecordContext recordContext;
        DeviceManager deviceManager;
        GridPanel uxFileGridPanel;
        BindingList<RecordFileInfo> recordedFileInfos = new BindingList<RecordFileInfo>();
        List<RecordFileInfo> selectedFileInfos = new List<RecordFileInfo>();
        Timer timer = new Timer();

        public FormRecord(DeviceManager deviceManager)
        {
            InitializeComponent();

            this.deviceManager = deviceManager;

            this.uxSaveDirectoryText.Text = @".\screenrecord";

            this.uxFileGridPanel = new GridPanel();
            this.uxFileGridPanel.Dock = DockStyle.Fill;
            this.uxFileGridPanel.AutoGenerateColumns = false;
            this.uxFileGridPanel.DataSource = this.recordedFileInfos;
            this.uxFileGridPanel.Columns.Add(
                this.CreateColumn(Properties.Resources.General_Name, nameof(RecordFileInfo.Name), 240));
            this.uxFileGridPanel.Columns.Add(
                this.CreateColumn(Properties.Resources.General_Size, nameof(RecordFileInfo.KiroBytes), 50, "#,##0 KB"));
            this.uxFileGridPanel.Columns.Add(
                this.CreateColumn(Properties.Resources.General_TimeSecondsLength, nameof(RecordFileInfo.RecordingSeconds), 50));
            this.uxFileGridPanel.Columns.Add(
                this.CreateColumn(Properties.Resources.General_DateTime, nameof(RecordFileInfo.DateTime), 120, "G"));
            foreach (DataGridViewColumn column in this.uxFileGridPanel.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            this.uxFileGridPanel.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            //this.uxFileGridPanel.ContextMenuStrip = this.fileGridContextMenu;
            //this.uxFileGridPanel.SelectionChanged += this.FileGridPanel_SelectionChanged;
            //this.uxFileGridPanel.KeyDown += this.UxFileGridPanel_KeyDown;
            //this.uxFileGridPanel.CellDoubleClick += (s, ee) => this.OpenSelectedFile();
            this.uxSplitContainer.Panel1.Controls.Add(this.uxFileGridPanel);

            this.timer.Interval = 1000;
            this.timer.Tick += (s, e) => this.UpdateControlState();

            this.uxStartButton.Click += this.UxStartButton_Click;
        }

        DataGridViewColumn CreateColumn(string name, string propertyName, int minimulWidth = -1, string format = null)
        {
            var column = new DataGridViewTextBoxColumn();
            column.Name = name;
            column.DataPropertyName = propertyName;
            column.MinimumWidth = minimulWidth;
            if (format != null)
            {
                column.DefaultCellStyle.Format = format;
            }
            return column;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            //this.axWindowsMediaPlayer1.URL = new Uri("test2.mp4", UriKind.Relative).ToString();
            //this.axWindowsMediaPlayer1.Ctlcontrols.play();
            this.UpdateControlState();
        }

        private void UxStartButton_Click(object sender, EventArgs e)
        {
            if(this.recordContext == null)
            {
                var saveTo = Path.Combine(this.uxSaveDirectoryText.Text, $"{DateTime.Now.Ticks}.mp4");
                this.recordContext = new RecordContext(this.deviceManager.ActiveDevice, saveTo, 10);
                this.recordContext.Finised += (s, path) =>
                {
                    var seconds = this.recordContext.RecodingSeconds;
                    this.recordContext = null;
                    if (path == null) return;
                    var fileInfo = new FileInfo(path);
                    var recordFileInfo = new RecordFileInfo()
                    {
                        FullPath = fileInfo.FullName,
                        Name = fileInfo.Name,
                        DateTime = fileInfo.LastWriteTime,
                        KiroBytes = fileInfo.Length / 1024,
                        RecordingSeconds = seconds
                    };
                    this.SafeInvoke(this.UpdateControlState);
                };
                this.recordContext.Start();
                this.timer.Start();
            }
            else
            {
                this.timer.Stop();
                this.recordContext.Stop();
            }
            this.UpdateControlState();
        }

        void UpdateControlState()
        {
            if(this.recordContext != null)
            {
                // 録画中
                var durationSeconds = (this.recordContext.StartedAt != DateTime.MaxValue) ?
                    (int)(DateTime.Now - this.recordContext.StartedAt).TotalSeconds :
                    0;
                this.uxStartButton.Text = string.Format(
                    Properties.Resources.FormRecord_ButtonLabel_Stop,
                    durationSeconds);
            }
            else
            {
                this.uxStartButton.Text = Properties.Resources.FormRecord_ButtonLabel_Start;
            }
        }

        class RecordFileInfo
        {
            public string FullPath { get; set; }
            public string Name { get; set; }
            public long KiroBytes { get; set; }
            public DateTime DateTime { get; set; }
            public int RecordingSeconds { get; set; }
        }

        // Start -> (Recording...) -> Stop/Timeout -> (Copying...) -> Finished
        class RecordContext : IDisposable
        {
            public enum RecordState { WaitingForStart, Recording, Pulling, Finished }
            public event EventHandler<string> Finised = delegate { };

            public RecordState State = RecordState.WaitingForStart;
            public DateTime StartedAt { get; private set; } = DateTime.MaxValue;
            public int RecodingSeconds { get; private set; }
            //public TimeSpan RecordingTime
            //{
            //    get { return ((this.stoppedAt != DateTime.MinValue) ? this.stoppedAt : DateTime.Now) - this.startedAt; }
            //}

            Device device;
            string saveTo;
            CommandContext recordCommandContext;
            CommandContext pullCommandContext;
            //DateTime startedAt;
            //DateTime stoppedAt = DateTime.MinValue;
            int limitSeconds;
            string filePathInDevice;

            //readonly int limitSecondsMax = 180;
            readonly string deviceTemporaryDirectoryPath = "/sdcard";

            public RecordContext(Device device, string saveTo, int limitSeconds = int.MaxValue)
            {
                this.device = device;
                this.saveTo = saveTo;
                this.limitSeconds = (limitSeconds >= 1) ? limitSeconds : 1;
                this.filePathInDevice = $"{this.deviceTemporaryDirectoryPath}/testtt.mp4";
            }

            public void Start()
            {
                if (this.State != RecordState.WaitingForStart) return;

                this.State = RecordState.Recording;
                var directoryPath = Path.GetDirectoryName(this.saveTo);
                if(!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                this.StartedAt = DateTime.Now;
                Debug.Print(Util.GetCurrentMethodName(true));
                var option = string.Empty;
                if(this.limitSeconds != int.MaxValue)
                {
                    option = $"--time-limit {this.limitSeconds}";
                }
                this.recordCommandContext = this.device.RunCommandOutputTextAsync(
                    //"shell screenrecord --time-limit 10 /sdcard/testt.mp4", output =>
                    $"shell screenrecord {option} {this.filePathInDevice}", output =>
                {
                    this.recordCommandContext = null;
                    if (!string.IsNullOrEmpty(output))
                    {
                        // なにかエラー
                        Trace.TraceError(output);
                        this.Cancel();
                    }
                    else
                    {
                        this.OnRecordFinished();
                    }
                });
            }

            public void Stop()
            {
                if (this.State == RecordState.WaitingForStart) return;

                this.recordCommandContext?.Cancel();
                this.OnRecordFinished();
            }

            public void Cancel()
            {
                if (this.State != RecordState.WaitingForStart)
                {
                    this.Finised(this, null);
                }
                this.Dispose();
            }

            void OnRecordFinished()
            {
                if (this.State != RecordState.Recording) return;

                this.State = RecordState.Pulling;
                this.RecodingSeconds = (DateTime.Now - this.StartedAt).Seconds;//TODO: これは簡易的。実際の動画時間はGetDetailOfで取るべき。
                this.pullCommandContext = this.device.RunCommandOutputTextAsync(
                    $"pull {this.filePathInDevice} {this.saveTo}", output =>
                {
                    this.pullCommandContext = null;
                    if (output.ToLower().Contains("error"))
                    {
                        // なにかエラー
                        Trace.TraceError(output);
                        this.Cancel();
                    }
                    else
                    {
                        this.OnPullFinished();
                    }
                });
            }

            void OnPullFinished()
            {
                this.State = RecordState.Finished;
                this.Finised(this, this.saveTo);
            }

            #region IDisposable Support
            bool disposed = false;

            public virtual void Dispose()
            {
                if (this.disposed) return;

                this.State = RecordState.Finished;
                this.recordCommandContext?.Cancel();
                this.recordCommandContext = null;
                this.pullCommandContext?.Cancel();
                this.pullCommandContext = null;

                this.disposed = true;
            }
            #endregion
        }
    }
}
