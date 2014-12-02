using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Business;
using Mediachase.BusinessFoundation.Data.Meta;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Catalog.Dto;
using Mediachase.Commerce.Catalog.Managers;
using Mediachase.Commerce.Core;

using MDP = Mediachase.MetaDataPlus;
using System.Configuration;
using Mediachase.Commerce.Customers;
using System.Xml.Serialization;
using System.Xml;
using System.Reflection;
using System.Web.Security;
using System.Data.SqlClient;
using Bokrondellen.ImportUtility.Entity;
using Bokrondellen.Initialization;


namespace Bokrondellen.ImportUtility
{
    class Program
    {
        private static MetaClassManager metaModel;
        private static MetaEnumItem[] organizationTypes;
        private static MetaEnumItem[] addressTypes;
        private readonly static List<KeyValuePair<string, Exception>> exceptionList = new List<KeyValuePair<string, Exception>>();

        static void Main(string[] args)
        {
            InitializeFramework.Initialize();

            string connectionString = ConfigurationManager.ConnectionStrings["EcfSqlConnection"].ConnectionString;

            SqlConnectionStringBuilder connectionBuilder = new SqlConnectionStringBuilder(connectionString);

            Console.WriteLine("{0,-20}{1}", "Data source:", connectionBuilder.DataSource);
            Console.WriteLine("{0,-20}{1}", "Initial catalog:", connectionBuilder.InitialCatalog);
            Console.WriteLine("{0,-20}{1}\n", "User ID:", connectionBuilder.UserID);
            Console.WriteLine("{0,-20}{1}\n", "Data folder:", Path.GetFullPath(Properties.Settings.Default.DataFolder));

            Console.WriteLine("[r = read], [a = add]\n");

            Console.Write("Enter command (followed by [ENTER]): ");
            string TheLine = Console.ReadLine();

            DataContext.Current = new DataContext(connectionString);

            metaModel = DataContext.Current.MetaModel;

            organizationTypes = metaModel.RegisteredTypes["OrganizationType"].EnumItems;
            addressTypes = metaModel.RegisteredTypes["AddressType"].EnumItems;

            switch (TheLine.Trim())
            {
                case "r": // Read operations
                    {
                        #region read
                        try
                        {
                            Console.WriteLine(String.Empty);
                            Console.WriteLine("- Meta classes  -");
                            // Enumerate the classes in current Meta Model
                            foreach (MetaClass metaClass in metaModel.MetaClasses)
                            {
                                Console.WriteLine("{0}\n    Title field name: {1}\n    Support cards:{2}", metaClass.Name, metaClass.TitleFieldName, metaClass.SupportsCards);
                            }


                            // BIC entry
                            Console.WriteLine(String.Empty);
                            Console.WriteLine("- Fields on BIC_entry -");
                            // As an example, take a look at the class Contact
                            foreach (MetaField metaField in metaModel.MetaClasses["BIC_entry"].Fields)
                            {
                                Console.WriteLine("{0}, {1}({2}) {3}", metaField.Name, metaField.TypeName, metaField.Attributes["MaxLength"], metaField.Attributes["IsUnique"]);
                            }



                            // PrintBICInfo(metaModel);


                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                        break;
                        #endregion

                    }

                case "a": // Add operations
                    {
                        try
                        {
                            Console.WriteLine("Press 'Y' to create mappings between thema, genre and commodity group.");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateGenreThemaCommodityGroupMappings();

                            Console.WriteLine("Press 'Y' to create Thema entries");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateThema();

                            Console.WriteLine("Press 'Y' to create Accounts");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateAccounts();

                            Console.WriteLine("Press 'Y' to update account email adresses");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                UpdateMembershipEmail();

                            Console.WriteLine("Press 'Y' to create BIC entries");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateBICEntries();
                            Console.WriteLine("Press 'Y' to create commodity groups");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateCommodityGroups();
                            Console.WriteLine("Press 'Y' to create genres");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateGenres();
                            Console.WriteLine("Press 'Y' to create awards");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateAwards();
                            Console.WriteLine("Press 'Y' to create reading age");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateReadingAge();
                            Console.WriteLine("Press 'Y' to create media types");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateMediaTypes();
                            Console.WriteLine("Press 'Y' to create media formats");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateFormats();
                            Console.WriteLine("Press 'Y' to create subjects");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateSubjects();
                            Console.WriteLine("Press 'Y' to create levels of education");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateLevelsOfEducation();
                            Console.WriteLine("Press 'Y' to create types of teaching aid");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateItemTypesOfTeachingAid();
                            Console.WriteLine("Press 'Y' to create high school courses");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateItemHichSchoolCourses();
                            Console.WriteLine("Press 'Y' to create high school programs");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateItemHichSchoolPrograms();
                            Console.WriteLine("Press 'Y' to create genres/bic cross values");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateGenresBicCrossValues();
                            Console.WriteLine("Press 'Y' to create genres/commodity group cross values");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateGenresCommodityGroupCrossValues();
                            Console.WriteLine("Press 'Y' to create item/bic cross values");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateItemBicCrossValues();

                            Console.WriteLine("Press 'Y' to create organizations");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateOrganizations("kunder.xml");

                            if (exceptionList.Count > 0)
                            {
                                Console.WriteLine("Import generated {0} exceptions", exceptionList.Count);
                                foreach (KeyValuePair<string, Exception> item in exceptionList)
                                {
                                    Console.WriteLine("  {0}: {1}", item.Key, item.Value.Message);
                                }
                            }
                            Console.WriteLine("Press 'Y' to create bookstores");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateOrganizations("bokhandlar.xml");
                            Console.WriteLine("Press 'Y' to create Language ");
                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                CreateLanguageEntries();

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        break;
                    }

                case "d": // Delete operations

                    break;

            }

            Console.WriteLine(String.Empty);
            Console.WriteLine("== Done ==");
            //Console.ReadLine();
        }


        

        private static void WaitForKey()
        {
            Console.WriteLine("Press 'Y' to continue...");
            Console.ReadKey();
        }

        private static void CreateBICEntries()
        {
            Console.WriteLine("Loading XML document for BIC");

            XDocument doc = XDocument.Load(Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, "bic.xml")));

            Console.WriteLine("Document contains {0} rows", doc.Root.Elements().Count());

            int i = 1;
            int j = 1;
            int c = 1;

            foreach (XElement element in doc.Root.Elements())
            {
                try
                {
                    string key = element.Element(element.Name.Namespace + "Code").Value;
                    string title = element.Element(element.Name.Namespace + "Description").Value;

                    EntityObject item = BusinessManager.List("BIC_entry",
                        new FilterElement[] {
                            new FilterElement("Key", FilterElementType.Equal, key)})
                        .FirstOrDefault();

                    if (item == null)
                    {
                        item = BusinessManager.InitializeEntity(("BIC_entry"));

                        item["Key"] = key;
                        item["Title"] = title;

                        BusinessManager.Create(item);

                        Console.WriteLine("{0}. Created {1}'{2}'", c++.ToString().PadLeft(6), item["Key"].ToString().PadLeft(14), item["Title"]);

                        i++;
                    }
                    else
                    {
                        item["Title"] = title;
                        BusinessManager.Update(item);

                        Console.WriteLine("{0}. Updated {1}'{2}'", c++.ToString().PadLeft(6), item["Key"].ToString().PadLeft(14), item["Title"]);

                        j++;
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine("{0}", ex.Message);
                }
            }

            // PrintBICInfo(metaModel);
            Console.WriteLine("\n{0} rows created", i - 1);
            Console.WriteLine("\n{0} rows updated", j - 1);
        }

        private static void CreateThema()
        {
            Console.WriteLine("Loading XML document for Thema");

            XDocument doc = XDocument.Load(Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, "thema.xml")));

            Console.WriteLine("Document contains {0} rows", doc.Root.Elements().Count());

            int i = 1;
            int j = 1;
            int c = 1;

