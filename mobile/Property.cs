using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using System.Xml;

namespace Suconbu.Mobile
{
    [XmlRoot("root")]
    public class PropertyGroup
    {
        [XmlAttribute("name")]
        public string Name;
        [XmlAttribute("prefix")]
        public string CommandPrefix;
        [XmlElement("property")]
        public List<Property> Properties;

        public static PropertyGroup FromXml(string path)
        {
            var serializer = new XmlSerializer(typeof(PropertyGroup));
            using (var reader = new StreamReader(path))
            {
                var group = serializer.Deserialize(reader) as PropertyGroup;
                group.Properties.ForEach(p => p.CommandPrefix = group.CommandPrefix);
                return group;
            }
        }

        public Property this[string name]
        {
            get { return this.Properties.Find(p => p.Name == name); }
        }
    }

    //   <property name="AC powered" type="bool" pattern="AC powered: (\w+)" push="set ac \1"/>
    public class Property
    {
        public enum DataType { String, Integer, Bool, Size }

        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("text")]
        public string Text { get; set; }
        [XmlAttribute("type")]
        public DataType Type { get; set; }
        [XmlAttribute("pattern")]
        public string PullPattern { get; set; }
        [XmlAttribute("pull")]
        public string PullCommand { get; set; }
        [XmlAttribute("push")]
        public string PushCommand { get; set; }
        [XmlAttribute("reset")]
        public string ResetCommand { get; set; }

        [XmlIgnore]
        public object Value
        {
            get { return this.internalValue; }
            set
            {
                if (!string.IsNullOrEmpty(this.ResetCommand)) this.Overridden = true;
                this.internalValue = value;
            }
        }
        [XmlIgnore]
        public object OriginalValue { get; private set; }
        [XmlIgnore]
        public bool Overridden { get; private set; }
        [XmlIgnore]
        public string CommandPrefix { get; internal set; }
        // PushAsyncを呼び出した後、値がデバイスに反映されるまでの間はtrue
        [XmlIgnore]
        public bool Pushing { get; private set; }

        object internalValue;

        public Property() { }

        public CommandContext PullAsync(Device device)
        {
            return device.RunCommandOutputTextAsync($"{this.CommandPrefix} {this.PullCommand}", output =>
            {
                if (this.TrySetValueFromString(output.Trim()))
                {
                    this.OriginalValue = this.internalValue;
                }
            });
        }

        public CommandContext PushAsync(Device device, bool force = false)
        {
            if (!force && this.Value?.ToString() == this.OriginalValue?.ToString()) return null;

            this.Pushing = true;

            string command;
            if (this.Type == DataType.Size)
            {
                var sizeValue = (Size)this.internalValue;
                command = string.Format(this.PushCommand, sizeValue.Width, sizeValue.Height);
            }
            else if (this.Type == DataType.Bool)
            {
                var boolValue = (bool)this.internalValue;
                command = string.Format(this.PushCommand, boolValue ? 1 : 0);
            }
            else
            {
                command = string.Format(this.PushCommand, this.internalValue.ToString());
            }
            return device.RunCommandOutputTextAsync($"{this.CommandPrefix} {command}", output => this.Pushing = false);
        }

        public CommandContext ResetAsync(Device device)
        {
            if (!string.IsNullOrEmpty(this.ResetCommand))
            {
                this.Overridden = false;
                return device.RunCommandOutputTextAsync($"{this.CommandPrefix} {this.ResetCommand}", output =>
                {
                    this.PullAsync(device).Wait();
                });
            }
            else
            {
                this.internalValue = this.OriginalValue;
                return this.PushAsync(device);
            }
        }

        public bool TrySetValueFromString(string input)
        {
            var match = Regex.Match(input, this.PullPattern);
            if (!match.Success) return false;

            if (this.Type == DataType.String)
            {
                this.internalValue = match.Groups[1].Value;
            }
            else if (this.Type == DataType.Integer)
            {
                this.internalValue = int.Parse(match.Groups[1].Value);
            }
            else if (this.Type == DataType.Bool)
            {
                this.internalValue = bool.Parse(match.Groups[1].Value);
            }
            else if (this.Type == DataType.Size)
            {
                this.internalValue = new Size(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
            }
            else
            {
                throw new NotSupportedException();
            }
            this.OriginalValue = this.OriginalValue ?? this.internalValue;
            return true;
        }

        public override string ToString()
        {
            return $"{this.Name} - {this.Value} : {this.Type}";
        }
    }
}
