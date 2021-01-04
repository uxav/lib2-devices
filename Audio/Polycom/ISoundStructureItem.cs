 
using UX.Lib2.Models;

namespace UX.Lib2.Devices.Audio.Polycom
{
    public interface ISoundstructureItem : IAudioLevelControl
    {
        Soundstructure Device { get; }
        bool SupportsFader { get; }
        double Fader { get; set; }
        double FaderMin { get; }
        double FaderMax { get; }
        void Init();
        bool Initialised { get; }
    }
}