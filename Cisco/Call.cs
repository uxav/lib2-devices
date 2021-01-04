 
using System;
using System.Text.RegularExpressions;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Cisco
{
    public class Call : CodecApiElement
    {
        #region Fields

        private readonly int _id;

        [CodecApiName("Status")]
        private CallStatus _status;

        [CodecApiName("DisplayName")]
        private string _displayName;

        [CodecApiName("TransmitCallRate")]
#pragma warning disable 649 // assigned using reflection
        private int _transmitCallRate;
#pragma warning restore 649

        [CodecApiName("RemoteNumber")]
        private string _remoteNumber;

        [CodecApiName("Direction")]
        private CallDirection _direction;

        [CodecApiName("DeviceType")]
#pragma warning disable 649 // assigned using reflection
        private CallDeviceType _deviceType;
#pragma warning restore 649

        [CodecApiName("CallbackNumber")]
#pragma warning disable 649 // assigned using reflection
        private string _callbackNumber;
#pragma warning restore 649

        [CodecApiName("CallType")]
#pragma warning disable 649 // assigned using reflection
        private CallType _callType;
#pragma warning restore 649

        [CodecApiName("AnswerState")]
#pragma warning disable 649 // assigned using reflection
        private CallAnswerState _answerState;
#pragma warning restore 649

        [CodecApiName("FacilityServiceId")]
#pragma warning disable 649 // assigned using reflection
        private int _facilityServiceId;
#pragma warning restore 649

        [CodecApiName("HoldReason")]
#pragma warning disable 649 // assigned using reflection
        private CallHoldReason _holdReason;
#pragma warning restore 649

        [CodecApiName("PlacedOnHold")]
#pragma warning disable 649 // assigned using reflection
        private bool _placedOnHold;
#pragma warning restore 649

        [CodecApiName("Protocol")]
#pragma warning disable 649 // assigned using reflection
        private CallProtocol _protocol;
#pragma warning restore 649

        [CodecApiName("ReceiveCallRate")]
#pragma warning disable 649 // assigned using reflection
        private int _receiveCallRate;
#pragma warning restore 649

        [CodecApiName("Duration")]
        private DateTime _startTime;

        #endregion

        #region Constructors

        internal Call(CiscoTelePresenceCodec codec, int id)
            : base(codec, id)
        {
            _id = id;
            _startTime = DateTime.Now;
        }

        internal Call(CiscoTelePresenceCodec codec, int id, StatusUpdateItem[] statusItems)
            : base(codec, id)
        {
            _id = id;
            _startTime = DateTime.Now;
            if (!codec.Calls.ContainsCallWithId(id))
                UpdateFromStatus(statusItems);
            Debug.WriteSuccess("{0} created!", this);
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public int Id
        {
            get { return _id; }
        }

        public CallAnswerState AnswerState
        {
            get { return _answerState; }
        }

        public CallType CallType
        {
            get { return _callType; }
        }

        public string CallbackNumber
        {
            get { return _callbackNumber; }
        }

        public CallDeviceType DeviceType
        {
            get { return _deviceType; }
        }

        public CallDirection Direction
        {
            get { return _direction; }
            internal set { _direction = value; }
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(_displayName))
                {
                    var name = _displayName;

                    if (!string.IsNullOrEmpty(CallbackNumber))
                    {
                        var match = Regex.Match(CallbackNumber, @"(?:\w+:)?((\w+)@[\-.]+)");
                        if (match.Success && match.Groups[2].Value == _displayName)
                        {
                            return match.Groups[1].Value;
                        }
                    }

                    return name;
                }

                if (!string.IsNullOrEmpty(CallbackNumber))
                {
                    var number = CallbackNumber;
                    var match = Regex.Match(number, @"(?:\w+:)?(.+)");
                    if (match.Success)
                    {
                        number = match.Groups[1].Value;
                    }
                    return number;
                }

                return
                    !string.IsNullOrEmpty(RemoteNumber) ? RemoteNumber : "No Name";
            }
            internal set { _displayName = value; }
        }

        public TimeSpan Duration
        {
            get { return DateTime.Now - _startTime; }
            set { _startTime = DateTime.Now - value; }
        }

        public int FacilityServiceId
        {
            get { return _facilityServiceId; }
        }

        public CallHoldReason HoldReason
        {
            get { return _holdReason; }
        }

        public bool PlacedOnHold
        {
            get { return _placedOnHold; }
        }

        public CallProtocol Protocol
        {
            get { return _protocol; }
        }

        public int ReceiveCallRate
        {
            get { return _receiveCallRate; }
        }

        public string RemoteNumber
        {
            get { return _remoteNumber; }
            internal set { _remoteNumber = value; }
        }

        /// <summary>
        /// The status of the call
        /// </summary>
        public CallStatus Status
        {
            get { return _status; }
            internal set { _status = value; }
        }

        /// <summary>
        /// True if the call is in progress
        /// </summary>
        public bool InProgress
        {
            get
            {
                switch (Status)
                {
                    case CallStatus.Dialling:
                    case CallStatus.Connecting:
                    case CallStatus.Ringing:
                    case CallStatus.EarlyMedia:
                    case CallStatus.Disconnecting:
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// True if the call is connected
        /// </summary>
        public bool Connected
        {
            get { return Status == CallStatus.Connected; }
        }

        /// <summary>
        /// True if call is on hold
        /// </summary>
        public bool OnHold
        {
            get { return Status == CallStatus.OnHold; }
        }

        public int TransmitCallRate
        {
            get { return _transmitCallRate; }
        }

        public bool Active
        {
            get { return Status != CallStatus.Idle && Status != CallStatus.Disconnecting; }
        }

        public Conference.Call Conference
        {
            get { return Codec.Conference.Call.ContainsKey(Id) ? Codec.Conference.Call[Id] : null; }
        }

        #endregion

        #region Methods

        public void Accept()
        {
            Codec.Send("xCommand Call Accept CallId: {0}", Id);
        }

        public void Disconnect()
        {
            Codec.Send("xCommand Call Disconnect CallId: {0}", Id);
        }

        public void DtmfSend(string dtmfString)
        {
            Codec.Send("xCommand Call DTMFSend CallId: {0} DTMFString: \"{1}\"", Id, dtmfString);
        }

        public void Forward(string displayName, string number)
        {
            var cmd = new CodecCommand("Call", "Forward");
            cmd.Args.Add("CallId", Id);
            cmd.Args.Add("DisplayName", displayName);
            cmd.Args.Add("Number", number);
            Codec.SendCommandAsync(cmd, (id, ok, result) =>
            {
                if (!ok) CloudLog.Error("Call.Forward method returned error");
            });
        }

        public void Hold()
        {
            Codec.Send("xCommand Call Hold CallId: {0}", Id);
        }

        public void Ignore()
        {
            Codec.Send("xCommand Call Ignore CallId: {0}", Id);
        }

        public void Reject()
        {
            Codec.Send("xCommand Call Reject CallId: {0}", Id);
        }

        public void Resume()
        {
            Codec.Send("xCommand Call Resume CallId: {0}", Id);
        }

        public void UnattendedTranser(string number)
        {
            var cmd = new CodecCommand("Call", "UnattendedTranser");
            cmd.Args.Add("CallId", Id);
            cmd.Args.Add("Number", number);
            Codec.SendCommandAsync(cmd, (id, ok, result) =>
            {
                if (!ok) CloudLog.Error("Call.UnattendedTranser method returned error");
            });
        }

        protected override void OnStatusChanged(CodecApiElement element, string[] propertyNamesWhichUpdated)
        {
            base.OnStatusChanged(element, propertyNamesWhichUpdated);
            Codec.Calls.OnCallStatusChange(Codec, CallStatusEventType.StatusUpdated, this);
        }

        public override string ToString()
        {
            return string.Format("{0} Call {1} ({2}) \"{3}\" {4} - {5}", Direction, Id, CallType, DisplayName,
                RemoteNumber, Status);
        }

        #endregion
    }

    public enum CallStatus
    {
        Idle,
        Dialling,
        Ringing,
        Connecting,
        Disconnecting,
        Connected,
        OnHold,
        EarlyMedia,
        Preserved,
        RemotePreserved
    }

    public enum CallType
    {
        Video,
        Audio,
        AudioCanEscalate,
        ForwardAllCall,
        Unknown
    }

    public enum CallAnswerState
    {
        Unanswered,
        Ignored,
        Autoanswered,
        Answered
    }

    public enum CallDeviceType
    {
        Endpoint,
// ReSharper disable once InconsistentNaming
        MCU
    }

    public enum CallDirection
    {
        Incoming,
        Outgoing
    }

    public enum CallHoldReason
    {
        Conference,
        Transfer,
        None
    }

    public enum CallProtocol
    {
        H320,
        H323,
// ReSharper disable once InconsistentNaming
        SIP,
        Spark,
        Unknown
    }
}