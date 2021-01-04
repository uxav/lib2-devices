 
using System.Collections.Generic;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Cisco.Network
{
    public class Ntp : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Server")]
        private Dictionary<int, Server> _servers = new Dictionary<int, Server>();

        [CodecApiNameAttribute("Status")]
#pragma warning disable 649 // assigned using reflection
        private NTPStatus _status;
#pragma warning restore 649

        [CodecApiNameAttribute("CurrentAddress")]
#pragma warning disable 649 // assigned using reflection
        private string _currentAddress;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal Ntp(CodecApiElement parent, string propertyName)
            : base(parent, propertyName)
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

        public string CurrentAddress
        {
            get { return _currentAddress; }
        }

        public ReadOnlyDictionary<int, Server> Servers
        {
            get { return new ReadOnlyDictionary<int, Server>(_servers); }
        }

        public NTPStatus Status
        {
            get { return _status; }
        }

        #endregion

        #region Methods
        #endregion
    }

    public enum NTPStatus
    {
        Discarded,
        Synced,
        NotSynced,
        Unknown,
        Off
    }
}