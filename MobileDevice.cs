using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SharpAdbClient;
using SharpAdbClient.Logs;
using Suconbu.Toolbox;

namespace Suconbu.MobileDebugging
{
    class MobileDevice
    {
        // deviceName, connectState(true:Connect, false:Disconnect)
        public event Action<string, bool> DeviceConnectChanged = delegate { };

        public static string SupportedToolName { get { return "adb"; } }

        public int DeviceInfoUpdateIntervalMilliseconds
        {
            get { return (int)this.deviceInfoUpdateTimer.Interval; }
            set
            {
                this.deviceInfoUpdateTimer.Interval = value;
                this.deviceInfoUpdateTimer.Enabled = value > 0;
            }
        }
        // e.g. Nexus_9
        public string Model { get { return this.targetDevice?.Model; } }
        // e.g. Tamachan
        public string Name { get { return this.targetDevice?.Name; } }
        // e.g. HXC8KSKL24PZB
        public string Id { get { return this.targetDevice?.Serial; } }
        // Display width/height[px]
        public Size DisplaySize { get { return this.displayInfo.Size; } }
        // DPI
        public int DisplayDensity { get { return this.displayInfo.Density; } }
        // 0-100%
        public float BatteryLevel { get { return this.batteryInfo.Level; } }
        // [Celsius degrees]
        public float BetteryTemperature { get { return this.batteryInfo.Temperature; } }

        public float CpuUsage { get; private set; }

        public bool LogReceiving { get { return this.logReceiver != null; } }

        AdbClient client;
        DeviceData targetDevice;
        DeviceMonitor monitor;
        DisplayInfo displayInfo;
        BatteryInfo batteryInfo;
        CancellationTokenSource logReceiveCanceler;
        LogReceiver logReceiver;
        System.Timers.Timer deviceInfoUpdateTimer;

        readonly string remoteWorkingDirectory = "/sdcard";
        readonly string localWorkingDirectory = ".";

        public static MobileDevice Open(string toolPath, string targetDeviceId = null)
        {
            AdbServer.Instance.StartServer(toolPath, false);

            var instance = new MobileDevice();

            instance.client = new AdbClient();
            instance.targetDevice = string.IsNullOrEmpty(targetDeviceId) ?
                instance.client.GetDevices().First() :
                instance.client.GetDevices().Find(d => d.Name == targetDeviceId);

            instance.monitor = new DeviceMonitor(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)));
            instance.monitor.DeviceConnected += (s, e) => { instance.DeviceConnectChanged(e.Device.Name, true); };
            instance.monitor.DeviceDisconnected += (s, e) => { instance.DeviceConnectChanged(e.Device.Name, false); };
            instance.monitor.Start();

            //instance.deviceInfoUpdateTimer = new Timer(instance.UpdateDeviceInfo, instance, instance.DeviceInfoUpdateIntervalMilliseconds, instance.DeviceInfoUpdateIntervalMilliseconds);
            instance.deviceInfoUpdateTimer = new System.Timers.Timer();
            instance.deviceInfoUpdateTimer.Elapsed += (s, e) =>
            {
                // 定期的に更新必要なものはここで
                BatteryInfo.GetAsync(instance.Id, info => instance.batteryInfo = info);
                Trace.TraceInformation($"displayInfo: {instance.displayInfo}");
                Trace.TraceInformation($"batteryInfo: {instance.batteryInfo}");
            };

            DisplayInfo.GetAsync(instance.Id, info => instance.displayInfo = info);

