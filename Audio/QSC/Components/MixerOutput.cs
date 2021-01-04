 
using System.Collections.Generic;

namespace UX.Lib2.Devices.Audio.QSC.Components
{
    public class MixerOutput : MixerItem
    {
        #region Fields
        #endregion

        #region Constructors

        internal MixerOutput(Mixer mixer, IEnumerable<QsysControl> fromControls)
            : base(mixer, fromControls, MixerItemType.Output)
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

        /// <summary>
        /// Returns the label if it exists, otherwise it's a generic name based on the index component name
        /// </summary>
        public override string Name
        {
            get { return Label.Length > 0 ? Label : string.Format("{0} Output {1}", Mixer.Name, Number); }
        }

        #endregion

        #region Methods
        #endregion
    }
}