 
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UX.Lib2.Devices.Audio.QSC.Components
{
    public class MixerItemCollection : IEnumerable<MixerItem>
    {
        #region Fields

        private readonly Dictionary<uint, MixerItem> _items;

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        internal MixerItemCollection(IEnumerable<MixerItem> fromItems)
        {
            _items = new Dictionary<uint, MixerItem>();
            foreach (var mixerItem in fromItems)
            {
                _items[mixerItem.Number] = mixerItem;
            }
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        public MixerItem this[uint number]
        {
            get { return _items[number]; }
        }

        #region Properties
        #endregion

        #region Methods

        public IEnumerator<MixerItem> GetEnumerator()
        {
            return _items.Values.OrderBy(i => i.Number).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}