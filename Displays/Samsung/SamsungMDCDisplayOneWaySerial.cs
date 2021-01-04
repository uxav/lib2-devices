using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using UXLib;
using UXLib.Extensions;
using UXLib.Models;

namespace UXLib.Devices.Displays.Samsung
{
    public class SamsungMDCDisplayOneWaySerial : DisplayDevice, ISerialDevice, IVolumeDevice
    {
        public SamsungMDCDisplayOneWaySerial(string name, int displayID, IROutputPort irPort)
        {
            this.Name = name;
            this.DisplayID = displayID;

            IRPort = irPort;
            IRPort.SetIRSerialSpec(eIRSerialBaudRates.ComspecBaudRate9600,
                eIRSerialDataBits.ComspecDataBits8, eIRSerialParityType.ComspecParityNone, eIRSerialStopBits.ComspecStopBits1,
                Encoding.ASCII);
        }

        IROutputPort IRPort { get; set; }
        public int DisplayID { get; protected set; }

        void SendCommand(CommandType command, byte[] data)
        {
            byte[] packet = SamsungMDCSocket.BuildCommand(command, this.DisplayID, data);

            if (this.IRPort != null)
                this.Send(packet, packet.Length);
        }

        void Send(byte[] bytes, int length)
        {
            // Packet must start with correct header
            if (bytes[0] == 0xAA)
            {
                int dLen = bytes[3];
                byte[] packet = new byte[dLen + 5];
                Array.Copy(bytes, packet, bytes.Length);
                int chk = 0;
                for (int i = 1; i < bytes.Length; i++)
                    chk = chk + bytes[i];
                packet[packet.Length - 1] = (byte)chk;
#if DEBUG
                CrestronConsole.Print("Samsung Tx: ");
                Tools.PrintBytes(packet, packet.Length);
#endif
                this.IRPort.SendSerialData(packet, packet.Length);
            }
            else
            {
                throw new FormatException("Packet did not begin with correct value");
            }
        }

        public override bool Power
        {
            get
            {
                return base.RequestedPower;
            }
            set
            {
                byte[] data = new byte[1];
                data[0] = Convert.ToByte(value);
                SendCommand(CommandType.Power, data);
                base.Power = value;

                if (this.PowerStatus == DevicePowerStatus.PowerOn && !this.Power)
                {
                    this.PowerStatus = DevicePowerStatus.PowerCooling;
                    new CTimer(SimulatePowerStatus, DevicePowerStatus.PowerOff, 5000);
                }
                else if (this.PowerStatus == DevicePowerStatus.PowerOff && this.Power)
                {
                    this.PowerStatus = DevicePowerStatus.PowerWarming;
                    new CTimer(SimulatePowerStatus, DevicePowerStatus.PowerOn, 18000);
                }
            }
        }

        void SimulatePowerStatus(object status)
        {
            this.PowerStatus = (DevicePowerStatus)status;
            if (this.PowerStatus == DevicePowerStatus.PowerOff && this.RequestedPower)
                this.Power = true;
            else if (this.PowerStatus == DevicePowerStatus.PowerOn && !this.RequestedPower)
                this.Power = false;
            else if (this.PowerStatus == DevicePowerStatus.PowerOn)
                this.Input = GetInputForCommandValue(requestedInput);
        }

        ushort _Volume;
        public ushort Volume
        {
            get
            {
                return _Volume;
            }
            set
            {
                if (value >= 0 && value <= 100)
                {
                    byte[] data = new byte[1];
                    data[0] = (byte)value;
                    SendCommand(CommandType.Volume, data);
                    _Volume = value;
                    if (VolumeChanged != null)
                        VolumeChanged(this, new VolumeChangeEventArgs(VolumeLevelChangeEventType.LevelChanged));
                }
            }
        }

        bool _Mute;
        public bool VolumeMute
        {
            get
            {
                return _Mute;
            }
            set
            {
                byte[] data = new byte[1];
                data[0] = Convert.ToByte(value);
                if (this.PowerStatus == DevicePowerStatus.PowerOn)
                {
                    SendCommand(CommandType.Mute, data);
                    _Mute = value;
                    if (VolumeChanged != null)
                        VolumeChanged(this, new VolumeChangeEventArgs(VolumeLevelChangeEventType.MuteChanged));
                }
            }
        }

        byte requestedInput = 0x00;

        public override DisplayDeviceInput Input
        {
            get
            {
                return base.Input;
            }
            set
            {
                byte[] data = new byte[1];
#if DEBUG
                CrestronConsole.PrintLine("Samsung Display set to {0}", value.ToString());
#endif
                requestedInput = GetInputCommandForInput(value);
                data[0] = requestedInput;
                SendCommand(CommandType.InputSource, data);
            }
        }

        DisplayDeviceInput GetInputForCommandValue(byte value)
        {
            switch (value)
            {
                case 0x14: return DisplayDeviceInput.VGA;
                case 0x18: return DisplayDeviceInput.DVI;
                case 0x1f: return DisplayDeviceInput.DVI;
                case 0x0c: return DisplayDeviceInput.Composite;
                case 0x04: return DisplayDeviceInput.SVideo;
                case 0x08: return DisplayDeviceInput.YUV;
                case 0x21: return DisplayDeviceInput.HDMI1;
                case 0x22: return DisplayDeviceInput.HDMI1;
                case 0x23: return DisplayDeviceInput.HDMI2;
                case 0x24: return DisplayDeviceInput.HDMI2;
                case 0x31: return DisplayDeviceInput.HDMI3;
                case 0x32: return DisplayDeviceInput.HDMI3;
                case 0x25: return DisplayDeviceInput.DisplayPort;
                case 0x60: return DisplayDeviceInput.MagicInfo;
                case 0x40: return DisplayDeviceInput.TV;
                case 0x1e: return DisplayDeviceInput.RGBHV;
            }
            throw new IndexOutOfRangeException("Input value out of range");
        }

        byte GetInputCommandForInput(DisplayDeviceInput input)
        {
            switch (input)
            {
                case DisplayDeviceInput.HDMI1: return 0x21;
                case DisplayDeviceInput.HDMI2: return 0x23;
                case DisplayDeviceInput.HDMI3: return 0x31;
                case DisplayDeviceInput.VGA: return 0x14;
                case DisplayDeviceInput.DVI: return 0x18;
                case DisplayDeviceInput.Composite: return 0x0c;
                case DisplayDeviceInput.YUV: return 0x08;
                case DisplayDeviceInput.DisplayPort: return 0x25;
                case DisplayDeviceInput.MagicInfo: return 0x60;
                case DisplayDeviceInput.TV: return 0x40;
                case DisplayDeviceInput.RGBHV: return 0x1e;
            }
            throw new IndexOutOfRangeException("Input not supported on this device");
        }

        #region IVolumeDevice Members


        public ushort VolumeLevel
        {
            get
            {
                return (ushort)Tools.ScaleRange(this.Volume, 0, 100, ushort.MinValue, ushort.MaxValue);
            }
            set
            {
                this.Volume = (ushort)(Tools.ScaleRange(value, ushort.MinValue, ushort.MaxValue, 0, 100));
            }
        }

        public bool SupportsVolumeMute
        {
            get { return true; }
        }

        public bool SupportsVolumeLevel
        {
            get { return false; }
        }

        public event VolumeDeviceChangeEventHandler VolumeChanged;

        #endregion

        public override void Initialize()
        {
            this.Power = false;
        }

        public override CommDeviceType CommunicationType
        {
            get { return CommDeviceType.OneWayIRSerial; }
        }
    }
}