using System;
using System.Linq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Cisco.Conference
{
    public class Call : CodecApiElement
    {
        #region Fields
        
        private readonly int _indexer;

        [CodecApiNameAttribute("BookingId")]
#pragma warning disable 649 // assigned using reflection
        private string _bookingId;
#pragma warning restore 649

        [CodecApiNameAttribute("Manufacturer")]
#pragma warning disable 649 // assigned using reflection
        private string _manufacturer;
#pragma warning restore 649

        [CodecApiNameAttribute("MicrophonesMuted")]
#pragma warning disable 649 // assigned using reflection
        private string _microphonesMuted;
#pragma warning restore 649

        [CodecApiNameAttribute("SoftwareID")]
#pragma warning disable 649 // assigned using reflection
        private string _softwareId;
#pragma warning restore 649

        [CodecApiName("AuthenticationRequest")]
#pragma warning disable 649 // assigned using reflection
        private AuthenticationRequest _authenticationRequest;
#pragma warning restore 649

        [CodecApiNameAttribute("Capabilities")]
        private Capabilities _capabilities;

        #endregion

        #region Constructors

        internal Call(CodecApiElement parent, string propertyName, int indexer)
            : base(parent, propertyName, indexer)
        {
            _indexer = indexer;
            _capabilities = new Capabilities(this, "Capabilities");
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event ConferenceCallAuthenticationRequestChangedEventHandler AuthenticationRequestChange;

        #endregion

        #region Delegates
        #endregion

        #region Properties

        public int Id
        {
            get { return _indexer; }
        }

        public string BookingId
        {
            get { return _bookingId; }
        }

        public string Manufacturer
        {
            get { return _manufacturer; }
        }

        public string MicrophonesMuted
        {
            get { return _microphonesMuted; }
        }

        public string SoftwareId
        {
            get { return _softwareId; }
        }

        public Capabilities Capabilities
        {
            get { return _capabilities; }
        }

        public enum AuthenticationRequest
        {
            None,
            HostPinOrGuest,
            HostPinOrGuestPin,
            PanelistPin
        }

        public AuthenticationRequest AuthRequest
        {
            get { return _authenticationRequest; }
        }

        public enum ParticipantRole
        {
            Host,
            Panelist,
            Guest
        }

        #endregion

        #region Methods

        public void Authenticate(ParticipantRole role, string pin)
        {
            var cmd = new CodecCommand("Conference/Call", "AuthenticationResponse");
            cmd.Args.Add("CallId", Id);
            cmd.Args.Add(role);
            if (!string.IsNullOrEmpty(pin))
            {
                cmd.Args.Add("Pin", pin);
            }
            var response = Codec.SendCommand(cmd);

#if DEBUG
            Debug.WriteInfo("Response from authenticate", response.Code.ToString());

            if (response.Xml != null)
            {
                Debug.WriteNormal(response.Xml.ToString());
            }
#endif
        }

        protected override void OnStatusChanged(CodecApiElement element, string[] propertyNamesWhichUpdated)
        {
            base.OnStatusChanged(element, propertyNamesWhichUpdated);
            if (propertyNamesWhichUpdated.Contains("AuthenticationRequest"))
            {
                if (AuthenticationRequestChange != null)
                {
                    try
                    {
                        AuthenticationRequestChange(this);
                    }
                    catch (Exception e)
                    {
                        CloudLog.Exception(e);
                    }
                }
            }
        }

        #endregion
    }

    public delegate void ConferenceCallAuthenticationRequestChangedEventHandler(Call call);
}