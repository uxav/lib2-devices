 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Audio.Yamaha
{
    public class YamahaDesk
    {
        private readonly YamahaDeskSocket _socket;

        private readonly Dictionary<string, string[]> _parameterInfo =
            new Dictionary<string, string[]>();

        private readonly Dictionary<string, SnapshotCollection> _snapshots =
            new Dictionary<string, SnapshotCollection>();

        private readonly Dictionary<string, SnapshotCollection> _parameters =
            new Dictionary<string, SnapshotCollection>();

        private int _numberOfParams;
        private int _numberOfScenes;
        private int _currentSceneNumber;
        private SceneCollection _scenes;
        private bool _currentSceneModified;

        public YamahaDesk(string deviceAddress)
        {
            _socket = new YamahaDeskSocket(deviceAddress);
            _socket.StatusChanged += SocketOnStatusChanged;
            _socket.ReceivedData += SocketOnReceivedData;
        }

        public event CurrentSceneChangeEventHandler CurrentSceneChange;

        public SceneCollection Scenes
        {
            get { return _scenes; }
        }

        public int CurrentSceneNumber
        {
            get { return _currentSceneNumber; }
        }

        public bool CurrentSceneModified
        {
            get { return _currentSceneModified; }
        }

        public void Send(string stringToSend)
        {
            _socket.Send(stringToSend);
        }

        public void Send(string formattedString, params object[] args)
        {
            _socket.Send(string.Format(formattedString, args));
        }

        private void SocketOnReceivedData(string dataString)
        {
            var elements = (from Match m in Regex.Matches(dataString, "(['\"])((?:\\\\\\1|.)*?)\\1|([^\\s\"']+)")
                select m.Groups[1].Length > 0 ? m.Groups[2].Value : m.Groups[3].Value).ToList();

            if(!elements.Any()) return;

            var data = elements.Skip(1);

            switch (elements.First())
            {
                case "OK":
                    OnOkResponse(data);
                    break;
                case "NOTIFY":
                    OnNotify(data);
                    break;
                case "ERROR":
                    OnError(data);
                    break;
            }
        }

        private void OnOkResponse(IEnumerable<string> data)
        {
            try
            {
                var enumerable = data as IList<string> ?? data.ToList();
                var command = enumerable.First();
                var address = enumerable.Skip(1).First();
                var values = enumerable.Skip(2).ToArray();
                var valueString = string.Empty;
                foreach (var d in values)
                {
                    if (d.Length == 0)
                    {
                        valueString = valueString + " \"\"";
                    }

                    else if (d.Contains(" "))
                    {
                        valueString = valueString + " \"" + d + "\"";
                    }

                    else
                    {
                        valueString = valueString + " " + d;
                    }
                }

                if (valueString.Length > 0)
                {
                    Debug.WriteSuccess("OK", "command: \"{0}\", address: \"{1}\", values:{2}", command, address,
                        valueString);
                }
                else
                {
                    Debug.WriteSuccess("OK", "command: \"{0}\", value: ", command, address);
                }

                switch (command)
                {
                    case "prmnum":
                        _numberOfParams = int.Parse(address);
                        _parameterInfo.Clear();
                        for (var index = 0; index < _numberOfParams; index++)
                        {
                            Send("prminfo {0}", index);
                        }
                        break;
                    case "prminfo":
                        var name = values[0];
                        var newValues = values.Skip(1).ToArray();
                        _parameterInfo[name] = newValues;
#if DEBUG
                        Debug.WriteInfo("New param info", "{0}: {1}", name, string.Join(", ", newValues));
#endif
                        if (_parameterInfo.Count == _numberOfParams)
                        {
                            InternalRegisterParameters();
                        }
                        break;
                    case "ssnum_ex":
                        var count = int.Parse(values[0]);

                        if (address == "MIXER:Lib/Scene")
                        {
                            _numberOfScenes = count;
                        }

                        if (_snapshots.ContainsKey(address))
                        {
                            for (var index = 0; index < count; index++)
                            {
                                Send("ssinfo_ex {0} {1}", address, index);
                            }
                        }
                        break;
                    case "sscurrent_ex":
                        _currentSceneNumber = int.Parse(values[0]);
                        _currentSceneModified = values[1] == "modified";
                        OnCurrentSceneChange(this, _currentSceneNumber);
                        break;
                    case "ssinfo_ex":
                        var paramIndex = int.Parse(values[0]);
                        if (_snapshots.ContainsKey(address))
                        {
                            if (_snapshots[address].ContainsItemAtIndex(paramIndex))
                            {
                                _snapshots[address].Update(paramIndex, values.Skip(1).ToArray());
                            }
                            else
                            {
                                _snapshots[address].Add(paramIndex, values.Skip(1).ToArray());
                            }

                            if (address == "MIXER:Lib/Scene" && _snapshots[address].Count() == _numberOfScenes)
                            {
                                Debug.WriteSuccess("Scene collection created!");
                                _scenes = new SceneCollection(this, _snapshots[address]);
                            }
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        private void OnNotify(IEnumerable<string> data)
        {
            try
            {
                var enumerable = data as IList<string> ?? data.ToList();
                var dataString = enumerable.First();
                foreach (var d in enumerable.Skip(1))
                {
                    dataString = dataString + " " + d;
                }
                Debug.WriteWarn("Notify", dataString);

                var command = enumerable.First();
                var address = enumerable.Skip(1).First();
                var values = enumerable.Skip(2).ToArray();

                switch (command)
                {
                    case "ssupdate_ex":
                        if (address == "MIXER:Lib/Scene")
                        {
                            var number = int.Parse(values.First());
                            Send("ssinfo_ex {0} {1}", address, number);
                        }
                        break;
                    case "sscurrent_ex":
                        if (address == "MIXER:Lib/Scene")
                        {
                            GetCurrentScene();
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        private void OnError(IEnumerable<string> data)
        {
            var enumerable = data as IList<string> ?? data.ToList();
            var dataString = enumerable.First();
            foreach (var d in enumerable.Skip(1))
            {
                dataString = dataString + " " + d;
            }
            Debug.WriteError("Error", dataString);
        }

        protected virtual void OnCurrentSceneChange(YamahaDesk desk, int scenenumber)
        {
            var handler = CurrentSceneChange;
            if (handler != null) handler(desk, scenenumber);
        }

        private void SocketOnStatusChanged(TCPClientSocketBase socket, SocketStatusEventType eventType)
        {
            switch (eventType)
            {
                case SocketStatusEventType.Connected:
                    //Send("scpmode keepalive 10000");
                    Send("devinfo protocolver");
                    Send("prmnum");
                    break;
                case SocketStatusEventType.Disconnected:

                    break;
            }
        }

        public void RegisterParameter(string paramName)
        {
            if (_parameterInfo.ContainsKey(paramName))
            {
                try
                {
                    var xTotal = int.Parse(_parameterInfo[paramName][0]);
                    var yTotal = int.Parse(_parameterInfo[paramName][1]);
                    for (int x = 0; x < xTotal; x++)
                    {
                        for (int y = 0; y < yTotal; y++)
                        {
                            Send("get {0} {1} {2}", paramName, x, y);
                        }
                    }
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }
            else
            {
                throw new Exception(string.Format("Unknown parameter \"{0}\"", paramName));
            }
        }

        public void RegisterSnapshotItem(string snapshotName)
        {
            if (_snapshots.ContainsKey(snapshotName))
            {
                throw new Exception(string.Format("Snapshot \"{0}\" already registered", snapshotName));
            }

            _snapshots[snapshotName] = new SnapshotCollection(this, snapshotName);

            Send("ssnum_ex {0}", snapshotName);
        }

        public void GetCurrentScene()
        {
            Send("sscurrent_ex MIXER:Lib/Scene");            
        }

        private void InternalRegisterParameters()
        {
            _snapshots.Clear();
            GetCurrentScene();
            RegisterSnapshotItem("MIXER:Lib/Scene");
            RegisterParameter("MIXER:Current/InCh/Fader/Level");
        }

        public void Initialize()
        {
            _socket.Connect();
        }
    }

    public delegate void CurrentSceneChangeEventHandler(YamahaDesk desk, int sceneNumber);
}