            return instance;
        }

        public string[] GetConnectedDeviceIds()
        {
            return this.client.GetDevices().Select(d => d.Serial).ToArray();
        }

        void GetDeviceInfo()
        {
        }

        public void StartLogReceive(Action<LogEntry> handler)
        {
            this.logReceiveCanceler?.Cancel();
            this.logReceiveCanceler = new CancellationTokenSource();
            this.logReceiver = new LogReceiver(handler);
            this.client.ExecuteRemoteCommandAsync("logcat", this.targetDevice, this.logReceiver, this.logReceiveCanceler.Token, -1);
        }

        public void StopLogReceive()
        {
            this.logReceiveCanceler?.Cancel();
            this.logReceiveCanceler = null;
            this.logReceiver = null;
        }

        public void GetScreenCaptureAsync(Action<Image> captured)
        {
            //AdbShell.ExecuteAsync(this.Id, "shell screencap -p", s =>
            //{
            //    Trace.TraceInformation(s);
            //});
            AdbShell.ExecuteBinaryOutputAsync(this.Id, "shell screencap -p", stream =>
            {
                //var buffer = new byte[stream.Length];
                //stream.Read(buffer, 0, (int)stream.Length);
                //File.WriteAllBytes("test.png", buffer);
                var image = Bitmap.FromStream(stream);
                captured?.Invoke(image);
            });
        }

        public void StartScreenRecord(string localPath, Action<string> finished, float durationSeconds = 0)
        {

        }

        public void StopScreenRecord()
        {

        }
    }

    class AdbShell
    {
        public Task Task { get { return this.process.Task; } }

        CommandProcess process;

        static readonly string commandName = "adb";

        public static AdbShell ExecuteAsync(string deviceId, string command, Action<string> outputHandler)
        {
            var instance = new AdbShell();
            instance.process = CommandProcess.ExecuteAsync(commandName, $"-s {deviceId} {command}", outputHandler, false);
            return instance;
        }

        public static AdbShell ExecuteTextOutputAsync(string deviceId, string command, Action<string> outputHandler)
        {
            var instance = new AdbShell();
            instance.process = CommandProcess.ExecuteAsync(commandName, $"-s {deviceId} {command}", outputHandler, true);
            return instance;
        }

        public static AdbShell ExecuteBinaryOutputAsync(string deviceId, string command, Action<Stream> outputHandler)
        {
            var instance = new AdbShell();
            instance.process = CommandProcess.ExecuteOutputBinaryAsync(commandName, $"-s {deviceId} {command}", outputHandler);
            return instance;
        }

        public void Wait()
        {
            this.process.Wait();
        }

        public void Cancel()
        {
            this.process.Cancel();
        }
    }

    class LogReceiver : IShellOutputReceiver
    {
        public bool ParsesErrors => throw new NotImplementedException();
        //object sender;
        Action<LogEntry> handler = delegate { };

        public LogReceiver(Action<LogEntry> handler)
        {
            //this.sender = sender;
            this.handler = handler;
        }

        public void AddOutput(string line)
        {
            this.handler(new LogEntry(line));
        }

        public void Flush()
        {
            ;// throw new NotImplementedException();
        }
    }

    enum LogLevel { Verbose, Debug, Info, Warn, Error, Assert }

    struct LogEntry
    {
        public uint Id;
        public int ProcessId;
        public int ThreadId;
        public LogLevel Level;
        public DateTime Time;
        public string Message;

        public LogEntry(string s)
        {
            this.Id = 0;
            this.ProcessId = 0;
            this.ThreadId = 0;
            this.Level = 0;
            this.Time = DateTime.Now;
            this.Message = s;
        }
    }

    class DisplayInfo
    {
        public Size Size { get; private set; }
        public int Density { get; private set; }

        public static void GetAsync(string deviceId, Action<DisplayInfo> handler)
        {
            Task.Run(() =>
            {
                var instance = new DisplayInfo();
                var tasks = new List<Task>();

                tasks.Add(AdbShell.ExecuteTextOutputAsync(deviceId, "shell wm size", output =>
                {
                    if (output == null) return;
                    var m = Regex.Match(output, @": (\d+)x(\d+)");
                    if (m.Success)
                    {
                        instance.Size = new Size(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
                    }
                }).Task);

                tasks.Add(AdbShell.ExecuteTextOutputAsync(deviceId, "shell wm density", output =>
                {
                    if (output == null) return;
                    var m = Regex.Match(output, @": (\d+)");
                    if (m.Success)
                    {
                        instance.Density = int.Parse(m.Groups[1].Value);
                    }
                }).Task);

                Task.WaitAll(tasks.ToArray());
                handler?.Invoke(instance);
            });
        }

        public override string ToString()
        {
            return $"Size:{this.Size} Density:{this.Density}";
        }
    }

    class BatteryInfo
    {
        public float Level { get; private set; }
        public float Temperature { get; private set; }

        public static void GetAsync(string deviceId, Action<BatteryInfo> handler)
        {
            AdbShell.ExecuteTextOutputAsync(deviceId, "shell dumpsys battery", output =>
            {
                if (output == null) return;
                var m = Regex.Match(output, @"level: (\d+).*temperature: (\d+)", RegexOptions.Multiline);
                if (m.Success)
                {
                    handler?.Invoke(new BatteryInfo()
                    {
                        Level = int.Parse(m.Groups[1].Value),
                        Temperature = int.Parse(m.Groups[2].Value) / 10.0f
                    });
                }
            });
        }

        public override string ToString()
        {
            return $"Level:{this.Level} Temperature:{this.Temperature}";
        }
    }
}