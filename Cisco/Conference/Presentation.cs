 
using System.Collections.Generic;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Cisco.Conference
{
    public class Presentation : CodecApiElement
    {
        #region Fields

        [CodecApiNameAttribute("LocalInstance")]
        private Dictionary<int, LocalInstance> _localInstance = new Dictionary<int, LocalInstance>();

        [CodecApiNameAttribute("CallId")]
#pragma warning disable 649 // assigned using reflection
        private int _callId;
#pragma warning restore 649

        [CodecApiNameAttribute("Mode")]
#pragma warning disable 649 // assigned using reflection
        private RemoteSendingMode _mode;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal Presentation(CodecApiElement parent, string propertyName)
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

        public int CallId
        {
            get { return _callId; }
        }

        public RemoteSendingMode Mode
        {
            get { return _mode; }
        }

        public ReadOnlyDictionary<int, LocalInstance> LocalInstance
        {
            get { return new ReadOnlyDictionary<int, LocalInstance>(_localInstance); }
        }

        #endregion

        #region Methods

        public void Start(SendingMode sendingMode)
        {
            Codec.Send("xCommand Presentation Start SendingMode: {0}", sendingMode);
        }

        public void Start(SendingMode sendingMode, int presentationSource)
        {
            Codec.Send("xCommand Presentation Start SendingMode: {0} PresentationSource: {1}", sendingMode, presentationSource);
        }

        public void Stop()
        {
            Codec.Send("xCommand Presentation Stop");            
        }

        #endregion
    }

    public enum SendingMode
    {
        LocalRemote,
        LocalOnly
    }

    public enum RemoteSendingMode
    {
        Off,
        Sending,
        Receiving
    }
}