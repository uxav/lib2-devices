using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using Crestron.SimplSharpPro.CrestronThread;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.Devices.Audio.QSC.Components;
using UX.Lib2.DeviceSupport;

namespace UX.Lib2.Devices.Audio.QSC
{
    /// <summary>
    /// An instance of control for a connection to a QSys Core using the QRC Protocol
    /// </summary>
    public class QsysCore : IEnumerable<ComponentBase>, IFusionAsset
    {
        private readonly string _name;
        private readonly string _username;
        private readonly string _password;
        private readonly QsysSocket _socket;
        private readonly CCriticalSection _awaitingAsyncRequestsLocked = new CCriticalSection();
        private readonly Dictionary<int, QsysRequestResponse> _awaitingAsyncRequests = new Dictionary<int, QsysRequestResponse>();
        private readonly Dictionary<string, ComponentBase> _components = new Dictionary<string, ComponentBase>();
        private readonly Dictionary<string, QsysChangeGroup> _changeGroups = new Dictionary<string, QsysChangeGroup>();
        private readonly CCriticalSection _awaitingResponsesLocked = new CCriticalSection();
        private readonly Dictionary<int, QsysResponse> _awaitingResponses = new Dictionary<int, QsysResponse>();
        private readonly CCriticalSection _awaitingEventsLocked = new CCriticalSection();
        private readonly Dictionary<int, CEvent> _awaitingEvents = new Dictionary<int, CEvent>();
        private bool _initialized;
        private string _designCode;
        private IEnumerable<string> _userDefinedComponentNames;
        private Thread _initializeThread;
        private bool _deviceCommunicating;
        private CTimer _commsCheckTimer;
        private CoreStatus _status;
        private int? _statusCode = null;
        private List<int> _reportedStatusCodes = new List<int>();
        private string _platform = "Q-Sys Core";

        /// <summary>
        /// Create an instance of a QsysCore
        /// </summary>
        /// <param name="deviceAddresses">The hostnames or ip addresses of the core(s)</param>
        /// <param name="name"></param>
        public QsysCore(IList<string> deviceAddresses, string name)
            : this(deviceAddresses, name, 1710)
        {

        }

        /// <summary>
        /// Create an instance of a QsysCore
        /// </summary>
        /// <param name="deviceAddresses">The hostnames or ip addresses of the core(s)</param>
        /// <param name="port">Override the default TCP port of 1710</param>
        /// <param name="name"></param>
        public QsysCore(IList<string> deviceAddresses, string name, int port)
        {
            _name = name;
            try
            {
                _socket = new QsysSocket(deviceAddresses, port, name);
                _socket.StatusChanged += (client, status) =>
                {
                    _initialized = status != SocketStatus.SOCKET_STATUS_CONNECTED;
                    DeviceCommunicating = status == SocketStatus.SOCKET_STATUS_CONNECTED;
                };

                _socket.RequestReceived += SocketOnRequestReceived;
                _socket.ResponseReceived += SocketOnResponseReceived;
                CloudLog.Debug("{0} instance created with address(es) \"{1}\" port {2}", GetType().Name,
                    String.Join(",", deviceAddresses.ToArray()), port);
                CrestronConsole.AddNewConsoleCommand(parameters => DefaultChangeGroup.Invalidate(), "QSysUpdateAll",
                    "Invalidate the default change group in the core",
                    ConsoleAccessLevelEnum.AccessOperator);
            }
            catch (Exception e)
            {
                CloudLog.Error("Error in {0}.ctor(), {1}", GetType().Name, e.Message);
            }
        }

        public QsysCore(IList<string> deviceAddresses, string name, string username, string password)
            : this(deviceAddresses, name)
        {
            _username = username;
            _password = password;
        }

        public QsysCore(IList<string> deviceAddresses, int port, string name, string username, string password)
            : this(deviceAddresses, name, port)
        {
            _username = username;
            _password = password;
        }

        /// <summary>
        /// Event called once the connection to the Core has Initialized
        /// </summary>
        public event QsysInitializedEventHandler HasIntitialized;

        protected virtual void OnHasIntitialized(QsysCore core)
        {
            var handler = HasIntitialized;
#if DEBUG
            CloudLog.Debug("QsysCore has initialized");
            foreach (var component in this)
            {
                CloudLog.Debug("QsysCore Component: {0}", component);
            }
#endif
            if (handler != null) handler(core);
        }

