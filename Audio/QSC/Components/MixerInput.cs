 
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UX.Lib2.Devices.Audio.QSC.Components
{
    public class MixerInput : MixerItem
    {
        #region Fields

        private readonly QsysControl _trimControl;
        private readonly QsysControl _panControl;
        private readonly QsysControl _soloControl;

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        internal MixerInput(Mixer mixer, IEnumerable<QsysControl> fromControls)
            : base(mixer, fromControls, MixerItemType.Input)
        {
            foreach (var control in fromControls)
            {
                if(control == null) continue;
                
                var details = Regex.Match(control.Name, ControlNameRegexPattern);
                var controlType = details.Groups[2].Value;

                switch (controlType)
                {
                    case "trim":
                        _trimControl = control;
                        break;
                    case "pan":
                        _panControl = control;
                        break;
                    case "solo":
                        _soloControl = control;
                        break;
                }
            }
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        /// <summary>
        /// Returns the label if it exists, otherwise it's a generic name based on the index component name
        /// </summary>
        public override string Name
        {
            get { return Label.Length > 0 ? Label : string.Format("{0} Input {1}", Mixer.Name, Number); }
        }

        public float PanPosition
        {
            get
            {
                if (_panControl != null)
                    return _panControl.Position;
                return (float) 0.5;
            }
            set
            {
                if (_panControl != null)
                    _panControl.Value = value;
            }
        }

        public bool IsStereo
        {
            get { return _panControl != null; }
        }

        #endregion

        #region Methods

        protected override void ControlOnValueChange(QsysControl control, QsysControlValueChangeEventArgs args)
        {
            base.ControlOnValueChange(control, args);

            
        }

        #endregion
    }
}