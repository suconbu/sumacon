using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tomochan154.Debugging;

namespace Suconbu.Sumacon
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
#if DEBUG
            Trace.Listeners.Add(new DailyLoggingTraceListener()
            {
                OutputDirectory = @".\log\",
                FileNameFormat = "{0:yyyy-MM-dd}_{1:0000}.log",
                DatetimeFormat = "{0:yyyy-MM-dd HH:mm:ss.fff}:"
            });
#endif
            Trace.TraceInformation("-------------------- Start --------------------");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormMain());
        }
    }
}
