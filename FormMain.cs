using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Suconbu.MobileDebugging;

namespace Suconbu.Sumacon
{
    public partial class FormMain : Form
    {
        MobileDevice d;

        public FormMain()
        {
            InitializeComponent();

            this.d = MobileDevice.Open(@"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe");
            this.d.DeviceInfoUpdateIntervalMilliseconds = 1000;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if(e.KeyCode == Keys.Enter)
            {
                if(this.d.LogReceiving)
                {
                    this.d.StopLogReceive();
                }
                else
                {
                    this.d.StartLogReceive(log =>
                    {
                        Trace.TraceInformation(log.Message);
                    });
                }
            }
            else if(e.KeyCode == Keys.P)
            {
                this.d.GetScreenCaptureAsync(image =>
                {
                    this.Invoke((MethodInvoker)(() => { this.pictureBox1.Image = image; }));
                });
            }
        }
    }
}
