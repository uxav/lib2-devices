 
namespace UX.Lib2.Devices.Cisco.Network
{
    public class Ethernet : CodecApiElement
    {
        [CodecApiNameAttribute("Speed")]
#pragma warning disable 649 // assigned using reflection
        private string _speed;
#pragma warning restore 649

        [CodecApiNameAttribute("MacAddress")]
#pragma warning disable 649 // assigned using reflection
        private string _macAddress;
#pragma warning restore 649

        #region Fields
        #endregion

        #region Constructors

        internal Ethernet(CodecApiElement parent, string propertyName)
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

        public string MacAddress
        {
            get { return _macAddress; }
        }

        public string Speed
        {
            get { return _speed; }
        }

        #endregion

        #region Methods
        #endregion
    }
}