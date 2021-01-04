using System.Text.RegularExpressions;
using UX.Lib2.DeviceSupport;

namespace UX.Lib2.Devices.Polycom
{
    public class Cameras
    {
        private PolycomCamera _near;
        private PolycomCamera _far;

        public Cameras(PolycomGroupSeriesCodec codec)
        {
            Codec = codec;
            codec.ReceivedFeedback += (seriesCodec, data) => OnReceive(data);
        }

        PolycomGroupSeriesCodec Codec { get; set; }

        public void Select(int camera)
        {
            Select(CameraType.Near, camera);
        }

        public void Select(CameraType type, int camera)
        {
            Codec.Send(string.Format("camera {0} {1}", type.ToString().ToLower(), camera));
        }

        public void RecallPreset(CameraType type, uint presetNumber)
        {
            Codec.Send(string.Format("preset {0} go {1}", type.ToString().ToLower(), presetNumber));
        }

        public void SetPreset(CameraType type, uint presetNumber)
        {
            Codec.Send(string.Format("preset {0} set {1}", type.ToString().ToLower(), presetNumber));
        }

        public ICamera Near
        {
            get
            {
                if (_near == null)
                {
                    _near = new PolycomCamera(Codec, CameraType.Near);
                }
                return _near;
            }
        }

        public ICamera Far
        {
            get
            {
                if (_far == null)
                {
                    _far = new PolycomCamera(Codec, CameraType.Far);
                }
                return _far;
            }
        }

        private int _selectedNearCamera;

        public int SelectedNearCamera
        {
            get
            {
                return _selectedNearCamera;
            }
        }

        public event SelectedNearCameraChangeEventHandler SelectedNearCameraChanged;

        public void Move(CameraType type, CameraDirection direction)
        {
            string d;
            switch (direction)
            {
                case CameraDirection.ZoomIn: d = "zoom+"; break;
                case CameraDirection.ZoomOut: d = "zoom-"; break;
                default: d = direction.ToString().ToLower(); break;
            }
            Codec.Send(string.Format("camera {0} move {1}", type.ToString().ToLower(), d));
        }

        public void Stop(CameraType type)
        {
            Codec.Send(string.Format("camera {0} move stop", type.ToString().ToLower()));
        }

        void OnReceive(string receivedString)
        {
            if (receivedString.Contains("camera near "))
            {
                var r = new Regex(@"camera (\w*)(?: source)? (\d)");
                var m = r.Match(receivedString);
                if (m != null && m.Groups[1].Value.ToLower() == "near")
                {
                    _selectedNearCamera = int.Parse(m.Groups[2].Value);
                    if (SelectedNearCameraChanged != null)
                        SelectedNearCameraChanged(Codec);
                }
            }
        }
    }

    public delegate void SelectedNearCameraChangeEventHandler(PolycomGroupSeriesCodec codec);

    public enum CameraType
    {
        Near,
        Far
    }

    public enum CameraDirection
    {
        Up,
        Down,
        Left,
        Right,
        ZoomIn,
        ZoomOut
    }
}