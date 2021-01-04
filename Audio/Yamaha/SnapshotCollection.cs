 
using System;
using System.Collections;
using System.Collections.Generic;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Audio.Yamaha
{
    public class SnapshotCollection : IEnumerable<Snapshot>
    {
        private readonly YamahaDesk _desk;
        private readonly string _address;
        private readonly Dictionary<int, Snapshot> _items = new Dictionary<int, Snapshot>();
 
        internal SnapshotCollection(YamahaDesk desk, string address)
        {
            _desk = desk;
            _address = address;
        }

        public Snapshot this[int index]
        {
            get { return _items[index]; }
        }

        public event SnapshotUpdateEventHandler SnapshotUpdated;

        public string Address
        {
            get { return _address; }
        }

        public bool ContainsItemAtIndex(int index)
        {
            return _items.ContainsKey(index);
        }

        internal void Add(int index, string[] values)
        {
            try
            {
                _items.Add(index, new Snapshot(_desk, _address, index, values));
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        internal void Update(int index, string[] values)
        {
            try
            {
                _items[index].Values = values;

                if (SnapshotUpdated != null)
                {
                    SnapshotUpdated(this, index);
                }
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        public IEnumerator<Snapshot> GetEnumerator()
        {
            return _items.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public delegate void SnapshotUpdateEventHandler(SnapshotCollection snapshots, int index);
}