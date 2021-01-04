 
using System;
using System.Collections.Generic;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Audio.Polycom
{
    public class VoipLine
    {
        public VoipLine(VoipOutChannel channel, uint number)
        {
            Number = number;
            VoipOutChannel = channel;
            _callInfoLine = new Dictionary<uint, string> {{1, string.Empty}, {2, string.Empty}};
            VoipOutChannel.Device.VoipInfoReceived += VoipInfoReceived;
        }

        public VoipOutChannel VoipOutChannel { get; protected set; }

        void VoipInfoReceived(ISoundstructureItem item, SoundstructureVoipInfoReceivedEventArgs args)
        {
            if (item != VoipOutChannel) return;
            var elements = SoundstructureSocket.ElementsFromString(args.Info);
            if (elements.Count <= 1) return;
            var lineNumber = uint.Parse(elements[0]);
            if (lineNumber != Number) return;
            try
            {
                switch (args.Command)
                {
                    case "voip_line_state":
                        try
                        {
                            State = (VoipLineState)Enum.Parse(typeof(VoipLineState), elements[1], true);
                            if (StateChanged != null)
                                StateChanged(this, State);
                        }
                        catch (Exception e)
                        {
                            ErrorLog.Error("Could not parse VoipLineState \"{2}\" for Line {0}, {1}",
                                lineNumber, e.Message, elements[1]);
                        }
                        break;
                    case "voip_line_label":
                        Label = elements[1];
                        break;
                    case "voip_call_appearance_line":
                        CallAppearance = uint.Parse(elements[1]);
                        break;
                    case "voip_call_appearance_state":
                        try
                        {
                            var state = (VoipCallAppearanceState)Enum.Parse(typeof(VoipCallAppearanceState), elements[1], true);
                            if (CallAppearanceState != state)
                            {
                                CallAppearanceState = state;
                                if (CallAppearanceState == VoipCallAppearanceState.Connected)
                                    _callConnectedTime = DateTime.Now;
                            }
                            try
                            {
                                if (CallAppearanceStateChanged != null)
                                    CallAppearanceStateChanged(this, new VoipLineCallAppearanceStateEventArgs(CallAppearance, CallAppearanceState));
                            }
                            catch (Exception e)
                            {
                                ErrorLog.Exception(string.Format("Error calling event {0}.CallAppearanceStateChanged", GetType().Name), e);
                            }
                        }
                        catch(Exception e)
                        {
                            ErrorLog.Error("Could not parse VoipCallAppearanceState \"{0}\" for Line {1}, {2}", elements[1], lineNumber, e.Message);
                        }
                        break;
                    case "voip_call_appearance_info":
                        // Not sure why this was > 3 instead of >= 3 !?!
                        if (elements.Count >= 3)
                        {
                            var lineIndex = uint.Parse(elements[1]);
                            _callInfoLine[lineIndex] = elements[2];
                            if (CallInfoLineChanged != null)
                                CallInfoLineChanged(this, new VoipLineCallInfoLineEventArgs(lineIndex, elements[2]));
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error parsing Voip feedback info in VoipLine[{0}], {1}", Number, e.Message);
                ErrorLog.Error("VoipInfoReceived() args.Command = \"{0}\" args.Info = \"{1}\"", args.Command, args.Info);
            }
        }

        public uint Number { get; protected set; }

        public VoipLineState State { get; protected set; }

        public event VoipLineStateEventHandler StateChanged;

        public uint CallAppearance { get; protected set; }

        public string Label { get; protected set; }

        private DateTime _callConnectedTime;
        public TimeSpan CallTimer
        {
            get { return DateTime.Now - _callConnectedTime; }
        }

        public VoipCallAppearanceState CallAppearanceState { get; protected set; }

        public event VoipLineCallAppearanceStateEventHandler CallAppearanceStateChanged;

        public bool Registered
        {
            get
            {
                return State != VoipLineState.Line_Not_Registered;
            }
        }

        private readonly Dictionary<uint, string> _callInfoLine;
        public ReadOnlyDictionary<uint, string> CallInfoLine
        {
            get
            {
                return new ReadOnlyDictionary<uint, string>(_callInfoLine);
            }
        }

        public event VoipLineCallInfoLineEventHandler CallInfoLineChanged;
    }

    public delegate void VoipLineStateEventHandler(VoipLine line, VoipLineState state);

    public delegate void VoipLineCallAppearanceStateEventHandler(VoipLine line, VoipLineCallAppearanceStateEventArgs args);

    public class VoipLineCallAppearanceStateEventArgs : EventArgs
    {
        public VoipLineCallAppearanceStateEventArgs(uint callAppearance, VoipCallAppearanceState state)
        {
            CallAppearance = callAppearance;
            State = state;
        }

        public uint CallAppearance;
        public VoipCallAppearanceState State;
    }

    public delegate void VoipLineCallInfoLineEventHandler(VoipLine line, VoipLineCallInfoLineEventArgs args);

    public class VoipLineCallInfoLineEventArgs
    {
        public VoipLineCallInfoLineEventArgs(uint lineIndex, string value)
        {
            Index = lineIndex;
            Value = value;
        }

        public uint Index;
        public string Value;

    }

    public enum VoipCallAppearanceState
    {
        Free,
        DialTone,
        Setup,
        Proceeding,
        Offering,
        Ringback,
        Ncas_Call_Hold,
        Disconnected,
        Connected
    }

    public enum VoipLineState
    {
        None,
        Line_Not_Registered,
        Line_Registered,
        Proceed,
        Offering,
        Call_Active,
        Conference,
        Call_On_Hold,
        Secure_RTP
    }
}