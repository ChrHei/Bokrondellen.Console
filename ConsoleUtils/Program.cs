using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace ConsoleUtils
{
    class Program
    {
        private static Dictionary<string, string> options = new Dictionary<string, string>();
        static void Main(string[] args)
        {
            ParseArgs(args);

            if (options.Keys.Any(s => s.Equals("action")))
            {
                try
                {
                    switch (options["action"])
                    {
                        case "split":
                            SplitXml();

                            break;
                        case "locate":
                            LocateElement();
                            break;

                        case "chunk":
                            ChunkFile();
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private static void ParseArgs(string[] args)
        {
            OptionSet optionSet = new OptionSet()
                {
                    { "a|action=", "Action to perform", v => options.Add("action", v)},
                    { "b|isbn=", "ISBN to use", v => options.Add("isbn", v)},
                    { "i|index=", "Index for splitting", v => options.Add("index", v)},
                    { "s|size=", "Index for splitting", v => options.Add("size", v)},
                    { "f|file=", "Source file", v => options.Add("file", v)}
                };

            optionSet.Parse(args);
        }

        private static void ChunkFile()
        {
            int chunkSize;
            int currentChunk = 1;

            if (int.TryParse(options["size"], out chunkSize))
            {
                Console.WriteLine("Creating files with {0} elements", chunkSize);
                string file = options["file"];

                if (File.Exists(file))
                {
                    FileInfo fileInfo = new FileInfo(file);

                    using (XmlTextReader reader = new XmlTextReader(file))
                    {
                        // läs fram till filinformation
                        while (reader.Read() && reader.Name != "filinformation") { }
                        // spara filinformation
                        string filInfoXml = "\t" + reader.ReadOuterXml();
                        
                        List<string> artikelList = new List<string>();

                        while (true)
                        {
                            if (artikelList.Count == chunkSize)
                            {
                                using (XmlTextWriter writer = new XmlTextWriter(Path.Combine(fileInfo.DirectoryName, Path.GetFileNameWithoutExtension(fileInfo.FullName) + string.Format("_part{0:D3}.xml", currentChunk++)), Encoding.UTF8))
                                {
                                    writer.Formatting = Formatting.Indented;

                                    writer.WriteStartElement("artikelregister");

                                    writer.WriteRaw(filInfoXml);

                                    foreach (string element in artikelList)
                                        writer.WriteRaw(element);

                                    writer.WriteEndElement();
                                }

                                artikelList.Clear();
                            }

                            if (reader.Name == "artikel")
                            {
                                artikelList.Add(reader.ReadOuterXml());
                            }
                            else if (!reader.Read())
                            {
                                break;
                            }
                        }
                    }
                }

            }
        }

        private static void LocateElement()
        {
            string isbn = options["isbn"];
            Console.WriteLine("Locating element with ISBN {0}", isbn);
            
                string file = options["file"];
                int pos = 0;

                if (File.Exists(file))
                {
                    FileInfo fileInfo = new FileInfo(file);

                    using (XmlTextReader reader = new XmlTextReader(file))
                    {
                        // läs fram till filinformation
                        while (reader.Read() && reader.Name != "filinformation") { }
                        // spara filinformation
                        string filInfoXml = "\t" + reader.ReadOuterXml();
                        List<string> artikelList = new List<string>();
                        while (true)
                        {
                            if (reader.Name == "artikel")
                            {
                                pos++;
                                XElement element = XElement.Parse(reader.ReadOuterXml());
                                if (element.Descendants()
                                    .Any(e => e.Name.LocalName == "artikelnummer" && e.Value == isbn))
                                {
                                    Console.WriteLine("Found ISBN '{0}' as element {1}.", isbn, pos);
                                    break;
                                }
                            }
                            else if (!reader.Read())
                            {
                                break;
                            }
                        }

                    }
                }
        }

        private static void SplitXml()
        {
            int splitIndex;

            if (int.TryParse(options["index"], out splitIndex))
            {
                Console.WriteLine("Splitting file at {0}", splitIndex);
                string file = options["file"];

                if (File.Exists(file))
                {
                    FileInfo fileInfo = new FileInfo(file);

                    using (XmlTextReader reader = new XmlTextReader(file))
                    {
                        // läs fram till filinformation
                        while (reader.Read() && reader.Name != "filinformation") { }
                        // spara filinformation
                        string filInfoXml = "\t" + reader.ReadOuterXml();
                        List<string> artikelList = new List<string>();
                        while (true)
                        {
                            if (artikelList.Count == splitIndex)
                                break;

                            if (reader.Name == "artikel")
                            {
                                artikelList.Add(reader.ReadOuterXml());
                            }
                            else if (!reader.Read() )
                            {
                                break;
                            }
                        }

                        using (XmlTextWriter writer = new XmlTextWriter(Path.Combine(fileInfo.DirectoryName, Path.GetFileNameWithoutExtension(fileInfo.FullName) + "_part1.xml"), Encoding.UTF8))
                        {
                            writer.Formatting = Formatting.Indented;

                            writer.WriteStartElement("artikelregister");

                            writer.WriteRaw(filInfoXml);

                            foreach (string element in artikelList)
                                writer.WriteRaw(element);

                            writer.WriteEndElement();
                        }

                        artikelList.Clear();

                        artikelList.Add(reader.ReadOuterXml());

                        while (true)
                        {
                            if (reader.Name == "artikel")
                            {
                                artikelList.Add(reader.ReadOuterXml());
                            }
                            else if (!reader.Read())
                            {
                                break;
                            }
                        }

                        using (XmlTextWriter writer = new XmlTextWriter(Path.Combine(fileInfo.DirectoryName, Path.GetFileNameWithoutExtension(fileInfo.FullName) + "_part2.xml"), Encoding.UTF8))
                        {
                            writer.Formatting = Formatting.Indented;

                            writer.WriteStartElement("artikelregister");

                            writer.WriteRaw(filInfoXml);

                            foreach (string element in artikelList)
                                writer.WriteRaw(element);

                            writer.WriteEndElement();
                        }

                        if (artikelList.Count > 0)
                        {

                        }
                    }
                }
            }
        }

    }
}
