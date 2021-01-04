 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Audio.Yamaha
{
    public class ParameterCollection : IEnumerable<Parameter>
    {
        private readonly string _address;
        private readonly List<Parameter> _items = new List<Parameter>(); 

        internal ParameterCollection(string address)
        {
            _address = address;
        }

        public Parameter this[int xIndex, int yIndex]
        {
            get { return _items.FirstOrDefault(p => p.XIndex == xIndex && p.YIndex == yIndex); }
        }

        public string Address
        {
            get { return _address; }
        }

        internal void Add(int xIndex, int yIndex, string[] values)
        {
            if (this[xIndex, yIndex] != null)
            {
                
                return;
            }

            try
            {

                _items.Add(new Parameter(_address, xIndex, yIndex, values));
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        public IEnumerator<Parameter> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}