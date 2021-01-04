 
namespace UX.Lib2.Devices.Cisco.Video
{
    public class Video : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Selfview")]
        private Selfview _selfview;

        [CodecApiNameAttribute("Input")]
        private Input _input;

        [CodecApiNameAttribute("Output")]
        private Output _output;

        [CodecApiNameAttribute("Presentation")]
        private Presentation _presentation;

        [CodecApiNameAttribute("Monitors")]
#pragma warning disable 649 // assigned using reflection
        private VideoMonitors _monitors;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal Video(CiscoTelePresenceCodec codec)
            : base(codec)
        {
            _selfview = new Selfview(this, "Selfview");
            _input = new Input(this, "Input");
            _output = new Output(this, "Output");
            _presentation = new Presentation(this, "Presentation");
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public Selfview Selfview
        {
            get { return _selfview; }
        }

        public Input Input
        {
            get { return _input; }
        }

        public Output Output
        {
            get { return _output; }
        }

        public Presentation Presentation
        {
            get { return _presentation; }
        }

        public VideoMonitors Monitors
        {
            get { return _monitors; }
        }

        public enum VideoMonitorsCommand
        {
            Auto,
            Single,
            Dual,
            DualPresentationOnly,
            TriplePresentationOnly,
            Triple
        }

        #endregion

        #region Methods

        public void MonitorsSet(VideoMonitorsCommand command)
        {
            Codec.Send("xConfiguration Video Monitors: {0}", command);
        }

        #endregion
    }

    public enum VideoMonitors
    {
        Single,
        Dual,
        DualPresentationOnly,
        Triple,
        TriplePresentationOnly,
        Quadruple
    }
}