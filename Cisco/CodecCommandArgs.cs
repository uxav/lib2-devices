 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UX.Lib2.Devices.Cisco
{
    internal class CodecCommandArgs : IEnumerable<CodecCommandArg>
    {
        #region Fields

        private readonly List<CodecCommandArg> _args = new List<CodecCommandArg>();

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        internal CodecCommandArgs()
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

        #endregion

        #region Methods

        public void Add(string argName, object value)
        {
            _args.Add(new CodecCommandArg(argName, value));
        }

        public void Add(CodecCommandArg arg)
        {
            _args.Add(arg);
        }

        public void Add(CodecCommandArg[] args)
        {
            _args.AddRange(args);
        }

        public void Add(Enum arg)
        {
            _args.Add(new CodecCommandArg(arg));
        }

        #endregion

        public IEnumerator<CodecCommandArg> GetEnumerator()
        {
            return _args.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}