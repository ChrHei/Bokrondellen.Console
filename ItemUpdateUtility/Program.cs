using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mediachase.Commerce.Catalog.Objects;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Catalog.Dto;
using Mediachase.MetaDataPlus.Configurator;
using Mediachase.MetaDataPlus;
using Mediachase.Commerce.Storage;
using BFData = Mediachase.BusinessFoundation.Data;
using System.Configuration;
using System.Data.SqlClient;
using Mediachase.Commerce.Core;
using System.Text.RegularExpressions;
using Mediachase.Commerce.Orders;
using Bokrondellen.Search.Solr;
using Mediachase.Search.Extensions;
using Mediachase.Search;
using Mediachase.BusinessFoundation.Data.Business;
using Mediachase.Commerce.Catalog.Managers;
using System.Xml.Linq;


namespace EVRY.One.Varnamo.ItemUpdateUtility
{
    class Program
    {
        private class WaitCursor
        {
            private static char[] CURSOR_CHARS = new char[] { '\\', '/', '-' };
            private int currentIndex = 0;
            private DateTime timeStamp = DateTime.Now;
            private TimeSpan treashold = new TimeSpan(0, 0, 0, 0, 200);

            public void PrintNext()
            {
                PrintNext(null);
            }

            public void PrintNext(string prefix)
            {
                if (currentIndex == CURSOR_CHARS.Length)
                    currentIndex = 0;

                if (timeStamp.Add(treashold) < DateTime.Now)
                {
                    if (string.IsNullOrEmpty(prefix))
                    {
                        Console.Write(CURSOR_CHARS[currentIndex++]);
                        Console.CursorLeft--;
                    }
                    else
                    {
                        string cursorString = string.Format("{0}: {1}", prefix.PadLeft(8, ' '), CURSOR_CHARS[currentIndex++]);
                        Console.Write(cursorString);
                        Console.CursorLeft = Console.CursorLeft - cursorString.Length;
                    }

                    timeStamp = DateTime.Now;
                }
            }

            public void Clear()
            {
                string clearString = new String(' ', Console.CursorLeft + 1);
                Console.Write(clearString);
                Console.CursorLeft = 0;
            }
        }

        private static string CONNECTION_STRING;
        private static Dictionary<string, string> ARGS = new Dictionary<string, string>();
        private static SearchManager SEARCH_MANAGER;
        private static ConsoleColor defaultColor;

