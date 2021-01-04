 
using System.Collections.Generic;
using System.Linq;

namespace UX.Lib2.Devices.Audio.Polycom
{
    public class VoipLineCollection : IEnumerable<VoipLine>
    {
        public VoipLineCollection(IEnumerable<VoipLine> lines)
        {
            _lines = new Dictionary<uint, VoipLine>();

            foreach (var line in lines.Where(line => !_lines.ContainsKey(line.Number)))
            {
                _lines.Add(line.Number, line);
            }
        }

        private readonly Dictionary<uint, VoipLine> _lines;

        public VoipLine this[uint lineNumber]
        {
            get
            {
                return _lines[lineNumber];
            }
        }

        public int Count { get { return _lines.Count; } }

        #region IEnumerable<VoipLine> Members

        public IEnumerator<VoipLine> GetEnumerator()
        {
            return _lines.Values.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}