using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharpPro.CrestronThread;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Exterity2
{
    public class Recordings
    {
        private readonly AvediaServer _server;

        internal Recordings(AvediaServer server)
        {
            _server = server;
#if true
            CrestronConsole.AddNewConsoleCommand(parameters =>
            {
                CrestronConsole.ConsoleCommandResponse("Getting recordings");
                GetRecordings();
            }, "GetRecordings",
                "Get recordings from IPTV server", ConsoleAccessLevelEnum.AccessOperator);
#endif
        }

        public event RecordingsUpdatedEventHandler RecordingsUpdated;

        protected virtual void OnRecordingsUpdated(RecordingsUpdatedEventArgs args)
        {
            var handler = RecordingsUpdated;
            if (handler == null) return;
            try
            {
                handler(this, args);
            }
            catch (Exception e)
            {
                CloudLog.Exception(e);
            }
        }

        public void StartRecording(string name, string description, DateTime startTime, DateTime endTime, string channelName, string channelIp, int channelPort)
        {
            var requestData = new
            {
                name,
                description,
                bitrate = 20000000,
                channel = new
                {
                    @channel = channelName,
                    address = new
                    {
                        @ip = channelIp,
                        @port = channelPort
                    }
                },
                schedule = new
                {
                    @start = startTime,
                    @stop = endTime,
                }
            };
            var json = JToken.FromObject(requestData);
            var request = new ServerRequest(_server.HostNameOrIpAddress, "/api/content/recording/scheduled",
                json.ToString(),
                (response, error) =>
                {
                    if (error != HTTP_CALLBACK_ERROR.COMPLETED)
                    {
                        CloudLog.Warn("Cannot communicate with AvediaServer to get recordings");
                        OnRecordingsUpdated(new RecordingsUpdatedEventArgs
                        {
                            EventType = RecordingUpdatedEventType.StartRecording,
                            RequestFailed = true,
                            FailReason = "Could not get response from server"
                        });
                        return;
                    }

                    if (response.Code != 201)
                    {
                        CloudLog.Error("{0} HttpResponse = {1}", GetType().Name, response.Code);
                        OnRecordingsUpdated(new RecordingsUpdatedEventArgs
                        {
                            EventType = RecordingUpdatedEventType.StartRecording,
                            RequestFailed = true,
                            FailReason = "Server responded with " + response.Code + " error",
                        });
                        return;
                    }

                    var data = JToken.Parse(response.ContentString).First;

                    var id = data["Id"].Value<int>();

                    CloudLog.Error("{0} HttpResponse = {1}", GetType().Name, response.Code);
                    OnRecordingsUpdated(new RecordingsUpdatedEventArgs
                    {
                        EventType = RecordingUpdatedEventType.StartRecording,
                        RequestFailed = false,
                        RecordingId = id
                    });
                });
            _server.QueueRequest(request);
        }

        public void StopRecording(int contentId, string name)
        {
            var requestData = new
            {
                name,
                schedule = new
                {
                    @stop = DateTime.Now,
                }
            };
            var json = JToken.FromObject(requestData);
            var request = new ServerRequest(_server.HostNameOrIpAddress, "/api/content/recording/scheduled/" + contentId,
                json.ToString(), RequestType.Put,
                (response, error) =>
                {
                    if (error != HTTP_CALLBACK_ERROR.COMPLETED)
                    {
                        CloudLog.Warn("Cannot communicate with AvediaServer to get recordings");
                        OnRecordingsUpdated(new RecordingsUpdatedEventArgs
                        {
                            EventType = RecordingUpdatedEventType.StopRecording,
                            RequestFailed = true,
                            FailReason = "Could not get response from server"
                        });
                        return;
                    }

                    if (response.Code != 200)
                    {
                        CloudLog.Error("{0} HttpResponse = {1}", GetType().Name, response.Code);
                        OnRecordingsUpdated(new RecordingsUpdatedEventArgs
                        {
                            EventType = RecordingUpdatedEventType.StopRecording,
                            RequestFailed = true,
                            FailReason = "Server responded with " + response.Code + " error",
                        });
                        return;
                    }

                    var data = JToken.Parse(response.ContentString).First;

                    var id = data["Id"].Value<int>();

                    CloudLog.Error("{0} HttpResponse = {1}", GetType().Name, response.Code);
                    OnRecordingsUpdated(new RecordingsUpdatedEventArgs
                    {
                        EventType = RecordingUpdatedEventType.StopRecording,
                        RequestFailed = false,
                        RecordingId = id
                    });
                });
            _server.QueueRequest(request);
        }

        public void GetRecordings()
        {
            var request = new ServerRequest(_server.HostNameOrIpAddress, "/api/content/recording/scheduled", (response, error) =>
            {
                var recordings = new List<Recording>();

                if (error != HTTP_CALLBACK_ERROR.COMPLETED)
                {
                    CloudLog.Warn("Cannot communicate with AvediaServer to get recordings");
                    OnRecordingsUpdated(new RecordingsUpdatedEventArgs
                    {
                        EventType = RecordingUpdatedEventType.GetRecordings,
                        RequestFailed = true,
                        FailReason = "Could not get response from server",
                        Recordings = new ReadOnlyCollection<Recording>(recordings)
                    });
                    return;
                }

                if (response.Code != 200)
                {
                    CloudLog.Error("{0} HttpResponse = {1}", GetType().Name, response.Code);
                    OnRecordingsUpdated(new RecordingsUpdatedEventArgs
                    {
                        EventType = RecordingUpdatedEventType.GetRecordings,
                        RequestFailed = true,
                        FailReason = "Server responded with " + response.Code + " error",
                        Recordings = new ReadOnlyCollection<Recording>(recordings)
                    });
                    return;
                }

                try
                {
                    var data = JToken.Parse(response.ContentString)["content"];
#if true
                    Debug.WriteNormal(Debug.AnsiPurple + data.ToString(Formatting.Indented) + Debug.AnsiReset);
#endif
                    foreach (var recordingData in data.Where(r => r["status"].Value<string>() == "RECORDING"))
                    {
                        try
                        {
                            recordings.Add(new Recording(recordingData));
                        }
                        catch (Exception e)
                        {
                            CloudLog.Error("Error parsing content data, {0}", e.Message);
                        }
                    }
#if true
                    foreach (var recording in recordings)
                    {
                        Debug.WriteSuccess("Recording " + recording.Id,
                            "{0} - {1}\r\nDuration: {2}\r\n Channel IP: {3} Port: {4}, Name: {5}", recording.Name,
                            recording.Description, recording.Duration, recording.ChannelIp,
                            recording.ChannelPort, recording.ChannelName);
                    }
#endif
                    OnRecordingsUpdated(new RecordingsUpdatedEventArgs
                    {
                        EventType = RecordingUpdatedEventType.GetRecordings,
                        RequestFailed = false,
                        Recordings = new ReadOnlyCollection<Recording>(recordings)
                    });
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e);

                    OnRecordingsUpdated(new RecordingsUpdatedEventArgs
                    {
                        EventType = RecordingUpdatedEventType.GetRecordings,
                        RequestFailed = true,
                        FailReason = "Server responded but data could not be parsed, " + e.Message,
                        Recordings = new ReadOnlyCollection<Recording>(recordings)
                    });
                }
            });
            _server.QueueRequest(request);
        }
    }

    public delegate void RecordingsUpdatedEventHandler(Recordings recordings, RecordingsUpdatedEventArgs args);

    public class RecordingsUpdatedEventArgs : EventArgs
    {
        public RecordingUpdatedEventType EventType { get; internal set; }
        public bool RequestFailed { get; internal set; }
        public string FailReason { get; internal set; }
        public int RecordingId { get; internal set; }
        public ReadOnlyCollection<Recording> Recordings { get; internal set; }
    }

    public enum RecordingUpdatedEventType
    {
        StartRecording,
        GetRecordings,
        StopRecording
    }

    public class Recording
    {
        private readonly int _id;
        private readonly string _name;
        private readonly string _description;
        private readonly DateTime _startTime;
        private readonly DateTime _endTime;
        private readonly string _channelName;
        private readonly string _channelIp;
        private readonly int _channelPort;
        private readonly TimeSpan _duration;

        internal Recording(JToken data)
        {
#if true
            Debug.WriteInfo("Recording Info Created", "\r\n" + data.ToString(Formatting.Indented));
#endif
            _id = data["id"].Value<int>();
            _name = data["name"].Value<string>();
            try
            {
                _duration = TimeSpan.Parse(data["duration"].Value<string>());
            }
            catch (Exception e)
            {
                CloudLog.Error("Error parsing recording duration");
            }
            _description = data["description"].Value<string>();
            _startTime = GetTimeDateFromTimeStamp(data["recording"]["schedule"]["start"].Value<string>());
            _endTime = GetTimeDateFromTimeStamp(data["recording"]["schedule"]["end"].Value<string>());
            _channelName = data["recording"]["channel"]["name"].Value<string>();
            _channelIp = data["recording"]["channel"]["address"]["ip"].Value<string>();
            _channelPort = data["recording"]["channel"]["address"]["port"].Value<int>();
        }

        private DateTime GetTimeDateFromTimeStamp(string timeStamp)
        {
            try
            {
                var match = Regex.Match(timeStamp,
                    @"(\d{4})-(\d{2})-(\d{2}) (\d{1,2}):(\d{1,2}):(\d{1,2})([-+]?\d{1,2})");
                var result = new DateTime(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value),
                    int.Parse(match.Groups[3].Value), int.Parse(match.Groups[4].Value), int.Parse(match.Groups[5].Value),
                    int.Parse(match.Groups[6].Value));
                if (match.Groups[7].Success)
                {
                    result.AddHours(int.Parse(match.Groups[7].Value));
                }
                return result;
            }
            catch
            {
                return new DateTime();
            }
        }

        public int Id
        {
            get { return _id; }
        }

        public string Name
        {
            get { return _name; }
        }

        public string Description
        {
            get { return _description; }
        }

        public DateTime StartTime
        {
            get { return _startTime; }
        }

        public DateTime EndTime
        {
            get { return _endTime; }
        }

        public TimeSpan Duration
        {
            get { return _duration; }
        }

        public string ChannelName
        {
            get { return _channelName; }
        }

        public string ChannelIp
        {
            get { return _channelIp; }
        }

        public int ChannelPort
        {
            get { return _channelPort; }
        }
    }
}