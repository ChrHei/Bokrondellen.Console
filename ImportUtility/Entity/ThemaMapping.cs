using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace Bokrondellen.ImportUtility.Entity
{
    [XmlRoot("root")]
    public class ThemaMappingList
    {
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(ThemaMappingList));

        [XmlElement("thema")]
        public List<ThemaMapping> Items = new List<ThemaMapping>();

        public void Add(ThemaMapping item)
        {
            Items.Add(item);
        }

        public void AddRange(IEnumerable<ThemaMapping> collection)
        {
            Items.AddRange(collection);
        }

        public string ToXmlString()
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            
            string result = null;

            using (StringWriter w = new StringWriter())
            {
                serializer.Serialize(w, this, ns);
                result = w.ToString();
            }

            return result;
        }

        public static ThemaMappingList Deserialize(Stream s)
        {
            return (ThemaMappingList)serializer.Deserialize(s);
        }

        public static ThemaMappingList Load(string path)
        {
            ThemaMappingList result = null;

            using (FileStream s = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                result = Deserialize(s);
            }

            return result;
        }
    }

    public class ThemaMapping
    {
        [XmlAttribute("kod")]
        public string Code { get; set; }
        [XmlElement("rubrik")]        
        public string Caption { get; set; }
        [XmlElement("genre")]
        public KeyValueEntity Genre { get; set; }
        [XmlElement("varugrupp")]
        public string CommodityGroup { get; set; }
        [XmlElement("barnvuxen")]
        public string ChildAdult { get; set; }
    }

    public class KeyValueEntity
    {
        [XmlElement("kod")]
        public string Code { get; set; }
        [XmlElement("rubrik")]
        public string Caption { get; set; }
    }
}
