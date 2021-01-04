using System;
using System.Linq;
using Crestron.SimplSharp;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.DeviceSupport;
using UX.Lib2.Models;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Polycom
{
    public class PolycomGroupSeriesCodec : ISourceDevice, IFusionAsset
    {
        private readonly SystemBase _system;
        private readonly string _ipAddress;
        private readonly string _password;
        private ContentPlayingMode _contentPlaying;
        private readonly ComPortHandler _port;
        private readonly PolycomSocketHandler _socket;
        private bool _isSleeping;
        private CTimer _sleepDelayTimer;
        private bool _deviceCommunicating;
        private static bool _first;
        private CTimer _timeoutTimer;
        private bool _supressSleep;
        private bool _selfView;
        private bool _videoMute;

        public PolycomGroupSeriesCodec(SystemBase system, IComPortDevice comPort)
        {
            _system = system;
            _port = new ComPortHandler(comPort);
            _port.DataReceived += OnReceive;
            Calls = new Calls(this);
            Calls.CallChanged += Calls_CallChanged;
            Microphones = new Microphones(this);
            Cameras = new Cameras(this);
            AddressBook = new AddressBook(this);
            Meetings = new CalendarMeetings(this);
        }

        public PolycomGroupSeriesCodec(SystemBase system, string ipAddress, string password)
        {
            _system = system;
            _ipAddress = ipAddress;
            _password = password;
            _socket = new PolycomSocketHandler(ipAddress, 24);
            _socket.StatusChanged += SocketOnStatusChanged;
            _socket.ReceivedData += OnReceive;
            Calls = new Calls(this);
            Calls.CallChanged += Calls_CallChanged;
            Microphones = new Microphones(this);
            Cameras = new Cameras(this);
            AddressBook = new AddressBook(this);
            Meetings = new CalendarMeetings(this);
            if (_first) return;
            _first = true;
            CrestronConsole.AddNewConsoleCommand(Send, "CodecSend", "Send a test command to codec",
                ConsoleAccessLevelEnum.AccessOperator);
        }

        public Calls Calls { get; protected set; }
        public Microphones Microphones { get; protected set; }
        public Cameras Cameras { get; protected set; }
        public AddressBook AddressBook { get; protected set; }
        public CalendarMeetings Meetings { get; protected set; }

        public event CodecBoolValueChangeEventHandler VideoMuteChange;

        public bool SupressSleep
        {
            get { return _supressSleep; }
            set { _supressSleep = value; }
        }

        public void Initialize()
        {
            if (_socket != null)
            {
                _socket.Connect();
            }
            else
            {
                _port.Initialize();
                _port.Send("\r\n\r\n");
            }
        }

        public void Send(string str)
        {
            if (_socket != null)
            {
                _socket.Send(str);
            }
            else
            {
                _port.Send(str);
            }
        }

        public void Dial(string dialString)
        {
            Send(string.Format("dial auto \"{0}\"", dialString));
        }

        public void Answer()
        {
            Send("answer video");
            Microphones.Unmute();
        }

        public void HangupAll()
        {
            Send("hangup all");
        }

        public void SendDTMF(char dtmfChar)
        {
            Send(string.Format("gendial {0}", dtmfChar));
        }

        public void ContentStart()
        {
            Send("vcbutton play");
        }

        public void ContentStart(uint input)
        {
            Send(string.Format("vcbutton play {0}", input));
        }

        public void ContentStop()
        {
            Send("vcbutton stop");
        }

        public event CodecContentPlayChangeEventHandler ContentPlayingChange;

        public ContentPlayingMode ContentPlaying
        {
            get { return _contentPlaying; }
            set
            {
                if (_contentPlaying != value)
                {
                    _contentPlaying = value;
                    if (ContentPlayingChange != null)
                    {
                        ContentPlayingChange(this);
                    }
                }
            }
        }

        public event CodecStandbyChangeChangeEventHandler StandbyChange;
        public event CodecHasReceivedFeedback ReceivedFeedback;

        protected virtual void OnReceivedFeedback(PolycomGroupSeriesCodec codec, string receiveddata)
        {
            var handler = ReceivedFeedback;

            try
            {
                if (handler != null) handler(codec, receiveddata);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        public void Wake()
        {
            CloudLog.Debug("{0}.Wake()", GetType().Name);
            Send("wake");
        }

        public void Sleep()
        {
            CloudLog.Debug("{0}.Sleep()", GetType().Name);
            ContentStop();
            Send("sleep");
        }

        public bool IsSleeping {
            get { return _isSleeping; }
            protected set
            {
                if (_isSleeping == value) return;
                _isSleeping = value;
                CloudLog.Debug("{0}.IsSleeping = {1}", GetType().Name, _isSleeping);
                if (StandbyChange != null)
                {
                    StandbyChange(this);
                }
            }
        }

        private void Calls_CallChanged(PolycomGroupSeriesCodec codec, CallChangeEventArgs args)
        {
#if DEBUG
            Debug.WriteSuccess("Call Change", "{0} {1} - {2} ({3}) - {4} ({5})",
                args.Call.CallType, args.Call.Id, args.Call.DisplayName, args.Call.Number, args.Call.State.ToString(),
                args.Call.Status.ToString());
#endif
            if (args.Call.Status == CallStatus.Connected)
            {
                Send("mute near get");                
            }
        }

        protected virtual void OnReceive(string receivedString)
        {
            DeviceCommunicating = true;

            Debug.WriteInfo("VC RX", receivedString);

            if (_port != null)
            {
                if (_timeoutTimer == null || _timeoutTimer.Disposed)
                {
                    _timeoutTimer = new CTimer(specific =>
                    {
                        DeviceCommunicating = false;
                    }, 60000);
                }
                else
                {
                    _timeoutTimer.Reset(60000);
                }
            }
#if DEBUG
            Debug.WriteNormal(Debug.AnsiPurple + "Codec Rx", Debug.AnsiReset + receivedString);
#endif
            if (receivedString.Contains("Password:"))
            {
                Send(_password);
            }

            if (receivedString == "vcbutton play")
            {
                ContentPlaying = ContentPlayingMode.PlayingNear;
                return;
            }

            if (receivedString == "vcbutton stop")
            {
                ContentPlaying = ContentPlayingMode.Stopped;
                return;
            }

            if (receivedString == "systemsetting selfview On")
            {
                _selfView = true;
                return;
            }

            if (receivedString == "systemsetting selfview Off")
            {
                _selfView = false;
                return;
            }

            if (receivedString == "videomute near on")
            {
                _videoMute = true;
                if (VideoMuteChange != null) VideoMuteChange(true);
                return;
            }

            if (receivedString == "videomute near off")
            {
                _videoMute = false;
                if (VideoMuteChange != null) VideoMuteChange(false);
                return;
            }

            if (receivedString.StartsWith("Control event: "))
            {
                if (receivedString.Contains("vcbutton play")) ContentPlaying = ContentPlayingMode.PlayingNear;
                else if (receivedString.Contains("vcbutton stop")) ContentPlaying = ContentPlayingMode.Stopped;
                else if (receivedString.Contains("vcbutton farplay")) ContentPlaying = ContentPlayingMode.PlayingFar;
                else if (receivedString.Contains("vcbutton farstop")) ContentPlaying = ContentPlayingMode.Stopped;
                return;
            }

            switch (receivedString)
            {
                case "wake":
                    IsSleeping = false;
                    Send("preset near go 0");
                    return;
                case "popupinfo: question: The system is going to sleep.":                    
                case "sleep":
                    IsSleeping = true;
                    return;
                case "popupinfo: question: The call has ended.":
                    if(Calls.InProgressCalls.Any())
                        Send("callinfo all");
                    return;
            }

            if (receivedString.Contains("Here is what I know about myself:"))
            {
                RegisterFeedback();
            }

            OnReceivedFeedback(this, receivedString);
        }

        public bool SelfView
        {
            get { return _selfView; }
            set
            {
                _selfView = value;
                Send("systemsetting selfview " + (value ? "on" : "off"));
            }
        }

        public bool VideoMute
        {
            get { return _videoMute; }
            set
            {
                _videoMute = value;
                Send("videomute near " + (value ? "on" : "off"));
            }
        }

        private void SocketOnStatusChanged(TCPClientSocketBase socket, SocketStatusEventType eventType)
        {
            switch (eventType)
            {
                case SocketStatusEventType.Connected:
                    DeviceCommunicating = true;
                    break;
                case SocketStatusEventType.Disconnected:
                    DeviceCommunicating = false;
                    break;
            }
        }

        public void RegisterFeedback()
        {
            Send("all register");
            Send("notify callstatus");
            Send("notify calendarmeetings");
            Send("callinfo all");
            Send("mute near get");
            Send("vcbutton get");
            Send("systemsetting get selfview");
            Send("videomute get");
            Meetings.StartTimer(_system);
        }

        public string Name { get { return "VC Codec"; } }

        public string ManufacturerName
        {
            get { return "Polycom"; }
        }

        public string ModelName
        {
            get { return Name; }
        }

        public string DiagnosticsName
        {
            get { return "Polycom Codec (" + DeviceAddressString + ")"; }
        }

        public bool DeviceCommunicating
        {
            get { return _deviceCommunicating; }
            private set
            {
                if (_deviceCommunicating == value) return;
                _deviceCommunicating = value;

                if (_port != null && _deviceCommunicating)
                {
                    RegisterFeedback();
                }

                OnDeviceCommunicatingChange(this, value);
            }
        }

        public string DeviceAddressString
        {
            get { return _ipAddress; }
        }

        public string SerialNumber { get; private set; }
        public string VersionInfo { get; private set; }

        public event DeviceCommunicatingChangeHandler DeviceCommunicatingChange;

        protected virtual void OnDeviceCommunicatingChange(IDevice device, bool communicating)
        {
            var handler = DeviceCommunicatingChange;
            if (handler == null) return;
            try
            {
                handler(device, communicating);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        public void UpdateOnSourceRequest()
        {
            
        }

        public void StartPlaying()
        {
            if (_sleepDelayTimer != null && !_sleepDelayTimer.Disposed)
            {
                _sleepDelayTimer.Dispose();
            }
            CloudLog.Debug("Codec is now being used");
            Wake();
        }

        public void StopPlaying()
        {
            CloudLog.Debug("Codec is no longer in use");
            if (Calls.ActiveCallsCount > 0)
                HangupAll();
            if(_supressSleep) return;
            _sleepDelayTimer = new CTimer(specific =>
            {
                _sleepDelayTimer.Dispose();
                Sleep();
            }, 5000);
        }

        public FusionAssetType AssetType
        {
            get
            {
                return FusionAssetType.VideoConferenceCodec;
            }
        }
    }

    public delegate void CodecStandbyChangeChangeEventHandler(PolycomGroupSeriesCodec codec);

    public delegate void CodecContentPlayChangeEventHandler(PolycomGroupSeriesCodec codec);

    public delegate void CodecHasReceivedFeedback(PolycomGroupSeriesCodec codec, string receivedData);

    public delegate void CodecBoolValueChangeEventHandler(bool value);

    public enum ContentPlayingMode
    {
        Stopped,
        PlayingNear,
        PlayingFar
    }
}