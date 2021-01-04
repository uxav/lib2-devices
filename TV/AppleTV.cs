using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.Reflection;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DM;
using UX.Lib2.DeviceSupport;

namespace UX.Lib2.Devices.TV
{
    public class AppleTV : ISourceDevice
    {
        #region Fields

        private readonly Cec _cec;
        private readonly IROutputPort _irPort;
        private const string IRFilePath = @"NVRAM\Apple_AppleTV_3rd_Generation.ir";

        #endregion

        #region Constructors

        public AppleTV(Cec cecPort)
        {
            _cec = cecPort;
        }

        public AppleTV(IROutputPort irPort)
        {
            _irPort = irPort;

            if (!File.Exists(IRFilePath))
            {
                Debug.WriteWarn("Apple TV IR File not found. Looking for file in resources...");
                var assembly = Assembly.GetExecutingAssembly();
                var stream = assembly.GetManifestResourceStream(GetType().GetCType(), @"Apple_AppleTV_3rd_Generation.ir");
                using (var fileStream = File.Create(IRFilePath))
                {
                    var buffer = new byte[8 * 1024];
                    int len;
                    while ((len = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, len);
                    }
                    fileStream.Close();
                }
                Debug.WriteSuccess("Apple TV IR file saved to", IRFilePath);
            }

            var irDriverId = _irPort.LoadIRDriver(IRFilePath);

#if DEBUG
            CrestronConsole.PrintLine("Created Apple TV source control using IR port {0}", _irPort.ID);
            CrestronConsole.PrintLine("Following IR Commands are available:");

            uint count = 0;
            foreach (var irCommandName in _irPort.AvailableIRCmds(irDriverId))
            {
                CrestronConsole.PrintLine(" - {0} [{1}]", irCommandName, count);
                count++;
            }
#endif
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event DeviceCommunicatingChangeHandler DeviceCommunicatingChange;

        #endregion

        #region Delegates
        #endregion

        #region Properties

        public string Name
        {
            get { return "AppleTV"; }
        }

        public string ManufacturerName
        {
            get { return "Apple"; }
        }

        public string ModelName
        {
            get { return "AppleTV"; }
        }

        public bool DeviceCommunicating { get; private set; }

        public string SerialNumber
        {
            get { return "Unknown"; }
        }

        public string DiagnosticsName
        {
            get { return Name + " (" + DeviceAddressString + ")"; }
        }

        public string DeviceAddressString
        {
            get { return _irPort.ToString(); }
        }

        public string VersionInfo
        {
            get { return "Unknown"; }
        }

        #endregion

        #region Methods

        public void ButtonPress(AppleTVButtonCommands command)
        {
            if (_cec != null)
            {
                switch (command)
                {
                    case AppleTVButtonCommands.Select:
                        _cec.Send.StringValue = "\x04\x44\x00";
                        break;
                    case AppleTVButtonCommands.Up:
                        _cec.Send.StringValue = "\x04\x44\x01";
                        break;
                    case AppleTVButtonCommands.Down:
                        _cec.Send.StringValue = "\x04\x44\x02";
                        break;
                    case AppleTVButtonCommands.Left:
                        _cec.Send.StringValue = "\x04\x44\x03";
                        break;
                    case AppleTVButtonCommands.Right:
                        _cec.Send.StringValue = "\x04\x44\x04";
                        break;
                    case AppleTVButtonCommands.Menu:
                        _cec.Send.StringValue = "\x04\x44\x0D";
                        break;
                    case AppleTVButtonCommands.Home:
                        _cec.Send.StringValue = "\x04\x44\x10";
                        break;
                    case AppleTVButtonCommands.PlayPause:
                        _cec.Send.StringValue = "\x04\x44\x44";
                        break;
                }
            }
            else if (_irPort != null)
            {
                switch (command)
                {
                    case AppleTVButtonCommands.Select:
                        _irPort.Press("SELECT");
                        break;
                    case AppleTVButtonCommands.Up:
                        _irPort.Press("UP_ARROW");
                        break;
                    case AppleTVButtonCommands.Down:
                        _irPort.Press("DOWN_ARROW");
                        break;
                    case AppleTVButtonCommands.Left:
                        _irPort.Press("LEFT_ARROW");
                        break;
                    case AppleTVButtonCommands.Right:
                        _irPort.Press("RIGHT_ARROW");
                        break;
                    case AppleTVButtonCommands.Menu:
                        _irPort.Press("MENU");
                        break;
                    case AppleTVButtonCommands.Home:
                        _irPort.Press("MENU");
                        break;
                    case AppleTVButtonCommands.PlayPause:
                        _irPort.Press("PLAY/PAUSE");
                        break;
                }
            }
        }

        public void ButtonRelease()
        {
            if (_cec != null)
                _cec.Send.StringValue = "\x04\x45";
            else if (_irPort != null)
            {
                _irPort.Release();
            }
        }

        public void Menu()
        {
            ButtonPress(AppleTVButtonCommands.Menu);
            ButtonRelease();
        }

        public void UpdateOnSourceRequest()
        {

        }

        public void StartPlaying()
        {
            Menu();
        }

        public void StopPlaying()
        {

        }

        public void Initialize()
        {

        }

        #endregion
    }

    public enum AppleTVButtonCommands
    {
        Select,
        Up,
        Down,
        Left,
        Right,
        Menu,
        Home,
        PlayPause
    }
}