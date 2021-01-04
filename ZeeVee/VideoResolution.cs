namespace UX.Lib2.Devices.ZeeVee
{
    public class VideoResolution
    {
        #region Fields
        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        internal VideoResolution(int horizontalSize, int verticalSize, float frameRate, bool interlaced)
        {
            Interlaced = interlaced;
            FrameRate = frameRate;
            VerticalSize = verticalSize;
            HorizontalSize = horizontalSize;
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public int HorizontalSize { get; private set; }
        public int VerticalSize { get; private set; }
        public float FrameRate { get; private set; }
        public bool Interlaced { get; private set; }

        #endregion

        #region Methods

        public override string ToString()
        {
            return string.Format("{0}x{1}@{2:0.##}Hz{3}",
                HorizontalSize, VerticalSize, FrameRate,
                Interlaced ? " Interlaced" : "");
        }

        #endregion
    }
}