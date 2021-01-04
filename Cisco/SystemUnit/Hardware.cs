 
namespace UX.Lib2.Devices.Cisco.SystemUnit
{
    public class Hardware : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Temperature")]
#pragma warning disable 649 // assigned using reflection
        private double _temperature;
#pragma warning restore 649

        [CodecApiNameAttribute("Module")]
        private Module _module;

        #endregion

        #region Constructors

        internal Hardware(CodecApiElement parent, string propertyName)
            : base(parent, propertyName)
        {
            _module = new Module(this, "Module");
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public Module Module
        {
            get { return _module; }
        }

        public double Temperature
        {
            get { return _temperature; }
        }

        #endregion

        #region Methods
        #endregion
    }
}