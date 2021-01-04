namespace UX.Lib2.Devices.Audio.Biamp
{
    public interface IChannels
    {
        IoChannelBase this[uint channel] { get; }
        int NumberOfChannels { get; }
    }
}