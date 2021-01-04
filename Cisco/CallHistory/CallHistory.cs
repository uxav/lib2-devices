 
using System.Linq;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Cisco.CallHistory
{
    public class CallHistory
    {
        #region Fields

        private readonly CiscoTelePresenceCodec _codec;

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        internal CallHistory(CiscoTelePresenceCodec codec)
        {
            _codec = codec;

#if DEBUG
            CrestronConsole.AddNewConsoleCommand(parameters => Get(Filter.All, 0, 50),
                "CHTest", "Test Call History", ConsoleAccessLevelEnum.AccessOperator);
            CrestronConsole.AddNewConsoleCommand(parameters => GetLastDialed(),
                "CHTestLast", "Test Call History", ConsoleAccessLevelEnum.AccessOperator);
#endif
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties
        #endregion

        #region Methods

        internal CallHistoryResults Get(CodecCommand cmd)
        {
#if true
            Debug.WriteInfo("Getting Call History");
            foreach (var arg in cmd.Args)
            {


                Debug.WriteInfo("  " + arg.Name, arg.Value.ToString());
            }
#endif
            var response = _codec.SendCommand(cmd);
            Debug.WriteInfo(response.Xml.ToString());
            var result = response.Xml.Element("Command").Element("CallHistoryGetResult");
            var info = result.Element("ResultInfo");
            var offset = int.Parse(info.Element("Offset").Value);
            var limit = int.Parse(info.Element("Limit").Value);
            var callHistoryItems = result.Elements("Entry").Select(item => new CallHistoryItem(_codec, item)).ToList();
#if DEBUG
            Debug.WriteSuccess("CallHistory Results", callHistoryItems.Count.ToString());
            foreach (var item in callHistoryItems)
            {
                Debug.WriteInfo(item.ToString());
            }
#endif
            return new CallHistoryResults(callHistoryItems, offset, limit);
        }

        public CallHistoryResults Get(Filter filter, int offset, int limit)
        {
            var cmd = new CodecCommand("CallHistory", "Get");
            cmd.Args.Add(filter);
            cmd.Args.Add("Offset", offset);
            cmd.Args.Add("Limit", limit);
            cmd.Args.Add("DetailLevel", "Full");
            return Get(cmd);
        }

        public CallHistoryItem GetLastDialed()
        {
            return Get(Filter.Placed, 0, 1).FirstOrDefault();
        }

        #endregion
    }

    public enum Filter
    {
        All,
        Missed,
        AnsweredElsewhere,
        Forwarded,
        Placed,
        NoAnswer,
        Received,
        Rejected,
        UnacknowledgedMissed
    }
}