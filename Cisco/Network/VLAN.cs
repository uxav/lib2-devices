 
namespace UX.Lib2.Devices.Cisco.Network
{
    public class Vlan : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Voice")]
        private Voice _voice;

        #endregion

        #region Constructors

        internal Vlan(CodecApiElement parent, string propertyName)
            : base(parent, propertyName)
        {
            _voice = new Voice(this, "Voice");
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public Voice Voice
        {
            get { return _voice; }
        }

        #endregion

        #region Methods
        #endregion
    }
}