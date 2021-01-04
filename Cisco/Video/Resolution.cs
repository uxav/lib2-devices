 
namespace UX.Lib2.Devices.Cisco.Video
{
    public class Resolution : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Height")]
#pragma warning disable 649 // assigned using reflection
        private int _height;
#pragma warning restore 649

        [CodecApiNameAttribute("Width")]
#pragma warning disable 649 // assigned using reflection
        private int _width;
#pragma warning restore 649

        [CodecApiNameAttribute("RefreshRate")]
#pragma warning disable 649 // assigned using reflection
        private int _refreshRate;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal Resolution(CodecApiElement parent, string propertyName)
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

        public int Height
        {
            get { return _height; }
        }

        public int Width
        {
            get { return _width; }
        }

        public int RefreshRate
        {
            get { return _refreshRate; }
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return string.Format("{0}x{1} @ {2}Hz", Width, Height, RefreshRate);
        }

        #endregion
    }
}