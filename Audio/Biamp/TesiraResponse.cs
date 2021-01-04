 
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Audio.Biamp
{
    public class TesiraResponse : TesiraMessage
    {
        private string _command;
        private readonly List<string> _otherCommandElements = new List<string>();
        private string _instanceId;

        internal TesiraResponse(string command, string message)
            : base(message)
        {
            _instanceId = string.Empty;
            if (string.IsNullOrEmpty(command))
            {
                throw new Exception("command argument cannot be null or empty");
            }
            Command = command;
        }

        public string Command
        {
            get { return _command; }
            private set
            {
                _command = value;

                if(this is TesiraErrorResponse) return;

                try
                {
                    var matches = Regex.Matches(_command, @"\w+");

                    if (matches.Count > 0)
                    {
                        _instanceId = matches[0].Value;
                    }

                    if (matches.Count > 1)
                    {
                        try
                        {
                            CommandType = (TesiraCommand) Enum.Parse(typeof (TesiraCommand), matches[1].Value, true);
                        }
                        catch (ArgumentException e)
                        {
                            CloudLog.Error("Could not parse TesiraCommand from \"{0}\"", matches[1].Value);
                            throw e;
                        }
                    }

                    if (matches.Count > 2)
                    {
                        try
                        {
                            AttributeCode = (TesiraAttributeCode)Enum.Parse(typeof(TesiraAttributeCode), matches[2].Value, true);
                        }
                        catch (ArgumentException e)
                        {
                            CloudLog.Error("Could not parse TesiraAttributeCode from \"{0}\"", matches[2].Value);
                            throw e;
                        }
                    }

                    if (matches.Count <= 3) return;

                    for (var i = 3; i < matches.Count; i++)
                    {
                        _otherCommandElements.Add(matches[i].Value);
                    }
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e, "Error parsing command response");
                }
            }
        }

        public override string Id
        {
            get { return _instanceId; }
        }

        public TesiraCommand CommandType { get; private set; }

        public TesiraAttributeCode AttributeCode { get; private set; }

        public string[] OtherCommandElements
        {
            get { return _otherCommandElements.ToArray(); }
        }

        public override TesiraMessageType Type
        {
            get { return string.IsNullOrEmpty(Message) ? TesiraMessageType.Ok : TesiraMessageType.OkWithResponse; }
        }

        public override string ToString()
        {
            return Type == TesiraMessageType.Ok ? Command : string.Format("{0}: {1}", Command, Message);
        }
    }
}