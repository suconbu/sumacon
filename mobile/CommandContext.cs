using System;
using System.Diagnostics;
using System.IO;
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
        public bool Finished { get; private set; }

        static bool first = true;
        Task task;
        Process process;
        BinaryWriter binaryWriter;

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
        public static CommandContext StartNewText(string command, string arguments, Action<string, string> onFinished)
        {
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
            return new CommandContext() { task = Task.Run(() => onFinished()) };
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

        /// <summary>
        /// 文字列を標準入力に書き込みます。
        /// </summary>
        public void PushInputBinary(byte[] inputs)
        {
            if(inputs == null)
            {
                this.binaryWriter.Close();
                this.binaryWriter = null;
                return;
            }
            this.binaryWriter = this.binaryWriter ?? new BinaryWriter(this.process.StandardInput.BaseStream);
            this.binaryWriter.Write(inputs);
            this.binaryWriter.Flush();
            //Console.WriteLine(string.Join(":", inputs.Select(i => i.ToString())));
        }

        bool StartCommandTextOutput(string command, string arguments, Action<string> onOutputReceived, Action<string> onErrorReceived, Action<string, string> onFinished)
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
            var errorBuffer = (onFinished != null) ? new StringBuilder() : null;

            Trace.TraceInformation($"{Util.GetCurrentMethodName()} - {command} {arguments}");

            var sw = Stopwatch.StartNew();
            if (!this.process.Start()) return false;

            this.process.ErrorDataReceived += (s, e) =>
            {
                if(e.Data != null && errorBuffer !=null) errorBuffer.AppendLine(e.Data);
                onErrorReceived?.Invoke(e.Data);
            };
            this.process.BeginErrorReadLine();

            this.process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null && outputBuffer != null) outputBuffer.AppendLine(e.Data);
                onOutputReceived?.Invoke(e.Data);
                if (e.Data == null)
                {
                    this.process?.CancelOutputRead();
                    this.process?.CancelErrorRead();
                    this.Finished = true;
                    Trace.TraceInformation($"{Util.GetCurrentMethodName()} - {command} {arguments} Finished {sw.ElapsedMilliseconds}ms");
                    onFinished?.Invoke(outputBuffer.ToString(), errorBuffer.ToString());
                    sw = null;
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
                this.Finished = true;
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

        public void Cancel()
        {
            if (!this.Finished) this.process?.Kill();
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

        public void Wait(Action onFinished)
        {
            CommandContext.StartNew(() =>
            {
                this.Wait();
                onFinished?.Invoke();
            });
        }

        CommandContext()
        {
            if (CommandContext.first)
            {
                ThreadPool.GetMinThreads(out var workerThreads, out var ioThreads);
                ThreadPool.SetMinThreads(40, ioThreads);
                CommandContext.first = false;
            }
        }
    }
}
