 
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Crestron.SimplSharp;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Cisco.UserInterface
{
    public class Presentation
    {
        private readonly CiscoTelePresenceCodec _codec;
        private readonly List<ExternalSource> _sources = new List<ExternalSource>(); 

        internal Presentation(CiscoTelePresenceCodec codec)
        {
            _codec = codec;
            _codec.EventReceived += CodecOnEventReceived;
        }

        public event UserInterfacePresentationExternalSourceSelectedEventHandler ExternalSourceSelected;

        public ReadOnlyCollection<ExternalSource> ExternalSources
        {
            get { return _sources.AsReadOnly(); }
        }

        public void ExternalSourceAdd(string sourceId, string sourceName, int connectorId, ExternalSourceType type)
        {
            var cmd = new CodecCommand("UserInterface/Presentation/ExternalSource", "Add");
            cmd.Args.Add("ConnectorId", connectorId);
            cmd.Args.Add("SourceIdentifier", sourceId);
            cmd.Args.Add("Name", sourceName);
            cmd.Args.Add("Type", type.ToString());
            _codec.SendCommand(cmd);
        }

        public void GetExternalSources()
        {
            var response = _codec.SendCommand(new CodecCommand("UserInterface/Presentation/ExternalSource", "List"));
            try
            {
                var xml = response.Xml.Element("Command").Element("ExternalSourceListResult");
                try
                {
                    var sources = new List<ExternalSource>();
#if DEBUG
                    Debug.WriteInfo("External Sources Get Response\r\n" + xml);
#endif
                    foreach (var element in xml.Elements("Source"))
                    {
                        sources.Add(new ExternalSource(_codec,
                            element.Element("SourceIdentifier").Value,
                            element.Element("Name").Value,
                            int.Parse(element.Element("ConnectorId").Value),
                            (ExternalSourceType)
                                Enum.Parse(typeof(ExternalSourceType), element.Element("Type").Value, true))
                        {
                            State =
                                (ExternalSourceState)
                                    Enum.Parse(typeof(ExternalSourceState), element.Element("State").Value, true)
                        });
                    }
                    _sources.Clear();
                    foreach (var source in sources)
                    {
                        _sources.Add(source);
                        Debug.WriteInfo("Found source", source.ToString());
                    }
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }
            catch (Exception e)
            {
                CloudLog.Exception(e, "Error getting External Source list from codec");
            }
        }

        public void ClearExternalSources()
        {
            _codec.SendCommand(new CodecCommand("UserInterface/Presentation/ExternalSource", "RemoveAll"));
        }

        private void CodecOnEventReceived(CiscoTelePresenceCodec codec, string name, Dictionary<string, string> properties)
        {
#if DEBUG
            var values = properties.Select(property => string.Format("- {0} = {1}", property.Key, property.Value)).ToArray();
            Debug.WriteInfo("Event received", "name: {0}\r\n{1}", name, string.Join("\r\n", values));
#endif
            if(name != "UserInterface Presentation ExternalSource Selected") return;

            try
            {
                var sourceId = properties["SourceIdentifier"];
                var source = _sources.FirstOrDefault(s => s.SourceIdentifier == sourceId);
                OnExternalSourceSelected(sourceId, source);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        protected virtual void OnExternalSourceSelected(string sourceidentifier, ExternalSource source)
        {
            var handler = ExternalSourceSelected;
            if (handler != null)
            {
                try
                {
                    handler(_codec, sourceidentifier, source);
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }
        }
    }

    public delegate void UserInterfacePresentationExternalSourceSelectedEventHandler(
        CiscoTelePresenceCodec codec, string sourceIdentifier, ExternalSource source);
}