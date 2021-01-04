using System;
using System.Collections.Generic;
using System.Linq;

namespace UX.Lib2.Devices.Polycom
{
    public class CalendarMeeting
    {
        private readonly PolycomGroupSeriesCodec _codec;
        private readonly string _id;
        private readonly DateTime _startTime;
        private readonly DateTime _endTime;
        private readonly string _name;
        private readonly List<string> _attendees = new List<string>();
        private readonly List<MeetingDiallingNumber> _diallingNumbers = new List<MeetingDiallingNumber>(); 

        internal CalendarMeeting(PolycomGroupSeriesCodec codec, string id, DateTime startTime, DateTime endTime,
            string name)
        {
            _codec = codec;
            _id = id;
            _startTime = startTime;
            _endTime = endTime;
            _name = name;
        }

        public string Id
        {
            get { return _id; }
        }

        public string Name
        {
            get
            {
                if (!IsPublic) return "Private Meeting";
                return string.IsNullOrEmpty(_name) ? "No Subject" : _name;
            }
        }

        public DateTime StartTime
        {
            get { return _startTime; }
        }

        public DateTime EndTime
        {
            get { return _endTime; }
        }

        public string TimeDetailsString
        {
            get
            {
                if (StartTime.Date == DateTime.Today)
                {
                    return string.Format("{0} - {1}", StartTime.ToString("h:mm tt"), EndTime.ToString("h:mm tt"));
                }

                return string.Format("{0} - {1}", StartTime.ToString("ddd h:mm tt"), EndTime.ToString("h:mm tt"));
            }
        }

        public bool HasEnded
        {
            get
            {
                return EndTime <= DateTime.Now;
            }
        }

        public bool IsCurrentMeeting
        {
            get
            {
                var now = DateTime.Now;
                return StartTime <= now && EndTime >= now;
            }
        }

        public bool IsToday
        {
            get { return StartTime.Date== DateTime.Today; }
        }

        public bool IsTomorrow
        {
            get { return StartTime.Date == (DateTime.Today + TimeSpan.FromDays(1)); }
        }

        public string Organizer { get; set; }
        public bool ReceivedInfo { get; set; }
        public string Location { get; set; }

        public List<string> Attendees
        {
            get { return _attendees; }
        }

        public IEnumerable<MeetingDiallingNumber> DiallingNumbers
        {
            get { return _diallingNumbers.OrderBy(d => d.Number.IsAllDigits()); }
        }

        internal void AddDiallingNumber(MeetingDiallingNumber number)
        {
            _diallingNumbers.Add(number);
        }

        public bool CanDial { get; internal set; }

        public bool IsPublic { get; internal set; }

        public void Dial()
        {
            if (CanDial)
            {
                _codec.Dial(DiallingNumbers.First().Number);
            }
            else
            {
                throw new Exception("No numbers to dial!");
            }
        }
    }
}