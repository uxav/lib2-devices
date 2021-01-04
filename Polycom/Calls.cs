using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Polycom
{
    public class Calls : IEnumerable<Call>
    {
        public Calls(PolycomGroupSeriesCodec codec)
        {
            Codec = codec;
            _calls = new Dictionary<int, Call>();
            codec.ReceivedFeedback += (seriesCodec, data) => OnReceive(data);
        }

        private readonly Dictionary<int, Call> _calls;

        public PolycomGroupSeriesCodec Codec { get; protected set; }

        public Call this[int id]
        {
            get
            {
                return _calls[id];
            }
        }

        public int ActiveCallsCount
        {
            get
            {
                return _calls.Values.Count(c => c.State != CallState.Ended);
            }
        }

        public IEnumerable<Call> ActiveCalls
        {
            get { return _calls.Values.Where(c => c.State != CallState.Ended); }
        }

        public IEnumerable<Call> InProgressCalls
        {
            get { return _calls.Values.Where(c => c.Status == CallStatus.Connecting || c.Status == CallStatus.Opened); }
        }

        void OnCallChange(Call call, CallChangeEventType changeEventType)
        {
            try
            {
                if (CallChanged != null)
                {
                    CallChanged(Codec, new CallChangeEventArgs(call, changeEventType));
                    if (call.State == CallState.Ended)
                        _calls.Remove(call.Id);
#if DEBUG
                    Debug.WriteSuccess("_calls.Count", _calls.Count.ToString());
#endif
                }
            }
            catch (Exception e)
            {
                CloudLog.Exception(e, "Error calling call change event");
            }
        }

        public event CallChangeEventHandler CallChanged;
        public event IncomingCallEventHandler IncomingCall;

        void OnReceive(string receivedString)
        {
            try
            {
                switch (receivedString)
                {
                    case "hanging up all":
                    case "system is not in a call":
                        var connectingCalls = this.Where(c => c.State != CallState.Complete).ToArray();
                        foreach (var call in connectingCalls)
                        {
                            call.State = CallState.Ended;
                            OnCallChange(call, CallChangeEventType.Ended);
                        }
                        break;
                    case "hanging up video":
                        var calls = _calls.Values.ToArray();
                        foreach (var call in calls.Where(c => c.HangingUp))
                        {
                            call.HangingUp = false;
                            OnCallChange(call, CallChangeEventType.Ended);
                        }
                        break;
                    default:
                        if (receivedString.StartsWith("cs: call["))
                        {
                            var regex = new Regex(@"(\w+)\[(.*?)\]");
                            Call call = null;
                            var isNewCall = false;

                            foreach (Match match in regex.Matches(receivedString))
                            {
                                switch (match.Groups[1].Value)
                                {
                                    case "call":
                                        var callId = int.Parse(match.Groups[2].Value);
                                        if (!_calls.ContainsKey(callId))
                                        {
                                            _calls.Add(callId, new Call(Codec, callId));
                                            isNewCall = true;
                                        }
                                        call = _calls[callId];
                                        break;
                                    case "chan":
                                        break;
                                    case "dialstr":
                                        if (call != null)
                                        {
                                            call.Number = match.Groups[2].Value;
                                        }
                                        break;
                                    case "state":
                                        if (call != null)
                                            call.State =
                                                (CallState) Enum.Parse(typeof (CallState), match.Groups[2].Value, true);
                                        break;
                                }
                            }

                            OnCallChange(call, (isNewCall) ? CallChangeEventType.NewCall : CallChangeEventType.Updated);
                        }

                        else if (receivedString.StartsWith("cleared: call["))
                        {
                            var callId = int.Parse(Regex.Match(receivedString, @"\[(.*?)\]").Groups[1].Value);
                            if (_calls.ContainsKey(callId))
                            {
                                _calls[callId].State = CallState.Cleared;
                                OnCallChange(_calls[callId], CallChangeEventType.Updated);
                            }
                        }

                        else if (receivedString.StartsWith("ended: call["))
                        {
                            var callId = int.Parse(Regex.Match(receivedString, @"\[(.*?)\]").Groups[1].Value);
                            if (_calls.ContainsKey(callId))
                            {
                                _calls[callId].State = CallState.Ended;
                                OnCallChange(_calls[callId], CallChangeEventType.Ended);
                            }
                        }

                            // callinfo:43:Polycom Group Series Demo:192.168.1.101:384:connected:notmuted:outgoing:videocall
                            // callinfo:<callid>:<far site name>:<far site number>:<speed>:<connection status>:<mute status>:<call direction>:<call type>
                        else if (receivedString.StartsWith("callinfo:"))
                        {
                            var callinfo = receivedString.Split(':');
                            var callId = int.Parse(callinfo[1]);
                            var farSiteName = callinfo[2];
                            var farSiteNumber = callinfo[3];
                            var speed = int.Parse(callinfo[4]);
                            var connectionStatus = callinfo[5];
                            var muted = (callinfo[6] != "notmuted");
                            var callDirection = (CallDirection) Enum.Parse(typeof (CallDirection), callinfo[7], true);
                            var callType = callinfo[8];

                            Call call = null;

                            var status = (CallStatus) Enum.Parse(typeof (CallStatus), connectionStatus, true);

                            if (!_calls.ContainsKey(callId)
                                && status != CallStatus.Inactive && status != CallStatus.Disconnecting &&
                                status != CallStatus.Disconnected)
                                _calls[callId] = new Call(Codec, callId);

                            if (_calls.ContainsKey(callId))
                                call = _calls[callId];

                            if (call == null) return;
                            call.DisplayName = farSiteName;
                            call.Number = farSiteNumber;
                            call.Status = status;
                            call.Speed = speed;
                            call.Direction = callDirection;
                            call.CallType = callType;

                            OnCallChange(call,
                                call.Status == CallStatus.Inactive
                                    ? CallChangeEventType.Ended
                                    : CallChangeEventType.Updated);
                        }

                            // notification:callstatus:outgoing:34:Polycom Group Series Demo:192.168.1.101:connected:384:0:videocall
                            // notification:callstatus:<call direction>:<call id>:
                            //  <far site name>:<far site number>:<connection status>:
                            //  <call speed>:<status-specific cause code from call engine>:<calltype>
                        else if (receivedString.StartsWith("notification:callstatus"))
                        {
                            var callinfo = receivedString.Split(':');
                            var callDirection = (CallDirection) Enum.Parse(typeof (CallDirection), callinfo[2], true);
                            var callId = int.Parse(callinfo[3]);
                            var farSiteName = callinfo[4];
                            var farSiteNumber = callinfo[5];
                            var connectionStatus = callinfo[6];
                            var speed = int.Parse(callinfo[7]);
                            var causeCode = int.Parse(callinfo[8]);
                            var callType = callinfo[9];

                            Call call = null;
                            var newCall = false;

                            var status = (CallStatus) Enum.Parse(typeof (CallStatus), connectionStatus, true);

                            if (!_calls.ContainsKey(callId)
                                && status != CallStatus.Inactive && status != CallStatus.Disconnecting &&
                                status != CallStatus.Disconnected)
                            {
                                _calls[callId] = new Call(Codec, callId);
                                newCall = true;
                            }

                            if (_calls.ContainsKey(callId))
                                call = _calls[callId];

                            if (call != null)
                            {
                                call.DisplayName = farSiteName;
                                call.Number = farSiteNumber;
                                call.Status = (CallStatus) Enum.Parse(typeof (CallStatus), connectionStatus, true);
                                call.Speed = speed;
                                call.Direction = callDirection;
                                call.CallType = callType;

                                OnCallChange(call, (newCall) ? CallChangeEventType.NewCall : CallChangeEventType.Updated);

                                if (call.Direction == CallDirection.Incoming && call.Status == CallStatus.Ringing &&
                                    IncomingCall != null)
                                    IncomingCall(Codec, call);

                                if (call.Direction == CallDirection.Outgoing && call.Status == CallStatus.Connected)
                                    Codec.Microphones.Unmute();
                            }
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                CloudLog.Exception(e, "Error parsing string: \"{0}\"", receivedString);
            }
        }

        #region IEnumerable<Call> Members

        public IEnumerator<Call> GetEnumerator()
        {
            return _calls.Values.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    public delegate void CallChangeEventHandler(PolycomGroupSeriesCodec codec, CallChangeEventArgs args);

    public delegate void IncomingCallEventHandler(PolycomGroupSeriesCodec codec, Call call);

    public class CallChangeEventArgs : EventArgs
    {
        public CallChangeEventArgs(Call call, CallChangeEventType eventType)
        {
            Call = call;
            EventType = eventType;
        }

        public Call Call;
        public CallChangeEventType EventType;
    }

    public enum CallChangeEventType
    {
        NewCall,
        Updated,
        Ended
    }
}