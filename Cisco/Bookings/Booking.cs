 
using System;
using System.Linq;
using Crestron.SimplSharp.CrestronXmlLinq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Cisco.Bookings
{
    public class Booking
    {
        private readonly XElement _bookingData;
        private DateTime? _startTime;
        private DateTime? _endTime;
        private string _organizer;

        internal Booking(XElement bookingData)
        {
            _bookingData = bookingData;
        }

        public string Id
        {
            get
            {
                try
                {
                    return _bookingData.Element("Id").Value;
                }
                catch (Exception e)
                {
                    CloudLog.Error("Could not get ID for booking", e.Message);
                    throw e;
                }
            }
        }

        public string Title
        {
            get
            {
                try
                {
                    return _bookingData.Element("Title").Value;
                }
                catch (Exception e)
                {
                    CloudLog.Error("Could not get ID for booking", e.Message);
                    throw e;
                }
            }
        }

        public TimeSpan StartTimeBuffer
        {
            get
            {
                try
                {
                    return TimeSpan.FromSeconds(int.Parse(_bookingData.Element("Time").Element("StartTimeBuffer").Value));
                }
                catch (Exception e)
                {
                    CloudLog.Error("Could not get StartTimeBuffer for booking, {0}", e.Message);
                    return TimeSpan.Zero;
                }
            }
        }

        public DateTime StartTime
        {
            get
            {
                if (_startTime != null) return (DateTime) _startTime;
                try
                {
                    _startTime = DateTime.Parse(_bookingData.Element("Time").Element("StartTime").Value);
                }
                catch (Exception e)
                {
                    CloudLog.Error("Could not get StartTime for booking, {0}", e.Message);
                    return new DateTime();
                }
                return (DateTime) _startTime;
            }
        }

        public DateTime EndTime
        {
            get
            {
                if (_endTime != null) return (DateTime) _endTime;
                try
                {
                    _endTime = DateTime.Parse(_bookingData.Element("Time").Element("EndTime").Value);
                }
                catch (Exception e)
                {
                    CloudLog.Error("Could not get EndTime for booking, {0}", e.Message);
                    return new DateTime();
                }
                return (DateTime) _endTime;
            }
        }

        public string Organizer
        {
            get
            {
                if (_organizer != null) return _organizer;
                try
                {
                    var firstName = _bookingData.Element("Organizer").Element("FirstName").Value;
                    var lastName = _bookingData.Element("Organizer").Element("LastName").Value;
                    var space = !string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName) ? " " : "";
                    _organizer = firstName + space + lastName;
                    return _organizer;
                }
                catch (Exception e)
                {
                    CloudLog.Error("Could not get Organizer for booking, {0}", e.Message);
                    _organizer = string.Empty;
                    return _organizer;
                }
            }
        }

        public void Dial(CiscoTelePresenceCodec codec, DialResult callback)
        {
            try
            {
                var calls = _bookingData.Element("DialInfo").Element("Calls").Elements("Call");
                foreach (
                    var number in
                        calls.Select(call => call.Element("Number")).Where(number => number != null && !number.IsEmpty))
                {
                    codec.Calls.Dial(number.Value, new[]
                    {
                        new CodecCommandArg("BookingId", Id),
                    }, callback);
                    return;
                }
                callback.Invoke(500, "No calls to dial for this booking", 0);
            }
            catch (Exception e)
            {
                CloudLog.Error("Error trying to dial booking, {0}", e.Message);
                callback.Invoke(500, string.Format("Error: {0}", e.Message), 0);
            }
        }
    }
}