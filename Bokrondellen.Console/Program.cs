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

namespace Bokrondellen.Console
{
    struct Actions
    {
        public const string Undefined = null;
        public const string UpdateMarket = "UpdateMarket";
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

            System.Console.WriteLine("Found {0} nodes.", nodes.CatalogNode.Length);

            foreach (CatalogNode node in nodes.CatalogNode)
            {
                System.Console.WriteLine("Loading entries in node {0}.", node.Name);

                Entries entries = CatalogContext.Current.GetCatalogEntries(catalogName, node.ID);

                System.Console.WriteLine("Found {0} entries in node {1}", entries.Entry.Length, node.Name);

                foreach (Entry entry in entries.Entry)
                {
                    logger.DebugFormat("Loading meta object for entry\t{0}\t{1}", entry.ID, entry.ItemAttributes["titel"] ?? entry.ItemAttributes["arbetstitel"]);
                    MetaObject metaObj = MetaObject.Load(CatalogContext.MetaDataContext, entry.CatalogEntryId, metaClass) as MetaObject;

                    metaObj.Modified = DateTime.UtcNow;
                    metaObj["_ExcludedCatalogEntryMarkets"] = disabledMarkets;

                    metaObj.AcceptChanges(CatalogContext.MetaDataContext);

                    CatalogEntryDto entryDto = CatalogContext.Current.GetCatalogEntryDto(metaObj.Id);

                    IndexCatalogEntry(entryDto, metaObj, catalogLanguages);

                    logger.InfoFormat("Saved excluded market(s) for {0}{1}:\t{2}", 
                        entry.ID, 
                        entry.ItemAttributes["titel"] ?? entry.ItemAttributes["arbetstitel"],
                        string.Join(", ", disabledMarkets.Select(i => i.Value).ToArray()));
                }

                System.Console.WriteLine("Updated {0} entries in node {1}", entries.Entry.Length, node.Name);
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
