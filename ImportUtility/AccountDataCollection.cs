using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Bokrondellen.ImportUtility
{
    [XmlRoot("root")]
    public class AccountDataCollection
    {
        private List<AccountData> _items;
        [XmlElement("row")]
        public List<AccountData> Items
        {
            get { return _items; }
            set { _items = value; }
        }

        public AccountDataCollection()
        {
            _items = new List<AccountData>();
        }

        public AccountData this[int index]
        {
            get { return _items[index]; }
            set { _items[index] = value; }
        }

    }
}
