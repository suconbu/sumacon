using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace Suconbu.Mobile
{
    public class Screen
    {
        public event EventHandler<SortedSet<string>> PropertyChanged = delegate { };

        public Size Size
        {
            get { return (Size)(this.propertyGroup[nameof(this.Size)].Value ?? Size.Empty); }
            set { this.SetAndPushValue(nameof(this.Size), value); }
        }
        public int Density
        {
            get { return (int)(this.propertyGroup[nameof(this.Density)].Value ?? 0); }
            set { this.SetAndPushValue(nameof(this.Density), value); }
        }

        readonly MobileDevice device;
        readonly PropertyGroup propertyGroup;

        public Screen(MobileDevice device, string xmlPath)
        {
            this.device = device;
            this.propertyGroup = PropertyGroup.FromXml(xmlPath);
        }

        /// <summary>
        /// すべてのプロパティを最新の値に更新します。
        /// </summary>
        public CommandContext PullAsync()
        {
            return CommandContext.StartNew(() =>
            {
                this.propertyGroup.Properties.ForEach(p =>
                {
                    if (!p.Pushing)
                    {
                        var latest = p.Value?.ToString();
                        p.PullAsync(this.device).Wait();
                        if (latest != p.Value?.ToString())
                        {
                            this.PropertyChanged(this, new SortedSet<string>(new[] { p.Name }));
                        }
                    }
                    else
                    {
                        ;
                    }
                });
            });
        }

        /// <summary>
        /// 指定されたプロパティを本来の値に戻します。
        /// プロパティ名を省略した時はすべてのプロパティが対象となります。
        /// </summary>
        public CommandContext ResetAsync(string propertyName = null)
        {
            if(string.IsNullOrEmpty(propertyName))
            {
                var property = this.propertyGroup.Properties.Find(p => p.Name == propertyName);
                if (property == null) return null;
                return property.ResetAsync(this.device);
            }
            else
            {
                return CommandContext.StartNew(() => this.propertyGroup.Properties.ForEach(p => p.ResetAsync(this.device).Wait()));
            }
        }

        void SetAndPushValue(string name, object value)
        {
            var property = this.propertyGroup[name];
            if (property != null && property.Value?.ToString() != value?.ToString())
            {
                property.Value = value;
                property.PushAsync(this.device);
                this.PropertyChanged(this, new SortedSet<string>(new[] { property.Name }));
            }
        }
    }
}
