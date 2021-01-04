using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DM;
using Crestron.SimplSharpPro.DM.Endpoints.Receivers;
using UX.Lib2.DeviceSupport;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Lightware
{
    public class LightwareMatrix : ISwitcher, IEnumerable<LightwareOutput>
    {
        #region Fields

        private readonly LightwareSocket _socket;
        private readonly Dictionary<int, LightwareOutput> _outputs = new Dictionary<int, LightwareOutput>();
        private readonly Dictionary<int, LightwareInput> _inputs = new Dictionary<int, LightwareInput>(); 

        #endregion

        #region Constructors

        public LightwareMatrix(string ipAddress)
        {
            _socket = new LightwareSocket(ipAddress);
            _socket.StatusChanged += SocketOnStatusChanged;
            _socket.ReceivedData += SocketOnReceivedData;

            CrestronConsole.AddNewConsoleCommand(Send, "LwSend",
                "Send a command to the lightware matrix", ConsoleAccessLevelEnum.AccessOperator);
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        
        public event SwitcherInputStatusChangedEventHandler InputStatusChanged;

        #endregion

        #region Delegates
        #endregion

        #region Properties

        internal LightwareSocket Socket
        {
            get { return _socket; }
        }

        public bool SupportsDMEndPoints
        {
            get { return false; }
        }

        public ReadOnlyDictionary<int, LightwareInput> Inputs
        {
            get { return new ReadOnlyDictionary<int, LightwareInput>(_inputs); }
        }

        public ReadOnlyDictionary<int, LightwareOutput> Outputs
        {
            get { return new ReadOnlyDictionary<int, LightwareOutput>(_outputs); }
        }

        public string DiagnosticsName
        {
            get { return  "Lightware Switcher (" + DeviceAddressString + ")"; }
        }

        #endregion

        #region Methods

        public void RouteVideo(uint input, uint output)
        {

        }

        public void RouteAudio(uint input, uint output)
        {

        }

        public uint GetVideoInput(uint output)
        {
            return 0;
        }

        public uint GetAudioInput(uint output)
        {
            return 0;
        }

        public bool InputIsActive(uint input)
        {
            return false;
        }

        public EndpointReceiverBase GetEndpointForOutput(uint output)
        {
            throw new NotImplementedException();
        }

        public HdmiInputWithCEC GetHdmiCecInput(uint input)
        {
            throw new NotImplementedException();
        }

        public void Init()
        {
            _socket.Connect();
        }

        private void SocketOnStatusChanged(TCPClientSocketBase socket, SocketStatusEventType eventType)
        {
            if (eventType == SocketStatusEventType.Connected)
            {
                Send("VC");
            }
        }

        internal void Send(string str)
        {
            Debug.WriteWarn("Lightware Tx", str);
            _socket.Send(@"{" + str + @"}");
        }

        internal void Send(string str, params object[] args)
        {
            Send(string.Format(str, args));
        }

        private void SocketOnReceivedData(string args)
        {
            Debug.WriteSuccess("Lightware Rx", args);

            if (args.StartsWith("(ALL "))
            {
                var index = 0;
                var matchData = new Dictionary<int, Match>();

                foreach (Match match in Regex.Matches(args, @" ?(\D)(\d{2})"))
                {
                    index ++;
                    matchData[index] = match;

                    if (!_outputs.ContainsKey(index))
                    {
                        _outputs.Add(index, new LightwareOutput(this, index));
                    }

                    if (!_inputs.ContainsKey(index))
                    {
                        _inputs.Add(index, new LightwareInput(this, index));
                    }
                }

                foreach (var match in matchData)
                {
                    var modifier = match.Value.Groups[1].Value;
                    var value = int.Parse(match.Value.Groups[2].Value);

                    Debug.WriteSuccess("Output " + index, "Input {0}{1}", value, modifier);

                    _outputs[match.Key].SetInputFeedback(value > 0 ? _inputs[value] : null, modifier);
                }
            }
        }

        public IEnumerator<LightwareOutput> GetEnumerator()
        {
            return _outputs.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        public string Name { get; private set; }
        public string ManufacturerName { get; private set; }
        public string ModelName { get; private set; }
        public bool DeviceCommunicating { get; private set; }
        public string DeviceAddressString { get; private set; }
        public string SerialNumber { get; private set; }
        public string VersionInfo { get; private set; }
        public event DeviceCommunicatingChangeHandler DeviceCommunicatingChange;
        public FusionAssetType AssetType { get; private set; }
    }
}