using Crestron.SimplSharpPro;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.DeviceSupport.Relays;

namespace UX.Lib2.Devices.FutureAutomations
{
    public class FaMotorController : IHoistController
    {
        private readonly IComPortDevice _comPort;

        private static readonly ComPort.ComPortSpec ComSpec = new ComPort.ComPortSpec()
        {
            BaudRate = ComPort.eComBaudRates.ComspecBaudRate9600,
            DataBits = ComPort.eComDataBits.ComspecDataBits8,
            Parity = ComPort.eComParityType.ComspecParityNone,
            StopBits = ComPort.eComStopBits.ComspecStopBits1,
            SoftwareHandshake = ComPort.eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
            HardwareHandShake = ComPort.eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
            ReportCTSChanges = false
        };

        public FaMotorController(IComPortDevice comPort)
        {
            _comPort = comPort;

            if (comPort is ComPort)
            {
                var result = ((ComPort) comPort).Register();
                if (result != eDeviceRegistrationUnRegistrationResponse.Success)
                {
                    CloudLog.Error("Could not register comport \"{0}\" for {1}", comPort.ToString(), GetType().Name);
                }
            }

            comPort.SetComPortSpec(ComSpec);
        }

        public void Up()
        {
            In();
        }

        public void Down()
        {
            Out();
        }

        public void Stop()
        {
            _comPort.Send("fa_stop\r");
        }

        public void In()
        {
            _comPort.Send("fa_in\r");
        }

        public void Out()
        {
            _comPort.Send("fa_out\r");
        }
    }
}