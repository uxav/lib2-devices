using System.Text;
using UX.Lib2.Sockets;

namespace UX.Lib2.Devices.FireInterfaces
{
    public class FireAlarmServerSocket : TCPServerSocketBase
    {
        public FireAlarmServerSocket(int portNumber, int numberOfConnections)
            : base(portNumber, numberOfConnections, 10000)
        {

        }

        protected override void OnClientReceive(uint clientIndex, byte[] dataBuffer, int count)
        {
            var receivedString = Encoding.ASCII.GetString(dataBuffer, 0, count);

            if (receivedString == "ping")
            {
                var pong = Encoding.ASCII.GetBytes("pong");
                Send(clientIndex, pong, 0, pong.Length);
                return;
            }

            Debug.WriteSuccess("Received from Client " + clientIndex, receivedString);

            var response = receivedString + "\r\n";

            var bytes = Encoding.ASCII.GetBytes(response);

            SendToAll(bytes, 0, bytes.Length);
        }

        protected override void OnClientDisconnect(uint clientIndex)
        {
            
        }
    }
}