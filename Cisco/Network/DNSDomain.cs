 
namespace UX.Lib2.Devices.Cisco.Network
{
    public class DnsDomain : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Name")]
#pragma warning disable 649 // assigned using reflection
        private string _name;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal DnsDomain(CodecApiElement parent, string propertyName)
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

        public string Name
        {
            get { return _name; }
        }

        #endregion

        #region Methods
        #endregion
    }
}