 
namespace UX.Lib2.Devices.Cisco.Network
{
    public class Voice : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("VlanId")]
#pragma warning disable 649 // assigned using reflection
        private int _vlanId;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal Voice(CodecApiElement parent, string propertyName)
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

        public int VlanId
        {
            get { return _vlanId; }
        }

        #endregion

        #region Methods
        #endregion
    }
}