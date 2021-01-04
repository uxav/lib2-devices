 
namespace UX.Lib2.Devices.Cisco.SIP
{
    public class Proxy : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Address")]
#pragma warning disable 649 // assigned using reflection
        private string _address;
#pragma warning restore 649

        [CodecApiNameAttribute("Status")]
#pragma warning disable 649 // assigned using reflection
        private ProxyStatus _status;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal Proxy(CodecApiElement parent, string propertyName, int indexer)
            : base(parent, propertyName, indexer)
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

        public ProxyStatus Status
        {
            get { return _status; }
        }

        #endregion

        #region Methods
        #endregion
    }

    public enum ProxyStatus
    {
        Active,
        DNSFailed,
        Off,
        Timeout,
        UnableTCP,
        UnableTLS,
        Unknown,
        AuthenticationFailed
    }
}