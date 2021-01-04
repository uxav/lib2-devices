 
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UX.Lib2.Models;

namespace UX.Lib2.Devices.Audio.QSC.Components
{
    public abstract class MixerItem : IGainControl, IAudioLevelControl
    {
        private readonly uint _index;
        private readonly Mixer _mixer;
        private readonly QsysControl _gainControl;
        private readonly QsysControl _muteControl;
        private readonly QsysControl _labelControl;
        private readonly QsysControl _invertControl;
        protected readonly string ControlNameRegexPattern;

        /// <summary>
        /// The default Constructor.
        /// </summary>
        internal MixerItem(Mixer mixer, IEnumerable<QsysControl> fromControls, MixerItemType type)
        {
            _mixer = mixer;
            ItemType = type;
            ControlNameRegexPattern = string.Format("{0}\\.(\\d+)\\.(\\w+)", type.ToString().ToLower());

            foreach (var control in fromControls)
            {
                if (control == null) continue;

                var details = Regex.Match(control.Name, ControlNameRegexPattern);
                _index = uint.Parse(details.Groups[1].Value);
                var controlType = details.Groups[2].Value;

                switch (controlType)
                {
                    case "gain":
                        _gainControl = control;
                        break;
                    case "mute":
                        _muteControl = control;
                        break;
                    case "label":
                        _labelControl = control;
                        break;
                    case "invert":
                        _invertControl = control;
                        break;
                }

                control.ValueChange += ControlOnValueChange;
            }
        }

        /// <summary>
        /// The mixer which contains this item
        /// </summary>
        public Mixer Mixer
        {
            get { return _mixer; }
        }

        /// <summary>
        /// The name of the item
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The 1 based index number of the item
        /// </summary>
        public uint Number
        {
            get { return _index; }
        }

        /// <summary>
        /// The label if the mixer uses labels, otherwise it returns empty string
        /// </summary>
        public string Label
        {
            get { return _labelControl != null ? _labelControl.String : string.Empty; }
        }

        /// <summary>
        /// The type of the item
        /// </summary>
        public MixerItemType ItemType { get; private set; }

        /// <summary>
        /// Set or get the gain by value
        /// </summary>
        public float GainValue
        {
            get { return _gainControl != null ? _gainControl.Value : 0; }
            set
            {
                if (_gainControl != null)
                    _gainControl.Value = value;
            }
        }

        public float GainMinValue
        {
            get { return -100; }
        }

        public float GainMaxValue
        {
            get { return 20; }
        }

        public float GainPosition
        {
            get { return _gainControl != null ? _gainControl.Position : 0; }
            set
            {
                if (_gainControl != null)
                    _gainControl.Position = value;
            }
        }

        public void GainRamp(float value, double time)
        {
            if (_gainControl != null)
                _gainControl.RampValue(value, time);
        }

        /// <summary>
        /// Set the inverted state of the audio
        /// </summary>
        public bool Inverted
        {
            get
            {
                if (_invertControl != null)
                    return _invertControl.Value > 0;
                return false;
            }
            set
            {
                if (_invertControl != null)
                    _invertControl.Value = value ? 1 : 0;
            }
        }

        /// <summary>
        /// Get the audio level control type. Default is NotDefined.
        /// </summary>
        public AudioLevelType ControlType { get; set; }

        /// <summary>
        /// True if this control supports Level control
        /// </summary>
        public bool SupportsLevel
        {
            get { return true; }
        }

        /// <summary>
        /// Set or Get the Audio Level / Volume Control
        /// </summary>
        public ushort Level
        {
            get { return _gainControl.PositionScaled; }
            set { _gainControl.PositionScaled = value; }
        }

        /// <summary>
        /// Get the Audio / Volume level as a string description
        /// </summary>
        public string LevelString
        {
            get { return _gainControl.String; }
        }

        /// <summary>
        /// True if this control supports Mute control
        /// </summary>
        public bool SupportsMute
        {
            get { return true; }
        }

        /// <summary>
        /// Set of Get the Audio Mute status
        /// </summary>
        public bool Muted
        {
            get { return _muteControl.Value > 0; }
            set { _muteControl.Value = value ? 1 : 0; }
        }

        /// <summary>
        /// Mute the Audio
        /// </summary>
        public void Mute()
        {
            _muteControl.Value = 1;
        }

        /// <summary>
        /// Unmute the Audio
        /// </summary>
        public void Unmute()
        {
            _muteControl.Value = 0;
        }

        public virtual void SetDefaultLevel()
        {
            Level = ushort.MaxValue / 2;
        }

        protected virtual void ControlOnValueChange(QsysControl control, QsysControlValueChangeEventArgs args)
        {
            if (control == _gainControl)
                OnAudioLevelChange(control.PositionScaled);
            else if (control == _muteControl)
                OnAudioMuteChange(control.Value > 0);
        }

        public virtual event AudioMuteChangeEventHandler MuteChange;

        private void OnAudioMuteChange(bool muted)
        {
            var handler = MuteChange;
            if (handler != null) handler(muted);
        }

        public virtual event AudioLevelChangeEventHandler LevelChange;

        private void OnAudioLevelChange(ushort level)
        {
            var handler = LevelChange;
            if (handler != null) handler(this, level);
        }

        public override string ToString()
        {
            return string.Format("{0}, {1} {2}", Name, _gainControl.String, _muteControl.String);
        }
    }

    public enum MixerItemType
    {
        Input,
        Output
    }
}