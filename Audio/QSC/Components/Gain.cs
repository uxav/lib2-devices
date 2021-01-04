 
using Newtonsoft.Json.Linq;
using UX.Lib2.Models;

namespace UX.Lib2.Devices.Audio.QSC.Components
{
    /// <summary>
    /// A gain type component
    /// </summary>
    public class Gain : ComponentBase, IAudioLevelControl, IGainControl
    {
        #region Fields
        #endregion

        #region Constructors

        internal Gain(QsysCore core, JToken data)
            : base(core, data)
        {
            RegisterControls(new[]
            {
                "gain", "mute"
            });

            foreach (var control in this)
            {
                control.ValueChange += ControlOnValueChange;
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

        public float GainValue
        {
            get { return HasControl("gain") ? this["gain"].Value : 0; }
            set
            {
                if (HasControl("gain"))
                    this["gain"].Value = value;
            }
        }

        public float GainMinValue
        {
            get { return Properties.ContainsKey("min_gain") ? float.Parse(Properties["min_gain"]) : -100; }
        }

        public float GainMaxValue
        {
            get { return Properties.ContainsKey("max_gain") ? float.Parse(Properties["max_gain"]) : 20; }
        }

        public float GainPosition
        {
            get { return HasControl("gain") ? this["gain"].Position : 0; }
            set
            {
                if (HasControl("gain"))
                    this["gain"].Position = value;
            }
        }

        #endregion

        #region Methods

        public void GainRamp(float value, double time)
        {
            if (HasControl("gain"))
                this["gain"].RampValue(value, time);
        }

        private void ControlOnValueChange(QsysControl control, QsysControlValueChangeEventArgs args)
        {
            switch (control.Name)
            {
                case "gain":
                    OnAudioLevelChange(control.PositionScaled);
                    break;
                case "mute":
                    OnAudioMuteChange(control.Value > 0);
                    break;
            }
        }

        #endregion

        #region Implementation of IAudioLevelControl

        /// <summary>
        /// Get the audio level control type. Default is NotDefined.
        /// </summary>
        public AudioLevelType ControlType { get; set; }

        /// <summary>
        /// True if this control supports Level control
        /// </summary>
        public bool SupportsLevel
        {
            get { return HasControl("gain"); }
        }

        /// <summary>
        /// Set or Get the Audio Level / Volume Control
        /// </summary>
        public ushort Level
        {
            get { return this["gain"].PositionScaled; }
            set { this["gain"].PositionScaled = value; }
        }

        /// <summary>
        /// Get the Audio / Volume level as a string description
        /// </summary>
        public string LevelString
        {
            get { return this["gain"].String; }
        }

        /// <summary>
        /// True if this control supports Mute control
        /// </summary>
        public bool SupportsMute
        {
            get { return HasControl("mute"); }
        }

        /// <summary>
        /// Set of Get the Audio Mute status
        /// </summary>
        public bool Muted
        {
            get { return SupportsMute && this["mute"].Value > 0; }
            set { this["mute"].Value = value ? 1 : 0; }
        }

        /// <summary>
        /// Mute the Audio
        /// </summary>
        public void Mute()
        {
            this["mute"].Value = 1;
        }

        /// <summary>
        /// Unmute the Audio
        /// </summary>
        public void Unmute()
        {
            this["mute"].Value = 0;
        }

        public virtual void SetDefaultLevel()
        {
            Level = ushort.MaxValue / 2;
        }

        public event AudioMuteChangeEventHandler MuteChange;

        private void OnAudioMuteChange(bool muted)
        {
            var handler = MuteChange;
            if (handler != null) handler(muted);
        }

        public event AudioLevelChangeEventHandler LevelChange;

        private void OnAudioLevelChange(ushort level)
        {
            var handler = LevelChange;
            if (handler != null) handler(this, level);
        }

        #endregion
    }
}