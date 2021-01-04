 
using Newtonsoft.Json.Linq;

namespace UX.Lib2.Devices.Audio.Biamp
{
    public class TesiraNotification : TesiraMessage
    {
        internal TesiraNotification(string message)
            : base(message)
        {
        }

        public override string Id
        {
            get
            {
                var json = TryParseResponse();
                if (json == null) return string.Empty;
                return json["publishToken"] != null ? json["publishToken"].Value<string>() : string.Empty;
            }
        }

        public override TesiraMessageType Type
        {
            get { return TesiraMessageType.Notification; }
        }

        public override string ToString()
        {
            return Message;
        }
    }
}