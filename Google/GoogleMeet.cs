using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.DeviceSupport;

namespace UX.Lib2.Devices.Google
{
    public class GoogleMeet : ISourceDevice
    {
        #region Fields

        private readonly ComPort _comPort;

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        public GoogleMeet(ComPort comPort)
        {
            _comPort = comPort;

            if (!_comPort.Registered && _comPort.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
            {
                CloudLog.Error("Could not register {0} for {1}", _comPort, GetType().Name);
            }

            _comPort.SetComPortSpec(ComPort.eComBaudRates.ComspecBaudRate9600,
                ComPort.eComDataBits.ComspecDataBits8, ComPort.eComParityType.ComspecParityNone,
                ComPort.eComStopBits.ComspecStopBits1, ComPort.eComProtocolType.ComspecProtocolRS232,
                ComPort.eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
                ComPort.eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
                false);
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event DeviceCommunicatingChangeHandler DeviceCommunicatingChange;

        protected virtual void OnDeviceCommunicatingChange(IDevice device, bool communicating)
        {
            var handler = DeviceCommunicatingChange;
            if (handler != null) handler(device, communicating);
        }

        #endregion

        #region Delegates
        #endregion

        #region Properties

        public string Name
        {
            get { return @"Google Meet"; }
        }

        public string ManufacturerName
        {
            get { return @"Google"; }
        }

        public string ModelName
        {
            get { return @"Chromebox For Meetings"; }
        }

        public bool DeviceCommunicating
        {
            get { return true; }
        }

        public string DeviceAddressString
        {
            get { return _comPort.ToString(); }
        }

        public string DiagnosticsName
        {
            get { return Name + " (" + DeviceAddressString + ")"; }
        }

        public string SerialNumber
        {
            get { return "Unknown"; }
        }

        public string VersionInfo
        {
            get { return "Unknown"; }
        }

        #endregion

        #region Methods

        public void UpdateOnSourceRequest()
        {
            
        }

        public void StartPlaying()
        {
            
        }

        public void StopPlaying()
        {

        }

        public void Initialize()
        {
            
        }

        public void SendCommand(GoogleMeetKeyCommand command)
        {
            var bytes = new byte[1];

            switch (command)
            {
                case GoogleMeetKeyCommand.Up:
                    bytes[0] = 202;
                    break;
                case GoogleMeetKeyCommand.Down:
                    bytes[0] = 201;
                    break;
                case GoogleMeetKeyCommand.Left:
                    bytes[0] = 200;
                    break;
                case GoogleMeetKeyCommand.Right:
                    bytes[0] = 199;
                    break;
                case GoogleMeetKeyCommand.Enter:
                    bytes[0] = 0x0d;
                    break;
                case GoogleMeetKeyCommand.EndCall: // Left-Shift + F1
                    bytes = new byte[] { 0x8f, 0x02, 0xb5 };
                    break;
                case GoogleMeetKeyCommand.MicMute: // Left-Ctrl + D
                    bytes = new byte[] { 0x8f, 0x01, 100 };
                    break;
                case GoogleMeetKeyCommand.Menu: // Escape
                    bytes[0] = 27;
                    break;
            }

//#if DEBUG
            CrestronConsole.Print("ComPort {0} Tx: ", _comPort.ID);
            Tools.PrintBytes(bytes, 0, bytes.Length);
//#endif

            _comPort.Send(bytes, bytes.Length);
        }

        public void SendChar(char chr)
        {
            if (chr > 0 && chr <= 127)
            {
                _comPort.Send((byte) chr);
            }
        }

        #endregion
    }

    public enum GoogleMeetKeyCommand
    {
        Up,
        Down,
        Left,
        Right,
        Enter,
        EndCall,
        MicMute,
        Menu
    }
}