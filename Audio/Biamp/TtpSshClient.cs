 
using System;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Ssh;
using Crestron.SimplSharp.Ssh.Common;
using Crestron.SimplSharpPro.CrestronThread;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Audio.Biamp
{
    public class TtpSshClient
    {
        private SshClient _client;
        private Thread _sshProcess;
        private bool _reconnect;
        private readonly CrestronQueue<string> _sendQueue = new CrestronQueue<string>(500);
        private readonly CrestronQueue<string> _requestsSent = new CrestronQueue<string>(500);
        private readonly CrestronQueue<string> _requestsAwaiting = new CrestronQueue<string>(500);
        private readonly string _address;
        private readonly string _username;
        private readonly string _password;
        private bool _programRunning = true;
        private bool _loggedIn;
#if DEBUG
        private readonly Stopwatch _stopWatch = new Stopwatch();
#endif
        private ClientStatus _connectionStatus;
        private ShellStream _shell;
        private CTimer _keepAliveTimer;
        private decimal _timeOutCount;

        private const int BufferSize = 100000;
        private const long KeepAliveTime = 30000;

        public TtpSshClient(string address, string username, string password)
        {
            _address = address;
            _username = username;
            _password = password;
            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                _programRunning = type != eProgramStatusEventType.Stopping;
                if (!_programRunning && Connected)
                {
                    _reconnect = false;
                    Send("bye");
                }
                //if (!_programRunning && _threadWait != null)
                //_threadWait.Set();
            };
        }


        public event TtpSshClientReceivedDataEventHandler ReceivedData;

        public event TtpSshClientConnectionStatusChangedHandler ConnectionStatusChange;

        public enum ClientStatus
        {
            Disconnected,
            AttemptingConnection,
            Connected
        }

        public string DeviceAddress
        {
            get { return _address; }
        }

        public bool Connected
        {
            get { return _connectionStatus == ClientStatus.Connected; }
        }

        public ClientStatus ConnectionStatus
        {
            get { return _connectionStatus; }
            private set
            {
                if (_connectionStatus == value) return;
                var previousValue = _connectionStatus;
                _connectionStatus = value;
                if (!(previousValue == ClientStatus.AttemptingConnection &&
                      _connectionStatus == ClientStatus.Disconnected))
                {
                    OnConnectionStatusChange(this, value);
                }
            }
        }

        public void Connect()
        {
            if (_client != null && _client.IsConnected)
            {
                CloudLog.Warn("{0} already connected", GetType().Name);
                return;
            }

            _reconnect = true;

            var info = new KeyboardInteractiveConnectionInfo(_address, 22, _username);
            info.AuthenticationPrompt += OnPasswordPrompt;

            _client = new SshClient(info);
            _client.ErrorOccurred += (sender, args) => CrestronConsole.PrintLine("ErrorOccurred: {0}", args.Exception.Message);
            _client.HostKeyReceived +=
                (sender, args) => CrestronConsole.PrintLine("HostKeyReceived: {1}, can trust = {0}", args.CanTrust,
                    args.HostKeyName);

            _sshProcess = new Thread(SshCommsProcess, null, Thread.eThreadStartOptions.CreateSuspended)
            {
                Name = "Tesira SSH Comms Handler",
                Priority = Thread.eThreadPriority.HighPriority
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
#if DEBUG
                Debug.WriteWarn("Tesira Enqueued Message", line);
#endif
                _sendQueue.Enqueue(line);
            }
            else
            {
                CloudLog.Warn("Could not send \"{0}\" to Tesira. No Connection", line);
            }
        }

        private void OnPasswordPrompt(object sender, AuthenticationPromptEventArgs authenticationPromptEventArgs)
        {
            foreach (
                var prompt in
                    authenticationPromptEventArgs.Prompts.Where(prompt => prompt.Request.Contains("Password:")))
            {
#if DEBUG
                Debug.WriteInfo("Tesira password prompt ... sending password");
#endif
                prompt.Response = _password;
            }
        }

        protected virtual void OnConnectionStatusChange(TtpSshClient client, ClientStatus status)
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

        private object SshCommsProcess(object userSpecific)
        {
            try
            {
                Thread.Sleep(1000);

                Debug.WriteInfo(string.Format("{0} attempting connection to {1}", GetType().Name, _address));

                var firstFail = false;

                while (!_client.IsConnected && _reconnect)
                {
                    try
                    {
                        ConnectionStatus = ClientStatus.AttemptingConnection;
                        _client.Connect();
                    }
                    catch
                    {
                        ConnectionStatus = ClientStatus.Disconnected;
                        if (!firstFail)
                        {
                            CloudLog.Warn("{0} could not connect to {1}, will retry every 30 seconds until connected",
                                GetType().Name, _address);
                            firstFail = true;
                        }
                        Thread.Sleep(30000);
                    }
                }

                if (!_client.IsConnected && !_reconnect)
                {
                    _client.Dispose();
                    _client = null;
                    ConnectionStatus = ClientStatus.Disconnected;
                    return null;
                }

                CloudLog.Notice("{0} Connected to {1}", GetType().Name, _address);

                _shell = _client.CreateShellStream("terminal", 80, 24, 800, 600, BufferSize);

                var buffer = new byte[BufferSize];
                var dataCount = 0;

                try
                {
                    while (_programRunning && _client.IsConnected)
                    {

                        while (_shell.CanRead && _shell.DataAvailable)
                        {
                            var incomingData = new byte[BufferSize];
                            var incomingDataCount = _shell.Read(incomingData, 0, incomingData.Length);
#if DEBUG
                            _stopWatch.Start();
                            Debug.WriteSuccess("Tesira rx {0} bytes", incomingDataCount);
                            //Debug.WriteNormal(Debug.AnsiBlue +
                            //                  Tools.GetBytesAsReadableString(incomingData, 0, incomingDataCount, true) +
                            //                  Debug.AnsiReset);
#endif
                            if (!Connected &&
                                Encoding.ASCII.GetString(incomingData, 0, incomingDataCount)
                                    .Contains("Welcome to the Tesira Text Protocol Server..."))
                            {
                                _requestsSent.Clear();
                                _requestsAwaiting.Clear();
                                _sendQueue.Enqueue("SESSION set verbose true");
                                ConnectionStatus = ClientStatus.Connected;
                                _keepAliveTimer = new CTimer(specific =>
                                {
#if DEBUG
                                    Debug.WriteInfo(GetType().Name + " Sending KeepAlive");
#endif
                                    _client.SendKeepAlive();
                                }, null, KeepAliveTime, KeepAliveTime);
                            }
                            else if (Connected)
                            {
                                for (var i = 0; i < incomingDataCount; i++)
                                {
                                    buffer[dataCount] = incomingData[i];

                                    if (buffer[dataCount] == 10)
                                    {
                                        //skip
                                    }
                                    else if (buffer[dataCount] != 13)
                                    {
                                        dataCount++;
                                    }
                                    else
                                    {
                                        if(dataCount == 0) continue;

                                        var line = Encoding.UTF8.GetString(buffer, 0, dataCount);
                                        dataCount = 0;
#if DEBUG
                                        Debug.WriteSuccess("Tesira Rx Line", Debug.AnsiPurple + line + Debug.AnsiReset);
#endif
                                        TesiraMessage message = null;

                                        if (line == "+OK")
                                        {
                                            var request = _requestsAwaiting.TryToDequeue();
                                            if (request != null)
                                            {
#if DEBUG
                                                Debug.WriteInfo("Request Response Received", request);
                                                Debug.WriteSuccess(line);
#endif
                                                message = new TesiraResponse(request, null);
                                            }
                                        }
                                        else if (line.StartsWith("+OK "))
                                        {
                                            var request = _requestsAwaiting.TryToDequeue();
                                            if (request != null)
                                            {
#if DEBUG
                                                Debug.WriteInfo("Request Response Received", request);
                                                Debug.WriteSuccess(line);
#endif
                                                message = new TesiraResponse(request, line.Substring(4));
                                            }
                                        }
                                        else if (line.StartsWith("-ERR "))
                                        {
                                            var request = _requestsAwaiting.TryToDequeue();
                                            if (request != null)
                                            {
#if DEBUG
                                                Debug.WriteInfo("Request Response Received", request);
                                                Debug.WriteError(line);
#endif
                                                message = new TesiraErrorResponse(request, line.Substring(5));
                                            }
                                            else
                                            {
                                                Debug.WriteError("Error received and request queue returned null!");
                                                Debug.WriteError(line);
                                                Debug.WriteError("Clearing all queues!");
                                                _requestsSent.Clear();
                                                _requestsAwaiting.Clear();
                                            }
                                        }
                                        else if (line.StartsWith("! "))
                                        {
#if DEBUG
                                                Debug.WriteWarn("Notification Received");
                                                Debug.WriteWarn(line);
#endif
                                                message = new TesiraNotification(line.Substring(2));
                                        }
                                        else if (!_requestsSent.IsEmpty)
                                        {
                                            Debug.WriteWarn("Last sent request", _requestsSent.Peek());

                                            if (_requestsSent.Peek() == line)
                                            {
                                                _requestsAwaiting.Enqueue(_requestsSent.Dequeue());
#if DEBUG
                                                Debug.WriteNormal("Now awaiting for response for command", line);
#endif
                                            }
                                        }

                                        if (message != null && ReceivedData != null && message.Type != TesiraMessageType.ErrorResponse)
                                        {
                                            if (ReceivedData == null) continue;
                                            try
                                            {
                                                _timeOutCount = 0;

                                                ReceivedData(this, message);
                                            }
                                            catch (Exception e)
                                            {
                                                CloudLog.Exception(e, "Error calling event handler");
                                            }
                                        }
                                        else if (message != null && message.Type == TesiraMessageType.ErrorResponse)
                                        {
                                            _timeOutCount = 0;

                                            CloudLog.Error("Error message from Tesira: \"{0}\"", message.Message);                                            
                                        }
                                    }
                                }
                            }
#if DEBUG
                            _stopWatch.Stop();
                            Debug.WriteNormal("Time to process: {0} ms", _stopWatch.ElapsedMilliseconds);
                            _stopWatch.Reset();
#endif
                            CrestronEnvironment.AllowOtherAppsToRun();
                        }

                        if (!_programRunning || !_client.IsConnected) break;
#if DEBUG
                        //Debug.WriteNormal(Debug.AnsiBlue +
                        //                  string.Format(
                        //                      "Shell Can Write = {0}, _sendQueue = {1}, _requestsSent = {2}, _requestsAwaiting = {3}",
                        //                      _shell.CanWrite, _sendQueue.Count, _requestsSent.Count,
                        //                      _requestsAwaiting.Count) + Debug.AnsiReset);
#endif
                        if (_shell.CanWrite && !_sendQueue.IsEmpty && _requestsSent.IsEmpty && _requestsAwaiting.IsEmpty)
                        {
                            var s = _sendQueue.Dequeue();

                            if (_keepAliveTimer != null && !_keepAliveTimer.Disposed)
                            {
                                _keepAliveTimer.Reset(KeepAliveTime, KeepAliveTime);
                            }
#if DEBUG
                            Debug.WriteWarn("Tesira Tx", s);
#endif
                            _timeOutCount = 0;
                            _shell.WriteLine(s);
                            _requestsSent.Enqueue(s);
                            Thread.Sleep(20);
                        }
                        else if(!_requestsSent.IsEmpty || !_requestsAwaiting.IsEmpty)
                        {
                            _timeOutCount ++;

                            if (_timeOutCount > 100)
                            {
                                CloudLog.Warn(
                                    "Error waiting to send requests in {0}, _requestsAwaiting.Count = {1}" +
                                    "and _requestsSent.Count = {2}. Clearing queues!",
                                    GetType().Name, _requestsAwaiting.Count, _requestsSent.Count);
                                _requestsAwaiting.Clear();
                                _requestsSent.Clear();
                                _timeOutCount = 0;
                            }

                            Thread.Sleep(20);
                        }
                    }
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }

                _loggedIn = false;

                if (_keepAliveTimer != null && !_keepAliveTimer.Disposed)
                {
                    _keepAliveTimer.Stop();
                    _keepAliveTimer.Dispose();
                    _keepAliveTimer = null;
                }

                if (_client != null && _client.IsConnected)
                {
                    _client.Dispose();
                    _client = null;
                }

                CloudLog.Notice("{0} Disconnected from {1}", GetType().Name, _address);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e, "Error in {0}.SshCommsProcess()", GetType().Name);
            }

            ConnectionStatus = ClientStatus.Disconnected;

            if (!_reconnect || !_programRunning)
            {
                return null;
            }

            Thread.Sleep(1000);

            CloudLog.Notice("Attempting reconnect to Tesira at {0}", _address);
            ConnectionStatus = ClientStatus.AttemptingConnection;

            Connect();

            return null;
        }
    }

    public delegate void TtpSshClientConnectionStatusChangedHandler(TtpSshClient client, TtpSshClient.ClientStatus status);
    public delegate void TtpSshClientReceivedDataEventHandler(TtpSshClient client, TesiraMessage message);
}