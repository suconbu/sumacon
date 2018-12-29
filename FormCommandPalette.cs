using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Suconbu.Sumacon
{
    public partial class FormCommandPalette : DockContent
    {
        public FormCommandPalette()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Util.TraverseControls(this, c => c.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, c.Font.Size));

            // 設定ファイル読み込み
            
        }
    }
}
