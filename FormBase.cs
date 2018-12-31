using Suconbu.Toolbox;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Suconbu.Sumacon
{
    public class FormBase : DockContent
    {
        bool formClosed = false;

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Util.TraverseControls(this, c => c.Font = SystemFonts.MessageBoxFont);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            this.formClosed = true;
        }

        protected void SafeInvoke(MethodInvoker action)
        {
            try
            {
                if (!this.formClosed) this.Invoke(action);
            }
            catch(Exception)
            {
                ;
            }
        }
    }
}
