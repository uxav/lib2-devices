using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Audio.QSC.Components
{
    public class GenericComponent : ComponentBase
    {
        #region Fields
        #endregion

        #region Constructors

        internal GenericComponent(QsysCore core, JToken data)
            : base(core, data)
        {
            CloudLog.Debug("Created {0}", this);
            CloudLog.Debug(data.ToString(Formatting.Indented));
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

        /// <summary>
        /// Register a control with the component
        /// </summary>
        /// <param name="responseCallBack">Callback delegate used to respond on result</param>
        /// <param name="controlName"></param>
        public new void RegisterControlAsync(RegisterControlResponse responseCallBack, string controlName)
        {
            base.RegisterControlAsync(responseCallBack, controlName);
        }

        #endregion
    }
}