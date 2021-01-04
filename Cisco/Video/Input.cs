 
using System.Collections.Generic;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Cisco.Video
{
    public class Input : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("Connector")]
        private Dictionary<int, InputConnector> _connectors = new Dictionary<int, InputConnector>();

        [CodecApiNameAttribute("Source")]
        private Dictionary<int, Source> _sources = new Dictionary<int, Source>();

        [CodecApiNameAttribute("MainVideoSource")]
#pragma warning disable 649 // assigned using reflection
        private int _mainVideoSource;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal Input(CodecApiElement parent, string propertyName)
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

        public ReadOnlyDictionary<int, InputConnector> Connectors
        {
            get { return new ReadOnlyDictionary<int, InputConnector>(_connectors); }
        }

        public Source MainVideoSource
        {
            get
            {
                if (_sources.ContainsKey(_mainVideoSource))
                {
                    return _sources[_mainVideoSource];
                }

                return null;
            }
        }

        public ReadOnlyDictionary<int, Source> Sources
        {
            get { return new ReadOnlyDictionary<int, Source>(_sources); }
        }

        #endregion

        #region Methods
        #endregion
    }
}