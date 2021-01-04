using UX.Lib2.DeviceSupport;

namespace UX.Lib2.Devices.Polycom
{
    public class PolycomCamera : ICamera
    {
        private readonly PolycomGroupSeriesCodec _codec;
        private readonly CameraType _type;

        internal PolycomCamera(PolycomGroupSeriesCodec codec, CameraType type)
        {
            _codec = codec;
            _type = type;
        }

        public PolycomGroupSeriesCodec Codec
        {
            get { return _codec; }
        }

        public void TiltUp()
        {
            _codec.Cameras.Move(_type, CameraDirection.Up);
        }

        public void TiltDown()
        {
            _codec.Cameras.Move(_type, CameraDirection.Down);
        }

        public void TiltStop()
        {
            _codec.Cameras.Stop(_type);
        }

        public void PanLeft()
        {
            _codec.Cameras.Move(_type, CameraDirection.Left);
        }

        public void PanRight()
        {
            _codec.Cameras.Move(_type, CameraDirection.Right);
        }

        public void PanStop()
        {
            _codec.Cameras.Stop(_type);
        }

        public void ZoomIn()
        {
            _codec.Cameras.Move(_type, CameraDirection.ZoomIn);
        }

        public void ZoomOut()
        {
            _codec.Cameras.Move(_type, CameraDirection.ZoomOut);
        }

        public void ZoomStop()
        {
            _codec.Cameras.Stop(_type);
        }
    }
}