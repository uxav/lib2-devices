 
using System;
using System.Linq;
using UX.Lib2.Models;

namespace UX.Lib2.Devices.Cisco.Audio
{
    public class Microphones : CodecApiElement, IAudioLevelControl
    {
        #region Fields

        [CodecApiNameAttribute("Mute")]
#pragma warning disable 649 // assigned using reflection
        private string _mute;
#pragma warning restore 649

        #endregion

        #region Constructors

        internal Microphones(CodecApiElement parent, string propertyName)
            : base(parent, propertyName)
        {
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event AudioMuteChangeEventHandler MuteChange;
        public event AudioLevelChangeEventHandler LevelChange;

        #endregion

        #region Delegates
        #endregion

        #region Properties

        public string Name
        {
            get { return "Codec Mic Mute"; }
        }

        public AudioLevelType ControlType
        {
            get { return AudioLevelType.Microphone; }
        }

        public bool SupportsLevel
        {
            get { return false; }
        }

        public ushort Level
        {
            get { return 0; }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string LevelString
        {
            get { return null; }
        }

        public bool SupportsMute
        {
            get { return true; }
        }

        public bool Muted
        {
            get { return _mute == "On"; }
            set
            {
                if (Muted == value) return;
                Codec.Send("xCommand Audio Microphones {0}", value ? "Mute" : "Unmute");
            }
        }

        #endregion

        #region Methods

        public void Mute()
        {
            Codec.Send("xCommand Audio Microphones Mute");
        }

        public void Unmute()
        {
            Codec.Send("xCommand Audio Microphones Unmute");
        }

        public virtual void SetDefaultLevel()
        {
            Level = ushort.MaxValue / 2;
        }

        public override string ToString()
        {
            return Muted ? "Muted" : "Unmuted";
        }

        protected override void OnStatusChanged(CodecApiElement element, string[] propertyNamesWhichUpdated)
        {
            base.OnStatusChanged(element, propertyNamesWhichUpdated);

            foreach (var name in propertyNamesWhichUpdated)
            {
                switch (name)
                {
                    case "Mute":
                        if (MuteChange != null)
                        {
                            MuteChange(Muted);
                        }
                        break;
                }
            }
        }

        #endregion
    }
}