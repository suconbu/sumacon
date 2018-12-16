using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Suconbu.Toolbox
{
    class CommandProcess
    {
        public Task Task { get; private set; }

        CancellationTokenSource canceller = new CancellationTokenSource();
        Process process;

        public static CommandProcess ExecuteAsync(string command, string arguments, Action<string> outputHandler = null, bool outputAccumlated = false, Action<string> errorHandler = null)
        {
            var instance = new CommandProcess();

            var p = new Process();
            p.StartInfo.FileName = command;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = outputHandler != null;
            p.StartInfo.RedirectStandardError = errorHandler != null;
            p.StartInfo.UseShellExecute = false;

            if (!p.Start()) return null;

            var outputBuffer = new StringBuilder();
            if (outputHandler != null)
            {
                if(outputAccumlated)
                {
                    p.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuffer.AppendLine(e.Data); };
                }
                else
                {
                    p.OutputDataReceived += (s, e) => outputHandler(e.Data);
                }
                p.BeginOutputReadLine();
            }
            if (errorHandler != null)
            {
                p.ErrorDataReceived += (s, e) => errorHandler(e.Data);
                p.BeginErrorReadLine();
            }

            instance.process = p;
            var cancelToken = instance.canceller.Token;
            instance.Task = Task.Run(() =>
            {
                p.WaitForExit();
                if (cancelToken.IsCancellationRequested) return;
                if (outputAccumlated)
                {
                    outputHandler(outputBuffer.ToString());
                }
                if (outputHandler != null) p.CancelOutputRead();
                if (errorHandler != null) p.CancelErrorRead();
            }, cancelToken);

            return instance;
        }

        public static CommandProcess ExecuteOutputBinaryAsync(string command, string arguments, Action<Stream> outputHandler)
        {
            var instance = new CommandProcess();

            var p = new Process();
            p.StartInfo.FileName = command;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = false;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.StandardOutputEncoding = Encoding.Unicode;

            if (!p.Start()) return null;

            instance.process = p;
            var cancelToken = instance.canceller.Token;
            instance.Task = Task.Run(() =>
            {
                using (var dataStream = new MemoryStream())
                {
                    p.StandardOutput.BaseStream.CopyTo(dataStream);
                    var data = dataStream.ToArray();
                    if (cancelToken.IsCancellationRequested) return;
                    using (var outputStream = new MemoryStream(data.Length))
                    {
                        for (int i = 0; i < data.Length - 1; i++)
                        {
                            if (!(data[i] == 0x0D && data[i + 1] == 0x0A)) outputStream.WriteByte(data[i]);
                        }
                        outputStream.WriteByte(data[data.Length - 1]);
                        outputStream.Position = 0;
                        outputHandler.Invoke(outputStream);
                    }
                }
            }, cancelToken);

            return instance;
        }

        public void Wait()
        {
            if (this.Task.Status == TaskStatus.Running) this.Task.Wait();
        }

        public void Cancel()
        {
            this.canceller.Cancel();
            this.process.Kill();
        }
    }
}
