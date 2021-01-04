 
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp.CrestronXmlLinq;
using Crestron.SimplSharp.Reflection;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Cisco.CallHistory
{
    public class CallHistoryItem
    {
        #region Fields

        private readonly CiscoTelePresenceCodec _codec;
        private string _displayName;

        #endregion

        #region Constructors

        internal CallHistoryItem(CiscoTelePresenceCodec codec, XElement element)
        {
            _codec = codec;
            var properties = GetType().GetCType().GetProperties().Where(p => p.CanWrite).ToArray();
            Debug.WriteInfo("Creating " + GetType().Name, "Properties writable = {0}", properties.Length);

            foreach (var propertyInfo in GetType().GetCType().GetProperties().Where(p => p.CanWrite))
            {
                try
                {
                    string eName;
                    switch (propertyInfo.Name)
                    {
                        case "Id":
                            eName = "CallHistoryId";
                            break;
                        case "StartTime":
                            eName = "StartTimeUTC";
                            break;
                        case "EndTime":
                            eName = "EndTimeUTC";
                            break;
                        default:
                            eName = propertyInfo.Name;
                            break;
                    }

                    var e = element.Element(eName);

                    if (propertyInfo.PropertyType == typeof (string).GetCType())
                    {
                        propertyInfo.SetValue(this, e.Value, null);
                    }
                    else if (propertyInfo.PropertyType == typeof(int).GetCType())
                    {
                        propertyInfo.SetValue(this, int.Parse(e.Value), null);
                    }
                    else if (propertyInfo.PropertyType == typeof(DateTime).GetCType())
                    {
                        propertyInfo.SetValue(this, DateTime.Parse(e.Value), null);
                    }
                    else if (propertyInfo.PropertyType.IsEnum)
                    {
                        propertyInfo.SetValue(this, Enum.Parse(propertyInfo.PropertyType, e.Value, false), null);
                    } 
                    else if (propertyInfo.PropertyType == typeof (bool).GetCType())
                    {
                        if (propertyInfo.Name == "IsAcknowledged")
                        {
                            IsAcknowledged = e.Value == "Acknowledged";
                        }
                        else
                        {
                            propertyInfo.SetValue(this, bool.Parse(e.Value.ToLower()), null);
                        }
                    }
                }
                catch (Exception e)
                {
                    CloudLog.Error("Parsing property {0} from call history info, {1}", propertyInfo.Name, e.Message);
                }
            }
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public int Id { get; private set; }
        public int CallId { get; private set; }
        public string RemoteNumber { get; private set; }
        public string CallbackNumber { get; private set; }

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(_displayName)) return "Unknown";
                var match = Regex.Match(_displayName, @"\w{3,5}:(.+)");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                return _displayName;
            }
            private set { _displayName = value; }
        }

        public CallDirection Direction { get; private set; }
        public string Protocol { get; private set; }
        public string CallRate { get; private set; }
        public CallType CallType { get; private set; }
        public string BookingId { get; private set; }

        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }

        public TimeSpan Duration
        {
            get { return EndTime - StartTime; }
        }

        public string TimeAgo
        {
            get
            {
                return (DateTime.Now - StartTime).ToPrettyTimeAgo();
            }
        }

        public int DaysAgo { get; private set; }

        public OccurrenceType OccurrenceType { get; private set; }

        public string DisconnectCause { get; private set; }
        public int DisconnectCauseCode { get; private set; }
        public DisconnectCauseType DisconnectCauseType { get; private set; }
        public string DisconnectCauseOrigin { get; private set; }

        public bool IsAcknowledged { get; private set; }

        public string EncryptionType { get; private set; }

        #endregion

        #region Methods

        public void Redial(DialResult callback)
        {
            _codec.Calls.DialNumber(CallbackNumber, callback);
        }

        public override string ToString()
        {
            return string.Format("\"{0}\" {1} {2} - {3}", DisplayName, RemoteNumber, OccurrenceType, TimeAgo);
        }

        #endregion
    }

    public enum OccurrenceType
    {
        Missed,
        AnsweredElsewhere,
        Forwarded,
        Placed,
        NoAnswer,
        Received,
        Rejected,
        UnacknowledgedMissed
    }
}