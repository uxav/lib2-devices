using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UX.Lib2.Devices.Polycom
{
    public class AddressBook
    {
        private AddressBookResultsHandler _callback;
        private List<AddressBookItem> _items = new List<AddressBookItem>();
        private List<AddressBookItem> _allItems = new List<AddressBookItem>();

        public AddressBook(PolycomGroupSeriesCodec codec)
        {
            Codec = codec;
            codec.ReceivedFeedback += (seriesCodec, data) => OnReceive(data);
        }

        PolycomGroupSeriesCodec Codec { get; set; }

        public List<AddressBookItem> AllItems
        {
            get { return _allItems; }
        }

        public void Get(AddressBookResultsHandler callback)
        {
            _callback = callback;
            Codec.Send("addrbook all");
        }

        public void SearchLocal(string searchString, int count, AddressBookResultsHandler callback)
        {
            _callback = callback;
            Codec.Send(string.Format("addrbook batch search \"{0}\" {1}", searchString, count));
        }

        public void Search(string searchString, AddressBookResultsHandler callback)
        {
            _callback = callback;
            Codec.Send(string.Format("gaddrbook batch search \"{0}\" 200", searchString));
        }

        public void Search(string searchString, int count, AddressBookResultsHandler callback)
        {
            _callback = callback;
            Codec.Send(string.Format("gaddrbook batch search \"{0}\" {1}", searchString, count));
        }

        public void Search(string searchString, int count, bool systemIsSkypeEnabled, AddressBookResultsHandler callback)
        {
            _items = new List<AddressBookItem>();
            if (systemIsSkypeEnabled)
            {
                _callback = callback;
                Codec.Send(string.Format("globaldir \"{0}\" {1}", searchString, count));
            }
            else
            {
                Search(searchString, count, callback);
            }
        }

        void OnReceive(string receivedString)
        {
            Regex endRegex;
            Regex entryRegex;
            Match match;

            if (receivedString.StartsWith("globaldir"))
            {
                endRegex = new Regex(@"^globaldir(?: \""?([^\""]*)\""? ([0-9]+))? done");
                entryRegex = new Regex(@"^globaldir *([0-9]+)\. *([^:]+) *: *(?:\w+#)?([^\ :]+)(?: *: *(\w*))?");

                match = entryRegex.Match(receivedString);
                if (match.Success)
                {
                    var index = int.Parse(match.Groups[1].Value);
#if DEBUG
                    Debug.WriteInfo(string.Format("Entry {0}", index), "{0} - {1}", match.Groups[2].Value,
                        match.Groups[3].Value);
#endif
                    if (index == 0)
                        _items = new List<AddressBookItem>();
                    var item = new AddressBookItem(Codec)
                    {
                        Name = match.Groups[2].Value,
                        Number = match.Groups[3].Value
                    };
                    item.ItemType = AddressBookItemType.SIP;
                    _items.Add(item);
                }
                else
                {
                    match = endRegex.Match(receivedString);
                    if (!match.Success) return;
#if DEBUG
                    Debug.WriteSuccess("Address book search done, searchstring: \"{0}\", count = {1}",
                        match.Groups[1].Success ? match.Groups[1].Value : string.Empty, _items.Count);
#endif
                    _callback(this, _items);
                }

                return;
            }

            if (!receivedString.StartsWith("gaddrbook") && !receivedString.StartsWith("addrbook")) return;

            endRegex = new Regex(@"^g?addrbook batch search (\w*) ([0-9]+) done");
            entryRegex = new Regex(@"^g?addrbook ([0-9]+)\. \""(.*)\"".+num:(\S*)(?:.+ext:(\S*))?");

            match = entryRegex.Match(receivedString);
            if (match.Success)
            {
                var index = int.Parse(match.Groups[1].Value);
#if DEBUG
                Debug.WriteInfo(string.Format("Entry {0}", index), "{0} - {1}", match.Groups[2].Value,
                    match.Groups[3].Value);
#endif
                if (index == 0)
                    _items = new List<AddressBookItem>();
                var item = new AddressBookItem(Codec)
                {
                    Name = match.Groups[2].Value,
                    Number = match.Groups[3].Value
                };
                if (match.Groups.Count > 3)
                    item.Extension = match.Groups[4].Value;
                if (receivedString.Contains("h323_num"))
                    item.ItemType = AddressBookItemType.H323;
                _items.Add(item);
            }
            else
            {
                match = endRegex.Match(receivedString);
                if (!match.Success)
                {
                    if (receivedString == "addrbook all done")
                    {
                        _allItems = new List<AddressBookItem>(_items);
                        _callback(this, _allItems);
                    }
                    return;
                }
#if DEBUG
                Debug.WriteSuccess("Address book search done", "searchstring: \"{0}\", count = {1}",
                    match.Groups[1], _items.Count);
#endif
                _callback(this, _items);
            }
        }
    }

    public delegate void AddressBookResultsHandler(AddressBook addressBook, IEnumerable<AddressBookItem> items);
}