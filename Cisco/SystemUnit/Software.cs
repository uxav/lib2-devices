 
namespace UX.Lib2.Devices.Cisco.SystemUnit
{
    public class Software : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("DisplayName")]
#pragma warning disable 649 // assigned using reflection
        private string _displayName;
#pragma warning restore 649

        [CodecApiNameAttribute("Name")]
#pragma warning disable 649 // assigned using reflection
        private string _name;
#pragma warning restore 649

        [CodecApiNameAttribute("ReleaseDate")]
#pragma warning disable 649 // assigned using reflection
        private string _releaseDate;
#pragma warning restore 649

        [CodecApiNameAttribute("Version")]
#pragma warning disable 649 // assigned using reflection
        private string _version;
#pragma warning restore 649

        [CodecApiNameAttribute("OptionKeys")]
        private OptionKeys _optionKeys;

        #endregion

        #region Constructors

        internal Software(CodecApiElement parent, string propertyName)
            : base(parent, propertyName)
        {
            _optionKeys = new OptionKeys(this, "OptionKeys");
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public string DisplayName
        {
            get { return _displayName; }
        }

        public string Name
        {
            get { return _name; }
        }

        public string ReleaseDate
        {
            get { return _releaseDate; }
        }

        public string Version
        {
            get { return _version; }
        }

        public OptionKeys OptionKeys
        {
            get { return _optionKeys; }
        }

        #endregion

        #region Methods
        #endregion
    }
}