        /// <summary>
        /// The Platform desciption of the Core
        /// </summary>
        public string Platform
        {
            get { return _platform; }
        }

        /// <summary>
        /// The value of the Core state
        /// </summary>
        public string State { get; private set; }

        /// <summary>
        /// The current running design name of the Core
        /// </summary>
        public string DesignName { get; private set; }

        /// <summary>
        /// The current unique value assigned to the current design running on the Core
        /// </summary>
        public string DesignCode
        {
            get { return _designCode; }
            private set
            {
                if (_designCode != null && _designCode == value) return;
                _components.Clear();
                _changeGroups.Clear();
                _designCode = value;
                CloudLog.Debug("New QSys Design Code! - {0}", _designCode);
            }
        }

        /// <summary>
        /// True if this Core is part of a redundant design
        /// </summary>
        public bool IsRedundant { get; private set; }

        /// <summary>
        /// True if the Core is running as an Emulator
        /// </summary>
        public bool IsEmulator { get; private set; }

        /// <summary>
        /// Access to the current Core Status info
        /// </summary>
        public CoreStatus Status
        {
            get { return _status; }
            set
            {
                if(value == null) return;

                _status = value;

                if (_statusCode != null && _status.Code == _statusCode) return;
                _statusCode = _status.Code;

                if (_status.Code > 0 && !_reportedStatusCodes.Contains(_status.Code))
                {
                    _reportedStatusCodes.Add(_status.Code);
                    /*CloudLog.Warn("{0} \"{1}\" Status = {2} \"{3}\"", GetType().Name, Name, _status.Code,
                        _status.String);*/
                }
                else if(_status.Code == 0)
                {
                    _reportedStatusCodes.Clear();
                    /*CloudLog.Notice("{0} \"{1}\" Status = {2} \"{3}\"", GetType().Name, Name, _status.Code,
                        _status.String);*/
                }

                if (StatusChanged == null) return;

                try
                {
                    StatusChanged(this, _status);
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }
        }

        public event QsysStatusChangeEventHandler StatusChanged;

        public string Name
        {
            get { return _name; }
        }

        public string ManufacturerName
        {
            get { return "QSC"; }
        }

        public string ModelName
        {
            get { return Platform; }
        }

        public string DiagnosticsName
        {
            get { return Platform + " (" + DeviceAddressString + ")"; }
        }

        public bool DeviceCommunicating
        {
            get { return _deviceCommunicating; }
            private set
            {
                if (_deviceCommunicating == value) return;

                _deviceCommunicating = value;

                if (DeviceCommunicatingChange == null) return;

                try
                {
                    DeviceCommunicatingChange(this, _deviceCommunicating);
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }
        }

        public string DeviceAddressString
        {
            get { return _socket.Addresses.FirstOrDefault() ?? string.Empty; }
        }

        public string SerialNumber
        {
            get { return "Unknown"; }
        }

        public string VersionInfo
        {
            get { return DesignCode; }
        }

        public event DeviceCommunicatingChangeHandler DeviceCommunicatingChange;

        /// <summary>
        /// True if Core is connected and initialized
        /// </summary>
        public bool Initialized
        {
            get { return _initialized; }
        }

        /// <summary>
        /// Access a Named Component on the Core
        /// </summary>
        /// <param name="componentName"></param>
        /// <returns></returns>
        public ComponentBase this[string componentName]
        {
            get
            {
                if (_components.ContainsKey(componentName))
                    return _components[componentName];

                CloudLog.Error("QsysCore does not contain component \"{0}\"", componentName);
                return null;
            }
        }

        public bool ContainsComponentWithName(string name)
        {
            return _components.ContainsKey(name);
        }

        /// <summary>
        /// Initialize the connection to the Core
        /// </summary>
        public void Initialize(IEnumerable<string> componentNames)
        {
            _userDefinedComponentNames = componentNames;
            _socket.Connect();
        }

        public QsysChangeGroup DefaultChangeGroup
        {
            get { return GetChangeGroup("default"); }
        }

        public QsysChangeGroup RampingChangeGroup
        {
            get { return GetChangeGroup("ramping"); }
        }

        /// <summary>
        /// Gets a change group by ID, if it doesn't already exist it will be created
        /// </summary>
        /// <param name="id">Change group ID</param>
        /// <returns>The change group object</returns>
        public QsysChangeGroup GetChangeGroup(string id)
        {
            if (_changeGroups.ContainsKey(id)) return _changeGroups[id];
            _changeGroups[id] = new QsysChangeGroup(this, id);
            return _changeGroups[id];
        }

        internal void RemoveChangeGroup(string id)
        {
            _changeGroups.Remove(id);
        }

        internal QsysResponse Request(string method, object args)
        {
            if(Thread.CurrentThread.ThreadType == Thread.eThreadType.SystemThread)
                throw new Exception("Cannot call QsysCore.Request synchronously in system thread!");
            var request = new QsysRequest(_socket, method, args);
            //CloudLog.Debug("{0}.Request(), ID = {1}", GetType().Name, request.Id);
            _awaitingEventsLocked.Enter();
            _awaitingEvents[request.Id] = new CEvent(true, false);
            _awaitingEventsLocked.Leave();
            _socket.SendRequest(request);
            var result = _awaitingEvents[request.Id].Wait(30000);
            _awaitingEvents[request.Id].Dispose();
            _awaitingEventsLocked.Enter();
            _awaitingEvents.Remove(request.Id);
            _awaitingEventsLocked.Leave();
            if (!result)
            {
                CloudLog.Error("{0} Request Time Out, Request: {1}, ID {2}", GetType().Name, request.Method, request.Id);
                throw new TimeoutException("Request did not process a response in suitable time");
            }
            var response = _awaitingResponses[request.Id];
            _awaitingResponsesLocked.Enter();
            _awaitingResponses.Remove(request.Id);
            _awaitingResponsesLocked.Leave();
            return response;
        }

        internal int RequestAsync(QsysRequestResponse callBack, string method, object args)
        {
            var request = new QsysRequest(_socket, method, args);
            _socket.SendRequest(request);
            try
            {
                _awaitingAsyncRequestsLocked.Enter();
                _awaitingAsyncRequests[request.Id] = callBack;
                _awaitingAsyncRequestsLocked.Leave();
            }
            catch (Exception e)
            {
                CloudLog.Error("Could not queue callback for QsysRequest ID {0}, {1}", request.Id, e.Message);
            }
            return request.Id;
        }

        private void ReceivedEngineStatusUpdate(QsysRequest request)
        {
            /*CloudLog.Debug("{0} received EngineStatus\r\n{1}", GetType().Name,
                        request.Args.ToString(Formatting.Indented));*/
#if true
            Debug.WriteInfo("Q-Sys socket status received");
            Debug.WriteNormal(Debug.AnsiBlue + request.Args.ToString(Formatting.Indented) + Debug.AnsiReset);
#endif
            _platform = request.Args["Platform"].Value<string>();
            State = request.Args["State"].Value<string>();
            DesignName = request.Args["DesignName"].Value<string>();
            DesignCode = request.Args["DesignCode"].Value<string>();
            IsRedundant = request.Args["IsRedundant"].Value<bool>();
            IsEmulator = request.Args["IsEmulator"].Value<bool>();
            Status = new CoreStatus(request.Args["Status"]);

            if (State == "Standby")
            {
                _socket.TryAnotherAddress();
                _socket.Disconnect(true);
            }

            if (!_initialized &&
                (_initializeThread == null || _initializeThread.ThreadState != Thread.eThreadStates.ThreadRunning))
            {
                _initializeThread = new Thread(specific => InitalizeProcess(), null)
                {
                    Name = "QsysCore Initializing Process Thread",
                    Priority = Thread.eThreadPriority.HighPriority
                };
            }
        }

        private object InitalizeProcess()
        {
            CloudLog.Debug("{0} Initializing has started...", GetType().Name);
            var sw = new Stopwatch();
            sw.Start();

            if (!string.IsNullOrEmpty(_username))
            {
                var logonResponse = Request("Logon", new
                {
                    User = _username,
                    Password = _password
                });

                Debug.WriteSuccess(GetType().Name + " Login Response", logonResponse);
            }

            if (_components.Count == 0)
            {
                CloudLog.Debug("{0} has no component details... requesting...", GetType().Name);
                var response = Request("Component.GetComponents", null);

                CloudLog.Debug("QsysCore has received list of user defined components, count = {0}",
                    response.Result.Children().Count());

                foreach (var component in response.Result.Children())
                {
                    CloudLog.Debug("QsysCore has compononent \"{0}\"", component["Name"].Value<string>());
                }

                foreach (var component in response.Result.Children()
                    .Where(c => _userDefinedComponentNames.Contains(c["Name"].Value<string>())))
                {
                    var name = component["Name"].Value<string>();
                    if (_components.ContainsKey(name)) continue;
                    switch (component["Type"].Value<string>())
                    {
                        case "audio_file_player":
                            _components[name] = new AudioFilePlayer(this, component);
                            break;
                        case "mixer":
                            _components[name] = new Mixer(this, component);
                            break;
                        case "system_mute":
                        case "gain":
                            _components[name] = new Gain(this, component);
                            break;
                        case "scriptable_controls":
                            _components[name] = new ScriptableControls(this, component);
                            break;
                        case "snapshot_controller":
                            _components[name] = new SnapshotController(this, component);
                            break;
                        case "softphone":
                            _components[name] = new SoftPhone(this, component);
                            break;
                        case "pots_control_status_core":
                            _components[name] = new PotsPhone(this, component);
                            break;
                        case "signal_presence":
                            _components[name] = new SignalPresence(this, component);
                            break;
                        case "sine":
                            _components[name] = new Sine(this, component);
                            break;
                        case "router_with_output":
                            _components[name] = new RouterWithOutput(this, component);
                            break;
                        case "io_status":
                            _components[name] = new IoStatus(this, component);
                            break;
                        default:
                            _components[name] = new GenericComponent(this, component);
                            break;
                    }
                }
            }
            else
            {
                CloudLog.Debug("{0} has component details... updating...", GetType().Name);
                foreach (var component in this)
                {
                    component.UpdateAsync();
                }
            }

            _initialized = true;

            sw.Stop();
            CloudLog.Debug("{0} has initialized, process time = {1}", GetType().Name, sw.Elapsed);

            OnHasIntitialized(this);

            DefaultChangeGroup.PollAuto(1.0);

            return null;
        }

        private void SocketOnRequestReceived(QsysSocket socket, QsysRequest request)
        {
            //CloudLog.Debug("{0}.SocketOnRequestReceived()\r\n{1}", GetType().Name, request);

            switch (request.Method)
            {
                case "EngineStatus":
                    ReceivedEngineStatusUpdate(request);
                    break;
                case "ChangeGroup.Poll":
                    var groupId = request.Args["Id"].Value<string>();

                    if (_changeGroups.ContainsKey(groupId))
                    {
                        var changes = request.Args["Changes"];
                        foreach (var change in changes.Where(change => change["Component"] != null))
                        {
                            this[change["Component"].Value<string>()][change["Name"].Value<string>()].UpdateFromData
                                (change);
                        }
                    }
                    break;
            }
        }

        private void SocketOnResponseReceived(QsysSocket socket, QsysResponse response)
        {
#if DEBUG
            //CloudLog.Debug("{0}.SocketOnResponseReceived(), ID = {1}{2}", GetType().Name, response.Id,
            //    response.IsError ? string.Format(", Error: {0}", response.ErrorMessage) : " OK");
#endif
            if (_awaitingEvents.ContainsKey(response.Id))
            {
#if DEBUG
                //CrestronConsole.PrintLine("Found awaiting CEvent for response ID {0}", response.Id);
#endif
                _awaitingResponsesLocked.Enter();
                _awaitingResponses[response.Id] = response;
                _awaitingResponsesLocked.Leave();
                _awaitingEvents[response.Id].Set();
                return;
            }
            if (!_awaitingAsyncRequests.ContainsKey(response.Id)) return;
            try
            {
                _awaitingAsyncRequests[response.Id](response);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
            _awaitingAsyncRequestsLocked.Enter();
            _awaitingAsyncRequests.Remove(response.Id);
            _awaitingAsyncRequestsLocked.Leave();
        }

        #region IEnumerable<QsysComponent> Members

        /// <summary>
        /// Get the Enmerator of the object
        /// </summary>
        public IEnumerator<ComponentBase> GetEnumerator()
        {
            return _components.Values.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        public FusionAssetType AssetType { get { return FusionAssetType.AudioProcessor; } }
    }

    internal delegate void QsysRequestResponse(QsysResponse response);

    /// <summary>
    /// The event handler for an Initalized event on a Core connetion
    /// </summary>
    /// <param name="core">The instance of the QsysCore which has Initialized</param>
    public delegate void QsysInitializedEventHandler(QsysCore core);

    public delegate void QsysStatusChangeEventHandler(QsysCore core, CoreStatus status);
}