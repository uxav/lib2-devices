using UX.Lib2.DeviceSupport;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.Cameras.Sony
{
    public class ViscaCamera : ICamera, IFusionAsset
    {
        private readonly IViscaSocket _socket;
        private readonly uint _id;
        private readonly string _ipAddress;
        private string _name = "Camera";
        private bool _deviceCommunicating;

        public ViscaCamera(IViscaSocket socket, uint id)
        {
            _socket = socket;
            _id = id;
            _ipAddress = socket.IpAddress;
        }

        public ViscaCamera(uint id, string ipAddress, int port)
        {
            _id = id;
            _socket = new ViscaTcpClient(ipAddress, port, 1000);
            ((ViscaTcpClient) _socket).StatusChanged += OnStatusChanged;
        }

        private void OnStatusChanged(TCPClientSocketBase socket, SocketStatusEventType eventType)
        {
            DeviceCommunicating = eventType == SocketStatusEventType.Connected;
        }

        public uint Id
        {
            get { return _id; }
        }

        public bool Initialized
        {
            get { return _socket.Initialized; }
        }

        public void Initialize()
        {
            if(_socket.Initialized) return;
            _socket.Initialize();
        }

        public void TiltUp()
        {
            Move(TiltDirection.Up, Speed.Medium);
        }

        public void TiltDown()
        {
            Move(TiltDirection.Down, Speed.Medium);
        }

        public void TiltStop()
        {
            var data = new byte[]
            {
                (byte) (0x80 + Id),
                0x01,
                0x06,
                0x01,
                0x00,
                0x00,
                0x03,
                0x03,
                0xff
            };
            _socket.Send(data);
        }

        public void PanLeft()
        {
            Move(PanDirection.Left, Speed.Medium);
        }

        public void PanRight()
        {
            Move(PanDirection.Right, Speed.Medium);
        }

        public void PanStop()
        {
            var data = new byte[]
            {
                (byte) (0x80 + Id),
                0x01,
                0x06,
                0x01,
                0x00,
                0x00,
                0x03,
                0x03,
                0xff
            };
            _socket.Send(data);
        }

        public void ZoomIn()
        {
            var data = new byte[]
            {
                (byte) (0x80 + Id),
                0x01,
                0x04,
                0x07,
                0x02,
                0xff
            };
            _socket.Send(data);
        }

        public void ZoomOut()
        {
            var data = new byte[]
            {
                (byte) (0x80 + Id),
                0x01,
                0x04,
                0x07,
                0x03,
                0xff
            };
            _socket.Send(data);
        }

        public void ZoomStop()
        {
            var data = new byte[]
            {
                (byte) (0x80 + Id),
                0x01,
                0x04,
                0x07,
                0x00,
                0xff
            };
            _socket.Send(data);
        }

        void Move(PanDirection direction, Speed speed)
        {
            var data = new byte[]
            {
                (byte) (0x80 + Id),
                0x01,
                0x06,
                0x01,
                (byte) speed,
                0x00,
                (byte) direction,
                0x03,
                0xff
            };
            _socket.Send(data);
        }

        void Move(TiltDirection direction, Speed speed)
        {
            var data = new byte[]
            {
                (byte) (0x80 + Id),
                0x01,
                0x06,
                0x01,
                0x00,
                (byte) speed,
                0x03,
                (byte) direction,
                0xff
            };
            _socket.Send(data);
        }

        public void RecallPreset(uint preset)
        {
            //8x 01 04 3F 02 0p FF
            var data = new byte[]
            {
                (byte) (0x80 + Id),
                0x01,
                0x04,
                0x3f,
                0x02,
                (byte) preset,
                0xff
            };
            _socket.Send(data);
        }

        public void SetPreset(uint preset)
        {
            //8x 01 04 3F 02 0p FF
            var data = new byte[]
            {
                (byte) (0x80 + Id),
                0x01,
                0x04,
                0x3f,
                0x01,
                (byte) preset,
                0xff
            };
            _socket.Send(data);
        }

        enum PanDirection
        {
            Left = 0x01,
            Right = 0x02,
            Stop = 0x03
        }

        enum TiltDirection
        {
            Up = 0x01,
            Down = 0x02,
            Stop = 0x03
        }

        enum Speed
        {
            Slow = 6,
            Medium = 14,
            Fast = 20,
        }

        public string Name
        {
            get { return _name; }
            private set { _name = value; }
        }

        public string ManufacturerName
        {
            get
            {
                return "Generic";
            }
        }

        public string DiagnosticsName
        {
            get { return "Camera at " + _ipAddress; }
        }

        public string ModelName
        {
            get { return "Unknonw"; }
        }

        public bool DeviceCommunicating
        {
            get { return _deviceCommunicating; }
            private set
            {
                if(_deviceCommunicating == value) return;
                _deviceCommunicating = value;
                OnDeviceCommunicatingChange(this, _deviceCommunicating);
            }
        }

        public string DeviceAddressString
        {
            get { return _ipAddress; }
        }

        public string SerialNumber
        {
            get { return "Unknonw"; }
        }

        public string VersionInfo
        {
            get { return "Unknown"; }
        }

        public event DeviceCommunicatingChangeHandler DeviceCommunicatingChange;

        protected virtual void OnDeviceCommunicatingChange(IDevice device, bool communicating)
        {
            var handler = DeviceCommunicatingChange;
            if (handler != null) handler(device, communicating);
        }

        public FusionAssetType AssetType
        {
            get
            {
                return FusionAssetType.Camera;
            }
        }
    }
}