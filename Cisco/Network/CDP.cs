 
namespace UX.Lib2.Devices.Cisco.Network
{
    public class Cdp : CodecApiElement
    {
        [CodecApiNameAttribute("Address")]
#pragma warning disable 649 // assigned using reflection
        private string _address;
#pragma warning restore 649

        [CodecApiNameAttribute("Capabilities")]
#pragma warning disable 649 // assigned using reflection
        private string _capabilities;
#pragma warning restore 649

        [CodecApiNameAttribute("DeviceId")]
#pragma warning disable 649 // assigned using reflection
        private string _deviceId;
#pragma warning restore 649

        [CodecApiNameAttribute("Duplex")]
#pragma warning disable 649 // assigned using reflection
        private string _duplex;
#pragma warning restore 649

        [CodecApiNameAttribute("Platform")]
#pragma warning disable 649 // assigned using reflection
        private string _platform;
#pragma warning restore 649

        [CodecApiNameAttribute("PortID")]
#pragma warning disable 649 // assigned using reflection
        private string _portId;
#pragma warning restore 649

        [CodecApiNameAttribute("PrimaryMgmtAddress")]
#pragma warning disable 649 // assigned using reflection
        private string _primaryMgmtAddress;
#pragma warning restore 649

        [CodecApiNameAttribute("SysName")]
#pragma warning disable 649 // assigned using reflection
        private string _sysName;
#pragma warning restore 649

        [CodecApiNameAttribute("SysObjectID")]
#pragma warning disable 649 // assigned using reflection
        private string _sysObjectId;
#pragma warning restore 649

        [CodecApiNameAttribute("VTPMgmtDomain")]
#pragma warning disable 649 // assigned using reflection
        private string _vtpMgmtDomain;
#pragma warning restore 649

        [CodecApiNameAttribute("Version")]
#pragma warning disable 649 // assigned using reflection
        private int _version;
#pragma warning restore 649

        [CodecApiNameAttribute("VoIPApplianceVlanID")]
#pragma warning disable 649 // assigned using reflection
        private int _voIPApplianceVlanId;
#pragma warning restore 649

        #region Fields
        #endregion

        #region Constructors

        internal Cdp(CodecApiElement parent, string propertyName)
            : base(parent, propertyName)
        {
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public string Address
        {
            get { return _address; }
        }

        public string Capabilities
        {
            get { return _capabilities; }
        }

        public string DeviceId
        {
            get { return _deviceId; }
        }

        public string Duplex
        {
            get { return _duplex; }
        }

        public string Platform
        {
            get { return _platform; }
        }

        public string PortId
        {
            get { return _portId; }
        }

        public string PrimaryMgmtAddress
        {
            get { return _primaryMgmtAddress; }
        }

        public string SysName
        {
            get { return _sysName; }
        }

        public string SysObjectId
        {
            get { return _sysObjectId; }
        }

        public string VtpMgmtDomain
        {
            get { return _vtpMgmtDomain; }
        }

        public int Version
        {
            get { return _version; }
        }

        public int VoIPApplianceVlanId
        {
            get { return _voIPApplianceVlanId; }
        }

        #endregion

        #region Methods
        #endregion
    }
}