namespace UX.Lib2.Devices.Lightware3
{
    public class LightwareInput : LightwareInputOutput
    {
        private string _name = string.Empty;
        private bool _locked;
        private bool _muted;
        private bool _embeddedAudio;
        private bool _encrypted;
        private bool _signalPresent;
        private bool _connected;

        public LightwareInput(LightwareMatrix device, uint number)
            : base(device, number)
        {

        }

        public override IOType Type
        {
            get { return IOType.Input; }
        }

        public override string Name
        {
            get { return _name; }
            internal set { _name = value; }
        }

        public bool Locked
        {
            get { return _locked; }
        }

        public bool Muted
        {
            get { return _muted; }
        }

        public bool EmbeddedAudio
        {
            get { return _embeddedAudio; }
        }

        public bool Encrypted
        {
            get { return _encrypted; }
        }

        public bool SignalPresent
        {
            get { return _signalPresent; }
        }

        public bool Connected
        {
            get { return _connected; }
        }

        internal bool UpdateStatusFeedback(bool locked, bool muted, bool embeddedAudio, bool encrypted,
            bool signalPresent, bool connected)
        {
            var changed = false;

            if (_locked != locked)
            {
                _locked = locked;
                changed = true;
            }

            if (_muted != muted)
            {
                _muted = muted;
                changed = true;
            }

            if (_embeddedAudio != embeddedAudio)
            {
                _embeddedAudio = embeddedAudio;
                changed = true;
            }

            if (_encrypted != encrypted)
            {
                _encrypted = encrypted;
                changed = true;
            }

            if (_signalPresent != signalPresent)
            {
                _signalPresent = signalPresent;
                changed = true;
            }

            if (_connected != connected)
            {
                _connected = connected;
                changed = true;
            }

            return changed;
        }
    }
}