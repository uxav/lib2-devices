 
using System.Collections;
using System.Collections.Generic;
using Crestron.SimplSharp;

namespace UX.Lib2.Devices.Cisco.Diagnostics
{
    public class Diagnostics : CodecApiElement, IEnumerable<Message>
    {
        #region Fields

        [CodecApiNameAttribute("Message")]
        private readonly Dictionary<int, Message> _messages = new Dictionary<int, Message>(); 

        #endregion

        #region Constructors

        internal Diagnostics(CiscoTelePresenceCodec codec)
            : base(codec)
        {

        }

        #endregion

        #region Finalizers

        #endregion

        #region Events

        #endregion

        #region Delegates

        #endregion

        #region Properties

        public ReadOnlyDictionary<int, Message> Messages
        {
            get { return new ReadOnlyDictionary<int, Message>(_messages); }
        }

        #endregion

        #region Methods

        protected override void OnStatusChanged(CodecApiElement element, string[] propertyNamesWhichUpdated)
        {
            base.OnStatusChanged(element, propertyNamesWhichUpdated);

            foreach (var message in Messages.Values)
            {
                Debug.WriteWarn(message.Level.ToString(), "{0} ({1})", message.Description, message.Type);
            }
        }

        public IEnumerator<Message> GetEnumerator()
        {
            return _messages.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}