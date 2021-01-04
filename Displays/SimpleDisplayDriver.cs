using System;
using Crestron.SimplSharp;
using UX.Lib2.DeviceSupport;

namespace UX.Lib2.Devices.Displays
{
    public abstract class SimpleDisplayDriver : DisplayDeviceBase
    {
        private readonly string _manufacturerName;
        private readonly TimeSpan _powerOnTime;
        private readonly TimeSpan _powerOffTime;
        private DisplayDeviceInput _currentInput;
        private CTimer _busyTimer;

        public SimpleDisplayDriver(string name, string manufacturerName, TimeSpan powerOnTime, TimeSpan powerOffTime)
            : base(name)
        {
            _manufacturerName = manufacturerName;
            _powerOnTime = powerOnTime;
            _powerOffTime = powerOffTime;
        }

        public override string ManufacturerName
        {
            get { return _manufacturerName; }
        }

        public override string SerialNumber
        {
            get { throw new NotImplementedException(); }
        }

        public override DisplayDeviceInput CurrentInput
        {
            get { return _currentInput; }
        }

        public override bool SupportsDisplayUsage
        {
            get { return false; }
        }

        protected override void SetPowerFeedback(DevicePowerStatus newPowerState)
        {
            if (newPowerState != PowerStatus && _busyTimer != null)
            {
                _busyTimer.Stop();
                _busyTimer.Dispose();
                _busyTimer = null;
            }

            if (Power && newPowerState == DevicePowerStatus.PowerCooling)
            {
                _busyTimer = new CTimer(specific => SetPowerFeedback(DevicePowerStatus.PowerOff),
                    (long) _powerOffTime.TotalMilliseconds);
            }

            else if (!Power && newPowerState == DevicePowerStatus.PowerWarming)
            {
                _busyTimer = new CTimer(specific => SetPowerFeedback(DevicePowerStatus.PowerOn),
                    (long) _powerOnTime.TotalMilliseconds);
            }

            else if (PowerStatus == DevicePowerStatus.PowerWarming && newPowerState == DevicePowerStatus.PowerOn)
            {
                SendInputCommand(_currentInput);
            }

            PowerStatus = newPowerState;
        }

        protected override void ActionPowerRequest(bool powerRequest)
        {
            if (powerRequest)
            {
                SendPowerOnCommand();

                if (!Power)
                {
                    SetPowerFeedback(DevicePowerStatus.PowerWarming);
                }
            }
            else
            {
                SendPowerOffCommand();

                if (Power)
                {
                    SetPowerFeedback(DevicePowerStatus.PowerCooling);
                }
            }
        }

        public override void SetInput(DisplayDeviceInput input)
        {
            if (!Power)
            {
                Power = true;
            }

            _currentInput = input;
            SendInputCommand(_currentInput);
        }

        protected abstract void SendPowerOnCommand();
        protected abstract void SendPowerOffCommand();
        protected abstract void SendInputCommand(DisplayDeviceInput input);
    }
}