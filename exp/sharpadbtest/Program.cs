using SharpAdbClient;
using SharpAdbClient.Logs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace sharpadbtest
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new AdbServer();
            StartServerResult result = server.StartServer(@"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe", false);
            result = server.StartServer(@"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe", false);

            var client = new AdbClient();
            foreach(var d in client.GetDevices())
            {
                Trace.TraceInformation(d.ToString());
            }

            var monitor = new DeviceMonitor(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)));
            monitor.DeviceConnected += (s, e) => { Trace.TraceInformation($"{e.Device} connected."); };
            monitor.DeviceDisconnected += (s, e) => { Trace.TraceInformation($"{e.Device} disconnected."); };
            monitor.Start();

            //client.RunLogServiceAsync(client.GetDevices().First(), log =>
            //{
            //    var alog = log as AndroidLogEntry;
            //    Trace.TraceInformation($"{alog}");
            //}, CancellationToken.None, LogId.Main);
            var cts = new CancellationTokenSource();
            var t = client.ExecuteRemoteCommandAsync("logcat", client.GetDevices().First(), new LogReceiver(), cts.Token, -1);
            Console.ReadKey();
            cts.Cancel();

            Console.ReadKey();
        }
        class LogReceiver : IShellOutputReceiver
        {
            public bool ParsesErrors => throw new NotImplementedException();

            public void AddOutput(string line)
            {
                Trace.TraceInformation($"LOG: {line}");
            }

            public void Flush()
            {
                ;// throw new NotImplementedException();
            }
        }
    }
}
