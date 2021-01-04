 
namespace UX.Lib2.Devices.Audio.QSC.Components
{
    public interface IGainControl
    {
        float GainValue { get; set; }
        float GainMinValue { get; }
        float GainMaxValue { get; }
        float GainPosition { get; set; }
        void GainRamp(float value, double time);
    }
}