 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Crestron.SimplSharp;
using UX.Lib2.Cloud.Logger;

namespace UX.Lib2.Devices.Cisco.Phonebook
{
    public class Phonebook
    {
        #region Fields

        private readonly CiscoTelePresenceCodec _codec;
        private readonly Dictionary<string, string> _folderNames; 

        #endregion

        #region Constructors

        internal Phonebook(CiscoTelePresenceCodec codec)
        {
            _codec = codec;
            _folderNames = new Dictionary<string, string>();
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties
        #endregion

        #region Methods

        public PhonebookSearchResults Search(PhonebookType type, string searchString, int limit, int offset, string folderId)
        {
            var cmd = new CodecCommand("Phonebook", "Search");
            cmd.Args.Add(type);
            cmd.Args.Add("SearchString", searchString);
            cmd.Args.Add("Limit", limit);
            cmd.Args.Add("Offset", offset);
            cmd.Args.Add("Recursive", !string.IsNullOrEmpty(searchString));
            if (!string.IsNullOrEmpty(folderId))
                cmd.Args.Add("FolderId", folderId);

#if DEBUG
            Debug.WriteInfo("Searching Phonebook");
            var sw = new Stopwatch();
            sw.Start();
            foreach (var arg in cmd.Args)
            {
                Debug.WriteInfo("  " + arg.Name, arg.Value.ToString());
            }
#endif
            var response = _codec.SendCommand(cmd);

#if DEBUG
            Debug.WriteInfo("Phonbook search response", "Code = {0}, Stopwatch = {1}", response.Code, sw.ElapsedMilliseconds);
#endif

            if (response.Code != 200)
            {
                CloudLog.Error("Error getting phonebook search, Codec Responded with {0} code", response.Code);
                return new PhonebookSearchResults(this, type, searchString, folderId, "Codec returned Error code: {0}", response.Code);
            }

            var result = response.Xml.Element("Command").Element("PhonebookSearchResult");

            if (result.Attribute("status").Value == "Error")
            {
                var message = result.Element("Reason").Value;
                CloudLog.Error("Error getting phonebook search: {0}", message);
                return new PhonebookSearchResults(this, type, searchString, message);
            }
            
            if (result.Attribute("status").Value == "OK" && result.IsEmpty)
            {
                return new PhonebookSearchResults(this, type, searchString);
            }

            try
            {
                var folders = result.Elements("Folder");
                var contacts = result.Elements("Contact");
                var info = result.Element("ResultInfo");
                var totalRows = int.Parse(info.Element("TotalRows").Value);
                var items =
                    folders.Select(
                        xElement =>
                            new PhonebookFolder(_codec, xElement, type)).Cast<PhonebookItem>().ToList();
                items.AddRange(
                    contacts.Select(
                        xElement =>
                            new PhonebookContact(_codec, xElement, type)).Cast<PhonebookItem>());

                foreach (var folder in folders)
                {
                    _folderNames[folder.Element("LocalId").Value] = folder.Element("Name").Value;
                }
#if DEBUG
                Debug.WriteSuccess("Search Results", "{0} to {1} of {2}", offset + 1, offset + items.Count, totalRows);
                foreach (var phonebookItem in items)
                {
                    Debug.WriteSuccess(phonebookItem.Type.ToString(), "{0} ({1}/{2})",
                        phonebookItem.Name, phonebookItem.Id, phonebookItem.ParentId);
                }
                sw.Stop();
                Debug.WriteInfo("Phonbook search returning", "Count = {0}, Stopwatch = {1}", items.Count, sw.ElapsedMilliseconds);
#endif
                if (!String.IsNullOrEmpty((string) cmd.Args.First(a => a.Name == "SearchString").Value))
                    return new PhonebookSearchResults(this, type, searchString, folderId, items, offset, limit, totalRows,
                        string.Empty, string.Empty);
               
                foreach (var element in result.Elements())
                {
                    string currentFolderId;

                    switch (element.Name)
                    {
                        case "Contact":
                            if (element.Element("FolderId") != null)
                            {
                                currentFolderId = element.Element("FolderId").Value;
                                return new PhonebookSearchResults(this, type, searchString, folderId, items, offset, limit,
                                    totalRows, currentFolderId, _folderNames[currentFolderId]);
                            }

                            return new PhonebookSearchResults(this, type, searchString, folderId, items, offset, limit, totalRows,
                                string.Empty, string.Empty);
                        case "Folder":
                            if (element.Element("ParentFolderId") != null)
                            {
                                currentFolderId = element.Element("ParentFolderId").Value;
                                return new PhonebookSearchResults(this, type, searchString, folderId, items, offset, limit,
                                    totalRows, currentFolderId, _folderNames[currentFolderId]);
                            }
                            return new PhonebookSearchResults(this, type, searchString, folderId, items, offset, limit, totalRows,
                                string.Empty, string.Empty);
                    }
                }

                return new PhonebookSearchResults(this, type, searchString, folderId, items, offset, limit, totalRows,
                    string.Empty, string.Empty);

            }
            catch (Exception e)
            {
                var message = string.Format("Error parsing phonebook data, {0}", e.Message);
                if (e is ThreadAbortException)
                {
                    message = "Thread was aborted";
                }
                else
                {
                    CloudLog.Error(message);
                }
                return new PhonebookSearchResults(this, type, searchString, message);
            }
        }

        public PhonebookSearchResults Search(PhonebookType type, string searchString, int limit, int offset)
        {
            return Search(type, searchString, limit, offset, null);
        }

#if DEBUG
        public string CreatePhoneBookFolder(string name)
        {
            var cmd = new CodecCommand("/Phonebook/Folder", "Add");
            cmd.Args.Add("Name", name);
            var result = _codec.SendCommand(cmd);
            return result.Xml.Element("Command").Elements().First().Element("Name").Value;
        }

        public void CreatePhoneBookEntry(string name, string folderId, string email, string number)
        {
            var cmd = new CodecCommand("/Phonebook/Contact", "Add");
            cmd.Args.Add("Name", name);
            cmd.Args.Add("FolderId", folderId);
            cmd.Args.Add("Number", email);
            cmd.Args.Add("Protocol", "SIP");
            var result = _codec.SendCommand(cmd);
            var id = result.Xml.Element("Command").Elements().First().Element("Name").Value;
            cmd = new CodecCommand("/Phonebook/ContactMethod", "Add");
            cmd.Args.Add("ContactId", id);
            cmd.Args.Add("Number", number);
            cmd.Args.Add("Protocol", "Auto");
            _codec.SendCommand(cmd);
        }
#endif
        #endregion
    }

    public enum PhonebookType
    {
        Local,
        Corporate
    }
}