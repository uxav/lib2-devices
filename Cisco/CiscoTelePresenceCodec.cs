 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronXmlLinq;
using Crestron.SimplSharpPro.CrestronThread;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.Devices.Cisco.Network;
using UX.Lib2.DeviceSupport;

namespace UX.Lib2.Devices.Cisco
{
    public class CiscoTelePresenceCodec : ISourceDevice, IFusionAsset, IInitializeComplete
    {
        #region Fields

        private readonly string _deviceAddressString;
        private readonly CiscoSshClient _sshClient;
        private readonly HttpsClient _httpsClient;
        private readonly Calls _calls;
        private readonly SystemUnit.SystemUnit _systemUnit;
        private readonly Cameras.Cameras _cameras;
        private readonly Standby _standby;
        private readonly Bookings.Bookings _bookings;
        private readonly CallHistory.CallHistory _callHistory;
        private readonly RoomAnalytics.RoomAnalytics _roomAnalytics;
        private readonly SIP.SIP _sip;
        private readonly Video.Video _video;
        private readonly Phonebook.Phonebook _phonebook;
        private readonly Diagnostics.Diagnostics _diagnostics;
        private readonly Conference.Conference _conference;
        private readonly Capabilities.Capabilities _capabilities;
        private readonly Audio.Audio _audio;
        private readonly UserInterface.UserInterface _userInterface;
        private readonly Dictionary<int, Network.Network> _network = new Dictionary<int, Network.Network>();
        private readonly NetworkServices _networkServices;
        private readonly CCriticalSection _commandCallbacksLock = new CCriticalSection();

        private readonly Dictionary<int, CodecCommandResponse> _commandCallbacks =
            new Dictionary<int, CodecCommandResponse>();

        private bool _disconnectCallsIfNotBeingUsed = true;
        private bool _initialized;

        #endregion

        #region Constructors

        public CiscoTelePresenceCodec(string address, string username, string password)
        {
            DisableAutoSleepOnStopPlaying = false;
            _deviceAddressString = address;
            _calls = new Calls(this);
            _systemUnit = new SystemUnit.SystemUnit(this);
            _cameras = new Cameras.Cameras(this);
            _standby = new Standby(this);
            _phonebook = new Phonebook.Phonebook(this);
            _bookings = new Bookings.Bookings(this);
            _audio = new Audio.Audio(this);
            _sip = new SIP.SIP(this);
            _roomAnalytics = new RoomAnalytics.RoomAnalytics(this);
            _video = new Video.Video(this);
            _callHistory = new CallHistory.CallHistory(this);
            _network[1] = new Network.Network(this, 1);
            _networkServices = new NetworkServices(this);
            _userInterface = new UserInterface.UserInterface(this);
            _diagnostics = new Diagnostics.Diagnostics(this);
            _conference = new Conference.Conference(this);
            _capabilities = new Capabilities.Capabilities(this);
            _sshClient = new CiscoSshClient(_deviceAddressString, username, password);
            _sshClient.ReceivedData += SshClientOnReceivedData;
            _sshClient.ConnectionStatusChange += SshClientOnConnectionStatusChange;
            _httpsClient = new HttpsClient(address, username, password);

#if DEBUG
            CrestronConsole.AddNewConsoleCommand(Send,
                "codecsend", "Send a codec xCommand",
                ConsoleAccessLevelEnum.AccessOperator);
            CrestronConsole.AddNewConsoleCommand(parameters =>
                Send(string.Format("xCommand {0}", parameters)),
                "xCommand", "Send a codec xCommand",
                ConsoleAccessLevelEnum.AccessOperator);
            CrestronConsole.AddNewConsoleCommand(parameters =>
                Calls.DialNumber(parameters, (code, description, call) =>
                {

                }),
                "Dial", "Dial a number",
                ConsoleAccessLevelEnum.AccessOperator);
            CrestronConsole.AddNewConsoleCommand(parameters => Send("xStatus"),
                "CodecStatus", "Get the full status of the codec",
                ConsoleAccessLevelEnum.AccessOperator);
#endif

        }

        #endregion

        #region Finalizers

        #endregion

        #region Events

