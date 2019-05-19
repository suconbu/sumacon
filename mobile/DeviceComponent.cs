using Suconbu.Toolbox;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Suconbu.Mobile
{
    public class DeviceComponent
    {
        public event EventHandler<IReadOnlyList<Property>> PropertyChanged = delegate { };

        public string Name { get { return this.propertyGroup.Name; } }
        public Property this[string name] { get { return this.propertyGroup[name]; } }

        protected readonly Device device;
        protected readonly PropertyGroup propertyGroup;

        readonly HashSet<string> pushing = new HashSet<string>();

        public DeviceComponent(Device device, string xmlPath)
        {
            this.device = device;
            this.propertyGroup = PropertyGroup.FromXml(xmlPath);
            foreach (var property in this.propertyGroup.Properties)
            {
                property.Component = this;
            }
        }

        /// <summary>
        /// すべてのプロパティを最新の値に更新します。
        /// </summary>
        public virtual CommandContext PullAsync()
        {
            Trace.TraceInformation($"{Util.GetCurrentMethodName()} - Name:{this.Name}");

            var contexts = new List<CommandContext>();

            // グループ一括
            contexts.Add(this.propertyGroup.PullAsync(this.device, this.pushing, this.OnPropertyChanged));

            // 個別
            foreach (var property in this.propertyGroup.Properties)
            {
                if (!this.pushing.Contains(property.Name) && !string.IsNullOrEmpty(property.PullCommand))
                {
                    contexts.Add(property.PullAsync(this.device, this.OnPullFinished));
                }
            }

            return CommandContext.StartNew(() => contexts.ForEach(c => c?.Wait()));
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

        public void SetAndPushValue(string name, object value)
        {
            var property = this.propertyGroup[name];
            if (property != null && property.Value?.ToString() != value?.ToString())
            {
                this.pushing.Add(property.Name);
                property.Value = value;
                property.PushAsync(this.device, this.OnPushFinished);
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

        void OnPushFinished(object sender, EventArgs e)
        {
            var property = sender as Property;
            this.pushing.Remove(property.Name);
            if(!string.IsNullOrEmpty(property.PropertyNameToUpdateAfterPush))
            {
                var pullProperty = this.propertyGroup[property.PropertyNameToUpdateAfterPush];
                if (!string.IsNullOrEmpty(pullProperty.PullCommand))
                {
                    pullProperty?.PullAsync(this.device);
                }
                else
                {
                    this.PullAsync();
                }
            }
        }
    }
}
