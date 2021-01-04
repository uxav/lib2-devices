using System;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.Devices.Audio.QSC.Components;

namespace UX.Lib2.Devices.Audio.QSC
{
    /// <summary>
    /// A control of a Component object on a QSys Core
    /// </summary>
    public sealed class QsysControl
    {
        private float _value;
        private float _position;
        private string _string;

        internal QsysControl(ComponentBase component, string name, JToken data)
        {
            Component = component;
            component.Controls[name] = this;
            UpdateFromData(data);
#if DEBUG
            CloudLog.Debug("Created object: {0}", this);
#endif
        }

        /// <summary>
        /// The Component which owns this instance of a control
        /// </summary>
        public ComponentBase Component { get; private set; }

        /// <summary>
        /// The name of the Control
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Get or set the string value of the control
        /// </summary>
        public string String
        {
            get { return _string; }
            set
            {
                object args = new
                {
                    Component.Name,
                    Controls = new[]
                    {
                        new
                        {
                            Name,
                            String = value
                        }
                    }
                };

                Component.Core.RequestAsync(OnUpdateResponse, "Component.Set", args);
            }
        }

        /// <summary>
        /// Set or get numeric value of the control
        /// </summary>
        public float Value
        {
            get { return _value; }
            set
            {
                object args = new
                {
                    Component.Name,
                    Controls = new[]
                    {
                        new
                        {
                            Name,
                            Value = value
                        }
                    }
                };

                Component.Core.RequestAsync(OnUpdateResponse, "Component.Set", args);
            }
        }

        /// <summary>
        /// Get or set position of the control between 0 and 1 as a float.
        /// </summary>
        public float Position
        {
            get { return _position; }
            set
            {
                object args = new
                {
                    Component.Name,
                    Controls = new[]
                    {
                        new
                        {
                            Name,
                            Position = value
                        }
                    }
                };

                Component.Core.RequestAsync(OnUpdateResponse, "Component.Set", args);
            }
        }

        /// <summary>
        /// Get or set the control position based upon a ushort value used for Crestron type interfacing
        /// </summary>
        public ushort PositionScaled
        {
            get { return (ushort) Tools.ScaleRange(Position, 0, 1, ushort.MinValue, ushort.MaxValue); }
            set { Position = (float) Tools.ScaleRange(value, ushort.MinValue, ushort.MaxValue, 0, 1); }
        }

        /// <summary>
        /// Trigger this control
        /// </summary>
        public void Trigger()
        {
            Value = 1;
        }

        /// <summary>
        /// Force an update from the core for the control asyncronously
        /// </summary>
        public int UpdateAsync()
        {
            return Component.Core.RequestAsync(OnUpdateResponse, "Component.Get", new
            {
                Component.Name,
                Controls = new[]
                {
                    new {Name}
                }
            });
        }

        /// <summary>
        /// Ramp the value internally in the qsys
        /// </summary>
        /// <param name="value">Target value</param>
        /// <param name="time">Time in seconds to ramp to new value</param>
        public void RampValue(float value, double time)
        {
            object args = new
            {
                Component.Name,
                Controls = new[]
                {
                    new
                    {
                        Name,
                        Value = value,
                        Ramp = time
                    }
                }
            };

            Component.Core.RequestAsync(OnUpdateResponse, "Component.Set", args);
            Component.Core.RampingChangeGroup.Add(this);
            Component.Core.RampingChangeGroup.PollAuto(0.1);
        }

        /// <summary>
        /// Ramp the position internally in the qsys
        /// </summary>
        /// <param name="position">Target position</param>
        /// <param name="time">Time in seconds to ramp to new value</param>
        public void RampPosition(float position, double time)
        {
            object args = new
            {
                Component.Name,
                Controls = new[]
                {
                    new
                    {
                        Name,
                        Position = position,
                        Ramp = time
                    }
                }
            };

            Component.Core.RequestAsync(OnUpdateResponse, "Component.Set", args);
            Component.Core.RampingChangeGroup.Add(this);
            Component.Core.RampingChangeGroup.PollAuto(0.1);
        }

        public event QsysControlValueChangeEventHandler ValueChange;

        private void OnUpdateResponse(QsysResponse response)
        {
            if (response.IsError) return;
            if (response.IsAck)
            {
                //CrestronConsole.PrintLine("{0}.{1} - Control.Set Response OK", Component.Name, Name);
                var id = UpdateAsync();
                //CrestronConsole.PrintLine("Requested update: {0}", id);
            }
            else
            {
                //CrestronConsole.PrintLine("Received update: {0}", response.Id);
                UpdateFromData(response.Result["Controls"].First);
                //CrestronConsole.PrintLine("UpdateFromData(data)\r\n{0}", response.Result.ToString(Formatting.Indented));
            }
        }

        internal void UpdateFromData(JToken data)
        {
            if (String.IsNullOrEmpty(Name))
                Name = data["Name"].Value<string>();
            else if (Name != data["Name"].Value<string>())
                return;

            var newString = data["String"].Value<string>();
            _position = data["Position"].Value<float>();

            var changed = false;
            var value = data["Value"].Value<float>();
            if (Math.Abs(_value - value) > 0.01)
            {
                _value = data["Value"].Value<float>();
                changed = true;
            }
            
            if (newString != _string)
            {
                changed = true;
                _string = newString;
            }
#if DEBUG
            CrestronConsole.PrintLine("Control updated: {0}", this);
#endif
            if (changed)
            {
                OnValueChange(this, _value, _string);
            }
        }

        private void OnValueChange(QsysControl control, float newValue, string stringValue)
        {
            try
            {
                var handler = ValueChange;
                var args = new QsysControlValueChangeEventArgs(newValue, stringValue);
                Component.OnControlChange(this, args);
                if (handler != null) handler(control, args);
            }
            catch (Exception e)
            {
                CloudLog.Exception("Error in QsysControl.OnValueChange()", e);
            }
        }

        /// <summary>
        /// A summary of the QsysControl
        /// </summary>
        /// <returns>String of summary</returns>
        public override string ToString()
        {
            var name = Component.Name + "." + Name;
            if (name.Length > 35)
                name = name.Substring(0, 35);
            var position = Tools.ScaleRange(_position, 0, 1, 0, 20);
            using (var sw = new StringWriter())
            {
                sw.Write(name.PadRight(35));
                sw.Write(" [");
                for (var i = 0; i < 20; i++)
                {
                    sw.Write(i < position ? "X" : "_");
                }
                sw.Write("\"" + String + "\" " + Value);
                return sw.ToString();
            }
        }
    }

    public class QsysControlValueChangeEventArgs : EventArgs
    {
        private readonly string _stringValue;

        public QsysControlValueChangeEventArgs(float newValue, string stringValue)
        {
            _stringValue = stringValue;
            NewValue = newValue;
        }

        public float NewValue { get; private set; }

        public string StringValue
        {
            get { return _stringValue; }
        }
    }

    public delegate void QsysControlValueChangeEventHandler(QsysControl control, QsysControlValueChangeEventArgs args);
}