        public event DeviceCommunicatingChangeHandler DeviceCommunicatingChange;
        internal event StatusReceivedHandler StatusReceived;
        internal event EventNotificationReceivedHandler EventReceived;
        public event ConfigurationChangeEventHandler ConfigurationChanged;

        #endregion

        #region Delegates

        #endregion

        #region Properties

        public string DeviceAddressString
        {
            get { return _deviceAddressString; }
        }

        public Audio.Audio Audio
        {
            get { return _audio; }
        }

        public Calls Calls
        {
            get { return _calls; }
        }

        public Cameras.Cameras Cameras
        {
            get { return _cameras; }
        }

        public bool CameraConnectorIdsAreBroken { get; set; }

        public Standby Standby
        {
            get { return _standby; }
        }

        public Bookings.Bookings Bookings
        {
            get { return _bookings; }
        }

        public SystemUnit.SystemUnit SystemUnit
        {
            get { return _systemUnit; }
        }

        public Diagnostics.Diagnostics Diagnostics
        {
            get { return _diagnostics; }
        }

        public CallHistory.CallHistory CallHistory
        {
            get { return _callHistory; }
        }

        public ReadOnlyDictionary<int, Network.Network> Network
        {
            get { return new ReadOnlyDictionary<int, Network.Network>(_network); }
        }

        public NetworkServices NetworkServices
        {
            get { return _networkServices; }
        }

        public RoomAnalytics.RoomAnalytics RoomAnalytics
        {
            get { return _roomAnalytics; }
        }

        public Video.Video Video
        {
            get { return _video; }
        }

        public SIP.SIP SIP
        {
            get { return _sip; }
        }

        public Phonebook.Phonebook Phonebook
        {
            get { return _phonebook; }
        }

        public UserInterface.UserInterface UserInterface
        {
            get { return _userInterface; }
        }

        public Capabilities.Capabilities Capabilities
        {
            get { return _capabilities; }
        }

        public Conference.Conference Conference
        {
            get { return _conference; }
        }

        public string Name
        {
            get { return string.IsNullOrEmpty(_systemUnit.ProductId) ? "Cisco Codec" : _systemUnit.ProductId; }
        }

        public string DiagnosticsName
        {
            get { return Name + " (" + DeviceAddressString + ")"; }
        }

        public string ManufacturerName
        {
            get { return "Cisco"; }
        }

        public string ModelName
        {
            get { return _systemUnit.ProductId; }
        }

        public bool DeviceCommunicating
        {
            get { return _sshClient.Connected; }
        }

        public string SerialNumber
        {
            get
            {
                try
                {
                    return _systemUnit.Hardware.Module.SerialNumber;
                }
                catch
                {
                    CloudLog.Warn("Could not return serial number for {0}", GetType().Name);
                }
                return string.Empty;
            }
        }

        public string VersionInfo
        {
            get
            {
                try
                {
                    return _systemUnit.Software.Version;
                }
                catch
                {
                    CloudLog.Warn("Could not return version info for {0}", GetType().Name);
                }
                return string.Empty;
            }
        }

        public bool InCall
        {
            get
            {
                return SystemUnit.State.NumberOfActiveCalls > 0 || SystemUnit.State.NumberOfInProgressCalls > 0 ||
                       SystemUnit.State.NumberOfSuspendedCalls > 0;
            }
        }

        public bool DisconnectCallsIfNotBeingUsed
        {
            get { return _disconnectCallsIfNotBeingUsed; }
            set { _disconnectCallsIfNotBeingUsed = value; }
        }

        public bool HttpsClientIsBusy
        {
            get { return _httpsClient.Busy; }
        }

        public FusionAssetType AssetType
        {
            get
            {
                return FusionAssetType.VideoConferenceCodec;
            }
        }

        public bool DisableAutoSleepOnStopPlaying { get; set; }

