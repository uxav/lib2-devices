using System.Text.RegularExpressions;

namespace UX.Lib2.Devices.Lightware
{
    public class LightwareOutput : LightwareInputOutput
    {
        #region Fields

        private LightwareInput _input;
        private string _statusModifier = string.Empty;

        #endregion

        #region Constructors

        public LightwareOutput(LightwareMatrix matrix, int id)
            : base(matrix, id)
        {
            
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event LightwareOutputInputChangeEventHandler InputChange;

        #endregion

        #region Delegates
        #endregion

        #region Properties

        public LightwareInput Input
        {
            get { return _input; }
            set { Matrix.Send("{0}@{1}", value != null ? value.Id : 0, Id); }
        }

        public bool Locked
        {
            get { return _statusModifier == "L" || _statusModifier == "U"; }
        }

        public bool Muted
        {
            get { return _statusModifier == "M" || _statusModifier == "U"; }
        }

        #endregion

        #region Methods

        internal void SetInputFeedback(LightwareInput input, string statusModifier)
        {
            if (_input == input && statusModifier != null && _statusModifier == statusModifier) return;
            _input = input;
            if (statusModifier != null)
            {
                _statusModifier = statusModifier;
            }
            OnInputChange(this);
        }

        protected virtual void OnInputChange(LightwareOutput output)
        {
            var handler = InputChange;
            if (handler != null) handler(output);
        }

        protected override void SocketOnReceivedData(string args)
        {
            base.SocketOnReceivedData(args);

            var match = Regex.Match(args, @"^\(O(\d{2}) I(\d{2})\)$");

            if (match.Success && int.Parse(match.Groups[1].Value) == Id)
            {
                Debug.WriteSuccess("Output Feedback", "{0} = Input {1}", Id, match.Groups[2].Value);

                var inputIndex = int.Parse(match.Groups[2].Value);

                _input = inputIndex > 0 ? Matrix.Inputs[inputIndex] : null;
                OnInputChange(this);
                return;
            }

            match = Regex.Match(args, @"^\((\d)LO(\d{2})\)$");

            if (match.Success && int.Parse(match.Groups[2].Value) == Id)
            {
                var status = int.Parse(match.Groups[1].Value);

                if (status == 1 && _statusModifier == "M")
                {
                    _statusModifier = "U";
                }
                else if (status == 1)
                {
                    _statusModifier = "L";
                }
                else if (status == 0 && _statusModifier == "U")
                {
                    _statusModifier = "M";
                }
                else
                {
                    _statusModifier = string.Empty;
                }

                OnInputChange(this);
            }
        }

        public override string ToString()
        {
            if (Locked)
            {
                return base.ToString() + "(LOCKED)";
            }
            return base.ToString();

        }

        #endregion
    }

    public delegate void LightwareOutputInputChangeEventHandler(LightwareOutput output);
}