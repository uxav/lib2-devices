 
using System;

namespace UX.Lib2.Devices.Cisco
{
    internal class CodecCommandArg
    {
        #region Fields

        private readonly string _argName;
        private readonly object _value;

        #endregion

        #region Constructors

        internal CodecCommandArg(string argName, object value)
        {
            _argName = argName;
            _value = value;
        }

        internal CodecCommandArg(Enum arg)
        {
            _argName = arg.GetType().Name;
            _value = arg.ToString();
        }
                
        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public string Name
        {
            get { return _argName; }
        }

        public object Value
        {
            get { return _value; }
        }

        public int Count { get; set; }

        #endregion

        #region Methods
        #endregion
    }
}