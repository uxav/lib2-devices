 
using System.Linq;

namespace UX.Lib2.Devices.Cisco.Cameras
{
    public class SpeakerTrack : CodecApiElement
    {
        [CodecApiName("ActiveConnector")]
#pragma warning disable 649 // assigned using reflection
        private int _activeConnector;
#pragma warning restore 649

        [CodecApiName("Availability")]
#pragma warning disable 649 // assigned using reflection
        private SpeakerTrackAvailability _availability;
#pragma warning restore 649

        [CodecApiName("Status")]
#pragma warning disable 649 // assigned using reflection
        private SpeakerTrackStatus _status;
#pragma warning restore 649

        internal SpeakerTrack(CodecApiElement parent, string propertyName) : base(parent, propertyName)
        {

        }

        public int ActiveConnector
        {
            get { return _activeConnector; }
        }

        public Camera ActiveCamera
        {
            get
            {
                if (ActiveConnector == 0) return null;
                var cameras = ParentElement as Cameras;
                return cameras.FirstOrDefault(c => c.DetectedConnector == ActiveConnector);
            }
        }

        public SpeakerTrackAvailability Availability
        {
            get { return _availability; }
        }

        public SpeakerTrackStatus Status
        {
            get { return _status; }
        }

        public void Activate()
        {
            Codec.Send("xCommand Cameras SpeakerTrack Activate");
        }

        public void Deactivate()
        {
            Codec.Send("xCommand Cameras SpeakerTrack Deactivate");
        }
    }

    public enum SpeakerTrackAvailability
    {
        Off,
        Unavailable,
        Available
    }

    public enum SpeakerTrackStatus
    {
        Inactive,
        Active
    }
}