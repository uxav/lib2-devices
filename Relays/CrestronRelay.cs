using Crestron.SimplSharp;
using Crestron.SimplSharpPro;

namespace UX.Lib2.Devices.Relays
{
    public class CrestronRelay
    {
        #region Fields

        private readonly Relay _relay;
        private CTimer _pulseTimer;

        #endregion

        #region Constructors

        /// <summary>
        /// The default Constructor.
        /// </summary>
        public CrestronRelay(Relay relay)
        {
            _relay = relay;
            _relay.StateChange += RelayOnStateChange;
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
        /// Returns true if relay is closed
        /// </summary>
        public bool Closed
        {
            get { return _relay.State; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Register the relay with the control system.
        /// </summary>
        public void Register()
        {
            _relay.Register();
        }

        /// <summary>
        /// Close the relay. If Pulse is running it will cancel the timer and remain closed.
        /// </summary>
        public void Close()
        {
            if (_pulseTimer != null && !_pulseTimer.Disposed)
            {
                _pulseTimer.Stop();
                _pulseTimer.Dispose();
            }

            _relay.Close();
        }

        /// <summary>
        /// Open the relay. If Pulse is running it will cancel the timer.
        /// </summary>
        public void Open()
        {
            if (_pulseTimer != null && !_pulseTimer.Disposed)
            {
                _pulseTimer.Stop();
                _pulseTimer.Dispose();
            }

            _relay.Open();
        }

        /// <summary>
        /// Pulse the relay for the time specified.
        /// Calling this while a pulse is running will restart the timer with the time specified
        /// </summary>
        /// <param name="time">Time in milliseconds</param>
        public void Pulse(int time)
        {
            if (_pulseTimer != null && !_pulseTimer.Disposed)
            {
                _pulseTimer.Stop();
                _pulseTimer.Dispose();
            }

            _relay.Close();

            _pulseTimer = new CTimer(specific =>
            {
                _pulseTimer.Dispose();
                Open();
            }, time);
        }

        private void RelayOnStateChange(Relay relay, RelayEventArgs args)
        {
#if DEBUG
            CrestronConsole.PrintLine("{0} is {1}", relay, args.State ? "Closed" : "Open");
#endif
        }

        #endregion
    }
}