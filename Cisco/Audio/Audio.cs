 
namespace UX.Lib2.Devices.Cisco.Audio
{
    public class Audio : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Microphones")]
        private Microphones _microphones;

        private readonly Volume _volume;

        [CodecApiNameAttribute("Input")]
        private Input _input;

        [CodecApiNameAttribute("Output")]
        private Output _output;

        #endregion

        #region Constructors

        internal Audio(CiscoTelePresenceCodec codec)
            : base(codec)
        {
            _microphones = new Microphones(this, "Microphones");
            _input = new Input(this, "Input");
            _volume = new Volume(this);
            _output = new Output(this, "Output");
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public Microphones Microphones
        {
            get { return _microphones; }
        }

        public Volume Volume
        {
            get { return _volume; }
        }

        public Input Input
        {
            get { return _input; }
        }

        public Output Output
        {
            get { return _output; }
        }

        #endregion

        #region Methods

        public void PlaySound(Sound sound)
        {
            Codec.Send("xCommand Audio Sound Play Sound: {0}", sound.ToString());
        }

        protected override void HandleUndefinedPropertyItem(StatusUpdateItem item)
        {
            switch (item.PropertyName)
            {
                case "Volume":
                    _volume.HandleUpdatedLevel(item.Value);
                    break;
                case "VolumeMute":
                    _volume.HandleUpdatedMute(item.StringValue);
                    break;
            }
        }

        #endregion

        public enum Sound
        {
            Alert,
            Binding,
            Bump,
            Busy,
            CallDisconnect,
            CallInitiate,
            CallWaiting,
            Dial,
            KeyInput,
            KeyInputDelete,
            KeyTone,
            Nav,
            NavBack,
            Notification,
            OK,
            Pairing,
            PresentationConnect,
            Ringing,
            SignIn,
            SpecialInfo,
            StartListening,
            TelephoneCall,
            VideoCall,
            VolumeAdjust,
            WakeUp,
            Announcement
        }
    }
}