        static void Main(string[] args)
        {
            Console.CursorVisible = false;
            defaultColor = Console.ForegroundColor;

            try
            {
                Initialize();

                ParseArgs(args);

                if (ARGS.Keys.Count == 0)
                    PrintSyntax();
                else
                {
                    string key = ARGS.Keys.FirstOrDefault();
                    switch (ARGS.Keys.First())
                    {
                        case "count":
                            Console.WriteLine("Counting entries matching search string...");
                            Count(ARGS[key]);
                            break;

                        case "delete":
                            Delete(ARGS[key]);
                            break;

                        case "update":
                            Update(ARGS["uri"]);
                            break;

                        case "compare":
                            Compare(ARGS);
                            break;

                        default:
                            PrintSyntax();
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                HandleError(e);
            }
            finally
            {
#if DEBUG
                Console.WriteLine("Press any key to continue... ");
                Console.ReadKey(true);
                Console.WriteLine();
#endif
                Console.CursorVisible = true;
            }

        }

        private static void HandleError(Exception e)
        {
            ConsoleColor defaultColor = Console.ForegroundColor;

            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\r\n{0}\r\n{1}", e.Message, e.StackTrace);
            }
            finally
            {
                Console.ForegroundColor = defaultColor;
            }
        }

        private static void Compare(Dictionary<string, string> arguments)
        {
            ValidateArguments(arguments, new string[] { "filter" });

            string searchPhrase = arguments["filter"];

            int currentRow = 0;
            int maxRows = Properties.Settings.Default.MaxRows;
            List<int> allKeys = new List<int>();

            WaitCursor wc = new WaitCursor();

            Console.Write("Executing search... ");
            SearchResults results = Search(searchPhrase, maxRows, currentRow);
            Console.WriteLine("done!");
            
            Console.Write("Loading rows");
            while (currentRow < results.TotalCount)
            {
                Console.Write(".");

                try
                {
                    int[] keys = GetKeyFieldValues<int>(results);
                    allKeys.AddRange(keys);
                }
                catch (Exception e)
                {
                    HandleError(e);
                }
                currentRow = currentRow + maxRows;
                results = Search(searchPhrase, maxRows, currentRow);
            }

            Console.WriteLine("\r\nFound {0} entries to compare.", allKeys.Count);
            int counter = 0;
            foreach (int key in allKeys)
            {
                wc.PrintNext((++counter).ToString());
                using (SqlConnection conn = new SqlConnection(CONNECTION_STRING))
                {
                    SqlCommand cmd = new SqlCommand(@"
                            SELECT ce.CatalogEntryId, ce.Code, book.titel, " + arguments["compare"] + @"
                            FROM CatalogEntry ce
                                INNER JOIN CatalogEntryEx_Book book
                                    ON ce.CatalogEntryId = book.ObjectId
                            WHERE ce.CatalogEntryId = @CatalogEntryId", conn);

                    cmd.Parameters.Add(new SqlParameter("@CatalogEntryId", key));

                    try
                    {
                        conn.Open();

                        SqlDataReader reader = cmd.ExecuteReader();

                        if (reader.HasRows)
                        {
                            reader.Read();
                            object dbValue = reader.GetValue(3);

                            Entry entry = CatalogContext.Current.GetCatalogEntry(key);

                            string fieldName = arguments["compare"];

                            ItemAttribute itemAttribute = entry.ItemAttributes[fieldName];

                            switch (itemAttribute.Type)
                            {
                                case "DateTime":
                                    DateTime? entryDateTime = string.IsNullOrEmpty(itemAttribute.Value[0]) ? null : TrimMilliseconds(DateTime.Parse(itemAttribute.Value[0]));
                                    DateTime? dbDateTime = Convert.IsDBNull(dbValue) ? null : TrimMilliseconds((DateTime)dbValue);

                                    if (!entryDateTime.Equals(dbDateTime))
                                    {
                                        UpdateEntry(entry, new XElement(fieldName, dbDateTime));
                                    }
                                    break;
                                default:

                                    break;
                            }
                        }
                    }
                    finally
                    {
                        if (conn.State == System.Data.ConnectionState.Open)
                            conn.Close();
                    }
                }
            }
            wc.Clear();
        }

        private static T[] GetKeyFieldValues<T>(ISearchResults results)
        {
            string name = results.SearchCriteria.KeyField;
            List<T> keyFieldValues = new List<T>();

            foreach (ISearchDocument doc in results.Documents)
            {
                for (int i = 0; i < doc.FieldCount; i++)
                {
                    ISearchField field = doc[i];
                    if (field != null && field.Name != null && field.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        keyFieldValues.Add((T)Convert.ChangeType(field.Value.ToString(), typeof(T)));
                    }
                }
            }

            return keyFieldValues.ToArray();
        }

        private static DateTime? TrimMilliseconds(DateTime dateTime)
        {
            if (dateTime == null)
                return null;

            return dateTime.AddMilliseconds(-dateTime.Millisecond);
        }

        private static void ValidateArguments(Dictionary<string, string> arguments, string[] requiredArguments)
        {
            List<string> missingArguments = new List<string>();

            foreach (string argument in requiredArguments)
                if (!arguments.Keys.Any(k => k.Equals(argument, StringComparison.CurrentCultureIgnoreCase)))
                    missingArguments.Add(argument);

            if (missingArguments.Count > 0)
                throw new ArgumentException("Missing argument(s)!", string.Join(", ", missingArguments.ToArray()));

        }

        private static void Initialize()
        {
            CONNECTION_STRING = ConfigurationManager.ConnectionStrings["EcfSqlConnection"].ConnectionString;

            BFData.DataContext.Current = new BFData.DataContext(CONNECTION_STRING);

            SetAppNameAndId();

            CatalogContext.MetaDataContext = new MetaDataContext(CONNECTION_STRING);
            OrderContext.MetaDataContext = new MetaDataContext(CONNECTION_STRING);
            MetaDataContext.DefaultCurrent = new MetaDataContext(CONNECTION_STRING);

            SEARCH_MANAGER = new SearchManager(AppContext.Current.ApplicationName);
        }

        private static void SetAppNameAndId()
        {
            Mediachase.Commerce.Core.AppContext.Current.ApplicationName = CoreConfiguration.Instance.DefaultApplicationName;

            using (SqlConnection conn = new SqlConnection(CONNECTION_STRING))
            {
                SqlCommand cmd = new SqlCommand(@"
                    Select * From Application
                    Where Name = @Name", conn);

                cmd.Parameters.Add(new SqlParameter("@Name", CoreConfiguration.Instance.DefaultApplicationName));

                try
                {

                    conn.Open();

                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        reader.Read();
                        AppContext.Current.ApplicationId = new Guid(reader["ApplicationId"].ToString());

                    }
                }
                finally
                {
                    if (conn.State != System.Data.ConnectionState.Closed)
                    {
                        conn.Close();
                    }
                }

            }
        }

        private static void Update(string Uri)
        {
            XDocument doc = XDocument.Load(Uri);

            foreach (XElement element in doc.Root.Elements("artikel"))
            {
                string artikelnummer = element.Attribute("artikelnummer").Value;
                Entry entry = CatalogContext.Current.GetCatalogEntry(artikelnummer);

                if (entry != null)
                {
                    Console.WriteLine("Updating item {0}", artikelnummer);
                    UpdateEntry(entry, element.Elements().ToArray());
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to load item {0}", artikelnummer);
                    Console.ForegroundColor = defaultColor;
                }
                Console.WriteLine();
            }


        }

        private static void Delete(string SearchPhrase)
        {
            int currentRow = 0;
            int maxRows = Properties.Settings.Default.MaxRows;
            List<int> allKeys = new List<int>();

            WaitCursor wc = new WaitCursor();

            Console.Write("Loading rows");
            ISearchResults results = Search(SearchPhrase, maxRows, currentRow);

            while (currentRow < results.TotalCount)
            {
                Console.Write(".");
                int[] keys = results.GetKeyFieldValues<int>();
                allKeys.AddRange(keys);

                currentRow = currentRow + maxRows;
                results = Search(SearchPhrase, maxRows, currentRow);
            }

            Console.WriteLine("\r\nFound {0} entries to delete", allKeys.Count);


            foreach (int key in allKeys)
            {
                try
                {
                    string code = null;
                    string titel = null;

                    using (SqlConnection conn = new SqlConnection(CONNECTION_STRING))
                    {
                        SqlCommand cmd = new SqlCommand(@"
                            SELECT ce.CatalogEntryId, ce.Code, book.titel
                            FROM CatalogEntry ce
                                INNER JOIN CatalogEntryEx_Book book
                                    ON ce.CatalogEntryId = book.ObjectId
                            WHERE ce.CatalogEntryId = @CatalogEntryId", conn);

                        cmd.Parameters.Add(new SqlParameter("@CatalogEntryId", key));

                        try
                        {
                            conn.Open();

                            SqlDataReader reader = cmd.ExecuteReader();

                            if (reader.HasRows)
                            {
                                Console.WriteLine("*** {0} ***", key);
                                reader.Read();
                                code = reader.GetString(1);
                                titel = reader.GetString(2);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            Console.WriteLine(e.StackTrace);
                            Console.WriteLine();

                        }
                        finally
                        {
                            if (conn.State == System.Data.ConnectionState.Open)
                                conn.Close();
                        }
                    }

                    if (code != null)
                    {
                        //string titel = entry.ItemAttributes["titel"].ToString();

                        Console.WriteLine("    Processing entry {0} {1}", code, titel.Length > 44 ? titel.Substring(0, 41) + "..." : titel);

                        EntityObject itemArticle = BusinessManager.List("ItemArticle", new BFData.FilterElement[] { new BFData.FilterElement("ItemNumber", BFData.FilterElementType.Equal, code) }).FirstOrDefault();

                        if (itemArticle != null)
                        {
                            EntityObject[] distInfos = BusinessManager.List("ItemArticle_Distributor", new BFData.FilterElement[] { new BFData.FilterElement("ItemArticleId", BFData.FilterElementType.Equal, itemArticle.PrimaryKeyId) });

                            foreach (EntityObject distinfo in distInfos)
                            {
                                Console.WriteLine("        Deleting distinfo.");
                                BusinessManager.Delete(distinfo);
                            }

                            BusinessManager.Delete(itemArticle);
                            Console.WriteLine("        Deleting item article.");
                        }

                        Console.WriteLine("        Deleting entry.");
                        CatalogContext.Current.DeleteCatalogEntry(key, true);
                    }
                    else
                    {
                        wc.PrintNext();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                    Console.WriteLine();
                }
            }
        }

        private static void Count(string SearchPhrase)
        {
            ISearchResults results = Search(SearchPhrase, 10, 0);

            Console.WriteLine("Found {0} matches to search phrase {1}", results.TotalCount, SearchPhrase);
        }

        private static SearchResults Search(string SearchPhrase, int RecordsToRecieve, int StartingRecord)
        {
            CatalogEntrySearchCriteria criteria = new CatalogEntrySearchCriteria();

            criteria.SearchPhrase = SearchPhrase;
            criteria.StartingRecord = StartingRecord;
            criteria.RecordsToRetrieve = RecordsToRecieve;

            SearchResults results = (SearchResults)SEARCH_MANAGER.Search(criteria);

            return results;
        }

        private static void DumpArgs()
        {
            foreach (KeyValuePair<string, string> arg in ARGS)
                Console.WriteLine("{0}:{1}", arg.Key, arg.Value);
        }

        private static void ParseArgs(string[] args)
        {
            var argsCollection = args.Select((v, i) => new { Index = i, Value = v }).ToArray();

            try
            {
                foreach (var x in argsCollection)
                    if (Regex.IsMatch(x.Value, @"^[/\-]"))
                        ARGS.Add(x.Value.Substring(1, x.Value.Length - 1), argsCollection[x.Index + 1].Value);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Invalid arguments.", e);
            }


        }

        //private static void UpdateEntry(string Isbn)
        //{
        //    Entry entry = CatalogContext.Current.GetCatalogEntry(Isbn);

        //    UpdateEntry(entry);
        //}

        private static void UpdateEntry(Entry Entry, params XElement[] Properties)
        {
            CatalogDto catalogDto = CatalogContext.Current.GetCatalogDto(); // Demo
            List<string> catalogLanguages = new List<string>();
            foreach (CatalogDto.CatalogRow catalog in catalogDto.Catalog)
            {
                if (catalog.Name.Trim() == "Böcker")
                {
                    catalogLanguages.Add(catalog.DefaultLanguage);
                    CatalogDto.CatalogLanguageRow[] languageRows = catalog.GetCatalogLanguageRows();
                    catalogLanguages.AddRange(languageRows.Select(r => r.LanguageCode));
                }
            }

            if (Entry != null)
            {
                // Load meta class
                MetaClass metaClass = MetaClass.Load(CatalogContext.MetaDataContext, "Book");

                if (metaClass != null)
                {
                    // Load meta object
                    MetaObject metaObj = MetaObject.Load(CatalogContext.MetaDataContext, Entry.CatalogEntryId, metaClass) as MetaObject;

                    if (metaObj != null)
                    {
                        metaObj.Modified = DateTime.UtcNow;
                        metaObj["titel"] = metaObj["titel"];    // to force update

                        foreach (XElement property in Properties)
                        {
                            MetaField mf = metaObj.MetaClass.MetaFields.OfType<MetaField>().Where(f => f.Name.Equals(property.Name.LocalName)).FirstOrDefault();
                            if (mf != null)
                            {
                                switch (mf.DataType)
                                {
                                    case MetaDataType.DictionarySingleValue:
                                        MetaDictionaryItem di = mf.Dictionary.OfType<MetaDictionaryItem>().Where(i => i.Value == property.Value).FirstOrDefault();
                                        if (di != null)
                                            metaObj[mf.Name] = di;
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine("{0} is not a valid value for meta field {1}.", property.Value, mf.Name);
                                            Console.ForegroundColor = defaultColor;
                                        }
                                        break;
                                    case MetaDataType.DateTime:
                                        metaObj[mf.Name] = property.Value != "" ? (DateTime?)DateTime.Parse(property.Value) : null;
                                        break;
                                    default:
                                        metaObj[mf.Name] = property.Value;
                                        break;
                                }
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Could not find {0} in meta class {1}.", property.Name.LocalName, metaObj.MetaClass.Name);
                                Console.ForegroundColor = defaultColor;
                            }
                        }

                        CatalogEntryDto entryDto = CatalogContext.Current.GetCatalogEntryDto(metaObj.Id);

                        metaObj.AcceptChanges(CatalogContext.MetaDataContext);
                        Console.WriteLine("Saved changes to {0}", entryDto.CatalogEntry[0].Code);

                        if (entryDto != null && entryDto.CatalogEntry.Count > 0)
                        {
                            IndexCatalogEntry(entryDto, metaObj, catalogLanguages);
                        }

                    }
                }
            }
        }

        private static void IndexCatalogEntry(CatalogEntryDto entry, MetaObject metaObj, IEnumerable<string> catalogLanguages)
        {
            CatalogContext.MetaDataContext.UseCurrentUICulture = false;
            int indexCounter = 0;

            MetaObjectSerialized serialized = new MetaObjectSerialized();

            foreach (string language in catalogLanguages)
            {
                CatalogContext.MetaDataContext.Language = language;
                //MetaObject metaObj = null;
                //metaObj = MetaObject.Load(CatalogContext.MetaDataContext, entryRow.CatalogEntryId, entryRow.MetaClassId);

                if (metaObj == null)
                    continue;

                serialized.AddMetaObject(language, metaObj);
                indexCounter++;
            }

            entry.CatalogEntry[0].SerializedData = serialized.BinaryValue;
            CatalogContext.MetaDataContext.UseCurrentUICulture = true;
            CatalogContext.Current.SaveCatalogEntry(entry);
        }

        private static void PrintSyntax()
        {
            string indent = new string(' ', 4);
            Console.WriteLine("SYNTAX\r\n");

            Console.WriteLine("{0}ItemUpdateUtility -count <SearchPhrase>", indent);
            Console.WriteLine("{0}ItemUpdateUtility -delete <SearchPhrase>", indent);
            Console.WriteLine("{0}ItemUpdateUtility -update <Uri>", indent);
            Console.WriteLine("{0}ItemUpdateUtility -compare <PropertyName> -filter <SearchPhrase>", indent);
        }
    }
}
