 
namespace UX.Lib2.Devices.Cisco.Video
{
    public class OutputConnector : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Connected")]
#pragma warning disable 649 // assigned using reflection
        private string _connected;
#pragma warning restore 649

        [CodecApiNameAttribute("MonitorRole")]
#pragma warning disable 649 // assigned using reflection
        private OutputMonitorRole _monitorRole;
#pragma warning restore 649

        [CodecApiNameAttribute("Type")]
#pragma warning disable 649 // assigned using reflection
        private OutputConnectorType _type;
#pragma warning restore 649

        [CodecApiNameAttribute("Resolution")]
        private Resolution _resolution;

        [CodecApiNameAttribute("ConnectedDevice")]
        private ConnectedDevice _connectedDevice;

        #endregion

        #region Constructors

        internal OutputConnector(CodecApiElement parent, string propertyName, int indexer)
            : base(parent, propertyName, indexer)
        {
            _resolution = new Resolution(this, "Resolution");
            _connectedDevice = new ConnectedDevice(this, "ConnectedDevice");
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public bool Connected
        {
            get { return _connected.ToLower() == "true"; }
        }

        public OutputMonitorRole MonitorRole
        {
            get { return _monitorRole; }
        }

        public OutputConnectorType Type
        {
            get { return _type; }
        }

        public Resolution Resolution
        {
            get { return _resolution; }
        }

        public ConnectedDevice ConnectedDevice
        {
            get { return _connectedDevice; }
        }

        #endregion

        #region Methods
        #endregion
    }

    public enum OutputMonitorRole
    {
        First,
        Second,
        Third,
        PresentationOnly,
        Recorder
    }

    public enum OutputConnectorType
    {
        HDMI,
        DVI
    }
}