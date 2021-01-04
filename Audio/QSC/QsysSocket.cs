using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.CrestronSockets;
using Crestron.SimplSharpPro.CrestronThread;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Audio.QSC
{
    public class QsysSocket
    {
        private readonly IList<string> _addresses;
        private readonly string _name;
        private readonly TCPClient _client;
        private int _nextId = 1;
        private CTimer _pollTimer;
        private Thread _thread;
        private bool _remainConnected;
        private EthernetAdapterType _adapterType;
        private readonly Dictionary<int, QsysRequest> _requests = new Dictionary<int, QsysRequest>();
        private string _currentAddress;

        internal QsysSocket(IList<string> addresses, int port, string name)
        {
            _addresses = addresses;
            _name = name;
            _currentAddress = addresses[0];
            _client = new TCPClient(_currentAddress, port, 8192) { Nagle = true };
            _client.SocketStatusChange += OnStatusChanged;
            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type == eProgramStatusEventType.Stopping) Disconnect();
            };
            CrestronEnvironment.EthernetEventHandler += args =>
            {
                if (args.EthernetAdapter != _adapterType) return;
                switch (args.EthernetEventType)
                {
                    case eEthernetEventType.LinkDown:
                        _client.HandleLinkLoss();
                        break;
                    case eEthernetEventType.LinkUp:
                        _client.HandleLinkUp();
                        break;
                }
            };
        }

        public IList<string> Addresses
        {
            get { return _addresses; }
        }

        public bool Connected
        {
            get { return _client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED; }
        }

        public void Connect()
        {
            if(_thread == null || _thread.ThreadState != Thread.eThreadStates.ThreadFinished)
            _remainConnected = true;
            _thread = new Thread(ConnectionThreadProcess, null)
            {
                Priority = Thread.eThreadPriority.HighPriority,
                Name = string.Format("{0} Handler Thread", GetType().Name)
            };
        }

        public void Disconnect()
        {
            Disconnect(false);
        }

        public void Disconnect(bool andReconnect)
        {
            _remainConnected = andReconnect;
            if (_client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                _client.DisconnectFromServer();
            }
        }

        public void TryAnotherAddress()
        {
            Debug.WriteInfo(GetType().Name, "Current address set to {0}, looking for another", _currentAddress);

            if (_addresses.Count > 1)
            {
                var otherAddress = _addresses.FirstOrDefault(a => a != _currentAddress);
                if (otherAddress != null)
                {
                    Debug.WriteInfo(GetType().Name, "Changing to ", otherAddress);
                    _currentAddress = otherAddress;
                    Debug.WriteWarn("Will try Qsys at " + _currentAddress + " instead!");
                }
            }
            else
            {
                Debug.WriteWarn(GetType().Name, "No other addresses");
            }
        }

        public void SendRequest(QsysRequest request)
        {
            if (request.Id <= 0) return;
            _requests[request.Id] = request;
            var content = request.ToString();
#if DEBUG
            Debug.WriteInfo(_name + " Tx", Debug.AnsiPurple + content + Debug.AnsiReset);
#endif
            Send(content);
        }

        private void Send(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data + "\0");
            _client.SendData(bytes, 0, bytes.Length);
        }

        public int GetNextId()
        {
            return _nextId++;
        }

        private void DoNothing()
        {
            Send(JsonConvert.SerializeObject(new
            {
                @jsonrpc = "2.0",
                @method = "NoOp",
                @params = new {}
            }));
        }

        protected object ConnectionThreadProcess(object o)
        {
            var memoryStream = new MemoryStream();
            var reader = new StreamReader(memoryStream);
            var receivedAnything = false;

            while (true)
            {
                var connectCount = 0;
                while (_remainConnected && !Connected)
                {
                    connectCount ++;
                    _client.AddressClientConnectedTo = _currentAddress;
                    Debug.WriteInfo(GetType().Name, "Address to connect to set to {0}", _currentAddress);
                    var result = _client.ConnectToServer();
                    if (result == SocketErrorCodes.SOCKET_OK)
                    {
                        CloudLog.Notice("{0} connected to {1}", GetType().Name, _client.AddressClientConnectedTo);
                        receivedAnything = false;
                        break;
                    }

                    TryAnotherAddress();

                    if (connectCount <= 4 || connectCount > 10) continue;

                    if (connectCount == 10)
                    {
                        CloudLog.Error("{0} failed to connect to any address, will keep trying in background",
                            GetType().Name);
                    }
                    else
                    {
                        CloudLog.Warn("{0} cannot connect to address: {1}", GetType().Name, _currentAddress);
                    }

                    CrestronEnvironment.AllowOtherAppsToRun();
                }

                _pollTimer = new CTimer(specific => DoNothing(), null, 30000, 30000);

                _adapterType = _client.EthernetAdapter;

                while (true)
                {
                    var dataCount = _client.ReceiveData();

                    if (dataCount <= 0)
                    {
                        Debug.WriteWarn(GetType().Name , "Disconnected!");
                        _pollTimer.Stop();
                        _pollTimer.Dispose();
                        _pollTimer = null;

                        if (_remainConnected)
                        {
                            if (!receivedAnything)
                            {
                                CloudLog.Warn(
                                    "{0} connected but didn't receive anything." +
                                    "Will wait for 1 minute before reconnection attempt. Upgrade may be in progress.",
                                    GetType().Name);
                                Thread.Sleep(60000);
                            }
                            break;
                        }

                        Debug.WriteWarn("Exiting Thread", Thread.CurrentThread.Name);
                        return null;
                    }

                    receivedAnything = true;
#if DEBUG
                    Debug.WriteInfo(GetType().Name, "{0} bytes in buffer", dataCount);
#endif
                    for (var i = 0; i < dataCount; i++)
                    {
                        var b = _client.IncomingDataBuffer[i];
                        if (b != 0)
                        {
                            memoryStream.WriteByte(b);
                            continue;
                        }

                        memoryStream.Position = 0;
                        try
                        {
                            
                            var data = JToken.Parse(reader.ReadToEnd());
                            memoryStream.SetLength(0);
#if DEBUG
                            Debug.WriteInfo(_name + " Rx",
                                Debug.AnsiBlue + data.ToString(Formatting.None) + Debug.AnsiReset);
#endif
                            if (data["method"] != null)
                                OnRequestReceived(this, new QsysRequest(data));
                            else if (data["id"] != null)
                            {
                                var id = data["id"].Value<int>();
                                if (_requests.ContainsKey(id))
                                {
                                    var request = _requests[id];
                                    _requests.Remove(id);
                                    OnResponseReceived(this, new QsysResponse(data, request));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            CloudLog.Exception("Error occured processing a complete data parcel from Core", e);
                        }
                    }

                    CrestronEnvironment.AllowOtherAppsToRun();
                    Thread.Sleep(0);
                }
            }
        }

        public event TCPClientSocketStatusChangeEventHandler StatusChanged;
        public event QsysSocketRequestReceivedHandler RequestReceived;
        public event QsysSocketResponseReceivedHandler ResponseReceived;

        protected virtual void OnStatusChanged(TCPClient mytcpclient, SocketStatus clientsocketstatus)
        {
            var handler = StatusChanged;
            if (handler != null)
            {
                try
                {
#if DEBUG
                    if (clientsocketstatus == SocketStatus.SOCKET_STATUS_CONNECTED)
                    {
                        Debug.WriteSuccess(GetType().Name + " Connected", "IP: " + mytcpclient.AddressClientConnectedTo);
                    }
                    else
                    {
                        Debug.WriteWarn(GetType().Name, "Socket status = " + clientsocketstatus);
                    }
#endif
                    handler(mytcpclient, clientsocketstatus);
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }
        }

        protected virtual void OnRequestReceived(QsysSocket socket, QsysRequest request)
        {
#if DEBUG
            //Debug.WriteSuccess(string.Format("{0}.OnRequestReceived()", GetType().Name));
            //Debug.WriteNormal(Debug.AnsiBlue + request + Debug.AnsiReset);
#endif
            var handler = RequestReceived;
            if (handler != null) handler(socket, request);
        }

        protected virtual void OnResponseReceived(QsysSocket socket, QsysResponse response)
        {
#if DEBUG
            //Debug.WriteSuccess(string.Format("{0}.OnResponseReceived()", GetType().Name));
            //Debug.WriteNormal(Debug.AnsiBlue + response + Debug.AnsiReset);
#endif
            var handler = ResponseReceived;
            if (handler != null) handler(socket, response);
        }
    }

    public delegate void QsysSocketRequestReceivedHandler(QsysSocket socket, QsysRequest request);

    public delegate void QsysSocketResponseReceivedHandler(QsysSocket socket, QsysResponse response);
}