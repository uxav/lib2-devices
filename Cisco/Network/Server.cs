 
namespace UX.Lib2.Devices.Cisco.Network
{
    public class Server : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Address")]
#pragma warning disable 649 // assigned using reflection
        private string _address;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal Server(CodecApiElement parent, string propertyName, int indexer)
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

        public string Address
        {
            get { return _address; }
        }

        #endregion

        #region Methods
        #endregion
    }
}