 
using Newtonsoft.Json.Linq;

namespace UX.Lib2.Devices.Audio.QSC.Components
{
    public class PotsPhone : PhoneBase
    {
        internal PotsPhone(QsysCore core, JToken data)
            : base(core, data)
        {
            //Unused controls
            /*call.autoanswer
             * call.autoanswer.rings
             * call.dnd
             * call.dtmf.rx
             * call.dtmf.tx
             * call.history
             * clear.call.history
             * dtmf.continue
             * dtmf.local.gain
             * dtmf.tone.enable
             * dtmftone.filename
             * entry.tone.enable
             * entry.tone.gain
             * entrytone.filename
             * exit.tone.enable
             * exit.tone.gain
             * exittone.filename
             * place.call
             * recent.calls
             * ringing.tone.enable
             * ringing.tone.gain
             * ringtone.filename
             * tone.player.all.files
             * tone.player.directory
             * tone.player.directory.ui
             * tone.player.fast.forward
             * tone.player.filename
             * tone.player.filename.ui
             * tone.player.gain
             * tone.player.locate
             * tone.player.loop
             * tone.player.mute
             * tone.player.pause
             * tone.player.pause.state.trigger
             * tone.player.paused
             * tone.player.play
             * tone.player.play.on.startup
             * tone.player.play.state.trigger
             * tone.player.playing
             * tone.player.progress
             * tone.player.remaining
             * tone.player.rewind
             * tone.player.root
             * tone.player.root.ui
             * tone.player.status
             * tone.player.stop
             * tone.player.stop.state.trigger
             * tone.player.stopped
             * tone.player.sync.state
             */
        }
    }
}