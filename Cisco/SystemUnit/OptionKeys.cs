 
namespace UX.Lib2.Devices.Cisco.SystemUnit
{
    public class OptionKeys : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Encryption")]
#pragma warning disable 649 // assigned using reflection
        private bool _encryption;
#pragma warning restore 649

        [CodecApiNameAttribute("MultiSite")]
#pragma warning disable 649 // assigned using reflection
        private bool _multiSite;
#pragma warning restore 649

        [CodecApiNameAttribute("RemoteMonitoring")]
#pragma warning disable 649 // assigned using reflection
        private bool _remoteMonitoring;
#pragma warning restore 649

        #endregion

        #region Constructors

        public OptionKeys(CodecApiElement parent, string propertyName)
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

        public bool Encryption
        {
            get { return _encryption; }
        }

        public bool MultiSite
        {
            get { return _multiSite; }
        }

        public bool RemoteMonitoring
        {
            get { return _remoteMonitoring; }
        }

        #endregion

        #region Methods
        #endregion
    }
}