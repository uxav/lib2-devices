 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UX.Lib2.Cloud.Logger;
using UX.Lib2.DeviceSupport;

namespace UX.Lib2.Devices.Cisco.Bookings
{
    public class Bookings : CodecApiElement, IEnumerable<Booking>
    {
        #region Fields

        [CodecApiName("Current")]
        private CurrentBooking _current;

        private List<Booking> _bookings = new List<Booking>();

        #endregion

        #region Constructors

        internal Bookings(CiscoTelePresenceCodec codec)
            : base(codec)
        {
            _current = new CurrentBooking(this, "Current");
            codec.EventReceived += CodecOnEventReceived;
        }

        #endregion

        #region Events

        public event BookingsUpdatedEventHandler BookingsUpdated;

        #endregion

        #region Delegates
        #endregion

        #region Properties

        public CurrentBooking Current
        {
            get { return _current; }
        }

        #endregion

        #region Methods

        public IEnumerable<Booking> GetBookings()
        {
            var cmd = new CodecCommand("Bookings", "List");
            cmd.Args.Add("Days", 1);
            cmd.Args.Add("DayOffset", 0);

            var bookings = new List<Booking>();

            var response = Codec.SendCommand(cmd);

            if (response.Code != 200)
            {
                CloudLog.Error("Error getting bookings search, Codec Responded with {0} code", response.Code);
                return bookings;
            }

            var result = response.Xml.Element("Command").Element("BookingsListResult");

            //Debug.WriteInfo("Bookings List");
            //Debug.WriteNormal(Debug.AnsiPurple + result + Debug.AnsiReset);

            if (result.Attribute("status").Value == "Error")
            {
                var message = result.Element("Reason").Value;
                CloudLog.Error("Error getting bookings search: {0}", message);
                return bookings;
            }

            bookings.AddRange(result.Elements("Booking").Select(data => new Booking(data)));

            return bookings.ToArray();
        }

        internal void GetBookingsAsync()
        {
            var cmd = new CodecCommand("Bookings", "List");
            cmd.Args.Add("Days", 1);
            cmd.Args.Add("DayOffset", 0);

            Codec.SendCommandAsync(cmd, (id, ok, response) =>
            {
                var bookings = new List<Booking>();
#if DEBUG
                Debug.WriteInfo("Bookings List");
                Debug.WriteNormal(Debug.AnsiPurple + response + Debug.AnsiReset);
#endif
                if (response.Attribute("status").Value == "Error")
                {
                    var message = response.Element("Reason").Value;
                    CloudLog.Error("Error getting bookings search (async): {0}", message);
                }

                bookings.AddRange(response.Elements("Booking").Select(data => new Booking(data)));

                _bookings = bookings;

                if (BookingsUpdated == null) return;
                try
                {
                    BookingsUpdated(Codec, bookings);
                }
                catch (Exception e)
                {
                    CloudLog.Exception(e, "Error calling event handler");
                }
            });
        }

        private void CodecOnEventReceived(CiscoTelePresenceCodec codec, string name, Dictionary<string, string> properties)
        {
            if(name != "Bookings") return;

            if (properties.ContainsKey("Updated"))
            {
                //Debug.WriteWarn("Bookings updated... getting bookings");
                GetBookingsAsync();
            }
        }

        protected override void OnStatusChanged(CodecApiElement element, string[] propertyNamesWhichUpdated)
        {
            base.OnStatusChanged(element, propertyNamesWhichUpdated);

            if (!propertyNamesWhichUpdated.Contains("Current") || string.IsNullOrEmpty(_current.Id)) return;

            if(!Codec.HasOpenSession) return;
            
            Debug.WriteWarn("Current Booking ID Changed = " + _current.Id);
            Debug.WriteWarn("Getting bookings");

            GetBookingsAsync();
        }

        #endregion

        public IEnumerator<Booking> GetEnumerator()
        {
            return _bookings.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public delegate void BookingsUpdatedEventHandler(CiscoTelePresenceCodec codec, IEnumerable<Booking> bookings);
}