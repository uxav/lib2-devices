 
namespace UX.Lib2.Devices.Cisco.SystemUnit
{
    public class Module : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("CompatibilityLevel")]
#pragma warning disable 649 // assigned using reflection
        private int _compatibilityLevel;
#pragma warning restore 649

        [CodecApiNameAttribute("SerialNumber")]
#pragma warning disable 649 // assigned using reflection
        private string _serialNumber;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal Module(CodecApiElement parent, string propertyName)
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

        public int CompatibilityLevel
        {
            get { return _compatibilityLevel; }
        }

        public string SerialNumber
        {
            get { return _serialNumber; }
        }

        #endregion

        #region Methods
        #endregion
    }
}