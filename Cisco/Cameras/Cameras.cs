 
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UX.Lib2.Devices.Cisco.Cameras
{
    public class Cameras : CodecApiElement, IEnumerable<Camera>
    {
        #region Fields

        [CodecApiName("Camera")]
#pragma warning disable 649 // assigned using reflection
        private Dictionary<int, Camera> _cameras = new Dictionary<int, Camera>();
#pragma warning restore 649

        [CodecApiNameAttribute("SpeakerTrack")]
        private SpeakerTrack _speakerTrack;

        #endregion

        #region Constructors

        internal Cameras(CiscoTelePresenceCodec codec)
            : base(codec)
        {
            _speakerTrack = new SpeakerTrack(this, "SpeakerTrack");
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public Camera this[int id]
        {
            get { return _cameras[id]; }
        }

        public IEnumerable<Camera> ConnectedCameras
        {
            get { return _cameras.Values.Where(c => c.Connected); }
        }

        public SpeakerTrack SpeakerTrack
        {
            get { return _speakerTrack; }
        }

        #endregion

        #region Methods

        public IEnumerator<Camera> GetEnumerator()
        {
            return _cameras.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}