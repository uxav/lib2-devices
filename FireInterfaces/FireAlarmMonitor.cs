using System;
using System.Collections.Generic;
using System.Text;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpProInternal;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.FireInterfaces
{
    public class FireAlarmMonitor
    {
        private readonly PortDevice _ioPort;
        private readonly FireAlarmServerSocket _socket;
        private bool _normalState;
        private bool _started;

        public FireAlarmMonitor(int port, int maxConnections, Versiport ioPort)
        {
            _ioPort = ioPort;
            ioPort.SetVersiportConfiguration(eVersiportConfiguration.DigitalInput);
            ioPort.VersiportChange += VersiportOnVersiportChange;
            _socket = new FireAlarmServerSocket(port, maxConnections);
            _socket.ClientConnected += SocketOnClientConnected;
        }

        public FireAlarmMonitor(int port, int maxConnections, DigitalInput digitalInput)
        {
            _ioPort = digitalInput;
            digitalInput.StateChange += DigitalInputOnStateChange;
            _socket = new FireAlarmServerSocket(port, maxConnections);
            _socket.ClientConnected += SocketOnClientConnected;
        }

        public event FireAlarmMonitorChangeEventHandler StateChanged;

        protected virtual void OnStateChanged(FireAlarmMonitor monitor, bool alertstate)
        {
            var handler = StateChanged;
            if (handler != null)
            {
                try
                {
                    handler(monitor, alertstate);
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }
        }

        public bool NormalState
        {
            get { return _normalState; }
        }

        public bool CurrentState
        {
            get
            {
                var v = _ioPort as Versiport;
                if (v != null)
                {
                    return v.DigitalIn;
                }
                return ((DigitalInput) _ioPort).State;
            }
        }

        public IEnumerable<string> Connections
        {
            get { return _socket.CurrentConnections; }
        }

        private void SocketOnClientConnected(TCPServerSocketBase socket, uint clientId)
        {
            var message = string.Format("firestate[{0}]\r\n", CurrentState == _normalState ? "NORMAL" : "ALERT");
            var bytes = Encoding.ASCII.GetBytes(message);
            _socket.Send(clientId, bytes, 0, bytes.Length);
        }

        private void VersiportOnVersiportChange(Versiport port, VersiportEventArgs args)
        {
            if (args.Event == eVersiportEvent.DigitalInChange)
            {
                CloudLog.Notice("Fire interface port state change: {0}", port.DigitalIn ? "Closed" : "Open");
            }
            else
            {
                return;
            }

            if (!_started) return;

            OnStateChanged(this, port.DigitalIn != _normalState);

            var message = string.Format("firestate[{0}]\r\n", port.DigitalIn == _normalState ? "NORMAL" : "ALERT");
            var bytes = Encoding.ASCII.GetBytes(message);
            _socket.SendToAll(bytes, 0, bytes.Length);
        }

        private void DigitalInputOnStateChange(DigitalInput digitalInput, DigitalInputEventArgs args)
        {
            CloudLog.Notice("Fire interface port state change: {0}", args.State ? "Closed" : "Open");

            if(!_started) return;

            OnStateChanged(this, args.State != _normalState);

            var message = string.Format("firestate[{0}]\r\n", args.State == _normalState ? "NORMAL" : "ALERT");
            var bytes = Encoding.ASCII.GetBytes(message);
            _socket.SendToAll(bytes, 0, bytes.Length);
        }

        public void Start()
        {
            if (_started)
            {
                throw new InvalidOperationException("Already started");
            }

            _started = true;
            var v = _ioPort as Versiport;
            if (v != null)
            {
                _normalState = v.DigitalIn;
            }
            else
            {
                var d = _ioPort as DigitalInput;
                if (d != null)
                {
                    _normalState = d.State;
                }
            }

            CloudLog.Notice("Fire interface normal state set initialized at {0}",
                _normalState ? "Closed" : "Open");

            _socket.Start();
        }

        public void Start(bool normalState)
        {
            if (_started)
            {
                throw new InvalidOperationException("Already started");
            }

            _started = true;
            _normalState = normalState;

            CloudLog.Notice("Fire interface normal state set initialized at {0}",
                _normalState ? "Closed" : "Open");

            _socket.Start();
        }

        public void Stop()
        {
            _started = false;
            _socket.Stop();
        }
    }

    public delegate void FireAlarmMonitorChangeEventHandler(FireAlarmMonitor monitor, bool alertState);
}