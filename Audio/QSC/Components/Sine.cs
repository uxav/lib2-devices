 
using Newtonsoft.Json.Linq;

namespace UX.Lib2.Devices.Audio.QSC.Components
{
    public class Sine : ComponentBase
    {
        #region Fields
        #endregion

        #region Constructors

        internal Sine(QsysCore core, JToken data)
            : base(core, data)
        {
            RegisterControl("level");
            RegisterControl("mute");
            RegisterControl("frequency");
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

        public bool On
        {
            get { return this["mute"].Value == 0; }
            set { this["mute"].Value = value ? 0 : 1; }
        }

        public float Frequency
        {
            get { return this["frequency"].Value; }
            set { this["frequency"].Value = value; }
        }

        #endregion
    }
}