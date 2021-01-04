 
using System.Collections.Generic;

namespace UX.Lib2.Devices.Audio.Biamp.Helpers
{
    public sealed class VoipCallStatusStateMap
    {
        private static readonly List<VoipCallStatusStateMap> myList = new List<VoipCallStatusStateMap>();
        public static readonly VoipCallStatusStateMap INIT = new VoipCallStatusStateMap(1, "Init");
        public static readonly VoipCallStatusStateMap FAULT = new VoipCallStatusStateMap(2, "Fault");
        public static readonly VoipCallStatusStateMap IDLE = new VoipCallStatusStateMap(3, "Idle");
        public static readonly VoipCallStatusStateMap DIAL_TONE = new VoipCallStatusStateMap(4, "Dial Tone");
        public static readonly VoipCallStatusStateMap SILENT = new VoipCallStatusStateMap(5, "Silent");
        public static readonly VoipCallStatusStateMap DIALING = new VoipCallStatusStateMap(6, "Dialing");
        public static readonly VoipCallStatusStateMap RINGBACK = new VoipCallStatusStateMap(7, "Ringback");
        public static readonly VoipCallStatusStateMap RINGING = new VoipCallStatusStateMap(8, "Incoming Call");
        public static readonly VoipCallStatusStateMap BUSY = new VoipCallStatusStateMap(10, "Busy");
        public static readonly VoipCallStatusStateMap REJECT = new VoipCallStatusStateMap(11, "Reject");
        public static readonly VoipCallStatusStateMap INVALID_NUMBER = new VoipCallStatusStateMap(12, "Invalid Number");
        public static readonly VoipCallStatusStateMap ACTIVE = new VoipCallStatusStateMap(13, "Active");
        public static readonly VoipCallStatusStateMap ACTIVE_MUTED = new VoipCallStatusStateMap(14, "Active Muted");
        public static readonly VoipCallStatusStateMap ON_HOLD = new VoipCallStatusStateMap(15, "On Hold");
        public static readonly VoipCallStatusStateMap WAITING_RING = new VoipCallStatusStateMap(16, "Waiting Ring");
        public static readonly VoipCallStatusStateMap CONF_ACTIVE = new VoipCallStatusStateMap(17, "Conference Active");
        public static readonly VoipCallStatusStateMap CONF_HOLD = new VoipCallStatusStateMap(18, "Conference Hold");
        public static readonly VoipCallStatusStateMap XFER_INIT = new VoipCallStatusStateMap(19, "Transfer Init");
        public static readonly VoipCallStatusStateMap XFER_SILENT = new VoipCallStatusStateMap(20, "Transfer Silent");
        public static readonly VoipCallStatusStateMap XFER_REQ_DIALING = new VoipCallStatusStateMap(21, "Transfer Request Dialing");
        public static readonly VoipCallStatusStateMap XFER_RINGBACK = new VoipCallStatusStateMap(25, "Transfer Ringback");
        public static readonly VoipCallStatusStateMap XFER_ACTIVE = new VoipCallStatusStateMap(24, "Transfer Active");
        public static readonly VoipCallStatusStateMap XFER_WAIT = new VoipCallStatusStateMap(29, "Transfer Wait");
        public static readonly VoipCallStatusStateMap XFER_DECISION = new VoipCallStatusStateMap(27, "Transfer Decision");
        public static readonly VoipCallStatusStateMap XFER_INIT_ERROR = new VoipCallStatusStateMap(28, "Transfer Init Error");
        public static readonly VoipCallStatusStateMap XFER_ON_HOLD = new VoipCallStatusStateMap(26, "Transfer On Hold");
        public static readonly VoipCallStatusStateMap XFER_REPLACES_PROCESS = new VoipCallStatusStateMap(23, "Transfer Replaces Process");
        public static readonly VoipCallStatusStateMap XFER_PROCESS = new VoipCallStatusStateMap(22, "Transfer Process");

        public ushort Number { private set; get; }

        public string Name { private set; get; }

        private VoipCallStatusStateMap(ushort number, string name)
        {
            Number = number;
            Name = name;
            myList.Add(this);
        }

        public static VoipCallStatusStateMap Find(ushort number)
        {
            return myList.Find(x => (int)x.Number == (int)number);
        }

        public static VoipCallStatusStateMap Find(string name)
        {
            return myList.Find(x => x.Name == name);
        }
    }
}