            foreach (XElement element in doc.Root.Elements())
            {
                try
                {
                    XNamespace ns = element.Name.Namespace;

                    string key = element.Element(ns + "Code").Value;
                    string title = element.Element(ns + "Description").Value;
                    bool allowImport = false;
                    int sortOrder;

                    bool.TryParse(element.Attribute(ns + "allowImport").Value, out allowImport);
                    int.TryParse(element.Attribute(ns + "sortOrder").Value, out sortOrder);

                    EntityObject item = BusinessManager.List("ItemThema",
                        new FilterElement[] {
                            new FilterElement("Key", FilterElementType.Equal, key)})
                        .FirstOrDefault();

                    if (item == null)
                    {
                        item = BusinessManager.InitializeEntity(("ItemThema"));

                        item["Key"] = key;
                        item["Title"] = title;
                        item["AllowImport"] = allowImport;
                        item["SortOrder"] = sortOrder == 0 ? null : (int?)sortOrder;


                        BusinessManager.Create(item);

                        Console.WriteLine("{0}. Created {1}'{2}'", c++.ToString().PadLeft(6), item["Key"].ToString().PadLeft(14), item["Title"]);

                        i++;
                    }
                    else
                    {
                        item["Title"] = title;
                        item["AllowImport"] = allowImport;
                        item["SortOrder"] = sortOrder == 0 ? null : (int?)sortOrder;

                        BusinessManager.Update(item);

                        Console.WriteLine("{0}. Updated {1}'{2}'", c++.ToString().PadLeft(6), item["Key"].ToString().PadLeft(14), item["Title"]);

                        j++;
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine("{0}", ex.Message);
                }
            }

            // PrintBICInfo(metaModel);
            Console.WriteLine("\n{0} rows created", i - 1);
            Console.WriteLine("\n{0} rows updated", j - 1);
        }

        private static void CreateGenreThemaCommodityGroupMappings()
        {
            ThemaMappingList list = new ThemaMappingList();
            int c = 1;

            list = ThemaMappingList.Load(Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, "genre-thema-varugrupp.xml")));

            foreach (ThemaMapping mapping in list.Items)
            {

                if (mapping.Genre != null && !string.IsNullOrEmpty(mapping.Genre.Code))
                {
                    EntityObject genre = BusinessManager.List("ItemGenre", new FilterElement[] { new FilterElement("ShortName", FilterElementType.Equal, mapping.Genre.Code) }).FirstOrDefault();
                    EntityObject thema = BusinessManager.List("ItemThema", new FilterElement[] { new FilterElement("Key", FilterElementType.Equal, mapping.Code) }).FirstOrDefault();
                    EntityObject commodityGroup = BusinessManager.List("ItemCommodityGroup", new FilterElement[] { new FilterElement("ShortName", FilterElementType.Equal, mapping.CommodityGroup) }).FirstOrDefault();

                    if (genre != null && thema != null)
                    {
                        //Console.WriteLine("{0} => {1}", mapping.Genre.Code, mapping.Code);
                        genre["ItemThemaId"] = thema.PrimaryKeyId;
                        BusinessManager.Update(genre);

                        Console.WriteLine("{0}. Created mapping {1} => {2}", c++.ToString().PadLeft(3), mapping.Genre.Code, mapping.Code);

                        if (commodityGroup != null)
                        {
                            thema["ItemCommodityGroupId"] = commodityGroup.PrimaryKeyId;
                            BusinessManager.Update(thema);

                            Console.WriteLine("{0}. Created mapping {1} => {2}", c.ToString().PadLeft(3), mapping.Code, mapping.CommodityGroup);
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(mapping.CommodityGroup))
                {
                    EntityObject thema = BusinessManager.List("ItemThema", new FilterElement[] { new FilterElement("Key", FilterElementType.Equal, mapping.Code) }).FirstOrDefault();
                    EntityObject commodityGroup = BusinessManager.List("ItemCommodityGroup", new FilterElement[] { new FilterElement("ShortName", FilterElementType.Equal, mapping.CommodityGroup) }).FirstOrDefault();

                    if (thema != null && commodityGroup != null)
                    {
                        thema["ItemCommodityGroupId"] = commodityGroup.PrimaryKeyId;
                        BusinessManager.Update(thema);

                        Console.WriteLine("{0}. Created mapping {1} => {2}", c.ToString().PadLeft(3), mapping.Code, mapping.CommodityGroup);
                    }
                }
            }
        }

        private static void CreateCommodityGroups()
        {
            Console.WriteLine("Loading XML document for commodity groups");

            XDocument doc = XDocument.Load(Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, "listvalues.xml")));

            XElement[] elements = doc.Root.Elements()
                .Where(e => e.Element("typ").Value == "artikel.varugrupp")
                .ToArray();

            Console.WriteLine("Document contains {0} rows", elements.Count());

            int i = 1;
            int j = 1;
            int c = 1;

            foreach (XElement element in elements)
            {
                try
                {
                    string title = element.Element(element.Name.Namespace + "description").Value;
                    string shortName = element.Element(element.Name.Namespace + "shortname").Value;
                    int sortIndex = int.Parse(element.Element(element.Name.Namespace + "sortindex").Value);

                    EntityObject item = BusinessManager.List("ItemCommodityGroup",
                        new FilterElement[] {
                            new FilterElement("ShortName", FilterElementType.Equal, shortName)})
                        .FirstOrDefault();

                    if (item == null)
                    {
                        item = BusinessManager.InitializeEntity(("ItemCommodityGroup"));

                        item["Title"] = title;
                        item["ShortName"] = shortName;
                        item["SortIndex"] = sortIndex;

                        BusinessManager.Create(item);

                        Console.WriteLine("{0}. Created {1} '{2}'", c++.ToString().PadLeft(6), shortName.PadLeft(8), title);

                        i++;
                    }
                    else
                    {
                        item["Title"] = title;
                        item["SortIndex"] = sortIndex;

                        BusinessManager.Update(item);

                        Console.WriteLine("{0}. Updated {1} '{2}'", c++.ToString().PadLeft(6), shortName.PadLeft(8), title);

                        j++;
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine("{0}", ex.Message);
                }
            }

            // PrintBICInfo(metaModel);
            Console.WriteLine("\n{0} rows created", i - 1);
            Console.WriteLine("\n{0} rows updated", j - 1);

            Console.WriteLine(string.Empty);
        }

        private static void CreateReadingAge()
        {
            KeyValuePair<string, string>[] items = new KeyValuePair<string, string>[] 
                { 
                    new KeyValuePair<string, string>("0-3", "5AB"),
                    new KeyValuePair<string, string>("3-6", "5AC"), 
                    new KeyValuePair<string, string>("6-9", "5AG"),
                    new KeyValuePair<string, string>("9-12", "5AK"), 
                    new KeyValuePair<string, string>("12-15", "5AN"),
                    new KeyValuePair<string, string>("Unga vuxna", "5AQ")
                };

            int i = 1;
            int j = 1;
            int c = 1;

            foreach (KeyValuePair<string, string> o in items)
            {
                try
                {
                    string title = o.Key;
                    string bic = o.Value;

                    EntityObject item = BusinessManager.List("ItemReadingAge",
                        new FilterElement[] {
                            new FilterElement("Title", FilterElementType.Equal, title)})
                        .FirstOrDefault();

                    EntityObject bicItem = BusinessManager.List("BIC_entry",
                        new FilterElement[] {
                            new FilterElement("Key", FilterElementType.Equal, bic)})
                        .First();


                    if (item == null)
                    {
                        item = BusinessManager.InitializeEntity(("ItemReadingAge"));

                        item["Title"] = title;
                        item["BIC_entryId"] = bicItem.PrimaryKeyId;
                        item["BIC_entry"] = bicItem["Title"];

                        BusinessManager.Create(item);

                        Console.WriteLine("{0}. Created {1}", c++.ToString().PadLeft(6), title);

                        i++;
                    }
                    else
                    {
                        item["Title"] = title;
                        item["BIC_entryId"] = bicItem.PrimaryKeyId;
                        item["BIC_entry"] = bicItem["Title"];

                        BusinessManager.Update(item);

                        Console.WriteLine("{0}. Updated {1}", c++.ToString().PadLeft(6), title);

                        j++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("{0}", ex.Message);
                }
            }
        }

        private static void CreateGenres()
        {
            Console.WriteLine("Loading XML document for genres");

            XDocument doc = XDocument.Load(Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, "listvalues.xml")));

            XElement[] elements = doc.Root.Elements()
                .Where(e => e.Element("typ").Value == "artikel.genre")
                .ToArray();

            Console.WriteLine("Document contains {0} rows", elements.Count());

            int i = 1;
            int j = 1;
            int c = 1;

            foreach (XElement element in elements)
            {
                try
                {
                    string title = element.Element(element.Name.Namespace + "description").Value;
                    string shortName = element.Element(element.Name.Namespace + "shortname").Value;
                    int sortIndex = int.Parse(element.Element(element.Name.Namespace + "sortindex").Value);

                    EntityObject item = BusinessManager.List("ItemGenre",
                        new FilterElement[] {
                            new FilterElement("ShortName", FilterElementType.Equal, shortName)})
                        .FirstOrDefault();

                    if (item == null)
                    {
                        item = BusinessManager.InitializeEntity(("ItemGenre"));

                        item["Title"] = title;
                        item["ShortName"] = shortName;
                        item["SortIndex"] = sortIndex;

                        BusinessManager.Create(item);

                        Console.WriteLine("{0}. Created {1} '{2}'", c++.ToString().PadLeft(6), shortName.PadLeft(8), title);

                        i++;
                    }
                    else
                    {
                        item["Title"] = title;
                        item["SortIndex"] = sortIndex;

                        BusinessManager.Update(item);

                        Console.WriteLine("{0}. Updated {1} '{2}'", c++.ToString().PadLeft(6), shortName.PadLeft(8), title);

                        j++;
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine("{0}", ex.Message);
                }
            }

            // PrintBICInfo(metaModel);
            Console.WriteLine("\n{0} rows created", i - 1);
            Console.WriteLine("\n{0} rows updated", j - 1);

            Console.WriteLine(string.Empty);
        }

