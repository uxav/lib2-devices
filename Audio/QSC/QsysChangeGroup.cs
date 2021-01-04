using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp.CrestronIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Audio.QSC
{
    /// <summary>
    /// A change group used to notify changes to component controls.
    /// You need to add a control or component to this before using
    /// any of the other methods!
    /// </summary>
    public class QsysChangeGroup
    {
        internal QsysChangeGroup(QsysCore qsysRemoteControl, string id)
        {
            Core = qsysRemoteControl;
            Id = id;
        }

        /// <summary>
        /// The Core that owns this object
        /// </summary>
        public QsysCore Core { get; private set; }

        /// <summary>
        /// The ID string of the change group
        /// </summary>
        public string Id { get; private set; }

        public QsysChangeGroup Add(IEnumerable<QsysControl> controls)
        {
            var qsysControls = controls as QsysControl[] ?? controls.ToArray();
            if (!qsysControls.Any())
            {
                CloudLog.Warn("Cannot add controls to changegroup... array is empty");
                return null;
            }
            if (qsysControls.Any(c => c.Component != qsysControls.First().Component)) return null;

            using (var writer = new StringWriter())
            {
                using (var json = new JsonTextWriter(writer))
                {
                    json.WriteStartObject();
                    json.WritePropertyName("Id");
                    json.WriteValue(Id);
                    json.WritePropertyName("Component");
                    json.WriteStartObject();
                    json.WritePropertyName("Name");
                    json.WriteValue(qsysControls.First().Component.Name);
                    json.WritePropertyName("Controls");
                    json.WriteStartArray();
                    foreach (var control in qsysControls)
                    {
                        json.WriteStartObject();
                        json.WritePropertyName("Name");
                        json.WriteValue(control.Name);
                        json.WriteEndObject();
                    }
                    json.WriteEndArray();
                    json.WriteEndObject();
                    json.WriteEndObject();
                }

                var args = JObject.Parse(writer.ToString());

                Core.RequestAsync(OnRequestResponse, "ChangeGroup.AddComponentControl", args);
            }

            return this;
        }

        public QsysChangeGroup Add(QsysControl control)
        {
            var args = new
            {
                Id,
                Component = new
                {
                    control.Component.Name,
                    Controls = new[]
                    {
                        new { control.Name }
                    }
                }
            };

            Core.RequestAsync(OnRequestResponse, "ChangeGroup.AddComponentControl", args);

            return this;
        }

        public void Destroy()
        {
            var args = new
            {
                Id
            };

            Core.RequestAsync(response =>
            {
                if (response.IsAck) Core.RemoveChangeGroup(Id);
            }, "ChangeGroup.Destroy", args);
        }

        /// <summary>
        /// Ask for an update of any controls registered to the group
        /// </summary>
        public void Poll()
        {
            Core.RequestAsync(OnRequestResponse, "ChangeGroup.Poll", new
            {
                Id                
            });
        }

        /// <summary>
        /// Setup automatic poll responses every so many seconds
        /// </summary>
        /// <param name="seconds">Time in seconds for the poll interval</param>
        public void PollAuto(double seconds)
        {
            Core.RequestAsync(OnRequestResponse, "ChangeGroup.AutoPoll", new
            {
                Id,
                Rate = seconds
            });
        }

        public void Invalidate()
        {
            Core.RequestAsync(OnRequestResponse, "ChangeGroup.Invalidate", new
            {
                Id
            });
        }

        private static void OnRequestResponse(QsysResponse response)
        {
            /*if (response.IsAck)
            {
                CrestronConsole.PrintLine("ChangeGroup Response: OK");
                CrestronConsole.PrintLine(" {0}:\r\n{1}", response.Request.Method, response.Request.Args);
            }
            else if (response.IsError)
                CrestronConsole.PrintLine("ChangeGroup Response: {0}", response.ErrorMessage);
             */
        }
    }
}