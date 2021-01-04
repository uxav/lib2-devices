namespace UX.Lib2.Devices.Cameras.Sony
{
    public interface IViscaSocket
    {
        bool Initialized { get; }
        void Send(byte[] data);
        string IpAddress { get; }
        void Initialize();
    }
}