        private static void CreateSubjects()
        {
            Console.WriteLine("Loading XML document for subjects");

            XDocument doc = XDocument.Load(Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, "listvalues.xml")));

            XElement[] elements = doc.Root.Elements()
                .Where(e => e.Element("typ").Value == "kb.skolamne")
                .ToArray();

            Console.WriteLine("Document contains {0} rows", elements.Count());

            int i = 1;
            int j = 1;
            int c = 1;

            foreach (XElement element in elements)
            {
                try
                {
                    string title = element.Element(element.Name.Namespace + "description").Value;
                    string shortName = element.Element(element.Name.Namespace + "shortname").Value;
                    int sortIndex = int.Parse(element.Element(element.Name.Namespace + "sortindex").Value);

                    EntityObject item = BusinessManager.List("ItemSubject",
                        new FilterElement[] {
                            new FilterElement("ShortName", FilterElementType.Equal, shortName)})
                        .FirstOrDefault();

                    if (item == null)
                    {
                        item = BusinessManager.InitializeEntity(("ItemSubject"));

                        item["Title"] = title;
                        item["ShortName"] = shortName;
                        item["SortIndex"] = sortIndex;

                        BusinessManager.Create(item);

                        Console.WriteLine("{0}. Created {1} '{2}'", c++.ToString().PadLeft(6), shortName.PadLeft(8), title);

                        i++;
                    }
                    else
                    {
                        item["Title"] = title;
                        item["SortIndex"] = sortIndex;

                        BusinessManager.Update(item);

                        Console.WriteLine("{0}. Updated {1} '{2}'", c++.ToString().PadLeft(6), shortName.PadLeft(8), title);

                        j++;
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine("{0}", ex.Message);
                }
            }

            // PrintBICInfo(metaModel);
            Console.WriteLine("\n{0} rows created", i - 1);
            Console.WriteLine("\n{0} rows updated", j - 1);

            Console.WriteLine(string.Empty);
        }

        private static void CreateLevelsOfEducation()
        {
            Console.WriteLine("Loading XML document for level of education");

            XDocument doc = XDocument.Load(Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, "listvalues.xml")));

            XElement[] elements = doc.Root.Elements()
                .Where(e => e.Element("typ").Value == "kb.utbildningsniva")
                .ToArray();

            Console.WriteLine("Document contains {0} rows", elements.Count());

            int i = 1;
            int j = 1;
            int c = 1;

            foreach (XElement element in elements)
            {
                try
                {
                    string title = element.Element(element.Name.Namespace + "description").Value;
                    string shortName = element.Element(element.Name.Namespace + "shortname").Value;
                    int sortIndex = int.Parse(element.Element(element.Name.Namespace + "sortindex").Value);

                    EntityObject item = BusinessManager.List("ItemLevelOfEducation",
                        new FilterElement[] {
                            new FilterElement("ShortName", FilterElementType.Equal, shortName)})
                        .FirstOrDefault();

                    if (item == null)
                    {
                        item = BusinessManager.InitializeEntity(("ItemLevelOfEducation"));

                        item["Title"] = title;
                        item["ShortName"] = shortName;
                        item["SortIndex"] = sortIndex;

                        BusinessManager.Create(item);

                        Console.WriteLine("{0}. Created {1} '{2}'", c++.ToString().PadLeft(6), shortName.PadLeft(8), title);

                        i++;
                    }
                    else
                    {
                        item["Title"] = title;
                        item["SortIndex"] = sortIndex;

                        BusinessManager.Update(item);

                        Console.WriteLine("{0}. Updated {1} '{2}'", c++.ToString().PadLeft(6), shortName.PadLeft(8), title);

                        j++;
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine("{0}", ex.Message);
                }
            }

            // PrintBICInfo(metaModel);
            Console.WriteLine("\n{0} rows created", i - 1);
            Console.WriteLine("\n{0} rows updated", j - 1);

            Console.WriteLine(string.Empty);
        }

        private static void CreateItemTypesOfTeachingAid()
        {
            Console.WriteLine("Loading XML document for types of teaching aid");

            XDocument doc = XDocument.Load(Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, "listvalues.xml")));

            XElement[] elements = doc.Root.Elements()
                .Where(e => e.Element("typ").Value == "kb.laromedelstyp")
                .ToArray();

            Console.WriteLine("Document contains {0} rows", elements.Count());

            int i = 1;
            int j = 1;
            int c = 1;

            foreach (XElement element in elements)
            {
                try
                {
                    string title = element.Element(element.Name.Namespace + "description").Value;
                    string shortName = element.Element(element.Name.Namespace + "shortname").Value;
                    int sortIndex = int.Parse(element.Element(element.Name.Namespace + "sortindex").Value);

                    EntityObject item = BusinessManager.List("ItemTypeOfTeachingAid",
                        new FilterElement[] {
                            new FilterElement("ShortName", FilterElementType.Equal, shortName)})
                        .FirstOrDefault();

                    if (item == null)
                    {
                        item = BusinessManager.InitializeEntity(("ItemTypeOfTeachingAid"));

                        item["Title"] = title;
                        item["ShortName"] = shortName;
                        item["SortIndex"] = sortIndex;

                        BusinessManager.Create(item);

                        Console.WriteLine("{0}. Created {1} '{2}'", c++.ToString().PadLeft(6), shortName.PadLeft(8), title);

                        i++;
                    }
                    else
                    {
                        item["Title"] = title;
                        item["SortIndex"] = sortIndex;

                        BusinessManager.Update(item);

                        Console.WriteLine("{0}. Updated {1} '{2}'", c++.ToString().PadLeft(6), shortName.PadLeft(8), title);

                        j++;
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine("{0}", ex.Message);
                }
            }

            // PrintBICInfo(metaModel);
            Console.WriteLine("\n{0} rows created", i - 1);
            Console.WriteLine("\n{0} rows updated", j - 1);

            Console.WriteLine(string.Empty);
        }

        private static void CreateItemHichSchoolCourses()
        {
            Console.WriteLine("Loading XML document for high school courses");

            XDocument doc = XDocument.Load(Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, "listvalues.xml")));

            XElement[] elements = doc.Root.Elements()
                .Where(e => e.Element("typ").Value == "kb.gymnasiekurs")
                .ToArray();

            Console.WriteLine("Document contains {0} rows", elements.Count());

            int i = 1;
            int j = 1;
            int c = 1;

            foreach (XElement element in elements)
            {
                try
                {
                    string title = element.Element(element.Name.Namespace + "description").Value;
                    string shortName = element.Element(element.Name.Namespace + "shortname").Value;
                    int sortIndex = int.Parse(element.Element(element.Name.Namespace + "sortindex").Value);

                    EntityObject item = BusinessManager.List("ItemHighSchoolCourse",
                        new FilterElement[] {
                            new FilterElement("ShortName", FilterElementType.Equal, shortName)})
                        .FirstOrDefault();

                    if (item == null)
                    {
                        item = BusinessManager.InitializeEntity(("ItemHighSchoolCourse"));

                        item["Title"] = title;
                        item["ShortName"] = shortName;
                        item["SortIndex"] = sortIndex;

                        BusinessManager.Create(item);

                        Console.WriteLine("{0}. Created {1} '{2}'", c++.ToString().PadLeft(6), shortName.PadLeft(8), title);

                        i++;
                    }
                    else
                    {
                        item["Title"] = title;
                        item["SortIndex"] = sortIndex;

                        BusinessManager.Update(item);

                        Console.WriteLine("{0}. Updated {1} '{2}'", c++.ToString().PadLeft(6), shortName.PadLeft(8), title);

                        j++;
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine("{0}", ex.Message);
                }
            }

            // PrintBICInfo(metaModel);
            Console.WriteLine("\n{0} rows created", i - 1);
            Console.WriteLine("\n{0} rows updated", j - 1);

            Console.WriteLine(string.Empty);
        }

        private static void CreateItemHichSchoolPrograms()
        {
            Console.WriteLine("Loading XML document for high school programs");

            XDocument doc = XDocument.Load(Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, "listvalues.xml")));

            XElement[] elements = doc.Root.Elements()
                .Where(e => e.Element("typ").Value == "kb.gymnasieprogram")
                .ToArray();

            Console.WriteLine("Document contains {0} rows", elements.Count());

            int i = 1;
            int j = 1;
            int c = 1;

            foreach (XElement element in elements)
            {
                try
                {
                    string title = element.Element(element.Name.Namespace + "description").Value;
                    string shortName = element.Element(element.Name.Namespace + "shortname").Value;
                    int sortIndex = int.Parse(element.Element(element.Name.Namespace + "sortindex").Value);

                    EntityObject item = BusinessManager.List("ItemHighSchoolProgram",
                        new FilterElement[] {
                            new FilterElement("ShortName", FilterElementType.Equal, shortName)})
                        .FirstOrDefault();

                    if (item == null)
                    {
                        item = BusinessManager.InitializeEntity(("ItemHighSchoolProgram"));

                        item["Title"] = title;
                        item["ShortName"] = shortName;
                        item["SortIndex"] = sortIndex;

                        BusinessManager.Create(item);

                        Console.WriteLine("{0}. Created {1} '{2}'", c++.ToString().PadLeft(6), shortName.PadLeft(8), title);

                        i++;
                    }
                    else
                    {
                        item["Title"] = title;
                        item["SortIndex"] = sortIndex;

                        BusinessManager.Update(item);

                        Console.WriteLine("{0}. Updated {1} '{2}'", c++.ToString().PadLeft(6), shortName.PadLeft(8), title);

                        j++;
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine("{0}", ex.Message);
                }
            }

            // PrintBICInfo(metaModel);
            Console.WriteLine("\n{0} rows created", i - 1);
            Console.WriteLine("\n{0} rows updated", j - 1);

            Console.WriteLine(string.Empty);
        }

        private static void CreateAwards()
        {
            Console.WriteLine("Loading XML document for awards");

            XDocument doc = XDocument.Load(Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, "listvalues.xml")));

            XElement[] elements = doc.Root.Elements()
                .Where(e => e.Element("typ").Value == "extra.utmarkelser")
                .ToArray();

            Console.WriteLine("Document contains {0} rows", elements.Count());

            int i = 1;
            int j = 1;
            int c = 1;

            foreach (XElement element in elements)
            {
                try
                {
                    string title = element.Element(element.Name.Namespace + "description").Value;
                    string shortName = element.Element(element.Name.Namespace + "shortname").Value;
                    int sortIndex = int.Parse(element.Element(element.Name.Namespace + "sortindex").Value);

                    EntityObject item = BusinessManager.List("ItemAward",
                        new FilterElement[] {
                            new FilterElement("ShortName", FilterElementType.Equal, shortName)})
                        .FirstOrDefault();

                    if (item == null)
                    {
                        item = BusinessManager.InitializeEntity(("ItemAward"));

                        item["Title"] = title;
                        item["ShortName"] = shortName;
                        item["SortIndex"] = sortIndex;

                        BusinessManager.Create(item);

                        Console.WriteLine("{0}. Created {1} '{2}'", c++.ToString().PadLeft(6), shortName.PadLeft(8), title);

                        i++;
                    }
                    else
                    {
                        item["Title"] = title;
                        item["SortIndex"] = sortIndex;

                        BusinessManager.Update(item);

                        Console.WriteLine("{0}. Updated {1} '{2}'", c++.ToString().PadLeft(6), shortName.PadLeft(8), title);

                        j++;
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine("{0}", ex.Message);
                }
            }

            // PrintBICInfo(metaModel);
            Console.WriteLine("\n{0} rows created", i - 1);
            Console.WriteLine("\n{0} rows updated", j - 1);

            Console.WriteLine(string.Empty);
        }

        private static void CreateMediaTypes()
        {
            Console.WriteLine("Loading XML document for media types");

            XDocument doc = XDocument.Load(Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, "media-bandtyp.xml")));

            XElement[] elements = doc.Root.Elements()
                .Where(e => e.Name == "mediatyp")
                .ToArray();

            Console.WriteLine("Document contains {0} rows", elements.Count());

            int i = 1;
            int j = 1;
            int c = 1;

            foreach (XElement element in elements)
            {
                try
                {
                    string title = element.Attribute("name").Value;
                    bool allowText = bool.Parse(element.Attribute("allowtext").Value);


                    EntityObject item = BusinessManager.List("ItemMediaType",
                        new FilterElement[] {
                            new FilterElement("Title", FilterElementType.Equal, title)})
                        .FirstOrDefault();

                    if (item == null)
                    {
                        item = BusinessManager.InitializeEntity(("ItemMediaType"));

                        item["Title"] = title;
                        item["AllowText"] = allowText;

                        BusinessManager.Create(item);

                        Console.WriteLine("{0}. Created {1}", c++.ToString().PadLeft(6), title);

                        i++;
                    }
                    else
                    {
                        item["Title"] = title;
                        item["AllowText"] = allowText;

                        BusinessManager.Update(item);

                        Console.WriteLine("{0}. Updated {1}", c++.ToString().PadLeft(6), title);

                        j++;
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine("{0}", ex.Message);
                }
            }

            // PrintBICInfo(metaModel);
            Console.WriteLine("\n{0} rows created", i - 1);
            Console.WriteLine("\n{0} rows updated", j - 1);

            Console.WriteLine(string.Empty);
        }

        private static void CreateFormats()
        {
            Console.WriteLine("Loading XML document for formats");

            XDocument doc = XDocument.Load(Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, "media-bandtyp.xml")));

            XElement[] elements = doc.Root.Elements()
                .Where(e => e.Name == "mediatyp" && !bool.Parse(e.Attribute("allowtext").Value))
                .Elements()
                .ToArray();

            Console.WriteLine("Document contains {0} rows", elements.Count());

            int i = 1;
            int j = 1;
            int c = 1;

            foreach (XElement element in elements)
            {
                try
                {
                    string title = element.Value;

                    EntityObject mediaType = BusinessManager.List("ItemMediaType",
                        new FilterElement[] {
                            new FilterElement("Title", FilterElementType.Equal, element.Parent.Attribute("name").Value)})
                        .First();

                    EntityObject item = BusinessManager.List("ItemFormat",
                        new FilterElement[] {
                            new FilterElement("Title", FilterElementType.Equal, title),
                            new FilterElement("ItemMediaTypeId", FilterElementType.Equal, mediaType.PrimaryKeyId)})
                        .FirstOrDefault();

                    if (item == null)
                    {
                        item = BusinessManager.InitializeEntity(("ItemFormat"));

                        item["Title"] = title;
                        item["ItemMediaTypeId"] = mediaType.PrimaryKeyId;
                        item["ItemMediaType"] = mediaType["Title"];

                        BusinessManager.Create(item);

                        Console.WriteLine("{0}. Created {1}", c++.ToString().PadLeft(6), title);

                        i++;
                    }
                    else
                    {
                        item["Title"] = title;
                        item["ItemMediaType"] = mediaType["Title"];

                        BusinessManager.Update(item);

                        Console.WriteLine("{0}. Updated {1}", c++.ToString().PadLeft(6), title);

                        j++;
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine("{0}", ex.Message);
                }
            }

            // PrintBICInfo(metaModel);
            Console.WriteLine("\n{0} rows created", i - 1);
            Console.WriteLine("\n{0} rows updated", j - 1);

            Console.WriteLine(string.Empty);
        }

        private static void CreateGenresBicCrossValues()
        {
            Console.WriteLine("Loading XML document for cross values");

            XDocument doc = XDocument.Load(Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, "crossvalues.xml")));

            XElement[] elements = doc.Root.Elements()
                .Where(e => e.Element("source").Value == "genre" && e.Element("dest").Value == "bic")
                .ToArray();

            Console.WriteLine("Document contains {0} BIC rows", elements.Count());

            int c = 0;
            int i = 0;
            int j = 0;

            foreach (XElement element in elements)
            {
                try
                {
                    string genreKey = element.Element(element.Name.Namespace + "code").Value;
                    string bicKey = element.Element(element.Name.Namespace + "value").Value;

                    EntityObject genre = BusinessManager.List("ItemGenre", new FilterElement[] {
                        new FilterElement("ShortName", FilterElementType.Equal, genreKey)
                    }).FirstOrDefault();

                    if (genre == null)
                    {
                        Console.WriteLine("WARNING! Could not load genre {0}", genreKey);
                        j++;
                        continue;
                    }

                    EntityObject bic = BusinessManager.List("BIC_entry", new FilterElement[] {
                        new FilterElement("Key", FilterElementType.Equal, bicKey)
                    }).FirstOrDefault();

                    if (bic == null)
                    {
                        Console.WriteLine("WARNING! Could not load BIC-code {0}", bicKey);
                        j++;
                        continue;
                    }

                    genre["BIC_entryId"] = bic.PrimaryKeyId;
                    BusinessManager.Update(genre);
                    i++;

                    Console.WriteLine("{0}. Updated {1} to '{2} {3}'", c++.ToString().PadLeft(6), genre["ShortName"].ToString().PadLeft(8), bic["Key"], bic["Title"]);

                }

                catch (Exception ex)
                {
                    Console.WriteLine("Update of {0} caused an exception: {1}", element.Element(element.Name.Namespace + "value").Value, ex.Message);
                }
            }

            // PrintBICInfo(metaModel);
            Console.WriteLine("\n{0} rows updated", i);
            Console.WriteLine("\n{0} rows failed", j);

            Console.WriteLine(string.Empty);
        }

        private static void CreateGenresCommodityGroupCrossValues()
        {
            Console.WriteLine("Loading XML document for cross values");

            XDocument doc = XDocument.Load(Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, "crossvalues.xml")));

            XElement[] elements = doc.Root.Elements()
                .Where(e => e.Element("source").Value == "genre" && e.Element("dest").Value == "varugrupp")
                .ToArray();

            Console.WriteLine("Document contains {0} commodity group rows", elements.Count());

            int c = 0;
            int i = 0;
            int j = 0;

            foreach (XElement element in elements)
            {
                try
                {
                    string genreKey = element.Element(element.Name.Namespace + "code").Value;
                    string commodityGroupKey = element.Element(element.Name.Namespace + "value").Value;

                    EntityObject genre = BusinessManager.List("ItemGenre", new FilterElement[] {
                        new FilterElement("ShortName", FilterElementType.Equal, genreKey)
                    }).FirstOrDefault();

                    if (genre == null)
                    {
                        Console.WriteLine("WARNING! Could not load genre {0}", genreKey);
                        j++;
                        continue;
                    }

                    EntityObject commodityGroup = BusinessManager.List("ItemCommodityGroup", new FilterElement[] {
                        new FilterElement("ShortName", FilterElementType.Equal, commodityGroupKey)
                    }).FirstOrDefault();

                    if (commodityGroup == null)
                    {
                        Console.WriteLine("WARNING! Could not load commodity group {0}", commodityGroupKey);
                        j++;
                        continue;
                    }

                    genre["ItemCommodityGroupId"] = commodityGroup.PrimaryKeyId;
                    BusinessManager.Update(genre);
                    i++;

                    Console.WriteLine("{0}. Updated {1} to '{2} {3}'", c++.ToString().PadLeft(6), genre["ShortName"].ToString().PadLeft(8), commodityGroup["ShortName"], commodityGroup["Title"]);

                }

                catch (Exception ex)
                {
                    Console.WriteLine("Update of {0} caused an exception: {1}", element.Element(element.Name.Namespace + "value").Value, ex.Message);
                }
            }

            // PrintBICInfo(metaModel);
            Console.WriteLine("\n{0} rows updated", i);
            Console.WriteLine("\n{0} rows failed", j);

            Console.WriteLine(string.Empty);
        }

        private static void CreateItemBicCrossValues()
        {
            Console.WriteLine("Loading XML document for cross values");

            XDocument doc = XDocument.Load(Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, "kb.xml")));

            XElement[] elements = doc.Root.Elements()
                .Where(e => e.Element("feld").Value == "bic")
                .ToArray();

            Console.WriteLine("Document contains {0} BIC rows", elements.Count());

            int c = 0;
            int i = 0;
            int j = 0;

            foreach (XElement element in elements)
            {
                try
                {
                    string bicKey = element.Element(element.Name.Namespace + "val").Value;
                    string itemNumber = element.Element(element.Name.Namespace + "artikelnummer").Value;

                    // först hämtar vi artikel
                    EntityObject item = BusinessManager.List("ItemArticle", new FilterElement[] {
                        new FilterElement("ItemNumber", FilterElementType.Equal, itemNumber)
                    }).FirstOrDefault();

                    if (item == null)
                    {
                        //Console.WriteLine("WARNING! Could not load item {0}", itemNumber);
                        j++;
                        continue;
                    }

                    // därefter hämtar vi BIC-objektet
                    EntityObject bic = BusinessManager.List("BIC_entry", new FilterElement[] {
                        new FilterElement("Key", FilterElementType.Equal, bicKey)
                    }).FirstOrDefault();

                    if (bic == null)
                    {
                        //Console.WriteLine("WARNING! Could not load bic entry {0}", bicKey);
                        j++;
                        continue;
                    }

                    // Här testar jag om det redan finns en rad med motsvarande nycklar
                    EntityObject[] bridgeRows = BusinessManager.List("ItemArticle_BIC_entry", new FilterElement[]{
                        new FilterElement("Field1Id", FilterElementType.Equal, item.PrimaryKeyId),
                        new FilterElement("Field2Id", FilterElementType.Equal, bic.PrimaryKeyId)});

                    if (bridgeRows.Length == 0)
                    {
                        // Här skapar jag själva "kopplingen"
                        EntityObject bridge = BusinessManager.InitializeEntity("ItemArticle_BIC_entry");

                        bridge["Field1Id"] = item.PrimaryKeyId;
                        bridge["Field1"] = item["Title"];
                        bridge["Field2Id"] = bic.PrimaryKeyId;
                        bridge["Field2"] = bic["Title"];

                        BusinessManager.Create(bridge);

                        i++;

                        Console.WriteLine("{0}. Created bridge between {1} and {2}", c++.ToString().PadLeft(6), itemNumber.PadLeft(8), bicKey);
                    }
                    else
                        Console.WriteLine("{0}. Bridge between item {1} and BIC {2} already exists", c++.ToString().PadLeft(6), itemNumber, bicKey);
                }

                catch (Exception ex)
                {
                    Console.WriteLine("Update of {0} caused an exception: {1}", element.Element(element.Name.Namespace + "artikelnummer").Value, ex.Message);
                }
            }

            // PrintBICInfo(metaModel);
            Console.WriteLine("\n{0} rows created", i);
            Console.WriteLine("\n{0} rows skipped", j);

            Console.WriteLine(string.Empty);
        }

        private static void CreateOrganizations(string xmlFileName)
        {
            MetaEnumItem organizationType = organizationTypes
                .Where(t => t.Name == "Organization")
                .First();

            Console.WriteLine("Loading XML document for customers");

            XDocument doc = XDocument.Load(Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, xmlFileName)));

            XElement[] elements = doc.Root.Elements()
                .Where(e => e.Name == "kund")
                .ToArray();

            Console.WriteLine("Document contains {0} customer rows", elements.Count());

            int i = 1;
            int j = 1;
            int c = 1;

            foreach (XElement element in elements)
            {
                try
                {
                    string id = element.Element(element.Name.Namespace + "id").Value;
                    string name = element.Element(element.Name.Namespace + "namn").Value;
                    string shortName = element.Element(element.Name.Namespace + "kortnamn").Value;
                    string typ = element.Element(element.Name.Namespace + "typ").Value;

                    string gln = element.Element(element.Name.Namespace + "gln").Value;
                    string primaryContactName = element.Element(element.Name.Namespace + "ansvarig").Value;
                    string webPage = element.Element(element.Name.Namespace + "hemsida").Value;
                    string organizationNumber = element.Element(element.Name.Namespace + "organisationsnummer").Value;
                    string bookstoreNumber = element.Element(element.Name.Namespace + "seelignummer").Value;

                    string parentOrganizationId = element.Element(element.Name.Namespace + "avtal_ref").Value;

                    

                    EntityObject item = BusinessManager.List("Organization",
                        new FilterElement[] {
                            new FilterElement("CustomId", FilterElementType.Equal, int.Parse(id))})
                        .FirstOrDefault();


                    if (item == null)
                    {
                        EntityObject[] items = BusinessManager.List("Organization",
                            new FilterElement[] {
                                new FilterElement("Name", FilterElementType.Equal, name)})
                            .ToArray();
                        
                        if (items.Length == 1)
                            item = items[0];
                        else if (items.Length > 1)
                        {
                            Console.WriteLine("{0}. Skipping {1} because of duplicat names.", c++.ToString().PadLeft(6), name);
                            continue;
                        }
                    }


                    EntityObject parentItem = BusinessManager.List("Organization",
                        new FilterElement[] {
                            new FilterElement("CustomId", FilterElementType.Equal, int.Parse(parentOrganizationId))})
                        .FirstOrDefault();

                    if (item == null)
                    {
                        item = BusinessManager.InitializeEntity(("Organization"));

                        item["CustomId"] = int.Parse(id);
                        item["Name"] = name;
                        item["OrganizationType"] = organizationType.Handle;
                        item["ShortName"] = shortName;

                        item["GLN"] = gln;
                        item["PrimaryContactName"] = primaryContactName;
                        item["WebPage"] = webPage;
                        item["OrganizationNumber"] = organizationNumber;
                        item["BookstoreNumber"] = bookstoreNumber;

                        if (parentItem != null)
                        {
                            item["ParentId"] = parentItem.PrimaryKeyId;
                        }

                        PrimaryKeyId customerKey = BusinessManager.Create(item);

                        Console.WriteLine("{0}. Creating {1}", c++.ToString().PadLeft(6), name);

                        switch (typ)
                        {
                            case "distributor":
                            case "grossist":
                                CreateDistributor(customerKey, name, element);
                                break;
                            case "forlag/distributor":
                                CreatePublisher(customerKey, name, element);
                                CreateDistributor(customerKey, name, element);
                                break;
                            case "forlag":
                                CreatePublisher(customerKey, name, element);
                                break;
                            case "bokhandel":
                                CreateBookStore(customerKey, name, element);
                                break;
                        }

                        CreateAddresses(element, customerKey);

                        i++;
                    }
                    else
                    {
                        item["CustomId"] = int.Parse(id);
                        item["Name"] = name;
                        item["OrganizationType"] = organizationType.Handle;
                        item["ShortName"] = shortName;

                        item["GLN"] = gln;
                        item["PrimaryContactName"] = primaryContactName;
                        item["WebPage"] = webPage;
                        item["OrganizationNumber"] = organizationNumber;
                        item["BookstoreNumber"] = bookstoreNumber;

                        if (parentItem != null)
                        {
                            item["ParentId"] = parentItem.PrimaryKeyId;
                        }



                        Console.WriteLine("{0}. Updating {1}", c++.ToString().PadLeft(6), name);

                        BusinessManager.Update(item);

                        switch (typ)
                        {
                            case "distributor":
                            case "grossist":
                                CreateDistributor((PrimaryKeyId)item.PrimaryKeyId, name, element);
                                break;
                            case "forlag/distributor":
                                CreateDistributor((PrimaryKeyId)item.PrimaryKeyId, name, element);
                                CreatePublisher((PrimaryKeyId)item.PrimaryKeyId, name, element);
                                break;
                            case "forlag":
                                CreatePublisher((PrimaryKeyId)item.PrimaryKeyId, name, element);
                                break;
                        }

                        CreateAddresses(element, (PrimaryKeyId)item.PrimaryKeyId);

                        j++;
                    }

                }

                catch (Exception ex)
                {
                    exceptionList.Add(new KeyValuePair<string, Exception>(element.Element(element.Name.Namespace + "id").Value, ex));
                    Console.WriteLine("{0}", ex.Message);
                }
            }

            Console.WriteLine("\n{0} rows created", i - 1);
            Console.WriteLine("\n{0} rows updated", j - 1);

            Console.WriteLine(string.Empty);
        }

        private static void CreatePublisher(PrimaryKeyId organizationKey, string name, XElement element)
        {
            string distributor_id = element.Element(element.Name.Namespace + "ref_id").Value;

            EntityObject publisher = BusinessManager.List("Publisher",
                new FilterElement[] {
                            new FilterElement("OrganizationId", FilterElementType.Equal, organizationKey)})
                .FirstOrDefault();

            EntityObject distributor = null;

            // Kontrollera om det finns en separat distributör
            if (!string.IsNullOrEmpty(distributor_id))
            {
                EntityObject distributorOrganization = BusinessManager.List("Organization",
                    new FilterElement[] {
                            new FilterElement("CustomId", FilterElementType.Equal, distributor_id)})
                    .FirstOrDefault();

                if (distributorOrganization != null)
                {
                    distributor = BusinessManager.List("Distributor",
                        new FilterElement[] {
                            new FilterElement("OrganizationId", FilterElementType.Equal, distributorOrganization.PrimaryKeyId)})
                        .FirstOrDefault();
                }
            }

            // Om separat distributör inte fanns, kontrollera om förlaget är sin egen distributör
            if (distributor == null)
            {
                distributor = BusinessManager.List("Distributor",
                    new FilterElement[] {
                            new FilterElement("OrganizationId", FilterElementType.Equal, organizationKey)})
                    .FirstOrDefault();
            }

            if (publisher == null)
            {
                publisher = BusinessManager.InitializeEntity(("Publisher"));

                publisher["OrganizationId"] = organizationKey;
                publisher["Title"] = name;
                publisher["MainDistributorId"] = distributor != null ? distributor.PrimaryKeyId : null;

                Console.WriteLine(" ".PadLeft(8) + "..creating publisher {0}", name);
                PrimaryKeyId distributorKey = BusinessManager.Create(publisher);
            }
            else
            {
                publisher["Title"] = name;
                publisher["MainDistributorId"] = distributor != null ? distributor.PrimaryKeyId : null;

                Console.WriteLine(" ".PadLeft(8) + "..updating publisher {0}", name);

                BusinessManager.Update(publisher);

            }

        }

        private static void CreateDistributor(PrimaryKeyId organizationKey, string name, XElement element)
        {
            int distributorId = int.Parse(element.Element(element.Name.Namespace + "id").Value);

            EntityObject item = BusinessManager.List("Distributor",
                new FilterElement[] {
                            new FilterElement("OrganizationId", FilterElementType.Equal, organizationKey)})
                .FirstOrDefault();

            string publiceringskanal = element.Element(element.Name.Namespace + "receive_orders").Value == "ftp" ? "Fil" : "Webb";

            if (item == null)
            {
                item = BusinessManager.InitializeEntity(("Distributor"));

                item["OrganizationId"] = organizationKey;
                item["Title"] = name;
                item["PublishChannel"] = publiceringskanal;

                Console.WriteLine(" ".PadLeft(8) + "..creating distributor {0}", name);
                PrimaryKeyId distributorKey = BusinessManager.Create(item);
            }
            else
            {
                item["Title"] = name;
                item["PublishChannel"] = publiceringskanal;

                Console.WriteLine(" ".PadLeft(8) + "..updating distributor {0}", name);

                BusinessManager.Update(item);
            }

            CreateDistributorCatalogNode(distributorId, name);
        }

        private static void CreateBookStore(PrimaryKeyId organizationKey, string name, XElement element)
        {
            EntityObject item = BusinessManager.List("BookStore",
                new FilterElement[] {
                    new FilterElement("OrganizationId", FilterElementType.Equal, organizationKey)})
                .FirstOrDefault();

            if (item == null)
            {
                item = BusinessManager.InitializeEntity(("BookStore"));

                item["OrganizationId"] = organizationKey;
                item["Title"] = name;

                Console.WriteLine(" ".PadLeft(8) + "..creating book store {0}", name);
                PrimaryKeyId distributorKey = BusinessManager.Create(item);
            }
            else
            {
                item["Title"] = name;

                Console.WriteLine(" ".PadLeft(8) + "..updating book store {0}", name);

                BusinessManager.Update(item);
            }


        }

        private static void CreateDistributorCatalogNode(int distributorId, string name)
        {
            string nodeCode = "D" + distributorId.ToString("000");

            CatalogDto catalogDto = CatalogContext.Current.GetCatalogDto();
            CatalogNodeDto nodeDto = CatalogContext.Current.GetCatalogNodeDto(nodeCode, new CatalogNodeResponseGroup(CatalogNodeResponseGroup.ResponseGroup.CatalogNodeFull));
            Mediachase.MetaDataPlus.Configurator.MetaClass nodeMetaClass = Mediachase.MetaDataPlus.Configurator.MetaClass.Load(CatalogContext.MetaDataContext, "CatalogNodeEx");

            CatalogDto.CatalogRow rootCatalog = catalogDto.Catalog.Where(r => r.Name.Trim() == "Böcker").FirstOrDefault();

            if (nodeDto.CatalogNode.Count == 0)
            {
                // create catalog node
                CatalogNodeDto.CatalogNodeRow newCatalogNodeRow = nodeDto.CatalogNode.NewCatalogNodeRow();

                // set node properties
                newCatalogNodeRow.ApplicationId = AppContext.Current.ApplicationId;
                newCatalogNodeRow.ParentNodeId = 0;
                newCatalogNodeRow.CatalogId = rootCatalog.CatalogId;
                newCatalogNodeRow.TemplateName = "NodeInfoTemplate";
                newCatalogNodeRow.Code = nodeCode;
                newCatalogNodeRow.StartDate = DateTime.Now;
                newCatalogNodeRow.EndDate = DateTime.Now.AddYears(200).ToUniversalTime();
                newCatalogNodeRow.IsActive = true;
                newCatalogNodeRow.MetaClassId = nodeMetaClass.Id;
                newCatalogNodeRow.SortOrder = 0;
                newCatalogNodeRow.Name = name;

                // add node row to data table
                if (newCatalogNodeRow.RowState == DataRowState.Detached)
                    nodeDto.CatalogNode.AddCatalogNodeRow(newCatalogNodeRow);

                // create seo data row
                CatalogNodeDto.CatalogItemSeoRow seoRow = nodeDto.CatalogItemSeo.NewCatalogItemSeoRow();

                // set seo data row properties
                seoRow.ApplicationId = AppContext.Current.ApplicationId;
                seoRow.CatalogNodeId = newCatalogNodeRow.CatalogNodeId;
                seoRow["CatalogEntryId"] = System.DBNull.Value;
                seoRow.Uri = nodeCode + ".aspx";
                seoRow.Title = name;
                seoRow.Description = string.Empty;
                seoRow.Keywords = string.Empty;
                seoRow.LanguageCode = "sv-SE";

                // add seo to data table 
                if (seoRow.RowState == DataRowState.Detached)
                    nodeDto.CatalogItemSeo.AddCatalogItemSeoRow(seoRow);

                // save changes in dataset
                CatalogContext.Current.SaveCatalogNode(nodeDto);
            }
            else
            {
                // get catalog node
                CatalogNodeDto.CatalogNodeRow catalogNodeRow = nodeDto.CatalogNode[0];

                // set node properties
                catalogNodeRow.Name = name;

                // get seo data row
                CatalogNodeDto.CatalogItemSeoRow seoRow = nodeDto.CatalogItemSeo.Where(r => r.CatalogNodeId == catalogNodeRow.CatalogNodeId && r["CatalogEntryId"] == System.DBNull.Value).FirstOrDefault();

                // if seo data row is missing, create new
                if (seoRow == null)
                    seoRow = nodeDto.CatalogItemSeo.NewCatalogItemSeoRow();

                // set seo data row properties
                seoRow.ApplicationId = AppContext.Current.ApplicationId;
                seoRow.CatalogNodeId = catalogNodeRow.CatalogNodeId;
                seoRow["CatalogEntryId"] = System.DBNull.Value;
                seoRow.Uri = nodeCode + ".aspx";
                seoRow.Title = name;
                seoRow.Description = string.Empty;
                seoRow.Keywords = string.Empty;
                seoRow.LanguageCode = "sv-SE";

                if (seoRow.RowState == DataRowState.Detached)
                    nodeDto.CatalogItemSeo.AddCatalogItemSeoRow(seoRow);

                // save changes in dataset
                CatalogContext.Current.SaveCatalogNode(nodeDto);
            }


            // add meta data to catalog node
            MDP.Configurator.MetaClass metaClass = MDP.Configurator.MetaClass.Load(CatalogContext.MetaDataContext, "CatalogNodeEx");
            MDP.MetaObject metaObj = MDP.MetaObject.Load(CatalogContext.MetaDataContext, nodeDto.CatalogNode[0].CatalogNodeId, metaClass);

            if (metaObj == null)
            {
                metaObj = new MDP.MetaObject(metaClass);
                metaObj.AcceptChanges(CatalogContext.MetaDataContext);
            }
        }

        private static void CreateAddresses(XElement customerXml, PrimaryKeyId customerKey)
        {
            MetaEnumItem publicAddressType = addressTypes.Where(t => t.Name == "Public").First();
            MetaEnumItem salesAddressType = addressTypes.Where(t => t.Name == "Sales").First();
            MetaEnumItem returnGoodsAddressType = addressTypes.Where(t => t.Name == "ReturnGoods").First();

            EntityObject[] customerAddresses = BusinessManager.List("Address", new FilterElement[] {
                new FilterElement("OrganizationId", FilterElementType.Equal, customerKey)});

            EntityObject publicAddress = customerAddresses.Where(a => a["AddressType"] != null && ((int[])a["AddressType"]).Any(k => k == publicAddressType.Handle)).FirstOrDefault();
            EntityObject salesAddress = customerAddresses.Where(a => a["AddressType"] != null && ((int[])a["AddressType"]).Any(k => k == salesAddressType.Handle)).FirstOrDefault();
            EntityObject returnGoodsAddress = customerAddresses.Where(a => a["AddressType"] != null && ((int[])a["AddressType"]).Any(k => k == returnGoodsAddressType.Handle)).FirstOrDefault();

            /// Public address
            if (publicAddress == null && HasPublicAddress(customerXml))
            {
                // initialize new public address
                Console.WriteLine(" ".PadLeft(8) + "..creating new public address");
                publicAddress = BusinessManager.InitializeEntity(("Address"));
                publicAddress["OrganizationId"] = customerKey;
                publicAddress["Organization"] = customerXml.Element("namn").Value;
                publicAddress["AddressType"] = new int[] { publicAddressType.Handle };

                SetPublicAddressProperty(customerXml, publicAddress);

                // save entity
                BusinessManager.Create(publicAddress);

            }
            else
            {
                if (HasPublicAddress(customerXml))
                {
                    Console.WriteLine(" ".PadLeft(8) + "..updating public address");

                    SetPublicAddressProperty(customerXml, publicAddress);

                    // update entity
                    BusinessManager.Update(publicAddress);
                }
                else if (publicAddress == null)
                {
                    Console.WriteLine(" ".PadLeft(8) + "..deleting public address");

                    // delete entity
                    BusinessManager.Delete(publicAddress);
                }
            }

            // sales address
            if (salesAddress == null && HasSalesAddress(customerXml))
            {
                // initialize new sales address
                Console.WriteLine(" ".PadLeft(8) + "..creating new sales address");
                salesAddress = BusinessManager.InitializeEntity(("Address"));
                salesAddress["OrganizationId"] = customerKey;
                salesAddress["AddressType"] = new int[] { salesAddressType.Handle };

                SetSalesAddressProperties(customerXml, salesAddress);

                BusinessManager.Create(salesAddress);
            }
            else
            {
                if (HasSalesAddress(customerXml))
                {
                    Console.WriteLine(" ".PadLeft(8) + "..updating sales address");

                    SetSalesAddressProperties(customerXml, salesAddress);

                    // update entity
                    BusinessManager.Update(salesAddress);
                }
                else if (salesAddress != null)
                {
                    Console.WriteLine(" ".PadLeft(8) + "..deleting sales address");

                    // delete entity
                    BusinessManager.Delete(salesAddress);
                }

            }

            // return goods address
            if (returnGoodsAddress == null && HasReturnGoodsAddress(customerXml))
            {
                // initialize new return goods address
                Console.WriteLine(" ".PadLeft(8) + "..creating new return goods address");
                returnGoodsAddress = BusinessManager.InitializeEntity(("Address"));
                returnGoodsAddress["OrganizationId"] = customerKey;
                returnGoodsAddress["AddressType"] = new int[] { returnGoodsAddressType.Handle };

                SetReturnGoodsAddressProperties(customerXml, returnGoodsAddress);

                BusinessManager.Create(returnGoodsAddress);
            }
            else
            {
                if (HasReturnGoodsAddress(customerXml))
                {
                    Console.WriteLine(" ".PadLeft(8) + "..updating return goods address");

                    SetReturnGoodsAddressProperties(customerXml, returnGoodsAddress);

                    // update entity
                    BusinessManager.Update(returnGoodsAddress);
                }
                else if (returnGoodsAddress != null)
                {
                    Console.WriteLine(" ".PadLeft(8) + "..deleting return goods address");

                    // delete entity
                    BusinessManager.Delete(returnGoodsAddress);
                }

            }
        }

        private static void SetPublicAddressProperty(XElement customerXml, EntityObject publicAddress)
        {
            // set public address properties
            publicAddress["Name"] = customerXml.Element("namn").Value;
            publicAddress["Line1"] = customerXml.Element("adress1").Value.Length > 80 ? customerXml.Element("adress1").Value.Substring(0, 80) : customerXml.Element("adress1").Value;
            publicAddress["Line2"] = customerXml.Element("adress2").Value.Length > 80 ? customerXml.Element("adress2").Value.Substring(0, 80) : customerXml.Element("adress2").Value;
            publicAddress["Line3"] = customerXml.Element("adress3").Value.Length > 80 ? customerXml.Element("adress3").Value.Substring(0, 80) : customerXml.Element("adress3").Value;
            publicAddress["PostalCode"] = customerXml.Element("postnummer").Value;
            publicAddress["City"] = customerXml.Element("ort").Value;
            publicAddress["Email"] = customerXml.Element("epost").Value;
            //publicAddress["GLN"] = customerXml.Element("gln").Value;
            //publicAddress["PrimaryContactName"] = customerXml.Element("ansvarig").Value;
            //publicAddress["Url"] = customerXml.Element("hemsida").Value;
        }

        private static void SetSalesAddressProperties(XElement customerXml, EntityObject salesAddress)
        {
            // set sales address properties
            salesAddress["Name"] = customerXml.Element("saljorganisation").Value;
            salesAddress["DaytimePhoneNumber"] = customerXml.Element("saljorganisation_tel").Value;
        }

        private static void SetReturnGoodsAddressProperties(XElement customerXml, EntityObject returnGoodsAddress)
        {
            // set return goods address properties
            returnGoodsAddress["Name"] = customerXml.Element("returgodsadress1").Value;
            returnGoodsAddress["Line1"] = customerXml.Element("returgodsadress2").Value;
            returnGoodsAddress["Line2"] = customerXml.Element("returgodsadress3").Value;
        }

        private static bool HasPublicAddress(XElement customerXml)
        {
            string[] nodeNames = new string[] {
                "namn",
                "adress1",
                "adress2",
                "adress3",
                "postnummer",
                "ort" };

            return customerXml.Elements()
                .Any(e => nodeNames
                    .Any(s => s == e.Name && !string.IsNullOrEmpty(e.Value)));
        }

        private static bool HasSalesAddress(XElement customerXml)
        {
            string[] nodeNames = new string[] {
                "saljorganisation",
                "saljorganisation_tel" };

            return customerXml.Elements()
                .Any(e => nodeNames
                    .Any(s => s == e.Name && !string.IsNullOrEmpty(e.Value)));
        }

        private static bool HasReturnGoodsAddress(XElement customerXml)
        {
            string[] nodeNames = new string[] {
                "returgodsadress1",
                "returgodsadress2",
                "returgodsadress3" };

            return customerXml.Elements()
                .Any(e => nodeNames
                    .Any(s => s == e.Name && !string.IsNullOrEmpty(e.Value)));
        }
        private static void PrintEntityProperties(EntityObject entity)
        {
            foreach (EntityObjectProperty prop in entity.Properties)
                Trace.WriteLine(string.Format("{0} = {1}", prop.Name, prop.Value));
        }

        private static void PrintBICInfo(MetaClassManager metaModel)
        {
            int keyLength = (int)metaModel.MetaClasses["BIC_entry"].Fields["Key"].Attributes["MaxLength"];
            Console.WriteLine(String.Empty);
            Console.WriteLine("- BIC entries at server -");
            // List() with filter and sort
            EntityObject[] items = BusinessManager.List("BIC_entry",
                new SortingElement[] 
                                {
                                    new SortingElement("Key", SortingElementType.Asc)
                                });

            Console.Write(new string('=', 80));
            Console.WriteLine("{0}{1}", "Key".PadRight(keyLength + 2), "Title");
            Console.Write(new string('=', 80));
            foreach (EntityObject item in items)
            {
                Console.WriteLine("{0}{1}", item["Key"].ToString().PadRight(keyLength + 2), item["Title"]);
            }
        }

        private static void CreateLanguageEntries()
        {
            Console.WriteLine("Loading XML document for Language");

            XDocument doc = XDocument.Load(Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, "listvalues.xml")));

            XElement[] elements = doc.Root.Elements()
                .Where(e => e.Element("typ").Value == "extra.sprak")
                .ToArray();

            Console.WriteLine("Document contains {0} rows", elements.Count());

            int i = 1;
            int j = 1;
            int c = 1;

            foreach (XElement element in elements)
            {
                try
                {
                    string title = element.Element(element.Name.Namespace + "description").Value;
                    string key = element.Element(element.Name.Namespace + "shortname").Value;
                    int sortIndex = int.Parse(element.Element(element.Name.Namespace + "sortindex").Value);

                    EntityObject item = BusinessManager.List("ItemLanguage",
                        new FilterElement[] {
                            new FilterElement("Key", FilterElementType.Equal, key)})
                        .FirstOrDefault();

                    if (item == null)
                    {
                        item = BusinessManager.InitializeEntity(("ItemLanguage"));

                        item["Title"] = title;
                        item["Key"] = key;
                        item["SortIndex"] = sortIndex;

                        BusinessManager.Create(item);

                        Console.WriteLine("{0}. Created {1} '{2}'", c++.ToString().PadLeft(6), key.PadLeft(8), title);

                        i++;
                    }
                    else
                    {
                        item["Title"] = title;
                        item["SortIndex"] = sortIndex;

                        BusinessManager.Update(item);

                        Console.WriteLine("{0}. Updated {1} '{2}'", c++.ToString().PadLeft(6), key.PadLeft(8), title);

                        j++;
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine("{0}", ex.Message);
                }
            }

            // PrintBICInfo(metaModel);
            Console.WriteLine("\n{0} rows created", i - 1);
            Console.WriteLine("\n{0} rows updated", j - 1);

            Console.WriteLine(string.Empty);

        }

        private static void CreateAccounts()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(AccountDataCollection));
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();

            ns.Add(string.Empty, string.Empty);

            AccountDataCollection accounts = new AccountDataCollection();

            string path = Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, "login.xml"));

            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                accounts = serializer.Deserialize(fs) as AccountDataCollection;
            }

            foreach (AccountData account in accounts.Items.Take(2))
            {
                EntityObject organizationEntity = BusinessManager.List("Organization", new FilterElement[] { new FilterElement("CustomId", FilterElementType.Equal, account.Id) })
                    .FirstOrDefault();

                if (organizationEntity != null)
                {
                    Console.WriteLine("Account {0} ({1}) belongs to {2}.", account.Namn, account.Username, organizationEntity["Name"]);

                    EntityObject contactEntity = BusinessManager.List("Contact", new FilterElement[] { 
                        new AndBlockFilterElement(
                            new FilterElement("OwnerId", FilterElementType.Equal, organizationEntity.PrimaryKeyId),
                            new FilterElement("FullName", FilterElementType.Equal, account.Namn))})
                    .FirstOrDefault();

                    if (contactEntity == null)
                        contactEntity = CreateContact(account, organizationEntity);
                }
            }

        }

        private static CustomerContact CreateContact(AccountData account, EntityObject organizationEntity)
        {

            MembershipUser membershipUser = Membership.CreateUser(account.Username, account.Password);

            CustomerContact customerContact = CustomerContact.CreateInstance(membershipUser);

            customerContact.OwnerId = organizationEntity.PrimaryKeyId;
            customerContact.FullName = account.Namn;
            //customerContact.Email = account

            customerContact.SaveChanges();

            Console.WriteLine("    Contact {0} was created for organization {1}", customerContact.FullName, organizationEntity["Name"]);

            return customerContact;
        }

        private static void UpdateMembershipEmail()
        {
            Console.WriteLine("Loading XML document for customers");

            XDocument customerXDocument = XDocument.Load(Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, "kunder.xml")));
            XDocument loginXDocument = XDocument.Load(Path.GetFullPath(Path.Combine(Properties.Settings.Default.DataFolder, "login.xml")));

            XElement[] userElements = loginXDocument.Element("root").Elements("row").ToArray();
            XElement[] customerElements = customerXDocument.Element("root").Elements("kund").ToArray();


            MembershipUserCollection allUsers = Membership.GetAllUsers();

            Console.WriteLine("Found {0} users.", allUsers.Count);
            Console.WriteLine("Found {0} user elements.", userElements.Length);
            Console.WriteLine("Found {0} customer elements.", customerElements.Length);
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();

            int count = 1;

            foreach(MembershipUser user in allUsers)
            {
                XElement userElement = userElements
                    .Where(e => e.Element("username").Value.ToLower() == user.UserName.ToLower())
                    .FirstOrDefault();

                if (userElement!= null)
                {
                    Console.WriteLine("  {0}. loading customer information for user {1}, {2}.", count++, user.UserName, userElement.Element("login_id").Value);

                    string customId = userElement.Element("id").Value;
                    XElement customerElement= customerElements
                        .Where(e => e.Element("id").Value == customId)
                        .FirstOrDefault();

                    if (customerElement != null)
                    {
                        string customerName = customerElement.Element("namn").Value;
                        string email = customerElement.Element("epost").Value;

                        Console.WriteLine("  {0}, customer {1} ({2}): {3}",
                            user.UserName,
                            customerName,
                            customId,
                            email);

                        if (string.IsNullOrEmpty(email))
                        {
                            Console.WriteLine("  missing email for customer {0} ({1}).", customerName, customId);
                            //Console.ReadKey();

                            user.Email = "info@heinback.se";
                        }
                        else
                        {
                            user.Email = email;
                            
                        }

                        Membership.UpdateUser(user);
                    }
                    else
                        Console.WriteLine("  {0}. could not find customer element for         user {1}.", count++, user.UserName);
                }
                else
                    Console.WriteLine("  {0}. could not find user information for user {1}.", count++, user.UserName);
            }
        }

    }
}
