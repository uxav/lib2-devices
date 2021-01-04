 
namespace UX.Lib2.Devices.Cisco.Conference
{
    public class MultiPoint : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Mode")]
#pragma warning disable 649 // assigned using reflection
        private MultiPointMode _mode;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal MultiPoint(CodecApiElement parent, string propertyName)
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

        public MultiPointMode Mode
        {
            get { return _mode; }
        }

        #endregion

        #region Methods
        #endregion
    }

    public enum MultiPointMode
    {
        Auto,
        CUCMMediaResourceGroupList,
        MultiSite,
        Off
    }
}