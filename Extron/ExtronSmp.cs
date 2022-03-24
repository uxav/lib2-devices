using System;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Extron
{
    public class ExtronSmp
    {
        private readonly ExtronSocket _socket;
        private eRecordStatus _recordStatus;
        private CTimer _pollTimer;
        private eStorageType _storageLocation;
        private long _bytesAvailable;
        private TimeSpan _timer;
        private TimeSpan _timeAvailable;

        public ExtronSmp(string ipAddress, string password)
        {
            _socket = new ExtronSocket(ipAddress, 23, password);
            _socket.ReceivedString += SocketOnReceivedData;
            _socket.StatusChanged += SocketOnStatusChanged;
        }

        public event ExtronSmpStatusUpdatedEventHandler StatusUpdated;

        public bool Connected
        {
            get { return _socket.Connected; }
        }

        private void SocketOnReceivedData(string receivedString)
        {
            try
            {
                //Debug.WriteSuccess("Extron Rx", receivedString);

                var match = Regex.Match(receivedString, @"RcdrY(\d+)");
                if (match.Success)
                {
                    return;
                }

                match = Regex.Match(receivedString,
                    @"<ChA(\d)\*ChB(\d)>\*<(\w+)>\*<(\w+)(?:\*(.+))?>\*<(\d+)(?:\*(.+))?>\*<([\d:]+)>\*<([\d:]+)(?:\*([\d:]+))?>");
                if (match.Success)
                {
                    try
                    {
                        RecordStatus = (eRecordStatus) Enum.Parse(typeof (eRecordStatus), match.Groups[3].Value, true);
                    }
                    catch (Exception e)
                    {
                        //CloudLog.Error(e.Message + " value = " + match.Groups[3].Value);
                    }
                    try
                    {
                        _storageLocation = (eStorageType) Enum.Parse(typeof (eStorageType), match.Groups[4].Value, true);
                    }
                    catch
                    {
                        _storageLocation = eStorageType.Unknown;
                    }
                    _bytesAvailable = long.Parse(match.Groups[6].Value)*1000;
                    _timer = TimeFromString(match.Groups[8].Value);
                    _timeAvailable = TimeFromString(match.Groups[9].Value);
                    OnStatusUpdated(this);
                }
                else
                {
                    CloudLog.Warn("No match for received string: \"{0}\"", receivedString);
                }
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        private static TimeSpan TimeFromString(string timeString)
        {
            var parts = timeString.Split(':');
            var time = TimeSpan.FromHours(int.Parse(parts[0]));
            time = time + TimeSpan.FromMinutes(int.Parse(parts[1]));
            time = time + TimeSpan.FromSeconds(int.Parse(parts[2]));
            return time;
        }

        public eStorageType StorageLocation
        {
            get { return _storageLocation; }
        }

        public TimeSpan Timer
        {
            get { return _timer; }
        }

        public TimeSpan TimeAvailable
        {
            get { return _timeAvailable; }
        }

        public long BytesAvailable
        {
            get { return _bytesAvailable; }
        }

        private void SocketOnStatusChanged(TCPClientSocketBase client, SocketStatusEventType eventType)
        {
            switch (eventType)
            {
                case SocketStatusEventType.Connected:
                    _pollTimer = new CTimer(item => PollRecordStatus(), null, 5000, 1000);
                    break;
                case SocketStatusEventType.Disconnected:
                    if (_pollTimer != null)
                    {
                        _pollTimer.Dispose();
                        _pollTimer = null;
                    }
                    break;
            }
        }

        protected virtual void OnStatusUpdated(ExtronSmp device)
        {
            var handler = StatusUpdated;
            if (handler == null) return;
            try
            {
                handler(device);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        public void PollRecordStatus()
        {
            _socket.Send("I");
        }

        public void Record()
        {
            _socket.Send("\x1bY1RCDR\r");
            _socket.Send("I");
        }

        public void Stop()
        {
            _socket.Send("\x1bY0RCDR\r");
            _socket.Send("I");
        }

        public void Pause()
        {
            _socket.Send("\x1bY2RCDR\r");
            _socket.Send("I");
        }

        public void SelectInput(uint input, uint output)
        {
            _socket.Send(string.Format("{0}*{1}!", input, output));
        }

        public void RecallPreset(uint preset)
        {
            _socket.Send(string.Format("8*{0}.", preset));
        }

        public void RecallPresetDualChannelMode(uint preset)
        {
            _socket.Send(string.Format("9*3*{0}.", preset));
        }

        public enum eChannel
        {
            ChannelA = 1,
            ChannelB = 2
        }

        public eRecordStatus RecordStatus
        {
            get { return _recordStatus; }
            private set
            {
                if (_recordStatus == value) return;

                _recordStatus = value;

                OnStatusUpdated(this);
            }
        }

        public string IpAddress
        {
            get { return _socket.HostAddress; }
        }

        public enum eRecordStatus
        {
            Stopped,
            Setup,
            Recording,
            Paused
        }

        public enum eStorageType
        {
            Unknown,
            Auto,
            Internal,
            UsbFront,
            UsbRear,
            UsbRcp
        }

        public void Initialize()
        {
            _socket.Connect();
        }
    }

    public delegate void ExtronSmpStatusUpdatedEventHandler(ExtronSmp device);
}