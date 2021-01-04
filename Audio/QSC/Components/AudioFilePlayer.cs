 
using System;
using Newtonsoft.Json.Linq;
using UX.Lib2.DeviceSupport;

namespace UX.Lib2.Devices.Audio.QSC.Components
{
    public class AudioFilePlayer : Gain, ITransportDevice
    {
        #region Fields
        #endregion

        #region Constructors

        internal AudioFilePlayer(QsysCore core, JToken data)
            : base(core, data)
        {
            RegisterControl("all.files");
            RegisterControl("play");
            RegisterControl("play.on.startup");
            RegisterControl("playing");
            RegisterControl("stop");
            RegisterControl("stopped");
            RegisterControl("pause");
            RegisterControl("paused");
            RegisterControl("playlist.next");
            RegisterControl("playlist.prev");
            RegisterControl("progress");
            RegisterControl("remaining");
            RegisterControl("status");
            RegisterControl("playlist.file");
            RegisterControl("root");
            RegisterControl("directory");
            RegisterControl("filename");
            RegisterControl("locate");
            RegisterControl("loop");
            RegisterControl("sync.state");
            RegisterControl("sync.playlist.seed");
            RegisterControl("sync.playlist.state");
            RegisterControl("root");
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
        /// Returns true if the player is playing
        /// </summary>
        public bool Playing
        {
            get { return HasControl("playing") && Convert.ToBoolean(this["playing"].Value); }
        }

        /// <summary>
        /// Returns true if the player is stopped
        /// </summary>
        public bool Stopped
        {
            get { return HasControl("stopped") && Convert.ToBoolean(this["stopped"].Value); }
        }

        /// <summary>
        /// Returns true if the player is paused
        /// </summary>
        public bool Paused
        {
            get { return HasControl("paused") && Convert.ToBoolean(this["paused"].Value); }
        }

        /// <summary>
        /// Get the current file progress as a scaled ushort value
        /// </summary>
        public ushort Progress
        {
            get { return HasControl("progress") ? this["progress"].PositionScaled : ushort.MinValue; }
        }

        /// <summary>
        /// Get the current file progress in time
        /// </summary>
        public TimeSpan Time
        {
            get { return HasControl("progress") ? TimeSpan.FromSeconds(this["progress"].Value) : TimeSpan.Zero; }
        }

        /// <summary>
        /// Get the current file progress remaining in time
        /// </summary>
        public TimeSpan Remaining
        {
            get { return HasControl("remaining") ? TimeSpan.FromSeconds(this["remaining"].Value) : TimeSpan.Zero; }
        }

        /// <summary>
        /// Get the current playing filename
        /// </summary>
        public string FileName 
        {
            get { return HasControl("filename") ? this["filename"].String : ""; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Start playing
        /// </summary>
        public void Play()
        {
            if(HasControl("play"))
                this["play"].Trigger();
        }

        /// <summary>
        /// Stop the player
        /// </summary>
        public void Stop()
        {
            if (HasControl("stop"))
                this["stop"].Trigger();
        }

        /// <summary>
        /// Pause playback
        /// </summary>
        public void Pause()
        {
            if (HasControl("pause"))
                this["pause"].Trigger();
        }

        /// <summary>
        /// Play next file
        /// </summary>
        public void SkipForward()
        {
            if (HasControl("playlist.next"))
                this["playlist.next"].Trigger();
        }

        /// <summary>
        /// Play previous file
        /// </summary>
        public void SkipBack()
        {
            if (HasControl("playlist.prev"))
                this["playlist.prev"].Trigger();
        }

        public void SendCommand(TransportDeviceCommand command)
        {
            switch (command)
            {
                case TransportDeviceCommand.Play:
                    Play();
                    break;
                case TransportDeviceCommand.Stop:
                    Stop();
                    break;
                case TransportDeviceCommand.Pause:
                    Pause();
                    break;
                case TransportDeviceCommand.SkipForward:
                    SkipForward();
                    break;
                case TransportDeviceCommand.SkipBack:
                    SkipBack();
                    break;
            }
        }

        /// <summary>
        /// Not implemented!
        /// </summary>
        /// <param name="command"></param>
        public void SendCommandPress(TransportDeviceCommand command)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented!
        /// </summary>
        /// <param name="command"></param>
        public void SendCommandRelease(TransportDeviceCommand command)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}