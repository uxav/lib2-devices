 
using System.Collections;
using System.Collections.Generic;

namespace UX.Lib2.Devices.Cisco.CallHistory
{
    public class CallHistoryResults : IEnumerable<CallHistoryItem>
    {
        #region Fields

        public readonly List<CallHistoryItem> _items;
        private readonly int _offset;
        private readonly int _limit;

        #endregion

        #region Constructors

        internal CallHistoryResults(IEnumerable<CallHistoryItem> items, int offset, int limit)
        {
            _offset = offset;
            _limit = limit;
            _items = new List<CallHistoryItem>(items);
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public CallHistoryItem this[int index]
        {
            get { return _items[index]; }
        }

        public int Offset
        {
            get { return _offset; }
        }

        public int Limit
        {
            get { return _limit; }
        }

        public int Count
        {
            get { return _items.Count; }
        }

        #endregion

        #region Methods

        public IEnumerator<CallHistoryItem> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}