 
namespace UX.Lib2.Devices.Cisco.Video
{
    public class Source : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("ConnectorId")]
#pragma warning disable 649 // assigned using reflection
        private int _connectorId;
#pragma warning restore 649

        [CodecApiNameAttribute("FormatStatus")]
#pragma warning disable 649 // assigned using reflection
        private SourceFormatStatus _formatStatus;
#pragma warning restore 649

        [CodecApiNameAttribute("FormatType")]
#pragma warning disable 649 // assigned using reflection
        private SourceFormatType _formatType;
#pragma warning restore 649

        [CodecApiNameAttribute("MediaChannelId")]
#pragma warning disable 649 // assigned using reflection
        private int _mediaChannelId;
#pragma warning restore 649

        [CodecApiNameAttribute("Resolution")]
        private Resolution _resolution;

        #endregion

        #region Constructors

        internal Source(CodecApiElement parent, string propertyName, int indexer)
            : base(parent, propertyName, indexer)
        {
            _resolution = new Resolution(this, "Resolution");
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public int ConnectorId
        {
            get { return _connectorId; }
        }

        public SourceFormatStatus FormatStatus
        {
            get { return _formatStatus; }
        }

        public SourceFormatType FormatType
        {
            get { return _formatType; }
        }

        public int MediaChannelId
        {
            get { return _mediaChannelId; }
        }

        public Resolution Resolution
        {
            get { return _resolution; }
        }

        #endregion

        #region Methods
        #endregion
    }

    public enum SourceFormatStatus
    {
        Ok,
        OutOfRange,
        NotFound,
        Interlaced,
        Error,
        Unknown
    }

    public enum SourceFormatType
    {
        Unknown,
        AnalogCVTBlanking,
        AnalogCVTReducedBlanking,
        AnalogGTFDefault,
        AnalogGTFSecondary,
        AnalogDiscreteTiming,
        AnalogDMTBlanking,
        AnalogCEABlanking,
        Digital
    }
}