 
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Audio.QSC.Components
{
    public class SnapshotController : ComponentBase
    {
        #region Fields

        private int _lastFeedbackValue = 0;

        #endregion

        #region Constructors

        internal SnapshotController(QsysCore core, JToken data)
            : base(core, data)
        {
            var count = 10;

            try
            {
                count = int.Parse(Properties["dataset_count"]);
            }
            catch (Exception e)
            {
                CloudLog.Warn("Error in Snapshot Controller ctor, {0}, defaulting to size of 10", e.Message);
            }

            for (var i = 1; i <= count; i++)
            {
                var control = RegisterControl(string.Format("load.{0}", i));
                if (control == null) break;
                RegisterControl(string.Format("match.{0}", i)).ValueChange += OnValueChange;
                RegisterControl(string.Format("last.{0}", i)).ValueChange += OnValueChange;
            }
        }

        private void OnValueChange(QsysControl control, QsysControlValueChangeEventArgs args)
        {
            if(!(args.NewValue > 0)) return;
            var match = Regex.Match(control.Name, @"(\w+).(\d+)");
            if(!match.Success) return;
            var type = match.Groups[1].Value;
            var value = int.Parse(match.Groups[2].Value);
            if(type != "last" && type != "match") return;
            if(value == _lastFeedbackValue) return;
            _lastFeedbackValue = value;
            if(ValueLoadedChange == null) return;
            ValueLoadedChange(this, _lastFeedbackValue);
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events

        public event SnapshotValueLoadedChangedEventHandler ValueLoadedChange;

        #endregion

        #region Delegates
        #endregion

        #region Properties
        #endregion

        #region Methods

        public void Load(int snapshot)
        {
            this[string.Format("load.{0}", snapshot)].Trigger();
            UpdateAsync();
        }

        public int GetMatch()
        {
            return (from control in Controls.Values.Where(c => c.Name.StartsWith("match."))
                where control.Value > 0
                select int.Parse((Regex.Match(control.Name, @"match.(\d+)").Groups[1].Value))).FirstOrDefault();
        }

        public int GetLast()
        {
            return (from control in Controls.Values.Where(c => c.Name.StartsWith("last."))
                    where control.Value > 0
                    select int.Parse((Regex.Match(control.Name, @"last.(\d+)").Groups[1].Value))).FirstOrDefault();
        }

        #endregion
    }

    public delegate void SnapshotValueLoadedChangedEventHandler(SnapshotController snapshotController, int value);
}