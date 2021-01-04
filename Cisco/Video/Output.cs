 
using System.Collections.Generic;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Cisco.Video
{
    public class Output : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Connector")]
        private Dictionary<int, OutputConnector> _connectors = new Dictionary<int, OutputConnector>();

        [CodecApiNameAttribute("Monitor")]
        private Dictionary<int, OutputMonitor> _monitors = new Dictionary<int, OutputMonitor>();

        #endregion

        #region Constructors

        internal Output(CodecApiElement parent, string propertyName)
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

        public ReadOnlyDictionary<int, OutputConnector> Connectors
        {
            get { return new ReadOnlyDictionary<int, OutputConnector>(_connectors); }
        }

        public ReadOnlyDictionary<int, OutputMonitor> Monitor
        {
            get { return new ReadOnlyDictionary<int, OutputMonitor>(_monitors); }
        }

        #endregion

        #region Methods
        #endregion
    }
}