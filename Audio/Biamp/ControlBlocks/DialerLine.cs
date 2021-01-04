 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.Devices.Audio.Biamp.Helpers;

namespace UX.Lib2.Devices.Audio.Biamp.ControlBlocks
{
    public class DialerLine : TesiraChannelBase
    {
        private readonly DialerBlock _dialerBlock;
        private string _lineLabel;
        private VoipCallStatusStateMap _state;
        private VoipCallStatusPromptMap _prompt;
        private string _cid;
        private DateTime _timeConnected;
        private CTimer _timerTimer;

        internal DialerLine(DialerBlock dialerBlock, uint channelNumber)
            : base(dialerBlock, channelNumber)
        {
            _dialerBlock = dialerBlock;
            dialerBlock.Device.Send(dialerBlock.InstanceTag, TesiraCommand.Get, TesiraAttributeCode.LineLabel,
                new[] { channelNumber });
        }

        public string LineLabel
        {
            get { return _lineLabel; }
        }

        public uint LineNumber
        {
            get { return ChannelNumber; }
        }

        public VoipCallStatusStateMap State
        {
            get { return _state; }
            set
            {
                if(_state == value) return;

                _state = value;

                CloudLog.Debug("Line {0} State now {1} \"{2}\"", LineNumber, _state.Number, _state.Name);

                if (_state == VoipCallStatusStateMap.IDLE)
                {
                    _timeConnected = DateTime.Now;
                }

                try
                {
                    if (CallStatusChange != null)
                    {
                        CallStatusChange(this, new DialerLineStatusChangeEventArgs
                        {
                            Cid = _cid,
                            CallTimer = CallTimer,
                            EventType = DialerLineStatusEventType.StateChanged,
                            Prompt = Prompt,
                            State = State
                        });
                    }
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e, "Error calling event handler");
                }
            }
        }

