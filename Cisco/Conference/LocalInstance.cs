 
namespace UX.Lib2.Devices.Cisco.Conference
{
    public class LocalInstance : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("SendingMode")]
#pragma warning disable 649 // assigned using reflection
        private LocalSendingMode _mode;
#pragma warning restore 649

        [CodecApiNameAttribute("Source")]
#pragma warning disable 649 // assigned using reflection
        private int _source;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal LocalInstance(CodecApiElement parent, string propertyName, int indexer)
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

        public LocalSendingMode Mode
        {
            get { return _mode; }
        }

        public int Source
        {
            get { return _source; }
        }

        #endregion

        #region Methods
        #endregion
    }

    public enum LocalSendingMode
    {
        LocalOnly,
        LocalRemote,
        Off
    }
}