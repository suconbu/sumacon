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
    class CommandContext
    {
        public string Command { get; private set; }
        public string Arguments { get; private set; }

        Task task;
        Process process;
        StringBuilder outputBuffer;

        /// <summary>
        /// 標準出力、標準エラーを逐次出力します。
        /// コマンドの実行が終了した時にはoutputReceivedにnullを渡します。
        /// </summary>
        public static CommandContext StartNew(string command, string arguments, Action<string> onOutputReceived, Action<string> onErrorReceived)
        {
            return StartNewInternal(command, arguments, false, onOutputReceived, onErrorReceived, null);
        }

        /// <summary>
        /// コマンド実行終了時に標準出力内容の全体を文字列としてfinishedに渡します。
        /// </summary>
        public static CommandContext StartNewText(string command, string arguments, Action<string> onFinished)
        {
            return StartNewInternal(command, arguments, false, null, null, context =>
            {
                onFinished?.Invoke(context.outputBuffer.ToString());
            });
        }

        /// <summary>
        /// コマンド実行終了時に標準出力をバイナリ化してそのストリームをfinishedに渡します。
        /// </summary>
        public static CommandContext StartNewBinary(string command, string arguments, Action<Stream> onFinished)
        {
            return StartNewInternal(command, arguments, true, null, null, context =>
            {
                using (var stream = context.GetBinaryOutputStream())
                {
                    onFinished?.Invoke(stream);
                }
            });
        }

        static CommandContext StartNewInternal(string command, string arguments, bool binary, Action<string> onOutputReceived, Action<string> onErrorReceived, Action<CommandContext> onFinished)
        {
            var instance = new CommandContext();
            instance.process = new Process();
            var info = instance.process.StartInfo;
            info.FileName = command;
            info.Arguments = arguments;
            info.CreateNoWindow = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = !binary;
            info.UseShellExecute = false;
            if (binary)
            {
                info.StandardOutputEncoding = Encoding.Unicode;
            }

            if(onFinished != null)
            {
                instance.outputBuffer = new StringBuilder();
            }

            if (!instance.process.Start()) return null;

            if (!binary)
            {
                instance.process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null && instance.outputBuffer != null)
                    {
                        instance.outputBuffer.AppendLine(e.Data);
                    }
                    onOutputReceived?.Invoke(e.Data);
                    if (e.Data == null)
                    {
                        instance.process.CancelOutputRead();
                        instance.process.CancelErrorRead();
                        onFinished?.Invoke(instance);
                    }
                };
                instance.process.BeginOutputReadLine();

                if(onErrorReceived != null)
                {
                    instance.process.ErrorDataReceived += (s, e) => onErrorReceived(e.Data);
                }
                instance.process.BeginErrorReadLine();
            }
            else
            {
                instance.task = Task.Run(() => onFinished(instance));
            }

            return instance;
        }

        Stream GetBinaryOutputStream()
        {
            if (this.process == null) return null;
            using (var dataStream = new MemoryStream())
            {
                this.process.StandardOutput.BaseStream.CopyTo(dataStream);
                var data = dataStream.ToArray();
                var outputStream = new MemoryStream(data.Length);
                for (int i = 0; i < data.Length - 1; i++)
                {
                    if (!(data[i] == 0x0D && data[i + 1] == 0x0A)) outputStream.WriteByte(data[i]);
                }
                outputStream.WriteByte(data[data.Length - 1]);
                outputStream.Position = 0;
                return outputStream;
            }
        }

        public void Cancel()
        {
            this.process.Kill();
            this.process = null;
            this.task = null;
        }

        public void Wait()
        {
            this.process.WaitForExit();
            this.task?.Wait();
            this.process = null;
            this.task = null;
        }

        public void Wait(int millisecondsTimeout)
        {
            this.process.WaitForExit(millisecondsTimeout);
            this.task?.Wait(millisecondsTimeout);
            this.process = null;
            this.task = null;
        }

        public void Wait(TimeSpan timeout)
        {
            this.process.WaitForExit((int)timeout.TotalMilliseconds);
            this.task?.Wait((int)timeout.TotalMilliseconds);
            this.process = null;
            this.task = null;
        }
    }
}
