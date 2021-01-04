using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DM;
using Crestron.SimplSharpPro.DM.Endpoints.Receivers;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.DeviceSupport;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Lightware3
{
    public class LightwareMatrix : ISwitcher
    {
        private readonly LightwareSocket _socket;
        private readonly Dictionary<uint, LightwareInput> _inputs = new Dictionary<uint, LightwareInput>();
        private readonly Dictionary<uint, LightwareOutput> _outputs = new Dictionary<uint, LightwareOutput>();
        private string _modelName = "Unknown Matrix";
        private string _versionInfo = string.Empty;
        private string _serialNumber;
        private bool _deviceCommunicating;

        public LightwareMatrix(string address)
        {
            _socket = new LightwareSocket(address);
            _socket.StatusChanged += SocketOnStatusChanged;
            _socket.ReceivedString += SocketOnReceivedString;
        }

        public string Name
        {
            get { return "Lightware " + _modelName; }
        }

        public string ManufacturerName { get { return "Lightware"; } }

        public string ModelName
        {
            get { return _modelName; }
        }

        public string DiagnosticsName
        {
            get { return ManufacturerName + " " + ModelName + " (" + DeviceAddressString + ")"; }
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
                    DeviceCommunicatingChange(this, value);
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }
        }

        public string DeviceAddressString
        {
            get { return _socket.HostAddress; }
        }

        public string SerialNumber
        {
            get { return _serialNumber; }
        }

        public string VersionInfo
        {
            get { return _versionInfo; }
            private set { _versionInfo = value; }
        }

        public FusionAssetType AssetType
        {
            get { return FusionAssetType.VideoSwitcher; }
        }

        public ReadOnlyDictionary<uint, LightwareInput> Inputs
        {
            get { return new ReadOnlyDictionary<uint, LightwareInput>(_inputs); }
        }

        public ReadOnlyDictionary<uint, LightwareOutput> Outputs
        {
            get { return new ReadOnlyDictionary<uint, LightwareOutput>(_outputs); }
        }

        public event DeviceCommunicatingChangeHandler DeviceCommunicatingChange;

        public event SwitcherInputStatusChangedEventHandler InputStatusChanged;

        public event RoutingChangedEventHandler RoutingChanged;

        public void Send(string stringToSend)
        {
            _socket.Send(stringToSend);
        }

        public void Send(string formatString, params object[] args)
        {
            _socket.Send(string.Format(formatString, args));
        }

        private void SocketOnStatusChanged(TCPClientSocketBase socket, SocketStatusEventType eventType)
        {
            switch (eventType)
            {
                case SocketStatusEventType.Connected:
                    Send("GET /MANAGEMENT/UID.*");
                    Send("GET /MEDIA/NAMES/VIDEO.*");
                    Send("GET /MEDIA/XP/VIDEO.*");
                    Send("OPEN /MEDIA/NAMES/VIDEO");
                    Send("OPEN /MEDIA/XP/VIDEO");
                    break;
                case SocketStatusEventType.Disconnected:
                    DeviceCommunicating = false;
                    break;
            }
        }

        private void SocketOnReceivedString(string receivedString)
        {
            DeviceCommunicating = true;
            var match = Regex.Match(receivedString, @"^([\w-]+) ([\w/]+)(?:(\.|:| )(.+))?$");
            if (!match.Success)
            {
                CloudLog.Error("Error processing line from lightware: \"{0}\"", receivedString);
                return;
            }
            var prefix = match.Groups[1].Value;
            var path = match.Groups[2].Value;
            var context = match.Groups[4].Value;
            Debug.WriteNormal(Debug.AnsiBlue + prefix + " " + Debug.AnsiPurple + path + Debug.AnsiCyan +
                              match.Groups[3].Value + Debug.AnsiGreen + context + Debug.AnsiReset);

            switch (prefix)
            {
                case "CHG":
                    OnReceiveChange(path, context);
                    break;
                case "pr":
                    OnReceiveProperty(path, context, true);
                    break;
                case "pw":
                    OnReceiveProperty(path, context, false);
                    break;
            }
        }

        private void OnReceiveChange(string path, string context)
        {
            Match match;
            switch (path)
            {
                case "/MEDIA/NAMES/VIDEO":
                    match = Regex.Match(context, @"^(I|O)(\d+)=(\d+);(.*)$");
                    var io = uint.Parse(match.Groups[2].Value);
                    switch (match.Groups[1].Value)
                    {
                        case "I":
                            if (_inputs.ContainsKey(io))
                            {
                                _inputs[io].Name = match.Groups[4].Value;
                            }
                            break;
                        case "O":
                            if (_outputs.ContainsKey(io))
                            {
                                _outputs[io].Name = match.Groups[4].Value;
                            }
                            break;
                    }
                    break;
                case "/MEDIA/XP/VIDEO":
                    match = Regex.Match(context, @"(\w+)=(.+)");
                    if (match.Success)
                    {
                        UpdateInputStatus(match.Groups[1].Value,
                            (from Match value in Regex.Matches(match.Groups[2].Value, @"([^;]+);")
                             select value.Groups[1].Value));
                    }
                    break;
            }
        }

        private void OnReceiveProperty(string path, string context, bool isReadOnly)
        {
            Match match;
            switch (path)
            {
                case "/MEDIA/NAMES/VIDEO":
                    match = Regex.Match(context, @"^(I|O)(\d+)=(\d+);(.*)$");
                    var io = uint.Parse(match.Groups[2].Value);
                    switch (match.Groups[1].Value)
                    {
                        case "I":
                            if (!_inputs.ContainsKey(io))
                            {
                                _inputs[io] = new LightwareInput(this, io) {Name = match.Groups[4].Value};
                            }
                            break;
                        case "O":
                            if (!_outputs.ContainsKey(io))
                            {
                                _outputs[io] = new LightwareOutput(this, io) {Name = match.Groups[4].Value};
                            }
                            break;
                    }
                    break;
                case "/MEDIA/XP/VIDEO":
                    match = Regex.Match(context, @"(\w+)=(.+)");
                    if (match.Success)
                    {
                        UpdateInputStatus(match.Groups[1].Value,
                            (from Match value in Regex.Matches(match.Groups[2].Value, @"([^;]+);")
                                select value.Groups[1].Value));
                    }
                    break;
                case "/MANAGEMENT/UID":
                    match = Regex.Match(context, @"(\w+)=(.*)");
                    if (match.Success)
                    {
                        switch (match.Groups[1].Value)
                        {
                            case "ProductName":
                                _modelName = match.Groups[2].Value;
                                break;
                            case "ProductSerialNumber":
                                _serialNumber = match.Groups[2].Value;
                                break;
                            case "FirmwareVersion":
                                _versionInfo = match.Groups[2].Value;
                                break;
                        }
                    }
                    break;
            }
        }

        private void UpdateInputStatus(string proptertyName, IEnumerable<string> values)
        {
            var inputsUpdated = new List<uint>();
            var outputsUpdated = new List<uint>();

            var io = 0U;
            foreach (var dataItem in values)
            {
                io ++;
                switch (proptertyName)
                {
                    case "DestinationConnectionStatus":
                        var input = uint.Parse(Regex.Match(dataItem, @"\d+").Value);
                        if (_outputs[io].UpdateVideoInputFeedback(_inputs.ContainsKey(input)
                            ? _inputs[input]
                            : null))
                        {
                            outputsUpdated.Add(io);
                        }
                        break;
                    case "SourcePortStatus":
                        if(dataItem.Length != 3) return;
                        var locked = dataItem[0] == 'L' || dataItem[0] == 'U';
                        var muted = dataItem[0] == 'M' || dataItem[0] == 'U';
                        var b = (byte) int.Parse(dataItem.Substring(1, 2), NumberStyles.HexNumber);
                        var embeddedAudio = GetBit(b, 7) && GetBit(b, 6);
                        var encrypted = GetBit(b, 5) && GetBit(b, 4);
                        var signalPresent = GetBit(b, 3) && GetBit(b, 2);
                        var connected = GetBit(b, 1) && GetBit(b, 0);
                        if (_inputs.ContainsKey(io) &&
                            _inputs[io].UpdateStatusFeedback(locked, muted, embeddedAudio, encrypted, signalPresent,
                                connected))
                        {
                            inputsUpdated.Add(io);
                        }
                        break;
                }
            }

            foreach (var i in inputsUpdated)
            {
                //Debug.WriteInfo(_inputs[i].ToString(), "Status changed, connected = {0}, signal = {1}",
                //    _inputs[i].Connected, _inputs[i].SignalPresent);
                try
                {
                    if (InputStatusChanged == null) continue;
                    InputStatusChanged(this, new SwitcherInputStatusChangeEventArgs(this, i));
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);
                }
            }

            foreach (var o in outputsUpdated)
            {
                Debug.WriteInfo(_outputs[o].ToString(), "Routing changed, Input = {0}", _outputs[o].VideoInput);
            }

            if (outputsUpdated.Any())
            {
                OnRoutingChanged(MediaType.Video,
                    Outputs.Where(o => outputsUpdated.Contains(o.Key)).Select(o => o.Value));
            }
        }

        protected virtual void OnRoutingChanged(MediaType type, IEnumerable<LightwareOutput> outputschanged)
        {
            var handler = RoutingChanged;
            if (handler != null) handler(this, type, outputschanged);
        }

        private bool GetBit(byte thebyte, int position)
        {
            return (1 == ((thebyte >> position) & 1));
        }

        public void Init()
        {
            _socket.Connect();
        }

        public void RouteVideo(uint input, uint output)
        {
            if (!_outputs.ContainsKey(output))
            {
                throw new ArgumentOutOfRangeException("output", "Output " + output + " not valid");
            }

            if (input > 0 && !_inputs.ContainsKey(input))
            {
                throw new ArgumentOutOfRangeException("input", "input " + input + " not valid");
            }

            if (input == 0)
            {
                _outputs[output].SetVideoInput(null);
            }
            else
            {
                _outputs[output].SetVideoInput(_inputs[input]);
            }
        }

        public void RouteAudio(uint input, uint output)
        {
            throw new NotImplementedException();
        }

        public uint GetVideoInput(uint output)
        {
            if (!_outputs.ContainsKey(output))
            {
                throw new ArgumentOutOfRangeException("output", "Output " + output + " not valid");
            }

            return _outputs[output].VideoInput != null ? _outputs[output].VideoInput.Number : 0;
        }

        public uint GetAudioInput(uint output)
        {
            throw new NotImplementedException();
        }

        public bool InputIsActive(uint input)
        {
            if (!_inputs.ContainsKey(input))
            {
                throw new ArgumentOutOfRangeException("input", "input " + input + " not valid");
            }

            return _inputs[input].SignalPresent;
        }

        public bool SupportsDMEndPoints
        {
            get { return false; }
        }

        public EndpointReceiverBase GetEndpointForOutput(uint output)
        {
            throw new NotImplementedException();
        }

        public HdmiInputWithCEC GetHdmiCecInput(uint input)
        {
            throw new NotImplementedException();
        }
    }

    public delegate void RoutingChangedEventHandler(
        LightwareMatrix matrix, MediaType type, IEnumerable<LightwareOutput> outputsChanged);

    public enum MediaType
    {
        Video,
        Audio
    }
}