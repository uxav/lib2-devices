using System;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using Crestron.SimplSharpPro.CrestronThread;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.ZeeVee
{
    public class ZeeVeeServerSocket
    {
        #region Fields

        private readonly TCPClient _client;
        private Thread _thread;
        private bool _remainConnected;
        private EthernetAdapterType _adapterType;
        private CTimer _pollTimer;

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        internal ZeeVeeServerSocket(string address, int port)
        {
            _client = new TCPClient(address, port, 100000) { Nagle = true };

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

#if DEBUG
            CrestronConsole.AddNewConsoleCommand(Send, "ZVSend", "Send a command to the ZeeVee socket",
                ConsoleAccessLevelEnum.AccessOperator);
#endif
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        public event ZeeVeeServerSocketReceiveDataHandler ReceivedData;

        #region Properties

        public bool Connected
        {
            get { return _client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED; }
        }

        #endregion

        #region Methods

        public void Connect()
        {
            if (_thread == null || _thread.ThreadState != Thread.eThreadStates.ThreadFinished)
                _remainConnected = true;
            _thread = new Thread(ConnectionThreadProcess, null)
            {
                Priority = Thread.eThreadPriority.MediumPriority,
                Name = string.Format("{0} Handler Thread", GetType().Name)
            };
        }

        public void Disconnect()
        {
            _remainConnected = false;
            if (_client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
                _client.DisconnectFromServer();
        }

        public void Send(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data + "\x0a");
            _client.SendData(bytes, 0, bytes.Length);
        }

        protected object ConnectionThreadProcess(object o)
        {
            var index = 0;
            var bytes = new byte[_client.IncomingDataBuffer.Length];

            try
            {
                while (true)
                {
                    var connectCount = 0;
                    while (_remainConnected && !Connected)
                    {
                        connectCount ++;
                        var result = _client.ConnectToServer();
                        if (result == SocketErrorCodes.SOCKET_OK)
                        {
                            CloudLog.Notice("{0} connected to {1}", GetType().Name, _client.AddressClientConnectedTo);
                            break;
                        }

                        if (connectCount <= 2 || connectCount > 5) continue;
                        if (connectCount == 5)
                            CloudLog.Error("{0} failed to connect to address: {1}, will keep trying in background",
                                GetType().Name, _client.AddressClientConnectedTo);
                        else
                            CloudLog.Warn("{0} cannot connect to address: {1}", GetType().Name,
                                _client.AddressClientConnectedTo);
                        CrestronEnvironment.AllowOtherAppsToRun();
                    }

                    _pollTimer = new CTimer(specific => Send("show device status all"), null, 1000, 30000);

                    _adapterType = _client.EthernetAdapter;

                    while (true)
                    {
                        var dataCount = _client.ReceiveData();

                        if (dataCount <= 0)
                        {
                            CloudLog.Debug("{0} Disconnected!", GetType().Name);
                            _pollTimer.Stop();
                            _pollTimer.Dispose();

                            if (_remainConnected)
                                break;

                            CloudLog.Debug("Exiting {0}", Thread.CurrentThread.Name);
                            return null;
                        }

                        var promptReceived = false;

                        if (dataCount >= 7)
                        {
                            var lastBytes = new byte[7];
                            Array.Copy(_client.IncomingDataBuffer, dataCount - 7, lastBytes, 0, 7);
                            var endString = Encoding.UTF8.GetString(lastBytes, 0, lastBytes.Length);
                            if (endString == "Zyper$ ")
                            {
                                promptReceived = true;
                            }
                        }
#if DEBUG
                        //CrestronConsole.PrintLine("{0} {1} bytes in buffer, promptReceived = {2}", GetType().Name, dataCount,
                        //    promptReceived);

                        //Tools.PrintBytes(_client.IncomingDataBuffer, 0, dataCount, true);
#endif
                        for (var i = 0; i < dataCount; i++)
                        {
                            bytes[index] = _client.IncomingDataBuffer[i];
                            index ++;
                        }

                        if (promptReceived)
                        {
                            if (bytes[0] == 0xFF)
                            {
#if DEBUG
                                var mode = 0;
                                foreach (var b in bytes)
                                {
                                    if (b == 0xFF)
                                    {
                                        mode = 0;
                                        CrestronConsole.Print("ZeeVee Socket Telnet Command Received:");
                                        continue;
                                    }
                                    if (mode == 0)
                                    {
                                        switch (b)
                                        {
                                            case 254:
                                                CrestronConsole.Print(" DONT");
                                                break;
                                            case 253:
                                                CrestronConsole.Print(" DO");
                                                break;
                                            case 252:
                                                CrestronConsole.Print(" WONT");
                                                break;
                                            case 251:
                                                CrestronConsole.Print(" WILL");
                                                break;
                                            default:
                                                CrestronConsole.Print(" {0}", b);
                                                break;
                                        }
                                        mode = 1;
                                        continue;
                                    }
                                    if (mode != 1) continue;
                                    switch (b)
                                    {
                                        case 1:
                                            CrestronConsole.PrintLine(" Echo");
                                            break;
                                        case 3:
                                            CrestronConsole.PrintLine(" Suppress Go Ahead");
                                            break;
                                        case 5:
                                            CrestronConsole.PrintLine(" Status");
                                            break;
                                        case 6:
                                            CrestronConsole.PrintLine(" Timing Mark");
                                            break;
                                        case 24:
                                            CrestronConsole.PrintLine(" Terminal Type");
                                            break;
                                        case 31:
                                            CrestronConsole.PrintLine(" Window Size");
                                            break;
                                        case 32:
                                            CrestronConsole.PrintLine(" Terminal Speed");
                                            break;
                                        case 33:
                                            CrestronConsole.PrintLine(" Remote Flow Control");
                                            break;
                                        case 34:
                                            CrestronConsole.PrintLine(" Linemode");
                                            break;
                                        case 36:
                                            CrestronConsole.PrintLine(" Environment Variables");
                                            break;
                                        default:
                                            CrestronConsole.PrintLine(" {0}", b);
                                            break;
                                    }
                                    mode = 2;
                                }
#endif
                                index = 0;
                            }
                            else
                            {
                                var data = Encoding.UTF8.GetString(bytes, 0, index - 7);
#if DEBUG
                                CrestronConsole.PrintLine("Received response from ZeeVee:\r\n{0}", data);
#endif
                                OnReceivedData(this, data);
                                index = 0;
                            }
                        }

                        CrestronEnvironment.AllowOtherAppsToRun();
                        Thread.Sleep(0);
                    }
                }
            }
            catch (Exception e)
            {
                CloudLog.Exception(string.Format("Error in {0}, {1}", Thread.CurrentThread.Name, e.Message), e);
                CrestronConsole.PrintLine("Error in {0}", Thread.CurrentThread.Name);
                CrestronConsole.PrintLine("Index = {0}", index);
                CrestronConsole.Print("Bytes: ");
                Tools.PrintBytes(bytes, 0, index, true);
                if(_pollTimer != null && !_pollTimer.Disposed)
                {
                    _pollTimer.Stop();
                    _pollTimer.Dispose();
                }
                if (_client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
                    _client.DisconnectFromServer();
                return null;
            }
        }

        protected virtual void OnReceivedData(ZeeVeeServerSocket socket, string data)
        {
            try
            {
                var handler = ReceivedData;
                if (handler != null) handler(socket, data);
            }
            catch (Exception e)
            {
                CloudLog.Exception("Error raising data received event for ZeeVee", e);
            }
        }

        #endregion
    }

    public delegate void ZeeVeeServerSocketReceiveDataHandler(ZeeVeeServerSocket socket, string data);
}