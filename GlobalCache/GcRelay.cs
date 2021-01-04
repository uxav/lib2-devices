using Crestron.SimplSharp;

namespace UX.Lib2.Devices.GlobalCache
{
    public class GcRelay
    {
        private readonly GcRelayInterface _device;
        private readonly uint _number;
        private CTimer _pulseTimer;

        internal GcRelay(GcRelayInterface device, uint number)
        {
            _device = device;
            _number = number;
        }

        public uint Number
        {
            get { return _number; }
        }

        public void Open()
        {
            _device.Send(string.Format("setstate,1:{0},0", _number));
        }

        public void Close()
        {
            _device.Send(string.Format("setstate,1:{0},1", _number));
        }

        public void Pulse(int time)
        {
            Close();
            _pulseTimer = new CTimer(specific => Open(), time);
        }

        public void Pulse()
        {
            Pulse(500);
        }
    }
}