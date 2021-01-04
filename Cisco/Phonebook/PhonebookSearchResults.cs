 
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UX.Lib2.Devices.Cisco.Phonebook
{
    public class PhonebookSearchResults : IEnumerable<PhonebookItem>
    {
        #region Fields

        private readonly List<PhonebookItem> _items;
        private readonly Phonebook _phonebook;
        private readonly PhonebookType _type;
        private readonly string _searchString;
        private readonly string _folderId;
        private readonly int _offset;
        private readonly int _limit;
        private readonly int _totalRows;
        private readonly string _currentFolderId;
        private readonly string _currentFolderName;
        private readonly bool _searchWasSuccessful = true;
        private readonly string _errorMessage = string.Empty;

        #endregion

        #region Constructors

        internal PhonebookSearchResults(Phonebook phonebook, PhonebookType type, string searchString, string folderId,
            IEnumerable<PhonebookItem> items, int offset, int limit, int totalRows, string currentFolderId,
            string currentFolderName)
        {
            _phonebook = phonebook;
            _type = type;
            _searchString = searchString;
            _folderId = folderId;
            _offset = offset;
            _limit = limit;
            _totalRows = totalRows;
            _currentFolderId = currentFolderId;
            _currentFolderName = currentFolderName;
            _items = new List<PhonebookItem>(items);
        }

        internal PhonebookSearchResults(Phonebook phonebook, PhonebookType type, string searchString,
            string errorMessage, params object[] args)
        {
            _phonebook = phonebook;
            _type = type;
            _searchString = searchString;
            _errorMessage = string.Format(errorMessage, args);
            _searchWasSuccessful = false;
            _items = new List<PhonebookItem>();
        }

        internal PhonebookSearchResults(Phonebook phonebook, PhonebookType type, string searchString)
        {
            _phonebook = phonebook;
            _type = type;
            _searchString = searchString;
            _errorMessage = "No Results";
            _searchWasSuccessful = true;
            _items = new List<PhonebookItem>();
        }

        #endregion

        #region Finalizers
        #endregion

        #region Events
        #endregion

        #region Delegates
        #endregion

        #region Properties

        public PhonebookItem this[int index]
        {
            get { return _items[index]; }
        }

        public int Offset
        {
            get { return _offset; }
        }

        public int Limit
        {
            get { return _limit; }
        }

        public int TotalRows
        {
            get { return _totalRows; }
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public string CurrentFolderId
        {
            get { return _currentFolderId; }
        }

        public string CurrentFolderName
        {
            get { return _currentFolderName; }
        }

        public string Summary
        {
            get { return string.Format("{0} to {1} of {2}", _offset + 1, _offset + _items.Count, _totalRows); }
        }

        public bool SearchWasSuccessful
        {
            get { return _searchWasSuccessful; }
        }

        public bool IsEmpty
        {
            get { return !_items.Any(); }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
        }

        public string SearchString
        {
            get { return _searchString; }
        }

        public PhonebookType Type
        {
            get { return _type; }
        }

        public bool MoreAvailable
        {
            get { return RemainingRowsAvailable > 0; }
        }

        public int RemainingRowsAvailable
        {
            get { return TotalRows - (Count + Offset); }
        }

        #endregion

        #region Methods

        public PhonebookSearchResults GetMore(int count)
        {
            return _phonebook.Search(_type, _searchString, count, Count + Offset, _folderId);
        }

        public IEnumerator<PhonebookItem> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}