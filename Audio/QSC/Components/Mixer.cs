 
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace UX.Lib2.Devices.Audio.QSC.Components
{
    public class Mixer : ComponentBase
    {
        #region Fields
        #endregion

        #region Constructors

        internal Mixer(QsysCore core, JToken data)
            : base(core, data)
        {
            var inputs = new List<MixerItem>();
            var controlNames = new List<string>();
            for (var i = 1; i <= NumberOfInputs; i++)
            {
                controlNames.AddRange(new[]
                {
                    string.Format("input.{0}.gain", i),
                    string.Format("input.{0}.mute", i),
                    string.Format("input.{0}.trim", i),
                    string.Format("input.{0}.solo", i),
                    string.Format("input.{0}.invert", i)
                });

                if ((NumberOfStereoInputs > 0 || NumberOfStereoOutputs > 0) && Properties["pan_strategy"] != "0")
                {
                    controlNames.Add(string.Format("input.{0}.pan", i));
                }

                if (HasLabels)
                {
                    controlNames.Add(string.Format("input.{0}.label", i));
                }
            }

            var controls = RegisterControls(controlNames.ToArray());
            for (var i = 1; i <= NumberOfInputs; i++)
            {
                var pattern = string.Format("input.{0}.", i);
                var matchedControls = controls.Where(c => c.Name.StartsWith(pattern));
                var input = new MixerInput(this, matchedControls);
                inputs.Add(input);
            }

            Inputs = new MixerItemCollection(inputs);

            controlNames.Clear();
            var outputs = new List<MixerItem>();
            for (var i = 1; i <= NumberOfOutputs; i++)
            {
                controlNames.AddRange(new[]
                {
                    string.Format("output.{0}.gain", i),
                    string.Format("output.{0}.mute", i),
                    string.Format("output.{0}.invert", i)
                });

                if (HasLabels)
                {
                    controlNames.Add(string.Format("output.{0}.label", i));
                }
            }

            controls = RegisterControls(controlNames.ToArray());
            for (var i = 1; i <= NumberOfOutputs; i++)
            {
                var pattern = string.Format("output.{0}.", i);
                var matchedControls = controls.Where(c => c.Name.StartsWith(pattern));
                var output = new MixerOutput(this, matchedControls);
                outputs.Add(output);
            }

            Outputs = new MixerItemCollection(outputs);
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public int NumberOfMonoInputs
        {
            get { return Properties.ContainsKey("n_inputs") ? int.Parse(Properties["n_inputs"]) : 0; }
        }

        public int NumberOfStereoInputs
        {
            get { return Properties.ContainsKey("n_inputs") ? int.Parse(Properties["n_stereo_inputs"]) : 0; }
        }

        public int NumberOfInputs
        {
            get { return NumberOfMonoInputs + NumberOfStereoInputs; }
        }

        public int NumberOfMonoOutputs
        {
            get { return Properties.ContainsKey("n_outputs") ? int.Parse(Properties["n_outputs"]) : 0; }
        }

        public int NumberOfStereoOutputs
        {
            get { return Properties.ContainsKey("n_outputs") ? int.Parse(Properties["n_stereo_outputs"]) : 0; }
        }

        public int NumberOfOutputs
        {
            get { return NumberOfMonoOutputs + NumberOfStereoOutputs; }
        }

        public bool HasLabels
        {
            get { return Properties.ContainsKey("label_controls") && Properties["label_controls"] == "True"; }
        }

        public MixerItemCollection Inputs { get; private set; }

        public MixerItemCollection Outputs { get; private set; }

        #endregion

        #region Methods

        internal override void OnControlChange(QsysControl control, QsysControlValueChangeEventArgs args)
        {
            base.OnControlChange(control, args);
        }

        #endregion
    }
}