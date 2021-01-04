 
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Reflection;
using Crestron.SimplSharp.Ssh;
using Crestron.SimplSharp.Ssh.Common;
using Crestron.SimplSharpPro.CrestronThread;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Cisco
{
    public class CiscoSshClient
    {
        #region Fields


        private SshClient _client;
        private Thread _sshProcess;
        private bool _programRunning = true;
        private ShellStream _shell;
        private readonly CrestronQueue<string> _sendQueue = new CrestronQueue<string>(30);
        private readonly string _address;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private bool _reconnect;
        private readonly CEvent _threadWait = new CEvent(true, false);
        private readonly string _heartbeatId;
        private CTimer _heartBeatTimer;
        private bool _loggedIn;
        private readonly Stopwatch _stopWatch = new Stopwatch();
        private SshClientStatus _connectionStatus;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a CodecSshClient
        /// </summary>
        internal CiscoSshClient(string address, int port, string username, string password)
        {
            _address = address;
            _port = port;
            _username = username;
            _password = password;
            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                _programRunning = type != eProgramStatusEventType.Stopping;
                if(_programRunning) return;

                _threadWait.Set();
                _reconnect = false;

                if (!Connected) return;

                CloudLog.Info("Program stopping, Sending bye to {0} at {1}", GetType().Name, _address);
                Send("bye");
            };
            _heartbeatId =
                CrestronEthernetHelper.GetEthernetParameter(
                    CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_MAC_ADDRESS,
                    CrestronEthernetHelper.GetAdapterdIdForSpecifiedAdapterType(EthernetAdapterType.EthernetLANAdapter));
        }

        /// <summary>
        /// Create a CodecSshClient
        /// </summary>
        internal CiscoSshClient(string address, string username, string password)
            : this(address, 22, username, password)
        {
            
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event CodecSshClientReceivedDataEventHandler ReceivedData;

        public event CodecSshClientConnectionStatusChangedHandler ConnectionStatusChange;

        #endregion

        #region Delegates
        #endregion

        #region Properties

        public bool Connected
        {
            get { return _connectionStatus == SshClientStatus.Connected; }
        }

        public SshClientStatus ConnectionStatus
        {
            get { return _connectionStatus; }
            private set
            {
                if(_connectionStatus == value) return;
                var previousValue = _connectionStatus;
                _connectionStatus = value;
                CloudLog.Notice("{0} set_ConnectionStatus: {1} for {2}", GetType().Name, value, _address);
                OnConnectionStatusChange(this, value);
            }
        }

        #endregion

        #region Methods

        public void Connect()
        {
            if (_client != null && _client.IsConnected)
            {
                CloudLog.Warn("{0} already connected", GetType().Name);
                return;
            }

            _reconnect = true;

            var info = new KeyboardInteractiveConnectionInfo(_address, _port, _username);
            info.AuthenticationPrompt += OnPasswordPrompt;

            _client = new SshClient(info);
            _client.ErrorOccurred +=
                (sender, args) => CloudLog.Exception(args.Exception, "SshClient.ErrorOccurred Event");
            _client.HostKeyReceived +=
                (sender, args) =>
                    CloudLog.Notice("{0} HostKeyReceived: {1}, can trust = {2}", GetType().Name, args.HostKeyName,
                        args.CanTrust);

            _sshProcess = new Thread(SshCommsProcess, null, Thread.eThreadStartOptions.CreateSuspended)
            {
                Name = "CodecSshClient Comms Handler",
                Priority = Thread.eThreadPriority.MediumPriority
            };

            _sshProcess.Start();
        }

        public void Disconnect()
        {
            if (_client == null || !_client.IsConnected)
            {
                CloudLog.Warn("{0} is not connected", GetType().Name);
                return;
            }

            _reconnect = false;
            _client.Disconnect();
        }

        public void Send(string line)
        {
            if (Connected)
            {
                _sendQueue.Enqueue(line);
                //_threadWait.Set();
            }
            else
            {
                CloudLog.Warn("Could not send \"{0}\" to codec. No Connection", line);
            }
        }

        private void OnPasswordPrompt(object sender, AuthenticationPromptEventArgs authenticationPromptEventArgs)
        {
            foreach (
                var prompt in
                    authenticationPromptEventArgs.Prompts.Where(prompt => prompt.Request.Contains("Password:")))
            {
#if DEBUG
                Debug.WriteInfo("Codec password prompt ... sending password");
#endif
                prompt.Response = _password;
            }
        }

        private object SshCommsProcess(object userSpecific)
        {
            var errorOnConnect = false;

            try
            {
                Thread.Sleep(1000);

                CloudLog.Info("{0} attempting connection to {1}", GetType().Name, _address);

                var firstFail = false;

                while (!_client.IsConnected && _reconnect)
                {
                    try
                    {
                        ConnectionStatus = SshClientStatus.AttemptingConnection;
                        _client.Connect();
                    }
                    catch
                    {
                        ConnectionStatus = SshClientStatus.Disconnected;
                        if (!firstFail)
                        {
                            CloudLog.Warn("{0} could not connect to {1}, will retry every 30 seconds until connected",
                                GetType().Name, _address);
                            firstFail = true;
                        }

                        if (_threadWait.WaitOne(30000))
                        {
                            CloudLog.Info("{0} - Signal sent to leave thread on attempt to connect", GetType().Name);
                            _reconnect = false;
                            break;
                        }
                    }
                }

                if (!_client.IsConnected && !_reconnect)
                {
                    _client.Dispose();
                    _client = null;
                    ConnectionStatus = SshClientStatus.Disconnected;
                    return null;
                }

                CloudLog.Notice("{0} Connected to {1}", GetType().Name, _address);

                _shell = _client.CreateShellStream("terminal", 80, 24, 800, 600, 100000);

                //_shell.DataReceived += (sender, args) => _threadWait.Set();

                var data = string.Empty;

                try
                {
                    while (_programRunning && _client.IsConnected)
                    {
                        while (_shell.CanRead && _shell.DataAvailable)
                        {
                            var bytes = new byte[100000];
                            var length = _shell.Read(bytes, 0, bytes.Length);
#if DEBUG
                            _stopWatch.Start();
                            Debug.WriteSuccess("Codec rx {0} bytes", length);
#endif
                            var incoming = Encoding.ASCII.GetString(bytes, 0, length);

                            if (!_loggedIn && incoming.Contains("Failed to connect to system software"))
                            {
                                CloudLog.Error("Error received from codec at \"{0}\" on connect:\r\n{1}",
                                    _address, incoming);
                                errorOnConnect = true;
                                break;
                            }
#if DEBUG
                            Debug.WriteNormal(Debug.AnsiBlue + incoming + Debug.AnsiReset);
#endif
                            data = data + incoming;

                            if (!_loggedIn)
                            {
                                foreach (var line in Regex.Split(data, "\r\n|\r|\n"))
                                {
                                    if (line == "*r Login successful")
                                    {
                                        _loggedIn = true;
                                        try
                                        {
                                            Debug.WriteSuccess("Connected!", "{0} logged into {1}", GetType().Name,
                                                _address);
                                            CloudLog.Info("{0} logged into {1}", GetType().Name, _address);

                                            _sendQueue.Enqueue(@"echo off");

                                            var adapterType = _client.EthernetAdapter;
                                            var adapterId =
                                                CrestronEthernetHelper.GetAdapterdIdForSpecifiedAdapterType(adapterType);
                                            var currentIp = CrestronEthernetHelper.GetEthernetParameter(
                                                CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_IP_ADDRESS,
                                                adapterId);
                                            var assembly = Assembly.GetExecutingAssembly().GetName();
                                            var softwareInfo = assembly.Name + " " + assembly.Version;
                                            var model = InitialParametersClass.ControllerPromptName;
                                            _sendQueue.Enqueue(string.Format(
                                                "xCommand Peripherals Connect ID: {0} Name: \"Crestron Control System\" HardwareInfo: \"{1}\" NetworkAddress: \"{2}\" SoftwareInfo: \"{3}\" Type: ControlSystem",
                                                _heartbeatId, string.Format("Crestron {0}", model), currentIp,
                                                softwareInfo));

                                            _sendQueue.Enqueue(@"xFeedback register /Configuration");
                                            _sendQueue.Enqueue(@"xFeedback register /Status/SystemUnit");
                                            _sendQueue.Enqueue(@"xFeedback register /Status/Diagnostics");
                                            _sendQueue.Enqueue(@"xFeedback register /Status/Audio");
                                            _sendQueue.Enqueue(@"xFeedback register /Status/Standby");
                                            _sendQueue.Enqueue(@"xFeedback register /Status/Video");
                                            _sendQueue.Enqueue(@"xFeedback register /Status/Cameras");
                                            _sendQueue.Enqueue(@"xFeedback register /Status/Call");
                                            _sendQueue.Enqueue(@"xFeedback register /Status/Conference");
                                            _sendQueue.Enqueue(@"xFeedback register /Status/UserInterface");
                                            _sendQueue.Enqueue(@"xFeedback register /Status/Bookings");
                                            _sendQueue.Enqueue(@"xFeedback register /Event/OutgoingCallIndication");
                                            _sendQueue.Enqueue(@"xFeedback register /Event/IncomingCallIndication");
                                            _sendQueue.Enqueue(@"xFeedback register /Event/CallSuccessful");
                                            _sendQueue.Enqueue(@"xFeedback register /Event/CallDisconnect");
                                            _sendQueue.Enqueue(@"xFeedback register /Event/Bookings");
                                            _sendQueue.Enqueue(@"xFeedback register /Event/CallHistory");
                                            _sendQueue.Enqueue(@"xFeedback register /Event/UserInterface/Extensions");
                                            _sendQueue.Enqueue(@"xFeedback register /Event/UserInterface/Presentation/ExternalSource");

                                            _sendQueue.Enqueue(@"xStatus");

                                            _heartBeatTimer =
                                                new CTimer(
                                                    specific =>
                                                        _sendQueue.Enqueue(
                                                            string.Format("xCommand Peripherals HeartBeat ID: {0}",
                                                                _heartbeatId)),
                                                    null, 60000, 60000);

                                            ConnectionStatus = SshClientStatus.Connected;
                                        }
                                        catch (Exception e)
                                        {
                                            CloudLog.Exception(e);
                                        }
                                    }
                                }

                                data = string.Empty;
                            }
                            else if (data.Contains("** end"))
                            {
                                var chunks = Regex.Matches(data, @"(\*(\w) [\S\s]*?)(?=\*\* end)");

                                data = string.Empty;

                                if (chunks.Count > 0)
                                {
                                    foreach (Match chunk in chunks)
                                    {
                                        var type = ReceivedDataType.Status;

                                        switch (chunk.Groups[2].Value)
                                        {
                                            case "e":
                                                type = ReceivedDataType.Event;
                                                break;
                                            case "r":
                                                type = ReceivedDataType.Response;
                                                break;
                                            case "c":
                                                type = ReceivedDataType.Configuration;
                                                break;
                                        }
                                        OnReceivedData(this, type, chunk.Groups[1].Value);
                                    }
                                }
                            }
#if DEBUG
                            else
                            {
                                Debug.WriteWarn("Waiting for more data...");
                            }

                            _stopWatch.Stop();
                            Debug.WriteNormal("Time to process: {0} ms", _stopWatch.ElapsedMilliseconds);
                            _stopWatch.Reset();
#endif
                            CrestronEnvironment.AllowOtherAppsToRun();
                        }

                        if (errorOnConnect)
                        {
                            try
                            {
                                _client.Disconnect();
                            }
                            catch (Exception e)
                            {
                                CloudLog.Warn("Error closing connection to codec after error, {0}", e.Message);
                            }

                            break;
                        }

                        if (!_programRunning || !_client.IsConnected) break;

                        while (_shell.CanWrite && !_sendQueue.IsEmpty)
                        {
                            var s = _sendQueue.Dequeue();
#if DEBUG
                            Debug.WriteWarn("Codec Tx", s);
#endif
                            _shell.WriteLine(s);
                        }
                    }

                    CloudLog.Debug("{0} left while(connected) loop", Thread.CurrentThread.Name);
                }
                catch (ObjectDisposedException e)
                {
                    CloudLog.Warn("{0} ObjectDisposedException, {1}, {2}", GetType().Name,
                        string.IsNullOrEmpty(e.ObjectName) ? "unknown" : e.ObjectName, e.Message);
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e, "Error handling comms process");
                }

                _loggedIn = false;

                try
                {
                    if (_heartBeatTimer != null && !_heartBeatTimer.Disposed)
                    {
                        _heartBeatTimer.Stop();
                        _heartBeatTimer.Dispose();
                    }

                    if (_client != null)
                    {
                        CloudLog.Info("Disposing {0}", _client.ToString());
                        _client.Dispose();
                        _client = null;
                    }
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e, "Error trying to dispose items in leaving thread");
                }

                CloudLog.Notice("{0} Disconnected from {1}", GetType().Name, _address);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e, "Error in {0}.SshCommsProcess()", GetType().Name);
            }

            ConnectionStatus = SshClientStatus.Disconnected;

            if (errorOnConnect)
            {
                CloudLog.Warn("Error message while connecting to {0}, Disconnected and will retry in 60 seconds", _address);
                
                if (_threadWait.WaitOne(60000))
                {
                    CloudLog.Info("{0} - Signal sent to leave thread on attempt to connect", GetType().Name);
                    _reconnect = false;
                }
            }

            if (!_reconnect)
            {
                CloudLog.Notice("Leaving connect thread for codec at {0}", _address);
                return null;
            }

            CloudLog.Notice("Waiting for 10 seconds before reconnect attempt to {0}", _address);
            if (_threadWait.WaitOne(10000))
            {
                CloudLog.Info("{0} - Signal sent to leave thread on attempt to connect", GetType().Name);
                _reconnect = false;
                return null;
            }

            CloudLog.Notice("Attempting reconnect to codec at {0}", _address);
            ConnectionStatus = SshClientStatus.AttemptingConnection;

            Connect();

            return null;
        }

        protected virtual void OnReceivedData(CiscoSshClient client, ReceivedDataType type, string data)
        {
            try
            {
                var handler = ReceivedData;
                if (handler != null)
                    handler(client, new CodecSshClientReceivedDataArgs()
                    {
                        DataType = type,
                        DataAsReceived = data,
                        Lines = Regex.Split(data, "\r\n|\r|\n")
                    });
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        protected virtual void OnConnectionStatusChange(CiscoSshClient client, SshClientStatus status)
        {
            var handler = ConnectionStatusChange;
            if (handler != null)
            {
                try
                {
                    handler(client, status);
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }
        }

        #endregion
    }

    public delegate void CodecSshClientConnectionStatusChangedHandler(CiscoSshClient client, SshClientStatus status);

    public enum SshClientStatus
    {
        Disconnected,
        AttemptingConnection,
        Connected
    }

    public delegate void CodecSshClientReceivedDataEventHandler(CiscoSshClient client, CodecSshClientReceivedDataArgs args);

    public class CodecSshClientReceivedDataArgs : EventArgs
    {
        public ReceivedDataType DataType { get; internal set; }
        public string DataAsReceived { get; internal set; }
        public string[] Lines { get; internal set; }
    }

    public enum ReceivedDataType
    {
        Status,
        Event,
        Response,
        Configuration
    }
}