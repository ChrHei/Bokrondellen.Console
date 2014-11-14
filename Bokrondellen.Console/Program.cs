using Bokrondellen.Initialization;
using log4net;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Catalog.Dto;
using Mediachase.Commerce.Catalog.Managers;
using Mediachase.Commerce.Catalog.Objects;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Storage;
using Mediachase.MetaDataPlus;
using Mediachase.MetaDataPlus.Configurator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Bokrondellen.Console.Business.StockStatus;

namespace Bokrondellen.Console
{
    struct Actions
    {
        public const string Undefined = null;
        public const string UpdateMarket = "UpdateMarket";
        public const string SetStockStatus = "SetStockStatus";
    }

    struct Switches
    {
        public const string Undefined = null;
        public const string Action = "action";
        public const string Catalog = "catalog";
        public const string Market = "market";

    }

    class Program
    {
        private readonly static char[] switchPrefix = new char[] { '/', '-' };
        private readonly static Dictionary<string, string> Args = new Dictionary<string, string>();
        private readonly static ILog logger = LogManager.GetLogger(typeof(Program));
        // Load meta class
        static MetaClass metaClass;


        static void Main(string[] args)
        {
            try
            {
                Initialize(args);
                string action = Args[Switches.Action];

                switch (action)
                {
                    case Actions.UpdateMarket:
                        UpdateMarket();
                        break;
                    case Actions.SetStockStatus:
                        SetStockStatus();
                        break;
                }
            }
            catch (Exception e)
            {
                logger.Error(e.Message, e);
                System.Console.WriteLine(e.Message);

            }
        }

        private static void Initialize(string[] args)
        {
            ParseArgs(args);

            InitializeFramework.Initialize();

            metaClass = MetaClass.Load(CatalogContext.MetaDataContext, "Book");

        }

        private static void UpdateMarket()
        {
            string catalogName;
            string market;
            try { catalogName = Args[Switches.Catalog]; }
            catch { throw new Exception(string.Format("Missing switch: -{0}", Switches.Catalog)); }
            try { market = Args[Switches.Market]; }
            catch { throw new Exception(string.Format("Missing switch: -{0}", Switches.Market)); }

            MetaDictionary allMarkets = MetaDictionary.Load(CatalogContext.MetaDataContext, MetaField.Load(CatalogContext.MetaDataContext, "_ExcludedCatalogEntryMarkets"));

            IMarketService marketService = ServiceLocator.Current.GetInstance<IMarketService>();
            ReferenceConverter referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();

            IMarket[] enabledMarkets = marketService.GetAllMarkets().Where(m => m.IsEnabled).ToArray();

            MetaDictionaryItem[] disabledMarkets = allMarkets
                .OfType<MetaDictionaryItem>()
                .Where(mdi => enabledMarkets.Any(em => em.MarketId.Value == mdi.Value && em.MarketId.Value != market))
                .ToArray();

            System.Console.WriteLine("Updating market for catalog {0}.", catalogName);

            CatalogDto.CatalogRow catalog = CatalogContext.Current.GetCatalogDto().Catalog.Where(c => c.Name == catalogName).First();

            List<string> catalogLanguages = catalog.GetCatalogLanguageRows().Select(l => l.LanguageCode).ToList();

            CatalogNodes nodes = CatalogContext.Current.GetCatalogNodes(catalogName, new CatalogNodeResponseGroup(CatalogNodeResponseGroup.ResponseGroup.CatalogNodeInfo));

            int totalNodeCount = nodes.CatalogNode.Length;
            int nodeCount = 0;
            System.Console.WriteLine("Found {0} nodes.", nodes.CatalogNode.Length);

            foreach (CatalogNode node in nodes.CatalogNode)
            {
                try
                {
                    nodeCount++;
                    System.Console.WriteLine("Loading entries in node {0}.", node.Name);

                    Entries entries = CatalogContext.Current.GetCatalogEntries(catalogName, node.ID);
                    if (entries.Entry != null)
                    {

                        int totalEntryCount = entries.Entry.Length;
                        int entryCount = 0;

                        System.Console.WriteLine("Found {0} entries in node {1}", totalEntryCount, node.Name);

                        foreach (Entry entry in entries.Entry)
                        {
                            entryCount++;
                            logger.DebugFormat("Loading meta object for entry {0}/{1}\t{2}\t{3}",
                                entryCount,
                                totalEntryCount,
                                entry.ID,
                                entry.ItemAttributes["titel"] ?? entry.ItemAttributes["arbetstitel"]);

                            MetaObject metaObj = MetaObject.Load(CatalogContext.MetaDataContext, entry.CatalogEntryId, metaClass) as MetaObject;

                            if (metaObj != null)
                            {
                                metaObj.Modified = DateTime.UtcNow;
                                metaObj["_ExcludedCatalogEntryMarkets"] = disabledMarkets;

                                metaObj.AcceptChanges(CatalogContext.MetaDataContext);

                                CatalogEntryDto entryDto = CatalogContext.Current.GetCatalogEntryDto(metaObj.Id);

                                IndexCatalogEntry(entryDto, metaObj, catalogLanguages);

                                logger.InfoFormat("Saved excluded market(s) for entry {0}/{1}\t{2}\t{3}\t{4}",
                                    entryCount,
                                    totalEntryCount,
                                    entry.ID,
                                    entry.ItemAttributes["titel"] ?? entry.ItemAttributes["arbetstitel"],
                                    string.Join(", ", disabledMarkets.Select(i => i.Value).ToArray()));
                            }
                            else
                            {
                                logger.WarnFormat("Failed to load meta object for {0} {1}", entry.ID, entry.ItemAttributes["titel"] ?? entry.ItemAttributes["arbetstitel"]);
                            }
                        }
                        System.Console.WriteLine("Updated {0} entries in node {1}/{2} {3}",
                            entries.Entry.Length,
                            nodeCount,
                            totalNodeCount,
                            node.Name);
                    }
                    else
                    {
                        logger.WarnFormat("Failed to load entries in node {0} {1}", node.ID, node.Name);

                    }
                }
                catch (Exception e)
                {
                    logger.Warn(string.Format("Failed to process node {0} {1}", node.ID, node.Name), e);
                }
            }
        }

