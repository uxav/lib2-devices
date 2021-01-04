 
using UX.Lib2.Models;

namespace UX.Lib2.Devices.Cisco.Diagnostics
{
    public class Message : CodecApiElement, IStatusMessageItem
    {
        #region Fields

        [CodecApiNameAttribute("Level")]
#pragma warning disable 649 // assigned using reflection
        private MessageLevelType _level;
#pragma warning restore 649

        [CodecApiNameAttribute("Type")]
#pragma warning disable 649 // assigned using reflection
        private MessageType _type;
#pragma warning restore 649

        [CodecApiNameAttribute("Description")]
#pragma warning disable 649 // assigned using reflection
        private string _description;
#pragma warning restore 649

        [CodecApiNameAttribute("References")]
#pragma warning disable 649 // assigned using reflection
        private string _references;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal Message(CodecApiElement parent, string propertyName, int indexer)
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

        public MessageLevelType Level
        {
            get { return _level; }
        }

        public MessageType Type
        {
            get { return _type; }
        }

        public string Description
        {
            get { return _description; }
        }

        public string References
        {
            get { return _references; }
        }

        public StatusMessageWarningLevel MessageLevel
        {
            get
            {
                switch (Level)
                {
                    case MessageLevelType.Info:
                        return StatusMessageWarningLevel.Notice;
                    case MessageLevelType.Warning:
                        return StatusMessageWarningLevel.Warning;
                    default:
                        return StatusMessageWarningLevel.Error;
                }
            }
        }

        public string MessageString { get { return Description; } }

        public string SourceDeviceName
        {
            get
            {
                if (string.IsNullOrEmpty(Codec.UserInterface.ContactInfo.Name))
                    return Codec.SystemUnit.ProductId;
                return Codec.SystemUnit.ProductId + " (" + Codec.UserInterface.ContactInfo.Name + ")";
            }
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return string.Format("{0}: {1} ({2})", Level, Description, Type);
        }

        #endregion

    }

    public enum MessageLevelType
    {
        Info,
        Error,
        Warning,
        Critical
    }

    public enum MessageType
    {
        ANATOnVCS,
        AbnormalCallTermination,
        AudioInternalSpeakerDisabled,
        AudioPairingInterference,
        AudioPairingNoise,
        AudioPairingRate,
        AudioPairingSNR,
        AudioPairingTokenDecode,
        CAPFOperationState,
        CTLinstallation,
        CUCMVendorConfigurationFile,
        CallProtocolDualStackConfig,
        CallProtocolIPStackPlatformCompatibility,
        CallProtocolVcsProvisioningCompatibility,
        CameraDetected,
        CameraId,
        CameraPairing,
        CameraSerial,
        CameraSoftwareVersion,
        CameraStatus,
        CamerasDetected,
        CaptivePortalDetected,
        CertificateExpiry,
        ConfigurationFile,
        ContactInfoMismatch,
        ControlSystemConnection,
        DefaultCallProtocolRegistered,
        ECReferenceDelay,
        EthernetDuplexMatches,
        FanStatus,
        FirstTimeWizardNotCompleted,
        H320GatewayStatus,
        H323GatekeeperStatus,
        HasActiveCallProtocol,
        HasValidReleaseKey,
        HdmiCecModeNoSound,
        HTTPFeedbackFailed,
        HTTPSModeSecurity,
        IPv4Assignment,
        IPv6Assignment,
        IPv6Mtu,
        ISDNLinkCompatibility,
        ISDNLinkIpStack,
        ITLinstallation,
        InvalidSIPTransportConfig,
        IpCameraStatus,
        LockDown,
        MacrosRuntimeStatus,
        MediaBlockingDetected,
        MediaPortRangeNegative,
        MediaPortRangeOdd,
        MediaPortRangeOverlap,
        MediaPortRangeTooSmall,
        MediaPortRangeValueSpace,
        MicrophoneReinforcement,
        MicrophonesConnected,
        MonitorDelay,
        NTPStatus,
        NetLinkStatus,
        NetSpeedAutoNegotiated,
        NetworkQuality,
        OSDVideoOutput,
        OutputConnectorLocations,
        PeripheralSoftwareVersion,
        PlatformSanity,
        PresentationSourceSelection,
        PresenterTrack,
        ProvisioningDeveloperOptions,
        ProvisioningModeAndStatus,
        ProvisioningStatus,
        RoomControl,
        SIPEncryption,
        SIPListenPortAndOutboundMode,
        SIPListenPortAndRegistration,
        SIPProfileRegistration,
        SIPProfileType,
        SelectedVideoInputSourceConnected,
        SipIceAndAnatConflict,
        SipOrH323ButNotBothEnabled,
        SoftwareUpgrade,
        SpeakerTrackEthernetConnection,
        SpeakerTrackFrontPanelMountedCorrectly,
        SpeakerTrackMicrophoneConnection,
        SpeakerTrackVideoInputs,
        TCPMediaFallback,
        TLSVerifyRequiredCerts,
        TemperatureCheck,
        TouchPanelConnection,
        TurnBandwidth,
        UltrasoundConfigSettings,
        UltrasoundSpeakerAvailability,
        ValidPasswords,
        VideoFromInternalCamera,
        VideoInputSignalQuality,
        VideoInputStability,
        VideoPortRangeNegative,
        VideoPortRangeOdd,
        VideoPortRangeTooSmall,
        VideoPortRangeValueSpace,
        MicrophoneOverloaded,
        WebexActivationRequired,
        WebexConnectivity,
        WebexOffline,
        WifiCARequired
    }
}