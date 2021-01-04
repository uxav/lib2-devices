 
namespace UX.Lib2.Devices.Cisco.Network
{
    public class Ipv6 : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Address")]
        private string _address;

        [CodecApiNameAttribute("Gateway")]
        private string _gateway;

        #endregion

        #region Constructors

        internal Ipv6(CodecApiElement parent, string propertyName)
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
            private set { _address = value; }
        }

        public string Gateway
        {
            get { return _gateway; }
            private set { _gateway = value; }
        }

        #endregion

        #region Methods
        #endregion
    }
}