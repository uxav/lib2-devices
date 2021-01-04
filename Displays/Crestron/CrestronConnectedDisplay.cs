using System;
using System.Collections.Generic;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronConnected;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.DeviceSupport;
using UX.Lib2.Models;

namespace UX.Lib2.Devices.Displays.Crestron
{
    public class CrestronConnectedDisplay : DisplayDeviceBase
    {
        private readonly RoomViewConnectedDisplay _display;
        private bool _firstFeedback;

        public CrestronConnectedDisplay(uint ipId, CrestronControlSystem controlSystem, string name) : base(name)
        {
            _display = new RoomViewConnectedDisplay(ipId, controlSystem)
            {
                Description = name
            };

            _display.OnlineStatusChange += DisplayOnOnlineStatusChange;

            IpIdFactory.Block(ipId, IpIdFactory.DeviceType.Other);

            _display.BaseEvent += DisplayOnBaseEvent;
            var result = _display.Register();
            if (result != eDeviceRegistrationUnRegistrationResponse.Success)
            {
                CloudLog.Error("Error trying to register device {0} with Id {1}, {2}", _display.GetType().Name, ipId,
                    result);
            }
        }

        public override string ManufacturerName
        {
            get { return "CrestronConnected"; }
        }

        public override string ModelName
        {
            get
            {
                return string.IsNullOrEmpty(_display.ProjectorName.StringValue)
                    ? "Unknown"
                    : _display.ProjectorName.StringValue;
            }
        }

        public override string DeviceAddressString
        {
            get
            {
                try
                {
                    return _display.IpAddressFeedback.StringValue;
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        public override string SerialNumber
        {
            get { return "Unknown"; }
        }

        public override string VersionInfo
        {
            get
            {
                try
                {
                    return _display.DeviceIdStringFeedback.StringValue;
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        public override DisplayDeviceInput CurrentInput
        {
            get { return DisplayDeviceInput.Unknown; }
        }

        public override IEnumerable<DisplayDeviceInput> AvailableInputs
        {
            get
            {
                return new List<DisplayDeviceInput>()
                {
                    DisplayDeviceInput.Unknown
                };
            }
        }

        public override bool SupportsDisplayUsage
        {
            get { return true; }
        }

        protected override void SetPowerFeedback(DevicePowerStatus newPowerState)
        {
            PowerStatus = newPowerState;

            if (_firstFeedback)
            {
                _firstFeedback = false;
                Power = Power;
            }
            else if (RequestedPower != Power)
            {
                //ActionPowerRequest(RequestedPower);
            }
        }

        protected override void ActionPowerRequest(bool powerRequest)
        {
            if (powerRequest) _display.PowerOn();
            else _display.PowerOff();
        }

        public override void SetInput(DisplayDeviceInput input)
        {
            
        }

        public override void Initialize()
        {
            
        }

        private void DisplayOnBaseEvent(GenericBase device, BaseEventArgs args)
        {
            switch (args.EventId)
            {
                case RoomViewConnectedDisplay.WarmingUpFeedbackEventId:
                    if (_display.WarmingUpFeedback.BoolValue)
                    {
                        SetPowerFeedback(DevicePowerStatus.PowerWarming);
                    }
                    else if (_display.PowerOnFeedback.BoolValue)
                    {
                        SetPowerFeedback(DevicePowerStatus.PowerOn);
                    }
                    break;
                case RoomViewConnectedDisplay.CoolingDownFeedbackEventId:
                    if (_display.CoolingDownFeedback.BoolValue)
                    {
                        SetPowerFeedback(DevicePowerStatus.PowerCooling);
                    }
                    else if (_display.PowerOffFeedback.BoolValue)
                    {
                        SetPowerFeedback(DevicePowerStatus.PowerOff);
                    }
                    break;
                case RoomViewConnectedDisplay.PowerOnFeedbackEventId:
                    if (_display.PowerOnFeedback.BoolValue)
                    {
                        SetPowerFeedback(DevicePowerStatus.PowerOn);
                    }
                    break;
                case RoomViewConnectedDisplay.PowerOffFeedbackEventId:
                    if (_display.PowerOffFeedback.BoolValue)
                    {
                        SetPowerFeedback(DevicePowerStatus.PowerOff);
                    }
                    break;
            }
        }

        private void DisplayOnOnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            DeviceCommunicating = args.DeviceOnLine;
        }
    }
}