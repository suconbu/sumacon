using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Suconbu.Sumacon
{
    class BindingDropDownButton<T> : ToolStripDropDownButton
    {
        public Dictionary<T, ToolStripDropDownItem> DataSource { get => this.dataSource; set => this.ChangeDataSource(value); }
        public T Value { get => this.value; set => this.ChangeValue(value); }

        Dictionary<T, ToolStripDropDownItem> dataSource;
        T value;

        protected override void OnDropDownItemClicked(ToolStripItemClickedEventArgs e)
        {
            base.OnDropDownItemClicked(e);
            this.Text = e.ClickedItem.Text;
            this.value = (T)e.ClickedItem.Tag;
        }

        void ChangeDataSource(Dictionary<T, ToolStripDropDownItem> dataSource)
        {
            this.DropDownItems.Clear();
            foreach(var d in dataSource)
            {
                d.Value.Tag = d.Key;
                this.DropDownItems.Add(d.Value);
            }
            this.dataSource = dataSource;
        }

        void ChangeValue(T value)
        {
            foreach(ToolStripDropDownItem item in this.DropDownItems)
            {
                if(((T)item.Tag).Equals(value))
                {
                    item.PerformClick();
                    break;
                }
            }
        }
    }
}
