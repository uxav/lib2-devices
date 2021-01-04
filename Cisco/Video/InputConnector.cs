 
namespace UX.Lib2.Devices.Cisco.Video
{
    public class InputConnector : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Connected")]
#pragma warning disable 649 // assigned using reflection
        private bool _connected;
#pragma warning restore 649

        [CodecApiNameAttribute("SignalState")]
#pragma warning disable 649 // assigned using reflection
        private InputSignalState _signalState;
#pragma warning restore 649

        [CodecApiNameAttribute("SourceId")]
#pragma warning disable 649 // assigned using reflection
        private int _sourceId;
#pragma warning restore 649

        [CodecApiNameAttribute("Type")]
#pragma warning disable 649 // assigned using reflection
        private InputConnectorType _type;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal InputConnector(CodecApiElement parent, string propertyName, int indexer)
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

        public bool Connected
        {
            get { return _connected; }
        }

        public InputSignalState SignalState
        {
            get { return _signalState; }
        }

        public int SourceId
        {
            get { return _sourceId; }
        }

        public InputConnectorType Type
        {
            get { return _type; }
        }

        #endregion

        #region Methods
        #endregion
    }

    public enum InputSignalState
    {
        OK,
        Unknown,
        Unsupported
    }

    public enum InputConnectorType
    {
        Camera,
        VGA,
        Composite,
        DVI,
        HDMI,
        Unknown,
        YC
    }
}