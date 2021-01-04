 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronXmlLinq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Cisco
{
    public class Calls : IEnumerable<Call>
    {
        #region Fields

        private readonly CiscoTelePresenceCodec _codec;
        private readonly CCriticalSection _lock = new CCriticalSection();
        private readonly Dictionary<int, Call> _calls = new Dictionary<int, Call>();
        private readonly CCriticalSection _dialCallbacksLock = new CCriticalSection();
        private readonly Dictionary<int, DialCallBackInfo> _dialCallbacks = new Dictionary<int, DialCallBackInfo>();

        #endregion

        #region Constructors

        internal Calls(CiscoTelePresenceCodec codec)
        {
            _codec = codec;
            _codec.StatusReceived += CodecOnStatusReceived;
            _codec.EventReceived += CodecOnEventReceived;
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event CallStatusChangedEventHandler CallStatusChange;

        public event CallSuccessEventHandler CallSuccess;

        public event CallDisconnectEventHandler CallDisconnected;

        public event CallIncomingEventHandler CallIncoming;

        #endregion

        #region Delegates
        #endregion

        public Call this[int id]
        {
            get
            {
                return _calls.ContainsKey(id) ? _calls[id] : null;
            }
        }

        #region Properties

        /// <summary>
        /// The total count of calls
        /// </summary>
        public int Count
        {
            get { return _calls.Count(c => c.Value.Status > CallStatus.Idle); }
        }

        public IEnumerable<Call> Connected
        {
            get
            {
                return
                    _calls.Values.Where(c => c.Status != CallStatus.Idle && !c.InProgress)
                        .OrderByDescending(c => c.Status == CallStatus.Connected);
            }
        }

        public IEnumerable<Call> InProgress
        {
            get { return _calls.Values.Where(c => c.InProgress); }
        }

        /// <summary>
        /// The count of calls which are currently connected
        /// </summary>
        public int ConnectedCount
        {
            get { return _calls.Count(c => c.Value.Status == CallStatus.Connected); }
        }

        #endregion

        #region Methods

        internal void Dial(string number, CodecCommandArg[] args, DialResult callback)
        {

#if DEBUG
            Debug.WriteInfo("Codec Dial", "Checking Capabilities");
            Debug.WriteInfo("  MaxCalls", "{0}", _codec.Capabilities.Conference.MaxCalls);
            Debug.WriteInfo("  MaxActiveCalls", "{0}", _codec.Capabilities.Conference.MaxActiveCalls);
            Debug.WriteInfo("  MaxAudioCalls", "{0}", _codec.Capabilities.Conference.MaxAudioCalls);
            Debug.WriteInfo("  MaxVideoCalls", "{0}", _codec.Capabilities.Conference.MaxVideoCalls);
            Debug.WriteInfo("  NumberOfActiveCalls", "{0}", _codec.SystemUnit.State.NumberOfActiveCalls);
            Debug.WriteInfo("  NumberOfSuspendedCalls", "{0}", _codec.SystemUnit.State.NumberOfSuspendedCalls);
            Debug.WriteInfo("  TotalNumberOfCalls", "{0}", _codec.SystemUnit.State.TotalNumberOfCalls);
#endif
            var numberOfConnectedVideoCalls = this.Count(c => c.Connected && c.CallType == CallType.Video);
            var shouldHoldACall = (_codec.SystemUnit.State.NumberOfActiveCalls > 0 &&
                                   _codec.SystemUnit.State.NumberOfActiveCalls ==
                                   _codec.Capabilities.Conference.MaxActiveCalls) ||
                                  (numberOfConnectedVideoCalls > 0 && numberOfConnectedVideoCalls ==
                                   _codec.Capabilities.Conference.MaxVideoCalls);

            if (shouldHoldACall)
            {
#if DEBUG
                Debug.WriteWarn("Codec needs to hold another call before calling!");
#endif
                try
                {
                    var lastConnectedCall = _codec.Calls.LastOrDefault(c => c.Connected);
                    if (lastConnectedCall != null)
                    {
                        lastConnectedCall.Hold();
                    }
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }

            var cmd = new CodecCommand("", "Dial");
            cmd.Args.Add("Number", number);
            cmd.Args.Add(args);
            var requestId = _codec.SendCommandAsync(cmd, (id, ok, result) =>
            {
                var callBackInfo = _dialCallbacks[id];

                _dialCallbacksLock.Enter();
                _dialCallbacks.Remove(id);
                _dialCallbacksLock.Leave();

                Debug.WriteInfo("Dial Result Callback", "Request {0}, OK = {1}\r\n{2}", id, ok, result);

                if (ok)
                {
                    var callId = int.Parse(result.Element("CallId").Value);
                    Debug.WriteSuccess("Dial Result OK, call {0}", callId);
                    callBackInfo.CallBack(0, "OK", callId);
                    return;
                }

                try
                {
                    var cause = int.Parse(result.Element("Cause").Value);
                    var message = result.Element("Description").Value;
                    Debug.WriteError("Dial Failed, Error {0} - {1}", cause, message);
                    callBackInfo.CallBack(cause, message, 0);
                }
                catch
                {
                    Debug.WriteError("Dial Result",
                    result != null ? result.ToString(SaveOptions.DisableFormatting) : "Unknown Error");
                }
            });

            var info = new DialCallBackInfo()
            {
                NumberDialed = number,
                CallBack = callback
            };

            _dialCallbacksLock.Enter();
            _dialCallbacks[requestId] = info;
            _dialCallbacksLock.Leave();
        }

        public void DialNumber(string number, DialResult callback)
        {
            Dial(number, new CodecCommandArg[] {}, callback);
        }

        public void DisconnectAll()
        {
            _codec.Send("xCommand Call Disconnect");
        }

        public bool ContainsCallWithId(int id)
        {
            return _calls.ContainsKey(id);
        }

        public void Join()
        {
            Join(this);
        }

        public void Join(IEnumerable<Call> calls)
        {
            var cmd = new CodecCommand("Call", "Join");
            foreach (var call in calls)
            {
                cmd.Args.Add("CallId", call.Id);
            }
            var response = _codec.SendCommand(cmd);
#if DEBUG
            Debug.WriteInfo(response.Xml.Element("Command").ToString());
#endif
        }

        private Call GetOrInsert(int id, CallStatus status, CallDirection direction)
        {
            if (_calls.ContainsKey(id))
                return _calls[id];

            var call = new Call(_codec, id)
            {
                Status = status,
                Direction = direction
            };

            _lock.Enter();
                _calls.Add(id, call);
            _lock.Leave();
            return call;
        }

        internal void Remove(int id)
        {
#if DEBUG
            Debug.WriteWarn("Removing call {0}", id);
#endif
            _lock.Enter();
            _calls.Remove(id);
            _lock.Leave();
        }

        internal virtual void OnCallStatusChange(CiscoTelePresenceCodec codec, CallStatusEventType eventType, Call call)
        {
            try
            {
                Debug.WriteSuccess(eventType.ToString(), call.ToString());
                var handler = CallStatusChange;
                if (handler != null) handler(codec, eventType, call);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e, "Error Calls.OnCallStatusChange() Event");
            }
        }

        internal virtual void OnCallSuccess(Call call)
        {
            try
            {
                Debug.WriteSuccess("Call {0} Success!", call.Id);

                var handler = CallSuccess;
                if (handler != null) handler(call);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        internal virtual void OnCallDisconnected(Call call, DisconnectCauseType causetype, string causestring)
        {
            try
            {
                Debug.WriteWarn("Call {0} {1}{2}", call.Id, causetype,
                    string.IsNullOrEmpty(causestring) ? "" : " - " + causestring);
                var handler = CallDisconnected;
                if (handler != null) handler(_codec, new CallDisconnectEventArgs(call, causetype, causestring));
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        protected virtual void OnCallIncoming(Call call)
        {
            try
            {
                var handler = CallIncoming;
                if (handler != null) handler(call);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        private void CodecOnStatusReceived(CiscoTelePresenceCodec codec, IEnumerable<StatusUpdateItem> items)
        {
            var statusUpdateItems = items as StatusUpdateItem[] ?? items.ToArray();
            foreach (var statusUpdateItem in statusUpdateItems.Where(i => i.Path.StartsWith("Call[")))
            {
                var id = int.Parse(Regex.Match(statusUpdateItem.Path, @"Call\[([0-9]+)\]").Groups[1].Value);

                if (_calls.ContainsKey(id))
                {
                    if (statusUpdateItem.Attributes == "ghost=True")
                    {
                        _calls[id].Status = CallStatus.Idle;
                        OnCallStatusChange(_codec, CallStatusEventType.Ended, _calls[id]);
                        _calls[id].Dispose();
                        Remove(id);
                    }
                    break;
                }

                var call = new Call(codec, id,
                    statusUpdateItems.Where(i => i.Path.StartsWith("Call[" + id + "]")).ToArray());

                _lock.Enter();
                _calls.Add(id, call);
                _lock.Leave();
            }
        }

        private void CodecOnEventReceived(CiscoTelePresenceCodec codec, string name, Dictionary<string, string> properties)
        {
            switch (name)
            {
                case "CallDisconnect":
                    OnCallDisconnected(_calls[int.Parse(properties["CallId"])],
                        (DisconnectCauseType) Enum.Parse(typeof (DisconnectCauseType), properties["CauseType"], false),
                        properties["CauseString"]);
                    break;
                case "CallSuccessful":
                    OnCallSuccess(_calls[int.Parse(properties["CallId"])]);
                    break;
                case "OutgoingCallIndication":
                    var id = int.Parse(properties["CallId"]);
                    if (_calls.ContainsKey(id))
                        OnCallStatusChange(_codec, CallStatusEventType.NewCall, _calls[id]);
                    break;
                case "IncomingCallIndication":
                    var call = GetOrInsert(int.Parse(properties["CallId"]), CallStatus.Ringing, CallDirection.Incoming);
                    call.Status = CallStatus.Ringing;
                    call.Direction = CallDirection.Incoming;
                    call.DisplayName = properties["DisplayNameValue"];
                    call.RemoteNumber = properties["RemoteURI"];
                    OnCallIncoming(call);
                    break;
            }
        }

        public IEnumerator<Call> GetEnumerator()
        {
            return _calls.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    public delegate void CallStatusChangedEventHandler(CiscoTelePresenceCodec codec, CallStatusEventType eventType, Call call);

    public delegate void CallSuccessEventHandler(Call call);

    public class CallDisconnectEventArgs : EventArgs
    {
        internal CallDisconnectEventArgs(Call disconnectedCall, DisconnectCauseType causeType, string causeString)
        {
            DisconnectedCall = disconnectedCall;
            CauseType = causeType;
            CauseString = causeString;
        }

        public Call DisconnectedCall { get; set; }
        public DisconnectCauseType CauseType { get; set; }
        public string CauseString { get; set; }

        public bool OtherCallsAreStillActive
        {
            get { return DisconnectedCall.Codec.Calls.Count(c => c.Id != DisconnectedCall.Id) > 0; }
        }
    }

    public delegate void CallDisconnectEventHandler(CiscoTelePresenceCodec codec, CallDisconnectEventArgs args);

    public delegate void CallIncomingEventHandler(Call call);

    public delegate void DialResult(int errorCode, string description, int callId);

    public enum DisconnectCauseType
    {
        OtherLocal,
        LocalDisconnect,
        UnknownRemoteSite,
        LocalBusy,
        LocalReject,
        InsufficientSecurity,
        OtherRemote,
        RemoteDisconnect,
        RemoteBusy,
        RemoteRejected,
        RemoteNoAnswer,
        CallForwarded,
        NetworkRejected
    }

    public enum CallStatusEventType
    {
        NewCall,
        StatusUpdated,
        Ended
    }

    struct DialCallBackInfo
    {
        public DialResult CallBack;
        public string NumberDialed;
    }
}