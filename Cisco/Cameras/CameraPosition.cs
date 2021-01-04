 
namespace UX.Lib2.Devices.Cisco.Cameras
{
    public class CameraPosition : CodecApiElement
    {
        #region Fields

        private readonly Camera _camera;

        [CodecApiName("Focus")]
#pragma warning disable 649 // assigned using reflection
        private int _focus;
#pragma warning restore 649

        [CodecApiName("Pan")]
#pragma warning disable 649 // assigned using reflection
        private int _pan;
#pragma warning restore 649

        [CodecApiName("Tilt")]
#pragma warning disable 649 // assigned using reflection
        private int _tilt;
#pragma warning restore 649

        [CodecApiName("Zoom")]
#pragma warning disable 649 // assigned using reflection
        private int _zoom;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal CameraPosition(CodecApiElement camera, string propertyName)
            : base(camera, propertyName)
        {
            _camera = (Camera) camera;
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public int Focus
        {
            get { return _focus; }
        }

        public int Pan
        {
            get { return _pan; }
        }

        public int Tilt
        {
            get { return _tilt; }
        }

        public int Zoom
        {
            get { return _zoom; }
        }

        #endregion

        #region Methods
        #endregion
    }
}