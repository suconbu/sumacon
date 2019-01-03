using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Suconbu.Mobile
{
    public class DeviceComponentBase
    {
        public event EventHandler<IReadOnlyList<Property>> PropertyChanged = delegate { };

        public string Name { get { return this.propertyGroup.Name; } }

        protected readonly Device device;
        protected readonly PropertyGroup propertyGroup;

        public DeviceComponentBase(Device device, string xmlPath)
        {
            this.device = device;
            this.propertyGroup = PropertyGroup.FromXml(xmlPath);
        }

        /// <summary>
        /// すべてのプロパティを最新の値に更新します。
        /// </summary>
        public virtual CommandContext PullAsync()
        {
            Trace.TraceInformation($"{Util.GetCurrentMethodName()} - Name:{this.Name}");
            return CommandContext.StartNew(() =>
            {
                foreach(var property in this.propertyGroup.Properties)
                {
                    if (!property.Pushing)
                    {
                        property.PullAsync(this.device, this.OnPullFinished);
                    }
                }
            });
        }

        /// <summary>
        /// 指定されたプロパティを本来の値に戻します。
        /// プロパティ名を省略した時はすべてのプロパティが対象となります。
        /// </summary>
        public CommandContext ResetAsync(string propertyName = null)
        {
            Trace.TraceInformation($"{Util.GetCurrentMethodName()} - Name:{this.Name} propertyName:{propertyName}");
            if (!string.IsNullOrEmpty(propertyName))
            {
                return this.propertyGroup.Properties.Find(p => p.Name == propertyName)?.ResetAsync(this.device);
            }
            else
            {
                return CommandContext.StartNew(() =>
                {
                    Parallel.ForEach(this.propertyGroup.Properties, property => property.ResetAsync(this.device)?.Wait());
                });
            }
        }

        public Property Find(string name)
        {
            return this.propertyGroup.Properties.Find(p => p.Name == name);
        }

        protected void SetAndPushValue(string name, object value)
        {
            var property = this.propertyGroup[name];
            if (property != null && property.Value?.ToString() != value?.ToString())
            {
                property.Value = value;
                property.PushAsync(this.device);
                this.OnPropertyChanged(new List<Property>() { property });
            }
        }

        protected void OnPropertyChanged(List<Property> properties)
        {
            this.PropertyChanged(this, properties);
        }

        void OnPullFinished(object sender, bool valueChanged)
        {
            if (valueChanged)
            {
                this.OnPropertyChanged(new List<Property>() { sender as Property });
            }
        }
    }
}
