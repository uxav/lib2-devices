 
using Newtonsoft.Json.Linq;

namespace UX.Lib2.Devices.Audio.QSC
{
    /// <summary>
    /// Status of a QSys Core
    /// </summary>
    public class CoreStatus
    {
        internal CoreStatus(JToken data)
        {
            Code = data["Code"].Value<int>();
            String = data["String"].Value<string>();
        }

        /// <summary>
        /// The status code of the Core
        /// </summary>
        public int Code { get; private set; }

        /// <summary>
        /// The status description of the Core
        /// </summary>
        public string String { get; private set; }

        /// <summary>
        /// The status summary of the core as a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0} \"{1}\"", Code, String);
        }
    }
}