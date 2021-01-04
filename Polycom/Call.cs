using System;

namespace UX.Lib2.Devices.Polycom
{
    public class Call
    {
        private CallState _state;
        private string _displayName;
        private string _number;

        public Call(PolycomGroupSeriesCodec codec, int id)
        {
            Codec = codec;
            Id = id;
            TimeStart = DateTime.Now;
        }

        public PolycomGroupSeriesCodec Codec { get; protected set; }
        public int Id { get; protected set; }

        public CallState State
        {
            get { return _state; }
            set
            {
                if (_state == value) return;

                _state = value;
                if (_state == CallState.Ended)
                    TimeEnd = DateTime.Now;
                if (_state == CallState.Complete)
                    TimeStart = DateTime.Now;
            }
        }

        public CallStatus Status { get; set; }

        public string Number
        {
            get { return _number ?? string.Empty; }
            set { _number    = value; }
        }

        public string DisplayName
        {
            get { return string.IsNullOrEmpty(_displayName) ? Number : _displayName; }
            set { _displayName = value; }
        }

        public CallDirection Direction { get; set; }
        public string CallType { get; set; }
        public int Speed { get; set; }
        public bool HangingUp { get; set; }

        public DateTime TimeStart { get; private set; }
        public DateTime TimeEnd { get; private set; }

        public TimeSpan Duration
        {
            get
            {
                if (_state != CallState.Ended)
                {
                    return DateTime.Now - TimeStart;
                }
                return TimeEnd - TimeStart;
            }
        }

        public void Hangup()
        {
            HangingUp = true;
            Codec.Send(string.Format("hangup video \"{0}\"", Id));
            State = CallState.Ended;
            Codec.Send(string.Format("callinfo callid \"{0}\"", Id));
        }
    }

    public enum CallDirection
    {
        Unknown,
        Outgoing,
        Incoming
    }

    public enum CallState
    {
        Allocated,
        Ringing,
        Connecting,
        Complete,
        Cleared,
        Ended
    }

    public enum CallStatus
    {
        Opened,
        Ringing,
        Connecting,
        Connected,
        Disconnecting,
        Disconnected,
        Inactive
    }
}