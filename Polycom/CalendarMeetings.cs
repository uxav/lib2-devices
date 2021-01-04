using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.Models;

namespace UX.Lib2.Devices.Polycom
{
    public class CalendarMeetings : IEnumerable<CalendarMeeting>
    {
        private readonly PolycomGroupSeriesCodec _codec;
        private ReceivingMode _feedbackReceivingMode;
        private readonly Dictionary<string, CalendarMeeting> _meetings = new Dictionary<string, CalendarMeeting>();
        private readonly Dictionary<string, CalendarMeeting> _meetingsTemp = new Dictionary<string, CalendarMeeting>();
        private CalendarMeeting _currentParsedMeeting;
        private bool _started;
        private static bool _testRegistered = false;

        public CalendarMeetings(PolycomGroupSeriesCodec codec)
        {
            _codec = codec;
            _codec.ReceivedFeedback += (c, data) => OnReceive(data);
            if(_testRegistered) return;
            _testRegistered = true;
            CrestronConsole.AddNewConsoleCommand(parameters =>
            {
                var now = DateTime.Now;
                var time = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Local);
                var meetings = new Dictionary<string, CalendarMeeting>();
                for (var m = 0; m < 3; m ++)
                {
                    var meeting = new CalendarMeeting(_codec, "id" + m, time, time + TimeSpan.FromHours(1),
                        "Test Meeting " + (m + 1));
                    time = time + TimeSpan.FromMinutes(90);
                    meetings["id" + m] = meeting;
                    meeting.Organizer = "John Smith";
                    meeting.CanDial = true;
                    meeting.IsPublic = true;
                    meeting.AddDiallingNumber(new MeetingDiallingNumber(parameters, DiallingNumberType.Video));
                    Debug.WriteInfo("Created test meeting", "{0} {1} {2}", meeting.Id, meeting.Name, meeting.TimeDetailsString);
                }
                _meetings.Clear();
                foreach (var meeting in meetings)
                {
                    _meetings[meeting.Key] = meeting.Value;
                }
                OnMeetingsUpdated(this, CurrentMeeting, NextMeeting);
            }, "TestGsMeetings", "Setup some test meetings", ConsoleAccessLevelEnum.AccessOperator);
        }

        public event CalendarMeetingsUpdatedEventHandler MeetingsUpdated;

        private enum ReceivingMode
        {
            NotSet,
            List,
            Info
        }

        public CalendarMeeting CurrentMeeting
        {
            get
            {
                return _meetings.Values.FirstOrDefault(m => m.IsCurrentMeeting);
            }
        }

        public CalendarMeeting NextMeeting
        {
            get
            {
                var now = DateTime.Now;
                return _meetings.Values.FirstOrDefault(m => m.StartTime > now);
            }
        }

        public IEnumerable<CalendarMeeting> CurrentOrFutureMeetings
        {
            get { return this.Where(m => !m.HasEnded); }
        }

        public IEnumerable<CalendarMeeting> FutureMeetings
        {
            get
            {
                var now = DateTime.Now;
                return this.Where(m => m.StartTime > now);
            }
        }

        public IEnumerable<CalendarMeeting> UpcomingMeetingsToday
        {
            get
            {
                return this.Where(m => m.StartTime > DateTime.Now && m.StartTime.Date == DateTime.Today);
            }
        }

        public void StartTimer(SystemBase system)
        {
            if (_started) return;

            _started = true;

            system.TimeChanged += SystemOnTimeChanged;
        }

        private void SystemOnTimeChanged(SystemBase system, DateTime time)
        {
            GetMeetings();
        }

        public void GetMeetings()
        {
            var now = DateTime.Now;
            var start = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
            var end = start + TimeSpan.FromDays(7);
            _codec.Send(string.Format("calendarmeetings list {0} {1}", start.ToString("yyyy-MM-dd:HH:mm"),
                end.ToString("yyyy-MM-dd:HH:mm")));
        }

