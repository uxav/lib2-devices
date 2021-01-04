 
using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.UI;

namespace UX.Lib2.Devices.Audio.QSC.Components
{
    public class PhoneBase : ComponentBase
    {
        private string _dialedNumber;

        internal PhoneBase(QsysCore core, JToken data) : base(core, data)
        {
            RegisterControl("call.connect");
            RegisterControl("call.connect.time");
            RegisterControl("call.number");
            RegisterControl("call.clear");
            RegisterControl("call.backspace");
            RegisterControl("call.details");
            RegisterControl("call.disconnect");
            RegisterControl("call.offhook");

            RegisterControl("call.state");
            RegisterControl("call.status");
            RegisterControl("call.ringing");
            RegisterControl("call.ring");
            RegisterControl("call.cid.number");
            RegisterControl("call.cid.name");
            RegisterControl("call.cid.date.time");
            RegisterControl("call.pinpad.#");
            RegisterControl("call.pinpad.*");

            for (var i = 0; i <= 9; i++)
            {
                RegisterControl(string.Format("call.pinpad.{0}", i));
            }

            RegisterControl("config.status");
            RegisterControl("config.status.led");
        }

        public event CallStatusChangeEventHandler CallStatusChange;

        public event CallTimerChangeEventHandler CallTimerChange;

        public event CallNameChangeEventHandler CallNameChange;

        public event CallDialStringChangeEventHandler NumberChange;

        public string Number
        {
            get { return this["call.number"].String; }
        }

        public bool OffHook
        {
            get { return this["call.offhook"].Value > 0; }
        }

        public bool Ringing
        {
            get { return this["call.ringing"].Value > 0; }
        }

        public string CidNumber
        {
            get { return this["call.cid.number"].String; }
        }

        public string CidName
        {
            get { return this["call.cid.name"].String; }
        }

        public TimeSpan CallTimer
        {
            get { return TimeSpan.FromSeconds(this["call.connect.time"].Value); }
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(CidName))
                    return CidName;
                return !string.IsNullOrEmpty(CidNumber) ? CidNumber : Number;
            }
        }

        public PhoneState State { get; private set; }

        public PhoneStatus Status { get; private set; }

        public void Connect()
        {
            this["call.connect"].Trigger();
        }

        public void Disconnect()
        {
            this["call.disconnect"].Trigger();
        }

        public void Clear()
        {
            this["call.clear"].Trigger();
        }

        public void Backspace()
        {
            this["call.backspace"].Trigger();
        }

        public void KeypadTrigger(char digit)
        {
            var controlName = string.Format("call.pinpad.{0}", digit);
            if (!HasControl(controlName))
            {
                throw new Exception("Keypad does not have control with char \'" + digit + "\'");
            }
            this[controlName].Trigger();
        }

        public void KeypadTrigger(UIKeypad keypad, UIKeypadButtonEventArgs args)
        {
            if (args.EventType == ButtonEventType.Pressed)
            {
                this[string.Format("call.pinpad.{0}", args.StringValue)].Trigger();
            }
        }

        internal override void OnControlChange(QsysControl control, QsysControlValueChangeEventArgs args)
        {
            try
            {
                switch (control.Name)
                {
                    case "call.state":
                        try
                        {
                            State = (PhoneState)Enum.Parse(typeof(PhoneState), control.String, true);
                        }
                        catch (Exception e)
                        {
                            CloudLog.Error("Could not parse call state value \"{0}\" into enum type {1}",
                                control.String, typeof(PhoneState).Name);
                        }
                        break;
                    case "call.status":
                        var match = Regex.Match(args.StringValue, @"(\w+)(?:[\ \-]+)?([\d\*\#]+)?");
                        if (!match.Success) return;
                        try
                        {
                            Status = (PhoneStatus)Enum.Parse(typeof(PhoneStatus), match.Groups[1].Value, true);
                        }
                        catch (Exception e)
                        {
                            CloudLog.Error("Could not parse call status value \"{0}\" into enum type {1}",
                                match.Groups[1].Value, typeof(PhoneStatus).Name);
                        }
                        _dialedNumber = match.Groups[2].Value;
                        OnCallStatusChange(this, new CallStatusChangeEventArgs(Status, _dialedNumber));
                        break;
                    case "call.connect.time":
                        OnCallTimerChange(this, CallTimer);
                        break;
                    case "call.ringing":
                        if (Ringing)
                            OnCallStatusChange(this, new CallStatusChangeEventArgs(PhoneStatus.Ringing, string.Empty));
                        break;
                    case "call.cid.number":
                    case "call.cid.name":
                        if(!Controls.ContainsKey("call.cid.number") || !Controls.ContainsKey("call.cid.name")) return;
                        OnCallNameChange(this, DisplayName);
                        break;
                    case "call.number":
                        OnNumberChange(args.StringValue);
                        break;
                }
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }

            base.OnControlChange(control, args);
        }

        protected virtual void OnNumberChange(string dialstring)
        {
            try
            {
                var handler = NumberChange;
                if (handler != null) handler(dialstring);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        protected virtual void OnCallTimerChange(PhoneBase phonecomponent, TimeSpan callTimer)
        {
            try
            {
                var handler = CallTimerChange;
                if (handler != null) handler(phonecomponent, callTimer);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        protected virtual void OnCallNameChange(PhoneBase phonecomponent, string displayname)
        {
            try
            {
                var handler = CallNameChange;
                if (handler != null) handler(phonecomponent, displayname);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        protected virtual void OnCallStatusChange(PhoneBase phonecomponent, CallStatusChangeEventArgs args)
        {
            try
            {
                var handler = CallStatusChange;
                if (handler != null) handler(phonecomponent, args);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }
    }

    public delegate void CallTimerChangeEventHandler(PhoneBase phoneComponent, TimeSpan timer);

    public delegate void CallNameChangeEventHandler(PhoneBase phoneComponent, string displayName);

    public delegate void CallDialStringChangeEventHandler(string dialString);

    public class CallStatusChangeEventArgs : EventArgs
    {
        private readonly PhoneStatus _status;
        private readonly string _number;

        internal CallStatusChangeEventArgs(PhoneStatus status, string number)
        {
            _status = status;
            _number = number;
        }

        public PhoneStatus Status
        {
            get { return _status; }
        }

        public string Number
        {
            get { return _number; }
        }
    }

    public delegate void CallStatusChangeEventHandler(PhoneBase phoneComponent, CallStatusChangeEventArgs args);

    public enum PhoneState
    {
        Inactive,
        Outgoing,
        Incoming,
        Active
    }
    
    public enum PhoneStatus
    {
        Idle,
        Initializing,
        Calibrating,
        Waiting,
        Call,
        Dialing,
        Ringing,
        Connected,
        Disconnected,
        Incoming,
        Callee
    }
}