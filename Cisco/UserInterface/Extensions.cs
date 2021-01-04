using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Cisco.UserInterface
{
    public class Extensions : CodecApiElement
    {
        [CodecApiNameAttribute("Widget")]
        private Dictionary<int, Widget> _widgets = new Dictionary<int, Widget>();

        public Extensions(CodecApiElement parent, string propertyName)
            : base(parent, propertyName)
        {
            Codec.EventReceived += CodecOnEventReceived;
        }

        public event CiscoCodecUIPanelClickedEventHandler PanelClicked;

        public event CiscoCodecUIWidgetEventHandler WidgetChanged;

        public ReadOnlyDictionary<string, Widget> Widgets
        {
            get
            {
                var dict = new Dictionary<string, Widget>();
                var widgets = _widgets.Values.ToArray();
                foreach (var widget in widgets)
                {
                    dict[widget.WidgetId] = widget;
                }
                return new ReadOnlyDictionary<string, Widget>(dict);
            }
        }

        protected override void OnStatusChanged(CodecApiElement element, string[] propertyNamesWhichUpdated)
        {
            base.OnStatusChanged(element, propertyNamesWhichUpdated);

            Debug.WriteSuccess("**** Extensions Updated ****", "propertyNamesWhichUpdated: {0}",
                string.Join(", ", propertyNamesWhichUpdated));
        }

        private void CodecOnEventReceived(CiscoTelePresenceCodec codec, string name, Dictionary<string, string> properties)
        {
            switch (name)
            {
                case "UserInterface Extensions Panel Clicked":
                    Debug.WriteSuccess("Panel Clicked", properties["PanelId"]);
                    OnPanelClicked(properties["PanelId"]);
                    break;
                case "UserInterface Extensions Widget Action":
                    Debug.WriteSuccess("Widget Action", "Type \"{0}\": {1} = {2}", properties["Type"],
                        properties["WidgetId"],
                        properties["Value"]);
                    OnWidgetChanged(new CiscoCodecUIWidgetEventArgs
                    {
                        Type = (Widget.ActionType) Enum.Parse(typeof (Widget.ActionType), properties["Type"], true),
                        Value = properties["Value"],
                        WidgetId = properties["WidgetId"]
                    });
                    break;
            }
        }

        protected virtual void OnPanelClicked(string panelid)
        {
            var handler = PanelClicked;
            if (handler == null) return;
            try
            {
                handler(Codec, panelid);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        protected virtual void OnWidgetChanged(CiscoCodecUIWidgetEventArgs args)
        {
            var handler = WidgetChanged;
            if (handler == null) return;
            try
            {
                handler(Codec, args);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }
    }

    public delegate void CiscoCodecUIPanelClickedEventHandler(CiscoTelePresenceCodec codec, string panelId);

    public class CiscoCodecUIWidgetEventArgs : EventArgs
    {
        public Widget.ActionType Type { get; internal set; }
        public string WidgetId { get; internal set; }
        public string Value { get; internal set; }
    }

    public delegate void CiscoCodecUIWidgetEventHandler(CiscoTelePresenceCodec codec, CiscoCodecUIWidgetEventArgs args);
}