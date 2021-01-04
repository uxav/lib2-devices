using System;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Cisco.UserInterface
{
    public class Widget : CodecApiElement
    {
        [CodecApiNameAttribute("WidgetId")]
#pragma warning disable 649 // assigned using reflection
        private string _widgetId;
#pragma warning restore 649

        [CodecApiNameAttribute("Value")]
#pragma warning disable 649 // assigned using reflection
        private string _value;
#pragma warning restore 649

        internal Widget(CodecApiElement parent, string propertyName, int indexer)
            : base(parent, propertyName, indexer)
        {

        }

        public enum ActionType
        {
            Pressed,
            Released,
            Clicked,
            Changed
        }

        public string WidgetId
        {
            get { return _widgetId; }
        }

        public string Value
        {
            get { return _value; }
        }

        public void SetValue(object value)
        {
            try
            {
                Codec.Send("xCommand UserInterface Extensions Widget SetValue WidgetId: \"{0}\" Value: \"{1}\"",
                    _widgetId, value);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        public void UnsetValue()
        {
            try
            {
                Codec.Send("xCommand UserInterface Extensions Widget UnsetValue WidgetId: \"{0}\"",
                    _widgetId);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }
    }
}