 
namespace UX.Lib2.Devices.Audio.Biamp
{
    public class TesiraErrorResponse : TesiraResponse
    {
        internal TesiraErrorResponse(string command, string response)
            : base(command, response)
        {

        }

        public override TesiraMessageType Type
        {
            get { return TesiraMessageType.ErrorResponse; }
        }
    }
}