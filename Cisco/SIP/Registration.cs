 
namespace UX.Lib2.Devices.Cisco.SIP
{
    public class Registration : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Reason")]
#pragma warning disable 649 // assigned using reflection
        private string _reason;
#pragma warning restore 649

        [CodecApiNameAttribute("Status")]
#pragma warning disable 649 // assigned using reflection
        private RegistrationStatus _status;
#pragma warning restore 649

        [CodecApiNameAttribute("URI")]
#pragma warning disable 649 // assigned using reflection
        private string _uri;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal Registration(CodecApiElement parent, string propertyName, int indexer)
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

        public string Reason
        {
            get { return _reason; }
        }

        public RegistrationStatus Status
        {
            get { return _status; }
        }

        public string Uri
        {
            get { return _uri; }
        }

        #endregion

        #region Methods
        #endregion
    }

    public enum RegistrationStatus
    {
        Deregister,
        Failed,
        Inactive,
        Registered,
        Registering
    }
}