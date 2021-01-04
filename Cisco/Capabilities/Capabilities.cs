 
namespace UX.Lib2.Devices.Cisco.Capabilities
{
    public class Capabilities : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Conference")]
        private Conference _conference;

        #endregion

        #region Constructors

        internal Capabilities(CiscoTelePresenceCodec codec)
            : base(codec)
        {
            _conference = new Conference(this, "Conference");
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public Conference Conference
        {
            get { return _conference; }
        }

        #endregion

        #region Methods
        #endregion
    }
}