        public VoipCallStatusPromptMap Prompt
        {
            get { return _prompt; }
            set
            {
                if (_prompt == value) return;

                var lastPrompt = _prompt;
                _prompt = value;

                CloudLog.Debug("Line {0} Prompt now {1} \"{2}\"", LineNumber, _prompt.Number, _prompt.Name);

                if (_prompt == VoipCallStatusPromptMap.CONNECTED)
                {
                    var now = DateTime.Now;
                    _timeConnected = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
                    if (_timerTimer == null || _timerTimer.Disposed)
                    {
                        _timerTimer = new CTimer(specific => OnCallTimerChange(), null, 1000, 1000);
                    }
                    else
                    {
                        _timerTimer.Reset(1000, 1000);
                    }
                }
                else if(lastPrompt == VoipCallStatusPromptMap.CONNECTED && _timerTimer != null && !_timerTimer.Disposed)
                {
                    _timerTimer.Stop();
                }

                try
                {
                    if (CallStatusChange != null)
                    {
                        CallStatusChange(this, new DialerLineStatusChangeEventArgs
                        {
                            Cid = _cid,
                            CallTimer = CallTimer,
                            EventType = DialerLineStatusEventType.PromptChanged,
                            Prompt = Prompt,
                            State = State
                        });
                    }
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e, "Error calling event handler");
                }
            }
        }

        public TimeSpan CallTimer
        {
            get
            {
                var now = DateTime.Now;
                now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
                return now - _timeConnected;
            }
        }

        public DialerBlock DialerBlock
        {
            get { return _dialerBlock; }
        }

        private void OnCallTimerChange()
        {
            try
            {
                if (CallStatusChange != null)
                {
                    CallStatusChange(this, new DialerLineStatusChangeEventArgs
                    {
                        Cid = _cid,
                        CallTimer = CallTimer,
                        EventType = DialerLineStatusEventType.CallTimerChanged,
                        Prompt = Prompt,
                        State = State
                    });
                }
            }
            catch (Exception e)
            {
                CloudLog.Exception(e, "Error calling event handler");
            }
        }

        public string Cid
        {
            get { return _cid; }
        }

        public event DialerLineStatusChangeEventHandler CallStatusChange;

        public void Dial(string dialString)
        {
            ControlBlock.Device.Send(ControlBlock.InstanceTag, TesiraCommand.Dial,
                new uint[] {LineNumber, 1}, dialString);
        }

        public void OnHook()
        {
            ControlBlock.Device.Send(ControlBlock.InstanceTag, TesiraCommand.OnHook,
                new uint[] {LineNumber, 1});
        }

        public void End()
        {
            ControlBlock.Device.Send(ControlBlock.InstanceTag, TesiraCommand.End,
                new uint[] {LineNumber, 1});
        }

        public void OffHook()
        {
            ControlBlock.Device.Send(ControlBlock.InstanceTag, TesiraCommand.OffHook,
                new uint[] {LineNumber, 1});
        }

        public void Answer()
        {
            ControlBlock.Device.Send(ControlBlock.InstanceTag, TesiraCommand.Answer,
                new uint[] {LineNumber, 1});
        }

        public void Dtmf(string dialChar)
        {
            ControlBlock.Device.Send(ControlBlock.InstanceTag, TesiraCommand.Dial,
                new uint[] {LineNumber, 1}, dialChar);
        }

        internal override void UpdateFromResponse(TesiraResponse response)
        {
#if DEBUG
            Debug.WriteSuccess(ControlBlock.InstanceTag + " Line " + ChannelNumber,
                "Received {0} response for {1}: {2}", response.CommandType, response.AttributeCode,
                response.TryParseResponse().ToString());
#endif
            switch (response.AttributeCode)
            {
                case TesiraAttributeCode.LineLabel:
                    _lineLabel = response.TryParseResponse()["value"].Value<string>();
                    break;
            }
        }

        internal override void UpdateValue(TesiraAttributeCode attributeCode, JToken value)
        {
            
        }

        internal void UpdateCallStates(IEnumerable<CallState> callStates)
        {
            var sData = callStates.FirstOrDefault(cs => cs.CallId == 0);
            if (sData != null)
            {
                Debug.WriteInfo("Line State for Line " + LineNumber,
                    "Action = {0}, CID = {1}, State = {2}, Prompt = {3}",
                    sData.Action, sData.Cid, sData.State, sData.Prompt);

                var match = Regex.Match(sData.Cid, @"^\""(\d*)\""\""(\d*)\""\""([^\""]*)\""");
                _cid = "Unknown";
                if (match.Success)
                {
                    var name = match.Groups[3].Value;
                    var ext = match.Groups[2].Value;

                    if (ext.Length > 0 && name.Length > 0)
                    {
                        _cid = name + " (" + ext + ")";
                    }
                    else if (ext.Length > 0)
                    {
                        _cid = ext;
                    }
                    else if (name.Length > 0)
                    {
                        _cid = name;
                    }
                }
                else
                {
                    _cid = sData.Cid;
                }

                var regex = new Regex(@"^\w+_(\d+)");
                var iState = (ushort) (ushort.Parse(regex.Match(sData.State).Groups[1].Value) - 1);
                State = VoipCallStatusStateMap.Find(iState);
                var iPrompt = (ushort)(ushort.Parse(regex.Match(sData.Prompt).Groups[1].Value) - 1);
                Prompt = VoipCallStatusPromptMap.Find(iPrompt);
            }
        }
    }

    public delegate void DialerLineStatusChangeEventHandler(DialerLine line, DialerLineStatusChangeEventArgs args);

    public class DialerLineStatusChangeEventArgs : EventArgs
    {
        internal DialerLineStatusChangeEventArgs()
        {
            
        }

        public uint CallAppearence
        {
            get { return 1; }
        }

        public DialerLineStatusEventType EventType { get; internal set; }
        public string Cid { get; internal set; }
        public TimeSpan CallTimer { get; internal set; }
        public VoipCallStatusStateMap State { get; internal set; }
        public VoipCallStatusPromptMap Prompt { get; internal set; }
    }

    public enum DialerLineStatusEventType
    {
        StateChanged,
        PromptChanged,
        CallTimerChanged
    }
}