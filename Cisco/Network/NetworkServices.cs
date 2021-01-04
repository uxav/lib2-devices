 
namespace UX.Lib2.Devices.Cisco.Network
{
    public class NetworkServices : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("NTP")]
        private Ntp _ntp;

        #endregion

        #region Constructors

        internal NetworkServices(CiscoTelePresenceCodec codec)
            : base(codec)
        {
            _ntp = new Ntp(this, "NTP");
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public Ntp Ntp
        {
            get { return _ntp; }
        }

        #endregion

        #region Methods
        #endregion
    }
}