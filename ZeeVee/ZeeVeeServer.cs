using System;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.ZeeVee
{
    /// <summary>
    /// A ZeeVee MaestroZ Server Connection
    /// </summary>
    public class ZeeVeeServer
    {
        #region Fields

        private readonly ZeeVeeServerSocket _socket;

        #endregion

        #region Constructors

        /// <summary>
        /// Create an instance of a server controlled video distribution system
        /// </summary>
        /// <param name="address">The IP Address or Hostname to connect using telnet</param>
        public ZeeVeeServer(string address)
        {
            _socket = new ZeeVeeServerSocket(address, 23);
            _socket.ReceivedData += SocketOnReceivedData;
            Devices = new ZeeVeeDeviceCollection(this);

            CrestronConsole.AddNewConsoleCommand(ConsoleRoute, "ZVRoute", "Route an encoder to a decoder",
                ConsoleAccessLevelEnum.AccessOperator);
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        /// <summary>
        /// The devices discovered by the server
        /// </summary>
        public ZeeVeeDeviceCollection Devices { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize the connection and discover the devices and status
        /// </summary>
        public void Initialize()
        {
            _socket.Connect();
        }

        /// <summary>
        /// Fast switch a decoder to an encoder
        /// </summary>
        /// <param name="encoder">The ZyperHD Encoder, use null for none</param>
        /// <param name="decoder">The ZyperHD Decoder</param>
        public void JoinFastSwitched(ZeeVeeEncoder encoder, ZeeVeeDecoder decoder)
        {
            var encoderName = encoder != null ? encoder.Name : "none";

            CloudLog.Debug("ZeeVee switch: \"{0}\" => \"{1}\"", encoderName, decoder.Name);
            _socket.Send(string.Format("join {0} {1} fast-switched",
                encoderName,
                decoder.Name));
            _socket.Send(string.Format("show device status {0}", decoder.Name));
        }

        private void ConsoleRoute(string cmdParameters)
        {
            try
            {
                var args = cmdParameters.Split(' ');
                var encoder = Devices.GetByName(args[0]);

                foreach (var device in Devices.Decoders)
                {
                    JoinFastSwitched(encoder as ZeeVeeEncoder, device);
                }
            }
            catch (Exception e)
            {
                CrestronConsole.ConsoleCommandResponse("Error: {0}", e.Message);
            }
        }

        private void SocketOnReceivedData(ZeeVeeServerSocket socket, string data)
        {
            if (data.StartsWith("show device status "))
            {
                var matches = Regex.Matches(data,
                    @"device\(([\w\:]+)\);[\s]+device.gen; model=(?:[\w]+), type=([\w]+)[\s\w\.\:\;\=\,\-//]+(?:(?=device\()|(?=lastChangeIdMax))",
                    RegexOptions.Multiline);
#if DEBUG
                //CrestronConsole.PrintLine("Received status update for {0} devices", matches.Count);
#endif
                foreach (Match match in matches)
                {
                    var macAddress = match.Groups[1].Value;
                    var type = (DeviceType)Enum.Parse(typeof(DeviceType), match.Groups[2].Value, true);

                    Devices.CreateOrUpdate(macAddress, type, match.ToString());
                }
            }
        }

        #endregion
    }
}