 
namespace UX.Lib2.Devices.Cisco.Network
{
    public class Ipv4 : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Address")]
#pragma warning disable 649 // assigned using reflection
        private string _address;
#pragma warning restore 649

        [CodecApiNameAttribute("Gateway")]
#pragma warning disable 649 // assigned using reflection
        private string _gateway;
#pragma warning restore 649

        [CodecApiNameAttribute("SubnetMask")]
#pragma warning disable 649 // assigned using reflection
        private string _subnetMask;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal Ipv4(CodecApiElement parent, string propertyName)
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

        public string Gateway
        {
            get { return _gateway; }
        }

        public string SubnetMask
        {
            get { return _subnetMask; }
        }

        #endregion

        #region Methods
        #endregion
    }
}