 
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UX.Lib2.Devices.Audio.Polycom
{
    public class SoundstructureItemCollection : IEnumerable<ISoundstructureItem>
    {
        internal SoundstructureItemCollection(IEnumerable<ISoundstructureItem> fromChannels)
        {
            _items = new Dictionary<string, ISoundstructureItem>();
            foreach (var item in fromChannels.Where(item => !_items.ContainsKey(item.Name)))
            {
                _items.Add(item.Name, item);
            }
        }

        readonly Dictionary<string, ISoundstructureItem> _items;

        public ISoundstructureItem this[string channelName]
        {
            get
            {
                return _items.ContainsKey(channelName) ? _items[channelName] : null;
            }
        }

        public bool Contains(string channelName)
        {
            return _items.ContainsKey(channelName);
        }

        #region IEnumerable<VirtualChannel> Members

        public IEnumerator<ISoundstructureItem> GetEnumerator()
        {
            return _items.Values.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}