using System;
using System.Collections.Generic;
using UX.Lib2.DeviceSupport;

namespace UX.Lib2.Devices.Displays.Epson
{
    public class EpsonProjector : DisplayDeviceBase
    {
        private readonly ComPortHandler _comPort;

        public EpsonProjector(string name, IComPortDevice comPort) : base(name)
        {
            _comPort = new ComPortHandler(comPort);
            _comPort.ReceivedString += ComPortOnReceivedString;
        }

        public override string ManufacturerName
        {
            get { return "Epson"; }
        }

        public override string ModelName
        {
            get { return "Unknown"; }
        }

        public override string DeviceAddressString
        {
            get { return _comPort.ToString(); }
        }

        public override string SerialNumber
        {
            get { return "Unknown"; }
        }

        public override string VersionInfo
        {
            get { return "Unknown"; }
        }

        public override DisplayDeviceInput CurrentInput
        {
            get { return DisplayDeviceInput.Unknown; }
        }

        public override IEnumerable<DisplayDeviceInput> AvailableInputs
        {
            get { throw new NotImplementedException(); }
        }

        public override bool SupportsDisplayUsage
        {
            get { return false; }
        }

        private void ComPortOnReceivedString(string receivedString)
        {
            if (receivedString.Length > 0)
            {
                DeviceCommunicating = true;
            }
        }

        protected override void SetPowerFeedback(DevicePowerStatus newPowerState)
        {
            PowerStatus = newPowerState;
        }

        protected override void ActionPowerRequest(bool powerRequest)
        {
            _comPort.Send(powerRequest ? "PWR ON" : "PWR OFF");
            SetPowerFeedback(powerRequest ? DevicePowerStatus.PowerOn : DevicePowerStatus.PowerOff);
        }

        public override void SetInput(DisplayDeviceInput input)
        {
            
        }

        public override void Initialize()
        {
            
        }
    }
}