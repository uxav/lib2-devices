 
namespace UX.Lib2.Devices.Cisco.Video
{
    public class ConnectedDevice : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Name")]
#pragma warning disable 649 // assigned using reflection
        private string _name;
#pragma warning restore 649

        [CodecApiNameAttribute("PreferredFormat")]
#pragma warning disable 649 // assigned using reflection
        private string _preferredFormat;
#pragma warning restore 649

        [CodecApiNameAttribute("CEC")]
        private Cec _cec;

        [CodecApiNameAttribute("ScreenSize")]
#pragma warning disable 649 // assigned using reflection
        private int _screenSize;
#pragma warning restore 649

        #endregion

        #region Constructors

        public ConnectedDevice(CodecApiElement parent, string propertyName)
            : base(parent, propertyName)
        {
            _cec = new Cec(this, "CEC");
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public string Name
        {
            get { return _name; }
        }

        public string PreferredFormat
        {
            get { return _preferredFormat; }
        }

        public Cec Cec
        {
            get { return _cec; }
        }

        public int ScreenSize
        {
            get { return _screenSize; }
        }

        #endregion

        #region Methods
        #endregion
    }
}