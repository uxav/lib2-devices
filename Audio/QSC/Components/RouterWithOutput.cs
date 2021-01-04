 
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Audio.QSC.Components
{
    public class RouterWithOutput : ComponentBase
    {
        private readonly Dictionary<uint, QsysControl> _controls = new Dictionary<uint, QsysControl>();
        private uint _selectedInput;

        internal RouterWithOutput(QsysCore core, JToken data)
            : base(core, data)
        {
            var names = new List<string>();
            for (uint i = 1; i <= NumberOfInputs; i++)
            {
                names.Add("output.1.input." + i + ".select");
            }

            RegisterControls(names);
            var count = 1U;
            foreach (var control in this)
            {
                _controls[count] = control;
                control.ValueChange += ControlOnValueChange;
                count ++;
            }
        }

        public RouterValueChangeEventHandler SelectedInputChange;

        public int NumberOfInputs
        {
            get { return Properties.ContainsKey("n_inputs") ? int.Parse(Properties["n_inputs"]) : 0; }
        }

        public uint SelectedInput
        {
            get { return _selectedInput; }
            set
            {
                if (!_controls.ContainsKey(value))
                {
                    throw new IndexOutOfRangeException(string.Format("{0} \"{1}\" does not contain input for value {2}",
                        GetType().Name, Name, value));
                }
                _controls[value].Value = 1;
                _selectedInput = value;
            }
        }

        private void ControlOnValueChange(QsysControl control, QsysControlValueChangeEventArgs args)
        {
            if (!(args.NewValue > 0) || !_controls.Values.Contains(control)) return;
            _selectedInput = _controls.First(k => k.Value == control).Key;
            CloudLog.Debug("Router \"{0}\" Selected Input = {1}", Name, _selectedInput);
            if (SelectedInputChange != null)
            {
                SelectedInputChange(this, _selectedInput);
            }
        }

        public delegate void RouterValueChangeEventHandler(RouterWithOutput router, uint selectedInput);
    }
}