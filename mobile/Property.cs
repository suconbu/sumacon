using Suconbu.Toolbox;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace Suconbu.Mobile
{
    [XmlRoot("root")]
    public class PropertyGroup
    {
        [XmlAttribute("name")]
        public string Name;
        [XmlAttribute("pull")]
        public string PullCommand;
        [XmlElement("property")]
        public List<Property> Properties;

        public static PropertyGroup FromXml(string path)
        {
            var serializer = new XmlSerializer(typeof(PropertyGroup));
            using (var reader = new StreamReader(path))
            {
                var group = serializer.Deserialize(reader) as PropertyGroup;
                group.Properties.ForEach(p =>
                {
                    p.InitializeValue();
                });
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
        // PushAsyncを呼び出した後、値がデバイスに反映されるまでの間はtrue
        [XmlIgnore]
        public bool Pushing { get; private set; }

        object internalValue;

        public Property() { }

        public void InitializeValue()
        {
            this.internalValue =
                (this.Type == DataType.Size) ? Size.Empty :
                (this.Type == DataType.Bool) ? false :
                (this.Type == DataType.Integer) ? 0 :
                (this.Type == DataType.String) ? string.Empty :
                this.internalValue;
        }

        public CommandContext PullAsync(Device device, EventHandler<bool> onFinished = null)
        {
            if (string.IsNullOrEmpty(this.PullCommand)) return null;

            return device.RunCommandOutputTextAsync(this.PullCommand, output =>
            {
                var previous = this.Value.ToString();
                if(this.TrySetValueFromString(output.Trim()))
                {
                    onFinished?.Invoke(this, previous != this.Value.ToString());
                }
            });
        }

        public CommandContext PushAsync(Device device)
        {
            if (string.IsNullOrEmpty(this.PushCommand)) return null;

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
            return device.RunCommandOutputTextAsync(command, output => this.Pushing = false);
        }

        public CommandContext ResetAsync(Device device)
        {
            if (!string.IsNullOrEmpty(this.ResetCommand))
            {
                this.Overridden = false;
                return device.RunCommandOutputTextAsync(this.ResetCommand, output =>
                {
                    this.PullAsync(device)?.Wait();
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

            this.internalValue =
                (this.Type == DataType.Size) ? new Size(
                    (int)Math.Round(double.Parse(match.Groups[1].Value)),
                    (int)Math.Round(double.Parse(match.Groups[2].Value))) :
                (this.Type == DataType.Bool) ? bool.Parse(match.Groups[1].Value) :
                (this.Type == DataType.Integer) ? (int)Math.Round(double.Parse(match.Groups[1].Value)) :
                (this.Type == DataType.String) ? match.Groups[1].Value :
                this.internalValue;
            this.OriginalValue = this.OriginalValue ?? this.internalValue;
            return true;
        }

        public override string ToString()
        {
            return $"{this.Name} - {this.Value} : {this.Type}";
        }
    }
}
