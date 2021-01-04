namespace UX.Lib2.Devices.Cisco.UserInterface
{
    public class ExternalSource
    {
        private readonly CiscoTelePresenceCodec _codec;
        private readonly string _sourceIdentifier;
        private readonly string _name;
        private readonly int _connectorId;
        private readonly ExternalSourceType _type;
        private ExternalSourceState _state;

        internal ExternalSource(CiscoTelePresenceCodec codec, string sourceIdentifier, string name, int connectorId, ExternalSourceType type)
        {
            _codec = codec;
            _sourceIdentifier = sourceIdentifier;
            _name = name;
            _connectorId = connectorId;
            _type = type;
        }

        public string SourceIdentifier
        {
            get { return _sourceIdentifier; }
        }

        public string Name
        {
            get { return _name; }
        }

        public int ConnectorId
        {
            get { return _connectorId; }
        }

        public ExternalSourceType Type
        {
            get { return _type; }
        }

        public ExternalSourceState State
        {
            get { return _state; }
            set
            {
                _state = value;
                _codec.Send(
                    "xCommand UserInterface Presentation ExternalSource State Set SourceIdentifier: {0} State: {1}",
                    _sourceIdentifier, _state);
            }
        }

        public override string ToString()
        {
            return string.Format("External Source \"{0}\", Id: {1}, ConnectorId: {2}, Type: {3}, State: {4}",
                Name, SourceIdentifier, ConnectorId, Type, State);
        }
    }

    public enum ExternalSourceState
    {
        Hidden,
        Ready,
        NotReady,
        Error
    }

    public enum ExternalSourceType
    {
        Camera,
        Desktop,
        DocumentCamera,
        Mediaplayer,
        PC,
        Whiteboard,
        Other
    }
}