﻿using Bokrondellen.Initialization;
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
using BFData = Mediachase.BusinessFoundation.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Bokrondellen.Console.Business.StockStatus;
using Bokrondellen.Console.Util;

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
        public const string MarketId = "marketid";

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
            //try
            //{
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
            //}
            //catch (Exception e)
            //{
            //    logger.Error(e.Message, e);
            //    System.Console.WriteLine(e.Message);
            //}
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
            string marketId;
            try { catalogName = Args[Switches.Catalog]; }
            catch { throw new Exception(string.Format("Missing switch: -{0}", Switches.Catalog)); }
            try { marketId = Args[Switches.MarketId]; }
            catch { throw new Exception(string.Format("Missing switch: -{0}", Switches.MarketId)); }

            MetaDictionary allMarkets = MetaDictionary.Load(CatalogContext.MetaDataContext, MetaField.Load(CatalogContext.MetaDataContext, "_ExcludedCatalogEntryMarkets"));

            ReferenceConverter referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();

            MetaDictionaryItem[] disabledMarkets = allMarkets
                .OfType<MetaDictionaryItem>()
                .Where(m => !m.Value.Equals(marketId, StringComparison.CurrentCultureIgnoreCase))
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
                    System.Console.WriteLine();

                    CatalogEntryDto entries = CatalogContext.Current.GetCatalogEntriesDto(catalogName, node.CatalogNodeId);
                    if (entries.CatalogEntry.Count > 0)
                    {
                        int totalEntryCount = entries.CatalogEntry.Count;
                        int entryCount = 0;

                        ProgressBar progressBar = new ProgressBar('\u2588', totalEntryCount, 40);

                        System.Console.WriteLine("Found {0} entries in node {1}", totalEntryCount, node.Name);
                        System.Console.WriteLine();

                        foreach (CatalogEntryDto.CatalogEntryRow entry in entries.CatalogEntry.Rows)
                        {
                            entryCount++;

                            logger.DebugFormat("Loading meta object for entry {0}/{1}\t{2}",
                                entryCount,
                                totalEntryCount,
                                entry.Code);

                            MetaObject metaObj = MetaObject.Load(CatalogContext.MetaDataContext, entry.CatalogEntryId, metaClass) as MetaObject;

                            if (metaObj != null)
                            {
                                metaObj.Modified = DateTime.UtcNow;
                                metaObj["_ExcludedCatalogEntryMarkets"] = disabledMarkets;

                                metaObj.AcceptChanges(CatalogContext.MetaDataContext);

                                CatalogEntryDto entryDto = CatalogContext.Current.GetCatalogEntryDto(metaObj.Id);

                                IndexCatalogEntry(entryDto, metaObj, catalogLanguages);

                                progressBar.Draw(entryCount);

                                logger.InfoFormat("Saved excluded market(s) for entry {0}/{1}\t{2}\t{3}\t{4}",
                                    entryCount,
                                    totalEntryCount,
                                    entry.CatalogEntryId,
                                    metaObj["titel"] ?? metaObj["arbetstitel"],
                                    string.Join(", ", disabledMarkets.Select(i => i.Value).ToArray()));
                            }
                            else
                            {
                                logger.WarnFormat("Failed to load meta object for {0} {1}", entry.CatalogEntryId, entry.Name);
                            }
                        }

                        System.Console.WriteLine("Updated {0} entries in node {1}/{2} {3}",
                            entries.CatalogEntry.Count,
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

            MetaDictionary allStockStatuses = EnsureStockStatuses();

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

                        System.Console.WriteLine();

                        //CatalogEntryDto entryDto = context.GetCatalogEntriesDto(catalog.CatalogId, node.CatalogNodeId);

                        SqlCommand cmdEntryCount = new SqlCommand("Select Count(*) from CatalogEntry, CatalogEntryEx_Book where CatalogEntryId = CatalogEntryEx_Book.ObjectId", conn);

                        int totalEntries = (int)(cmdEntryCount.ExecuteScalar() ?? 0);

                        ProgressBar progressBar = new ProgressBar('\u2588', totalEntries, 40);

                        SqlCommand cmdEntries = new SqlCommand(@"select cn.CatalogNodeId, cn.Code NodeCode,
                                ce.CatalogEntryId, ce.Code EntryCode 
                                from CatalogEntry ce
                                    inner join CatalogEntryEx_Book book
                                        on ce.CatalogEntryId = book.ObjectId
	                                left join (NodeEntryRelation rel
		                                inner join CatalogNode cn
			                                on rel.CatalogNodeId = cn.CatalogNodeId)
		                                on ce.CatalogEntryId = rel.CatalogEntryId
                                where ce.CatalogId = @CatalogId", conn);

                        cmdEntries.Parameters.AddWithValue("@CatalogId", catalog.CatalogId);

                        using (SqlDataReader entryReader = cmdEntries.ExecuteReader())
                        {
                            int counter = 1;
                            while (entryReader.Read())
                            {
                                int catalogEntryId = entryReader.GetInt32(2);
                                string code = entryReader.GetString(3);

                                CatalogEntryDto entryDto = CatalogContext.Current.GetCatalogEntryDto(catalogEntryId);

                                BFData.Business.EntityObject itemArticle = BFData.Business.BusinessManager.List("ItemArticle", new BFData.FilterElement[] { 
                                    new BFData.FilterElement("ItemNumber", BFData.FilterElementType.Equal, code) }).FirstOrDefault();

                                MetaObject metaObj = MetaObject.Load(CatalogContext.MetaDataContext, catalogEntryId, bookMetaClass);
                                //metaObj.Modified = DateTime.UtcNow;

                                using (SqlConnection conn2 = new SqlConnection(MetaDataContext.DefaultCurrent.ConnectionString))
                                {
                                    conn2.Open();
                                    SqlCommand stockStatusCmd = new SqlCommand(@"
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
                                            where ia.ItemNumber = @ItemNumber", conn2);

                                    stockStatusCmd.Parameters.AddWithValue("@ItemNumber", code);

                                    List<string> stockStatuses = new List<string>();

                                    using (SqlDataReader stockStatusReader = stockStatusCmd.ExecuteReader())
                                    {
                                        while (stockStatusReader.Read())
                                        {
                                            string stockStatus = Convert.IsDBNull(stockStatusReader["StockStatus"]) ? null : (string)stockStatusReader["StockStatus"];
                                            int stockStatusKey = Convert.IsDBNull(stockStatusReader["Id"]) ? 0 : (int)stockStatusReader["id"];

                                            stockStatuses.Add(stockStatus);
                                        }
                                    }
                                    metaObj["lagerstatus"] = allStockStatuses.Cast<MetaDictionaryItem>().Where(d => stockStatuses.Any(s => s == d.Value)).ToArray();
                                }

                                metaObj.AcceptChanges(CatalogContext.MetaDataContext);

                                IndexCatalogEntry(entryDto, metaObj, catalogLanguages);

                                progressBar.Draw(counter++);
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
            System.Console.WriteLine("Update completed. Press any key to continue...");
            System.Console.ReadKey(true);

        }

        private static MetaDictionary EnsureStockStatuses()
        {
            MetaDictionary stockStatuses = MetaDictionary.Load(CatalogContext.MetaDataContext, MetaField.Load(CatalogContext.MetaDataContext, "lagerstatus"));
            /*
            1	Ej utkommen
            2	Finns i lager
            3	Tillfälligt slut
            4	Spärrad
            5	Utkommer ej
            6	Beställningsvara
            7	Definitivt slut
            */
            if (stockStatuses.Count == 0)
            {
                stockStatuses.Add("Ej utkommen");
                stockStatuses.Add("Finns i lager");
                stockStatuses.Add("Tillfälligt slut");
                stockStatuses.Add("Spärrad");
                stockStatuses.Add("Utkommer ej");
                stockStatuses.Add("Beställningsvara");
                stockStatuses.Add("Definitivt slut");

            }

            return stockStatuses;
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