        public void OnReceive(string data)
        {
            switch (data)
            {
                case "calendarmeetings list begin":
                    _meetingsTemp.Clear();
#if DEBUG
                    Debug.WriteInfo("Being Meetings List!");
#endif
                    _feedbackReceivingMode = ReceivingMode.List;
                    return;
                case "calendarmeetings list end":
                    _meetings.Clear();
                    foreach (var meeting in _meetingsTemp)
                    {
                        _meetings[meeting.Key] = meeting.Value;
                    }
#if DEBUG
                    Debug.WriteInfo("End Meetings List!");
#endif
                    // if meetings count is 0, we will not have any info so need to call events now
                    if (_meetings.Count == 0)
                    {
                        OnMeetingsUpdated(this, null, null);
                    }

                    _feedbackReceivingMode = ReceivingMode.NotSet;
                    return;
                case "calendarmeetings info start":
#if DEBUG
                    Debug.WriteInfo("Begin Meetings Info!");
#endif
                    _feedbackReceivingMode = ReceivingMode.Info;
                    return;
                case "calendarmeetings info end":
#if DEBUG
                    Debug.WriteInfo("End Meetings Info!");
#endif
                    _feedbackReceivingMode = ReceivingMode.NotSet;
                    _currentParsedMeeting.ReceivedInfo = true;
                    _currentParsedMeeting = null;

                    if (_meetings.Values.All(m => m.ReceivedInfo))
                    {
#if DEBUG
                        Debug.WriteInfo("Should have received all meetings info!!");
#endif
                        OnMeetingsUpdated(this, CurrentMeeting, NextMeeting);
                    }
                    return;
                default:
                    if(_feedbackReceivingMode == ReceivingMode.NotSet)
                        return;
                    break;
            }

            switch (_feedbackReceivingMode)
            {
                case ReceivingMode.List:
                    try
                    {
#if DEBUG
                        Debug.WriteInfo("Should be meeting list item");
#endif
                        var match = Regex.Match(data, @"^(\w+)\|([^\|]+)\|([\d-:]+)\|([\d-:]+)\|([^\|]*)$");
                        if (match.Success)
                        {
                            var dateMatch = new Regex(@"^(\d{4})-(\d{2})-(\d{2}):(\d{2}):(\d{2})$");

                            var stMatch = dateMatch.Match(match.Groups[3].Value);
                            var startTime = new DateTime(int.Parse(stMatch.Groups[1].Value),
                                int.Parse(stMatch.Groups[2].Value), int.Parse(stMatch.Groups[3].Value),
                                int.Parse(stMatch.Groups[4].Value), int.Parse(stMatch.Groups[5].Value), 0);

                            var etMatch = dateMatch.Match(match.Groups[4].Value);
                            var endTime = new DateTime(int.Parse(etMatch.Groups[1].Value),
                                int.Parse(etMatch.Groups[2].Value), int.Parse(etMatch.Groups[3].Value),
                                int.Parse(etMatch.Groups[4].Value), int.Parse(etMatch.Groups[5].Value), 0);

                            var meeting = new CalendarMeeting(_codec, match.Groups[2].Value, startTime, endTime,
                                match.Groups[5].Value.Trim());

                            _meetingsTemp.Add(meeting.Id, meeting);
#if DEBUG
                            Debug.WriteSuccess("Meeting", "{0} - {1}, {2}, {3}", meeting.StartTime.ToString("t"),
                                meeting.EndTime.ToString("t"), meeting.Name, meeting.Id);
#endif
                            _codec.Send(string.Format("calendarmeetings info {0}", meeting.Id));
                        }
                    }
                    catch (Exception e)
                    {
                        CloudLog.Exception(e);
                    }
                    break;
                    case ReceivingMode.Info:
                    try
                    {
#if DEBUG
                        Debug.WriteInfo("Should be meeting list info");
#endif
                        var infoMatch = Regex.Match(data, @"^([\d-:]+)\|([\d-:]+)\|([\w]+)\|([\w]+)$");
                        if (infoMatch.Success)
                        {
                            _currentParsedMeeting.CanDial = infoMatch.Groups[3].Value == "dialable";
                            _currentParsedMeeting.IsPublic = infoMatch.Groups[4].Value == "public";
                            return;
                        }

                        var matches = Regex.Matches(data, @"\|?([^\|]*)");

                        if (matches.Count > 0)
                        {
                            switch (matches[0].Groups[1].Value)
                            {
                                case "id":
                                    var id = matches[1].Groups[1].Value;
                                    if (_meetings.ContainsKey(id))
                                    {
                                        _currentParsedMeeting = _meetings[id];
                                    }
                                    break;
                                case "organizer":
                                    _currentParsedMeeting.Organizer = matches[1].Groups[1].Value;
                                    break;
                                case "location":
                                    _currentParsedMeeting.Location = matches[1].Groups[1].Value;
                                    break;
                                case "attendee":
                                    _currentParsedMeeting.Attendees.Add(matches[1].Groups[1].Value);
                                    break;
                                case "dialingnumber":
                                    var type =
                                        (DiallingNumberType)
                                            Enum.Parse(typeof (DiallingNumberType), matches[1].Groups[1].Value, true);
                                    var number = matches[2].Groups[1].Value;
                                    _currentParsedMeeting.AddDiallingNumber(new MeetingDiallingNumber(number, type));
                                    break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        CloudLog.Exception(e);
                    }
                    break;
            }
        }

        protected virtual void OnMeetingsUpdated(IEnumerable<CalendarMeeting> meetings, CalendarMeeting currentmeeting, CalendarMeeting nextmeeting)
        {
            try
            {
                var handler = MeetingsUpdated;
                if (handler != null) handler(meetings, currentmeeting, nextmeeting);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        public IEnumerator<CalendarMeeting> GetEnumerator()
        {
            return _meetings.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public delegate void CalendarMeetingsUpdatedEventHandler(
        IEnumerable<CalendarMeeting> meetings, CalendarMeeting currentMeeting, CalendarMeeting nextMeeting);
}