        private static void SetStockStatus()
        {
            ICatalogSystem context = CatalogContext.Current;
            CatalogDto catalogs = context.GetCatalogDto();

            // Load meta class
            MetaClass bookMetaClass = MetaClass.Load(CatalogContext.MetaDataContext, "Book");

            StockStatusItemCollection stockStatusColl = new StockStatusItemCollection();
            stockStatusColl.Load();

            List<string> catalogLanguages = new List<string>();
            foreach (var catalog in catalogs.Catalog.Rows.Cast<CatalogDto.CatalogRow>())
            {
                catalogLanguages.Add(catalog.DefaultLanguage);
                CatalogDto.CatalogLanguageRow[] languageRows = catalog.GetCatalogLanguageRows();
                catalogLanguages.AddRange(languageRows.Select(r => r.LanguageCode));

                using (SqlConnection conn = new SqlConnection(MetaDataContext.DefaultCurrent.ConnectionString))
                {
                    conn.Open();
                    try
                    {

                        System.Console.WriteLine("Catalog: {0} {1}", catalog.CatalogId, catalog.Name);

                        CatalogNodeDto nodeDto = context.GetCatalogNodesDto(catalog.CatalogId, new CatalogNodeResponseGroup(CatalogNodeResponseGroup.ResponseGroup.CatalogNodeInfo));

                        foreach (CatalogNodeDto.CatalogNodeRow node in nodeDto.CatalogNode)
                        {
                            System.Console.WriteLine("    Node: {0} {1}", node.Code, node.Name);

                            CatalogEntryDto entries = context.GetCatalogEntriesDto(catalog.CatalogId, node.CatalogNodeId);

                            foreach (CatalogEntryDto.CatalogEntryRow entry in entries.CatalogEntry)
                            {
                                //EntityObject itemArticle = BusinessManager.List("ItemArticle", new FilterElement[] { new FilterElement("ItemNumber", FilterElementType.Equal, entry.Code) }).FirstOrDefault();

                                MetaObject metaObj = MetaObject.Load(MetaDataContext.Instance, entry.CatalogEntryId, bookMetaClass);
                                //metaObj.Modified = DateTime.UtcNow;

                                SqlCommand cmd = new SqlCommand(@"
                                select distinct 
	                                sst.Id, sst.StockStatus
                                from cls_ItemArticle ia
	                                inner join cls_ItemArticle_Distributor iad
		                                on ia.ItemArticleId = iad.ItemArticleId
	                                inner join 
	                                (
		                                select Id, FriendlyName StockStatus, OrderId
		                                from mcmd_MetaEnum
		                                where TypeName = 'StockStatusType'
	                                ) sst
		                                on iad.StockStatus = sst.Id
                                where ia.ItemNumber = @ItemNumber", conn);

                                cmd.Parameters.AddWithValue("@ItemNumber", entry.Code);

                                List<int> stockStatuses = new List<int>();

                                using (SqlDataReader reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        string stockStatus = Convert.IsDBNull(reader["StockStatus"]) ? null : (string)reader["StockStatus"];
                                        int stockStatusKey = Convert.IsDBNull(reader["Id"]) ? 0 : (int)reader["id"];

                                        stockStatuses.Add(stockStatusKey);
                                    }
                                }

                                int flag = stockStatusColl.GetFlag(stockStatuses);

                                System.Console.WriteLine("      {0} {1} {2}",
                                    entry.Code,
                                    Convert.ToString(flag, 2).PadLeft(8, '0'),
                                    entry.Name.Length > 23 ? entry.Name.Substring(0, 20) + "..." : entry.Name);
                            }
                        }
                    }
                    finally
                    {
                        if (conn.State == System.Data.ConnectionState.Open)
                            conn.Close();
                    }
                    System.Console.WriteLine();
                }
            }

        }

        private static void IndexCatalogEntry(CatalogEntryDto entry, MetaObject metaObj, IEnumerable<string> catalogLanguages)
        {
            CatalogContext.MetaDataContext.UseCurrentThreadCulture = false;
            int indexCounter = 0;

            MetaObjectSerialized serialized = new MetaObjectSerialized();

            foreach (string language in catalogLanguages)
            {
                CatalogContext.MetaDataContext.Language = language;

                if (metaObj == null)
                    continue;

                serialized.AddMetaObject(language, metaObj);
                indexCounter++;
            }

            entry.CatalogEntry[0].SerializedData = serialized.BinaryValue;
            CatalogContext.MetaDataContext.UseCurrentThreadCulture = true;
            CatalogContext.Current.SaveCatalogEntry(entry);
        }

        static void ParseArgs(string[] args)
        {
            var switches = args.Select((s, i) => new { Index = i, Value = s })
                .Where(o => switchPrefix.Any(p => p == o.Value[0]));

            foreach (var argSwitch in switches)
            {
                if (args.Length > argSwitch.Index + 1)
                {
                    string sw = argSwitch.Value.Substring(1).ToLower();

                    Args.Add(
                        sw,
                        args[argSwitch.Index + 1]);
                }
            }

        }
    }
}
