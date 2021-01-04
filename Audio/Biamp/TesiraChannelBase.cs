 
using Newtonsoft.Json.Linq;

namespace UX.Lib2.Devices.Audio.Biamp
{
    public abstract class TesiraChannelBase
    {
        private readonly TesiraBlockBase _controlBlock;
        private readonly uint _channelNumber;
        private string _name;

        protected TesiraChannelBase(TesiraBlockBase controlBlock, uint channelNumber)
        {
            _controlBlock = controlBlock;
            _channelNumber = channelNumber;
        }

        public TesiraBlockBase ControlBlock
        {
            get { return _controlBlock; }
        }

        public string BlockName
        {
            get { return _controlBlock.Name; }
        }

        public uint ChannelNumber
        {
            get { return _channelNumber; }
        }

        public virtual string Name
        {
            get
            {
                if (string.IsNullOrEmpty(_name))
                {
                    return BlockName + " Chan 1";
                }
                return _name;
            }
            set { _name = value; }
        }

        internal abstract void UpdateFromResponse(TesiraResponse response);
        internal abstract void UpdateValue(TesiraAttributeCode attributeCode, JToken value);
    }
}