        public bool HasOpenSession
        {
            get { return _httpsClient.InSession; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Call to start a session with the HTTPS Client to the codec.
        /// This will close down an existing session prior to calling the begin session call.
        /// </summary>
        public bool StartSession()
        {
            return _httpsClient.StartSession();
        }

        public void Send(string stringToSend)
        {
            _sshClient.Send(stringToSend);
        }

        public void Send(string stringToSend, params object[] args)
        {
            _sshClient.Send(string.Format(stringToSend, args));
        }

        internal CodecResponse SendCommand(CodecCommand command)
        {
            var xml = command.XmlString;
#if DEBUG
            Debug.WriteInfo("Sending command", "{0}", command.Command);
#endif
            return _httpsClient.PutXml(xml);
        }

        internal int SendCommandAsync(CodecCommand command, CodecCommandResponse callback)
        {
            var xml = command.XmlString;
#if DEBUG
            CrestronConsole.PrintLine("Command: ({0} bytes)\r\n{1}", xml.Length, xml);
#endif
            var id = _httpsClient.PutXmlAsync(xml, response =>
            {
#if DEBUG
                CrestronConsole.PrintLine("Command response received with id:{0}", response.Request.Id);
#endif
                var call = _commandCallbacks[response.Request.Id];

                _commandCallbacksLock.Enter();
                _commandCallbacks.Remove(response.Request.Id);
                _commandCallbacksLock.Leave();
                if (response.Code == 200 && response.Xml != null)
                {
                    try
                    {
                        var result = response.Xml.Element("Command").Elements().First();
#if DEBUG
                        CrestronConsole.PrintLine("Request {0} {1} = {2}", response.Request.Id, result.XName.LocalName,
                            result.Attribute("status").Value);
#endif
                        call(response.Request.Id, result.Attribute("status").Value == "OK", result);
                        return;
                    }
                    catch (Exception e)
                    {
                        CloudLog.Exception(e);
                    }
                }
                else if (response.Code == 401)
                {
                    CloudLog.Warn(
                        "Received 401 code in {0}.SendCommandAsync(CodecCommand command, CodecCommandResponse callback)",
                        GetType().Name);
                }
                call(response.Request.Id, false, null);
            });
            _commandCallbacksLock.Enter();
            _commandCallbacks[id] = callback;
            _commandCallbacksLock.Leave();
#if DEBUG
            CrestronConsole.PrintLine("Sending command \"{0}\" with id:{1}", command.Command, id);
#endif
            return id;
        }

        private void SshClientOnConnectionStatusChange(CiscoSshClient client, SshClientStatus status)
        {
            switch (status)
            {
                case SshClientStatus.Connected:
                    Debug.WriteSuccess("CODEC CONNECTED");
                    OnDeviceCommunicatingChange(this, true);
                    break;
                case SshClientStatus.Disconnected:
                    OnDeviceCommunicatingChange(this, false);
                    _initialized = false;
                    break;
            }
        }

        private void SshClientOnReceivedData(CiscoSshClient client, CodecSshClientReceivedDataArgs args)
        {
            var matches = Regex.Matches(args.DataAsReceived,
                @"\*\w ([^\r\n\""\:]+) (\w+)?(?:\(([\w\=]+)\))?:? ?\""?([^\r\n\""]+)?\""?");

            if (matches.Count <= 0) return;

            switch (args.DataType)
            {
                case ReceivedDataType.Configuration:
                    foreach (Match configMatch in matches)
                    {
                        try
                        {
                            OnConfigurationChanged(this, new ConfigurationChangeEventArgs
                            {
                                Path = configMatch.Groups[1].Value,
                                PropertyName = configMatch.Groups[2].Value,
                                Value = configMatch.Groups[4].Value
                            });
                        }
                        catch (Exception e)
                        {
                            CloudLog.Exception(e);
                        }
                    }
                    break;
                case ReceivedDataType.Status:
                    OnStatusReceived((from Match statusMatch in matches
                        select new StatusUpdateItem(statusMatch))
                        .ToArray());
                    if(_initialized) return;
                    _initialized = true;
                    CloudLog.Notice("{0} Initialized OK", this);
                    break;
                case ReceivedDataType.Event:
                    var items = new Dictionary<string, Dictionary<string, string>>();
                    foreach (Match eventMatch in matches)
                    {
                        if (!items.ContainsKey(eventMatch.Groups[1].Value))
                        {
                            items.Add(eventMatch.Groups[1].Value, new Dictionary<string, string>());
                        }

                        if (eventMatch.Groups[4].Success)
                        {
                            items[eventMatch.Groups[1].Value][eventMatch.Groups[2].Value] = eventMatch.Groups[4].Value;
                        }
                        else
                        {
                            items[eventMatch.Groups[1].Value][eventMatch.Groups[2].Value] = string.Empty;
                        }
                    }

                    foreach (var item in items)
                    {
                        OnEventReceived(item.Key, item.Value);
                    }
                    break;
                case ReceivedDataType.Response:
                    foreach (Match response in matches)
                    {
#if DEBUG
                        Debug.WriteInfo("Response", "{0} {1} {2}",
                            response.Groups[1].Value,
                            response.Groups[2].Value,
                            response.Groups[3].Value);
#endif
                    }
                    break;
            }
        }

        protected virtual void OnConfigurationChanged(CiscoTelePresenceCodec codec, ConfigurationChangeEventArgs args)
        {
            var handler = ConfigurationChanged;
            if (handler != null)
            {
                try
                {
                    handler(codec, args);
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }
        }

        protected virtual void OnStatusReceived(StatusUpdateItem[] items)
        {
            try
            {
#if DEBUG
                Debug.WriteSuccess("Status Received");
                foreach (var statusUpdateItem in items)
                {
                    Debug.WriteNormal(Debug.AnsiPurple + statusUpdateItem + Debug.AnsiReset);
                }
#endif
                var handler = StatusReceived;
                if (handler != null) handler(this, items);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        protected virtual void OnEventReceived(string name, Dictionary<string, string> properties)
        {
            var handler = EventReceived;
            if (handler != null) handler(this, name, properties);
        }

        protected virtual void OnDeviceCommunicatingChange(IDevice device, bool communicating)
        {
            var handler = DeviceCommunicatingChange;
            if (handler != null)
            {
                try
                {
                    handler(device, communicating);
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }
        }

        public void UpdateOnSourceRequest()
        {
            
        }

        public void StartPlaying()
        {
            _standby.Wake();
        }

        public void StopPlaying()
        {
            if (_calls.Count > 0 && DisconnectCallsIfNotBeingUsed)
            {
                _calls.DisconnectAll();
                Thread.Sleep(1000);
            }

            if (DisableAutoSleepOnStopPlaying) return;

            if (_standby.State != StandbyState.Standby && _calls.Count == 0)
            {
                _standby.Sleep();
            }
        }

        public void Initialize()
        {
            _httpsClient.StartSession();
            _sshClient.Connect();
        }

        public bool CheckInitializedOk()
        {
            return _initialized;
        }

        public void HttpsClientAbort()
        {
            _httpsClient.Abort();
        }

        public void DtmfSend(string dtmfString)
        {
            Send("xCommand Call DTMFSend DTMFString: \"{0}\"", dtmfString);
        }

        #endregion
    }

    internal delegate void CodecCommandResponse(int id, bool ok, XElement result);

    internal delegate void StatusReceivedHandler(CiscoTelePresenceCodec codec, StatusUpdateItem[] items);

    public class ConfigurationChangeEventArgs
    {
        public string Path { get; set; }
        public string PropertyName { get; set; }
        public string Value { get; set; }
    }

    public delegate void ConfigurationChangeEventHandler(CiscoTelePresenceCodec codec, ConfigurationChangeEventArgs args);

    internal delegate void EventNotificationReceivedHandler(
        CiscoTelePresenceCodec codec, string name, Dictionary<string, string> properties);

    public class StatusUpdateItem
    {
        internal StatusUpdateItem(Match match)
        {
            Path = Regex.Replace(match.Groups[1].Value, @" ([0-9]+) ?", @"[$1]");
            Path = Regex.Replace(Path, " ", ".");
            PropertyName = match.Groups[2].Value;
            Attributes = match.Groups[3].Value;
            StringValue = match.Groups[4].Value;

            try
            {
                Value = double.Parse(StringValue);
            }
            catch
            {
            }
        }

        public string Path { get; private set; }
        public string PropertyName { get; private set; }
        public string Attributes { get; private set; }
        public double Value { get; private set; }
        public string StringValue { get; private set; }

        public override string ToString()
        {
            return string.Format("{0}{1}{2} = {3}", Path, Path.EndsWith("]") ? "" : ".", PropertyName, StringValue);
        }
    }
}