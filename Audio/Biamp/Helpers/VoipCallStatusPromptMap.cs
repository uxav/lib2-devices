 
using System.Collections.Generic;

namespace UX.Lib2.Devices.Audio.Biamp.Helpers
{
    public sealed class VoipCallStatusPromptMap
    {
        private static readonly List<VoipCallStatusPromptMap> myList = new List<VoipCallStatusPromptMap>();
        public static readonly VoipCallStatusPromptMap NONE = new VoipCallStatusPromptMap(1, "None");
        public static readonly VoipCallStatusPromptMap STARTING = new VoipCallStatusPromptMap(2, "Starting");
        public static readonly VoipCallStatusPromptMap REGISTERING = new VoipCallStatusPromptMap(3, "Registering");
        public static readonly VoipCallStatusPromptMap SIP_USER_NOT_CONFIGURED = new VoipCallStatusPromptMap(6, "SIP User Not Configured");
        public static readonly VoipCallStatusPromptMap ENTER_NUMBER = new VoipCallStatusPromptMap(7, "Enter Number");
        public static readonly VoipCallStatusPromptMap CONNECTING = new VoipCallStatusPromptMap(8, "Connecting");
        public static readonly VoipCallStatusPromptMap INCOMING_CALL_FROM = new VoipCallStatusPromptMap(9, "Incoming Call");
        public static readonly VoipCallStatusPromptMap PEER_BUSY = new VoipCallStatusPromptMap(10, "Peer Busy");
        public static readonly VoipCallStatusPromptMap CALL_CANNOT_BE_COMPLETED = new VoipCallStatusPromptMap(11, "Call Cannont Be Completed");
        public static readonly VoipCallStatusPromptMap ON_HOLD = new VoipCallStatusPromptMap(12, "On Hold");
        public static readonly VoipCallStatusPromptMap CALL_ON_HELD = new VoipCallStatusPromptMap(13, "On Held");
        public static readonly VoipCallStatusPromptMap CONFERENCE = new VoipCallStatusPromptMap(14, "Conference");
        public static readonly VoipCallStatusPromptMap CONFERENCE_ON_HOLD = new VoipCallStatusPromptMap(15, "Conference On Hold");
        public static readonly VoipCallStatusPromptMap CONNECTED = new VoipCallStatusPromptMap(16, "Connected");
        public static readonly VoipCallStatusPromptMap CONNECTED_MUTED = new VoipCallStatusPromptMap(17, "Connected Muted");
        public static readonly VoipCallStatusPromptMap AUTH_FAILURE = new VoipCallStatusPromptMap(18, "Auth Failure");
        public static readonly VoipCallStatusPromptMap PROXY_NOT_CONFIGURED = new VoipCallStatusPromptMap(19, "Proxy Not Configured");
        public static readonly VoipCallStatusPromptMap NETWORK_INIT = new VoipCallStatusPromptMap(20, "Network Init");
        public static readonly VoipCallStatusPromptMap DHCP_IN_PROGRESS = new VoipCallStatusPromptMap(21, "DHCP In Progress");
        public static readonly VoipCallStatusPromptMap NETWORK_LINK_DOWN = new VoipCallStatusPromptMap(22, "Network Link Down");
        public static readonly VoipCallStatusPromptMap NETWORK_LINK_UP = new VoipCallStatusPromptMap(23, "Network Link Up");
        public static readonly VoipCallStatusPromptMap IPADDR_CONFLICT = new VoipCallStatusPromptMap(24, "IP Address Conflict");
        public static readonly VoipCallStatusPromptMap NETWORK_CONFIGURED = new VoipCallStatusPromptMap(25, "Network Configured");
        public static readonly VoipCallStatusPromptMap CODEC_NEGOTIATION_FAILURE = new VoipCallStatusPromptMap(26, "CODEC Negotiation Failure");
        public static readonly VoipCallStatusPromptMap UNEXPECTED_ERROR = new VoipCallStatusPromptMap(27, "Unexpected Error");
        public static readonly VoipCallStatusPromptMap AUTH_USER_NOT_CONFIGURED = new VoipCallStatusPromptMap(28, "Auth User Not Configured");
        public static readonly VoipCallStatusPromptMap AUTH_PASSWORD_NOT_CONFIGURED = new VoipCallStatusPromptMap(29, "Auth Password Not Configured");
        public static readonly VoipCallStatusPromptMap DND = new VoipCallStatusPromptMap(30, "Do Not Disturb");
        public static readonly VoipCallStatusPromptMap INVALID_NUMBER = new VoipCallStatusPromptMap(31, "Invalid Dialed Number");
        public static readonly VoipCallStatusPromptMap TEMP_UNAVAILABLE = new VoipCallStatusPromptMap(32, "Temporary Not Available");
        public static readonly VoipCallStatusPromptMap DECLINED = new VoipCallStatusPromptMap(33, "Call is Declined");
        public static readonly VoipCallStatusPromptMap SERVICE_UNAVAILABLE = new VoipCallStatusPromptMap(34, "Service Unavailable");
        public static readonly VoipCallStatusPromptMap FORBIDDEN = new VoipCallStatusPromptMap(35, "Call Forbidden");
        public static readonly VoipCallStatusPromptMap BEING_XFER_TO = new VoipCallStatusPromptMap(36, "Call is Being Transfer to");
        public static readonly VoipCallStatusPromptMap XFER_IN_PROCESS = new VoipCallStatusPromptMap(37, "Transfer in Process");
        public static readonly VoipCallStatusPromptMap XFER_TIME_OUT = new VoipCallStatusPromptMap(38, "Transfer Timeout");
        public static readonly VoipCallStatusPromptMap PROXY_UNAVAILABLE = new VoipCallStatusPromptMap(39, "Proxy Unavailable");

        public int Number { private set; get; }

        public string Name { private set; get; }

        private VoipCallStatusPromptMap(int number, string name)
        {
            Number = number;
            Name = name;
            myList.Add(this);
        }

        public static VoipCallStatusPromptMap Find(int number)
        {
            return myList.Find(x => x.Number == number);
        }

        public static VoipCallStatusPromptMap Find(string name)
        {
            return myList.Find(x => x.Name == name);
        }
    }
}