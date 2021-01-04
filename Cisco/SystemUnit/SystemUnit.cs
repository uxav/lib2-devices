 
using System;

namespace UX.Lib2.Devices.Cisco.SystemUnit
{
    public class SystemUnit : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Uptime")]
#pragma warning disable 649 // assigned using reflection
        private TimeSpan _uptime;
#pragma warning restore 649

        [CodecApiNameAttribute("ProductId")]
#pragma warning disable 649 // assigned using reflection
        private string _productId;
#pragma warning restore 649

        [CodecApiNameAttribute("ProductPlatform")]
#pragma warning disable 649 // assigned using reflection
        private string _productPlatform;
#pragma warning restore 649

        [CodecApiNameAttribute("ProductType")]
#pragma warning disable 649 // assigned using reflection
        private string _productType;
#pragma warning restore 649

        [CodecApiNameAttribute("Software")]
        private Software _software;

        [CodecApiNameAttribute("State")]
        private State _state;

        [CodecApiNameAttribute("Hardware")]
        private Hardware _hardware;

        #endregion

        #region Constructors

        internal SystemUnit(CiscoTelePresenceCodec codec)
            : base(codec)
        {
            _software = new Software(this, "Software");
            _state = new State(this, "State");
            _hardware = new Hardware(this, "Hardware");
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public string ProductId
        {
            get { return _productId; }
        }

        public string ProductPlatform
        {
            get { return _productPlatform; }
        }

        public string ProductType
        {
            get { return _productType; }
        }

        public TimeSpan Uptime
        {
            get { return _uptime; }
        }

        public Software Software
        {
            get { return _software; }
        }

        public State State
        {
            get { return _state; }
        }

        public Hardware Hardware
        {
            get { return _hardware; }
        }

        #endregion

        #region Methods
        #endregion
    }
}