namespace UX.Lib2.Devices.Lightware3
{
    public class LightwareOutput : LightwareInputOutput
    {
        private string _name = string.Empty;
        private LightwareInput _videoInput;

        public LightwareOutput(LightwareMatrix device, uint number)
            : base(device, number)
        {

        }

        public override IOType Type
        {
            get { return IOType.Output; }
        }

        public override string Name
        {
            get { return _name; }
            internal set { _name = value; }
        }

        public LightwareInput VideoInput
        {
            get { return _videoInput; }
        }

        public void SetVideoInput(LightwareInput input)
        {
            Device.Send("CALL /MEDIA/XP/VIDEO:switch(I{0}:O{1})", input != null ? input.Number : 0, Number);
        }

        internal bool UpdateVideoInputFeedback(LightwareInput lightwareInput)
        {
            if (lightwareInput == _videoInput) return false;

            _videoInput = lightwareInput;
            return true;
        }
    }
}