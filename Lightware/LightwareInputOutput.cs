using System.Text.RegularExpressions;

namespace UX.Lib2.Devices.Lightware
{
    public abstract class LightwareInputOutput
    {
        #region Fields

        private readonly LightwareMatrix _matrix;
        private readonly int _id;
        private string _name;

        #endregion

        #region Constructors

        protected LightwareInputOutput(LightwareMatrix matrix, int id)
        {
            _matrix = matrix;
            _id = id;
            _matrix.Socket.ReceivedData += SocketOnReceivedData;
            _matrix.Send("{0}name#{1}=?", this is LightwareOutput ? "o" : "i", _id);
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event LightwareInputOutputNameChangeEventHandler NameChange;

        #endregion

        #region Delegates
        #endregion

        #region Properties

        public int Id
        {
            get { return _id; }
        }

        public LightwareMatrix Matrix
        {
            get { return _matrix; }
        }

        public string Name
        {
            get { return _name; }
        }

        public InputOutputType Type
        {
            get
            {
                if (this is LightwareOutput)
                {
                    return InputOutputType.Output;
                }
                return InputOutputType.Input;
            }
        }

        #endregion

        #region Methods

        protected virtual void SocketOnReceivedData(string args)
        {
            if (args.Contains("NAME#"))
            {
                var match = Regex.Match(args, @"\((\w)NAME#(\d+)=([\w\ ]*)\)");
                if (!match.Success || int.Parse(match.Groups[2].Value) != _id) return;

                if ((match.Groups[1].Value != "I" || !(this is LightwareInput)) &&
                    (match.Groups[1].Value != "O" || !(this is LightwareOutput))) return;

                var name = match.Groups[3].Value;
                if (_name == name) return;
                _name = name;
                OnNameChange(this);
            }
        }

        protected virtual void OnNameChange(LightwareInputOutput item)
        {
            var handler = NameChange;
            if (handler != null) handler(item);
        }

        public override string ToString()
        {
            return string.Format("{0} {1} \"{2}\"", Type, Id, Name);
        }

        #endregion
    }

    public delegate void LightwareInputOutputNameChangeEventHandler(LightwareInputOutput item);

    public enum InputOutputType
    {
        Input,
        Output
    }
}