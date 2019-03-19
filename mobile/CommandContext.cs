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
    public class CommandContext
    {
        public enum NewLineMode { CrLf, CrCrLf }
        public string Command { get; private set; }
        public string Arguments { get; private set; }

        static bool first = true;
        Task task;
        Process process;
        //StringBuilder outputBuffer;
        bool finished;

        /// <summary>
        /// 標準出力、標準エラーを逐次出力します。
        /// コマンドの実行が終了した時にはoutputReceivedにnullを渡します。
        /// </summary>
        public static CommandContext StartNew(string command, string arguments, Action<string> onOutputReceived, Action<string> onErrorReceived)
        {
            var instance = new CommandContext();
            return instance.StartCommandTextOutput(command, arguments, onOutputReceived, onErrorReceived, null) ? instance : null;
        }

        /// <summary>
        /// コマンド実行終了時に標準出力内容の全体を文字列としてonFinishedに渡します。
        /// </summary>
        public static CommandContext StartNewText(string command, string arguments, Action<string> onFinished)
        {
            //return StartNewInternal(command, arguments, false, null, null, context =>
            //{
            //    onFinished?.Invoke(context.outputBuffer.ToString());
            //});
            var instance = new CommandContext();
            return instance.StartCommandTextOutput(command, arguments, null, null, onFinished) ? instance : null;
        }

        /// <summary>
        /// コマンド実行終了時に標準出力をバイナリ化してそのストリームをonFinishedに渡します。
        /// </summary>
        public static CommandContext StartNewBinary(string command, string arguments, NewLineMode mode, Action<Stream> onFinished)
        {
            var instance = new CommandContext();
            return instance.StartCommandBinaryOutput(command, arguments, mode, onFinished) ? instance : null;
        }

        /// <summary>
        /// コマンドは実行せず単にonFinishedを呼び出します。
        /// </summary>
        public static CommandContext StartNew(Action onFinished)
        {
            var instance = new CommandContext();
            instance.task = Task.Run(() => onFinished());
            return instance;
        }

        /// <summary>
        /// 文字列を標準入力に書き込みます。
        /// </summary>
        public void PushInput(params string[] inputs)
        {
            foreach (var input in inputs)
            {
                this.process.StandardInput.WriteLine(input);
            }
        }

        bool StartCommandTextOutput(string command, string arguments, Action<string> onOutputReceived, Action<string> onErrorReceived, Action<string> onFinished)
        {
            this.process = new Process();
            var info = this.process.StartInfo;
            info.FileName = command;
            info.Arguments = arguments;
            info.CreateNoWindow = true;
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.UseShellExecute = false;

            var outputBuffer = (onFinished != null) ? new StringBuilder() : null;

            Trace.TraceInformation($"{Util.GetCurrentMethodName()} - {command} {arguments}");

            if (!this.process.Start()) return false;
            
            if (onErrorReceived != null)
            {
                this.process.ErrorDataReceived += (s, e) => onErrorReceived(e.Data);
            }
            this.process.BeginErrorReadLine();

            this.process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null && outputBuffer != null)
                {
                    outputBuffer.AppendLine(e.Data);
                }
                onOutputReceived?.Invoke(e.Data);
                if (e.Data == null)
                {
                    this.process?.CancelOutputRead();
                    this.process?.CancelErrorRead();
                    this.finished = true;
                    onFinished?.Invoke(outputBuffer.ToString());
                }
            };
            this.process.BeginOutputReadLine();

            return true;
        }

        bool StartCommandBinaryOutput(string command, string arguments, NewLineMode mode, Action<Stream> onFinished)
        {
            this.process = new Process();
            var info = this.process.StartInfo;
            info.FileName = command;
            info.Arguments = arguments;
            info.CreateNoWindow = true;
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = false;
            info.UseShellExecute = false;

            if (!this.process.Start()) return false;

            this.task = Task.Run(() =>
            {
                this.finished = true;
                if (this.process == null) return;
                using (var stream = new MemoryStream())
                {
                    this.process.StandardOutput.BaseStream.CopyTo(stream);
                    var data = stream.ToArray();
                    stream.Position = 0;
                    var newLine = (mode == NewLineMode.CrCrLf) ? new[] { 0x0d, 0x0d, 0xa } : new[] { 0x0d, 0x0a };

                    using (var outputStream = new MemoryStream())
                    {
                        int offset;
                        for (offset = 0; offset <= data.Length - newLine.Length; offset++)
                        {
                            var match = true;
                            for (int i = 0; i < newLine.Length; i++)
                            {
                                if (data[offset + i] != newLine[i])
                                {
                                    match = false;
                                    break;
                                }
                            }

                            if (match)
                            {
                                outputStream.WriteByte(0x0A);
                                offset += newLine.Length - 1;
                            }
                            else
                            {
                                outputStream.WriteByte(data[offset]);
                            }
                        }
                        for (; offset < data.Length; offset++)
                        {
                            outputStream.WriteByte(data[offset]);
                        }
                        outputStream.Position = 0;
                        onFinished?.Invoke(outputStream);
                    }
                }
            });

            return true;
        }

        //static CommandContext StartNewInternal(string command, string arguments, bool binary, Action<string> onOutputReceived, Action<string> onErrorReceived, Action<CommandContext> onFinished)
        //{
        //    var instance = new CommandContext();
        //    instance.process = new Process();
        //    var info = instance.process.StartInfo;
        //    info.FileName = command;
        //    info.Arguments = arguments;
        //    info.CreateNoWindow = true;
        //    info.RedirectStandardInput = true;
        //    info.RedirectStandardOutput = true;
        //    info.RedirectStandardError = !binary;
        //    info.UseShellExecute = false;
        //    //if (binary)
        //    //{
        //    //    info.StandardOutputEncoding = Encoding.Unicode;
        //    //}

        //    if(onFinished != null)
        //    {
        //        instance.outputBuffer = new StringBuilder();
        //    }

        //    Trace.TraceInformation($"{Util.GetCurrentMethodName()} - {command} {arguments}");

        //    if (!instance.process.Start()) return null;

        //    if (!binary)
        //    {
        //        instance.process.OutputDataReceived += (s, e) =>
        //        {
        //            if (e.Data != null && instance.outputBuffer != null)
        //            {
        //                instance.outputBuffer.AppendLine(e.Data);
        //            }
        //            onOutputReceived?.Invoke(e.Data);
        //            if (e.Data == null)
        //            {
        //                instance.process?.CancelOutputRead();
        //                instance.process?.CancelErrorRead();
        //                instance.finished = true;
        //                onFinished?.Invoke(instance);
        //            }
        //        };
        //        instance.process.BeginOutputReadLine();

        //        if(onErrorReceived != null)
        //        {
        //            instance.process.ErrorDataReceived += (s, e) => onErrorReceived(e.Data);
        //        }
        //        instance.process.BeginErrorReadLine();
        //    }
        //    else
        //    {
        //        instance.task = Task.Run(() =>
        //        {
        //            instance.finished = true;
        //            onFinished(instance);
        //        });
        //    }

        //    return instance;
        //}

        //Stream GetBinaryOutputStream()
        //{
        //    if (this.process == null) return null;
        //    using (var stream = new MemoryStream())
        //    {
        //        this.process.StandardOutput.BaseStream.CopyTo(stream);
        //        return stream;
        //        //var data = dataStream.ToArray();
        //        //var outputStream = new MemoryStream(data.Length);
        //        //if (data.Length > 0)
        //        //{
        //        //    bool replace0D0D0A = false;
        //        //    for (int i = 0; i < Math.Min(16, data.Length - 2); i++)
        //        //    {
        //        //        if (data[i] == 0x0D && data[i + 1] == 0x0D && data[i + 2] == 0x0A)
        //        //        {
        //        //            replace0D0D0A = true;
        //        //            break;
        //        //        }
        //        //    }
        //        //    for (int i = 0; i < data.Length - 2; i++)
        //        //    {
        //        //        if (replace0D0D0A && data[i] == 0x0D && data[i + 1] == 0x0D && data[i + 2] == 0x0A)
        //        //        {
        //        //            outputStream.WriteByte(0x0A);
        //        //            i += 2;
        //        //        }
        //        //        else if (!replace0D0D0A && data[i] == 0x0D && data[i + 1] == 0x0A)
        //        //        {
        //        //            outputStream.WriteByte(0x0A);
        //        //            i += 1;
        //        //        }
        //        //        else
        //        //        {
        //        //            outputStream.WriteByte(data[i]);
        //        //        }
        //        //    }
        //        //    outputStream.WriteByte(data[data.Length - 2]);
        //        //    outputStream.WriteByte(data[data.Length - 1]);
        //        //}
        //        //outputStream.Position = 0;
        //        //return outputStream;
        //    }
        //}

        public void Cancel()
        {
            if (!finished)
            {
                this.process?.Kill();
            }
            this.process = null;
            this.task = null;
        }

        public void Wait()
        {
            this.process?.WaitForExit();
            this.task?.Wait();
            this.process = null;
            this.task = null;
        }

        public void Wait(int millisecondsTimeout)
        {
            this.process?.WaitForExit(millisecondsTimeout);
            this.task?.Wait(millisecondsTimeout);
            this.process = null;
            this.task = null;
        }

        public void Wait(TimeSpan timeout)
        {
            this.process?.WaitForExit((int)timeout.TotalMilliseconds);
            this.task?.Wait((int)timeout.TotalMilliseconds);
            this.process = null;
            this.task = null;
        }

        CommandContext()
        {
            if (CommandContext.first)
            {
                ThreadPool.GetMinThreads(out var workerThreads, out var ioThreads);
                ThreadPool.SetMinThreads(20, ioThreads);
                CommandContext.first = false;
            }
        }
    }
}
