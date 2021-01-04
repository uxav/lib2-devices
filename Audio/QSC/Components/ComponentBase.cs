 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Audio.QSC.Components
{
    /// <summary>
    /// A Component element of a QSys Core
    /// </summary>
    public class ComponentBase : IEnumerable<QsysControl>
    {
        private readonly Dictionary<string, string> _properties = new Dictionary<string, string>();
        internal readonly Dictionary<string, QsysControl> Controls = new Dictionary<string, QsysControl>();
        private readonly Dictionary<int, RegisterControlResponse> _registerControlCallBacks = new Dictionary<int, RegisterControlResponse>();
        private readonly string _typeString;

        internal ComponentBase(QsysCore core, JToken data)
        {
            try
            {
                Core = core;
                Name = data["Name"].Value<string>();
                _typeString = data["Type"].Value<string>();

                try
                {
                    Type =
                        (QsysComponentType)
                            Enum.Parse(typeof (QsysComponentType),
                                Regex.Replace(_typeString, @"(?:^|_)([a-z])", m => m.Groups[1].ToString().ToUpper()),
                                false);
                }
                catch
                {
                    Type = QsysComponentType.Unknown;
                }

                foreach (var property in data["Properties"])
                {
                    _properties[property["Name"].Value<string>()] = property["Value"].Value<string>();
                }
#if DEBUG
                var details = this + " created";
                details = details + "\r\nProperties:";
                details = Properties.Aggregate(details,
                    (current, property) => current + string.Format("\r\n   - {0} = {1}", property.Key, property.Value));
                CloudLog.Debug(details);
#endif
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        public event QsysControlValueChangeEventHandler ControlValueChange;

        /// <summary>
        /// The QSys core which the component belongs to
        /// </summary>
        public QsysCore Core { get; private set; }

        /// <summary>
        /// The name of the Component
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The type of component in the core
        /// </summary>
        public QsysComponentType Type { get; private set; }

        /// <summary>
        /// Access to the controls registered to the component
        /// </summary>
        /// <param name="controlName">The name of the control</param>
        /// <returns>A QsysControl object</returns>
        public QsysControl this[string controlName]
        {
            get
            {
                if (Controls.ContainsKey(controlName))
                    return Controls[controlName];
                CloudLog.Error("Qsys Component \"{0}\" does not contain control \"{1}\"",
                    Name, controlName);
                return null;
            }
        }

        /// <summary>
        /// The properties of the component
        /// </summary>
        public ReadOnlyDictionary<string, string> Properties
        {
            get { return new ReadOnlyDictionary<string, string>(_properties); }
        }

        /// <summary>
        /// Find if the component has a control registered with a specific name
        /// </summary>
        /// <param name="controlName">The name of the control</param>
        /// <returns>True is control exists</returns>
        public bool HasControl(string controlName)
        {
            return Controls.ContainsKey(controlName);
        }

        /// <summary>
        /// Regsiter and return a Control object syncronously.
        /// Do not use in main thread!!
        /// </summary>
        /// <param name="controlName">The name of the control</param>
        /// <returns>The QsysControl object</returns>
        public QsysControl RegisterControl(string controlName)
        {
            object args = new
            {
                Name,
                Controls = new[]
                {
                    new { @Name = controlName }
                }
            };

            var response = Core.Request("Component.Get", args);

            if (response.IsError) return null;

            var name = response.Result["Controls"].First["Name"].Value<string>();
            if (!Controls.ContainsKey(name))
            {
                new QsysControl(this, name, response.Result["Controls"].First);
            }
            else
            {
                Controls[name].UpdateFromData(response.Result["Controls"].First);
            }
            return Controls[name];
        }

        public IEnumerable<QsysControl> RegisterControls(IEnumerable<string> controlNames)
        {
            var controls = new List<object>();

            foreach (var controlName in controlNames)
            {
                controls.Add(new {@Name = controlName});
            }

            object args = new
            {
                Name,
                Controls = controls
            };

            var response = Core.Request("Component.Get", args);

            if (response.IsError) return null;

            var result = new List<QsysControl>();
            foreach (var control in response.Result["Controls"])
            {
                var name = control["Name"].Value<string>();
                if (!Controls.ContainsKey(name))
                {
                    new QsysControl(this, name, control);
                }
                else
                {
                    Controls[name].UpdateFromData(control);
                }
                result.Add(Controls[name]);
            }
            return result;
        }

        /// <summary>
        /// Register a control with the component
        /// </summary>
        /// <param name="responseCallBack">Callback delegate used to respond on result</param>
        /// <param name="controlName"></param>
        public void RegisterControlAsync(RegisterControlResponse responseCallBack, string controlName)
        {
            object args = new
            {
                Name,
                Controls = new []
                {
                    new { @Name = controlName }
                }
            };

            _registerControlCallBacks[Core.RequestAsync(OnRegisterControlResponse, "Component.Get", args)] = responseCallBack;
        }

        private void OnRegisterControlResponse(QsysResponse response)
        {
            var callBack = _registerControlCallBacks[response.Id];
            _registerControlCallBacks.Remove(response.Id);
            if (response.IsError)
            {
                callBack(false, null);
                return;
            }

            foreach (var control in response.Result["Controls"])
            {
                var name = control["Name"].Value<string>();
                if (!Controls.ContainsKey(name))
                {
                    new QsysControl(this, name, control);
                }
                else
                {
                    Controls[name].UpdateFromData(control);
                }
                callBack(true, Controls[name]);
            }
        }

        /// <summary>
        /// Force an update from the core for the controls asyncronously.
        /// </summary>
        public void UpdateAsync()
        {
            var controls = new List<object>();
            foreach (var name in this.Select(control => control.Name))
            {
                object control = new
                {
                    Name = name
                };
                controls.Add(control);
            }

            object args = new
            {
                Name,
                Controls = controls
            };

            Core.RequestAsync(response =>
            {
                if(response.IsError || response.Result == null) return;

                foreach (var control in response.Result["Controls"])
                {
                    this[control["Name"].Value<string>()].UpdateFromData(control);
                }
            }, "Component.Get", args);
        }

        internal virtual void OnControlChange(QsysControl control, QsysControlValueChangeEventArgs args)
        {
            var handler = ControlValueChange;
            if (handler != null)
            {
                try
                {
                    handler(control, args);
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }
        }

        public override string ToString()
        {
            return string.Format("{0} {1} \"{2}\"", GetType().Name, _typeString, Name);
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerator<QsysControl> GetEnumerator()
        {
            return Controls.Values.GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// A response called on result of trying to register a control for a component
    /// </summary>
    /// <param name="success">True is it was successull</param>
    /// <param name="control">The control object if successful, null if failed</param>
    public delegate void RegisterControlResponse(bool success, QsysControl control);

    public enum QsysComponentType
    {
        Unknown,
        AudioFilePlayer,
        Gain,
        Mixer,
        ProjectLink
    }
}