using Suconbu.Toolbox;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;

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
        public List<Property> Properties = new List<Property>();

        public static PropertyGroup FromXml(string path)
        {
            if (path == null) return new PropertyGroup();
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
            get { return this.Properties.FirstOrDefault(p => p.Name == name); }
        }

        public CommandContext PullAsync(Device device, ICollection<string> exceptedPropertyNames = null, Action<List<Property>> onPropertyChanged = null)
        {
            if (string.IsNullOrEmpty(this.PullCommand)) return null;

            var changedProperties = new List<Property>();
            return device.RunCommandAsync(this.PullCommand, output =>
            {
                if (output == null)
                {
                    if (changedProperties.Count > 0) onPropertyChanged?.Invoke(changedProperties);
                    return;
                }

                foreach(var p in this.Properties)
                {
                    // 個別のpullコマンドを持ってたらそれにお任せするのでここでは対象外
                    if (string.IsNullOrEmpty(p.PullCommand) &&
                        !changedProperties.Contains(p) &&
                        (exceptedPropertyNames == null || !exceptedPropertyNames.Contains(p.Name)))
                    {
                        var previous = p.Value?.ToString();
                        if (p.TrySetValueFromString(output.Trim()))
                        {
                            if (previous != p.Value?.ToString()) changedProperties.Add(p);
                        }
                    }
                }
            });
        }
    }

    //   <property name="AC powered" type="bool" pattern="AC powered: (\w+)" push="set ac \1"/>
    public class Property
    {
        public enum DataType { String, Integer, Float, Bool, Size, Point }

        [XmlAttribute("name")]
        public string Name { get; set; }
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
        [XmlAttribute("update")]
        public string PropertyNameToUpdateAfterPush { get; set; }

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
        public DeviceComponent Component { get; set; }
        // PushAsyncを呼び出した後、値がデバイスに反映されるまでの間はtrue
        //[XmlIgnore]
        //public bool Pushing { get; private set; }

        object internalValue;

        public Property() { }

        public void InitializeValue()
        {
            this.internalValue =
                (this.Type == DataType.Size) ? Size.Empty :
                (this.Type == DataType.Point) ? Point.Empty :
                (this.Type == DataType.Bool) ? false :
                (this.Type == DataType.Integer) ? 0 :
                (this.Type == DataType.Float) ? 0.0f :
                (this.Type == DataType.String) ? string.Empty :
                this.internalValue;
        }

        public CommandContext PullAsync(Device device, EventHandler<bool> onFinished = null)
        {
            if (string.IsNullOrEmpty(this.PullCommand)) return null;

            return device.RunCommandOutputTextAsync(this.PullCommand, (output, error) =>
            {
                var previous = this.Value?.ToString();
                if(this.TrySetValueFromString(output.Trim()))
                {
                    onFinished?.Invoke(this, previous != this.Value.ToString());
                }
            });
        }

        public CommandContext PushAsync(Device device, EventHandler onFinished = null)
        {
            if (string.IsNullOrEmpty(this.PushCommand)) return null;
            if (this.internalValue == null) return null;

            string command;
            if (this.Type == DataType.Size)
            {
                var sizeValue = (Size)this.internalValue;
                command = string.Format(this.PushCommand, sizeValue.Width, sizeValue.Height);
            }
            else if (this.Type == DataType.Point)
            {
                var pointValue = (Point)this.internalValue;
                command = string.Format(this.PushCommand, pointValue.X, pointValue.Y);
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
            return device.RunCommandOutputTextAsync(command, (output, error) => onFinished?.Invoke(this, EventArgs.Empty));
        }

        public CommandContext ResetAsync(Device device)
        {
            if (!string.IsNullOrEmpty(this.ResetCommand))
            {
                this.Overridden = false;
                return device.RunCommandOutputTextAsync(this.ResetCommand, (output, error) =>
                {
                    if (string.IsNullOrEmpty(this.PullCommand))
                    {
                        this.Component.PullAsync()?.Wait();
                    }
                    else
                    {
                        this.PullAsync(device)?.Wait();
                    }
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
            try
            {
                this.internalValue =
                    (this.Type == DataType.Size) ? new Size(
                        (int)Math.Round(double.Parse(match.Groups[1].Value)),
                        (int)Math.Round(double.Parse(match.Groups[2].Value))) :
                    (this.Type == DataType.Point) ? new Point(
                        (int)Math.Round(double.Parse(match.Groups[1].Value)),
                        (int)Math.Round(double.Parse(match.Groups[2].Value))) :
                    (this.Type == DataType.Bool) ? this.ParseAsBool(match.Groups[1].Value) :
                    (this.Type == DataType.Integer) ? (int)Math.Round(double.Parse(match.Groups[1].Value)) :
                    (this.Type == DataType.Float) ? float.Parse(match.Groups[1].Value) :
                    (this.Type == DataType.String) ? match.Groups[1].Value :
                    this.internalValue;
            }
            catch(Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return false;
            }
            this.OriginalValue = this.OriginalValue ?? this.internalValue;
            return true;
        }

        public override string ToString()
        {
            return $"{this.Name} - {this.Value} : {this.Type}";
        }

        bool ParseAsBool(string input)
        {
            bool result = false;
            if (!bool.TryParse(input, out result))
            {
                if(int.TryParse(input, out var value))
                {
                    result = (value != 0);
                }
            }
            return result;
        }
    }
}
