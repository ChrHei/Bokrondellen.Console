using Common.Logging;
using EPiServer.Events.ChangeNotification;
using EPiServer.Events.ChangeNotification.EventQueue;
using EPiServer.Events.ChangeNotification.Implementation;
using EPiServer.Framework.Cache;
using EPiServer.Framework.Initialization;
using EPiServer.Framework.TypeScanner;
using EPiServer.ServiceLocation;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Sql;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Core;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Inventory;
using Mediachase.Commerce.Inventory.Database;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Markets.Database;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Pricing;
using Mediachase.Commerce.Pricing.Database;
using Mediachase.MetaDataPlus;
using Mediachase.Search;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Bokrondellen.Initialization
{
    public class InitializeFramework
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(InitializeFramework));
        private static string _connectionString;

        public static void InitializeContext()
        {
            Logger.Debug("Start context initialization");

            if (string.IsNullOrEmpty(GetConnectionString()))
            {
                Logger.Debug("Connection string is null");
                return;
            }

            Logger.Debug(string.Format("Connection string: '{0}'", GetConnectionString()));

            //HttpContext.Current = new HttpContext(new HttpRequest("", "http://bokrondellen.local/", ""), new HttpResponse(new StringWriter()));

            InitializeServiceLocator();

            if (DataContext.Current == null)
                DataContext.Current = new DataContext(GetConnectionString());

            if (SqlContext.Current == null)
                SqlContext.Current = new SqlContext(GetConnectionString());

            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");

            var siteContext = SiteContext.Current;
            siteContext.AppPath = "/default.aspx";
            siteContext.LanguageName = "en";
            siteContext.SiteId = Guid.Empty;

            CatalogContext.MetaDataContext = new MetaDataContext(GetConnectionString());
            OrderContext.MetaDataContext = new MetaDataContext(GetConnectionString());
            MetaDataContext.DefaultCurrent = new MetaDataContext(GetConnectionString());

            Logger.Debug("End context initialization");
        }

        private static void InitializeServiceLocator()
        {
            Logger.Debug("Start service locator initialization");

            var container = new Container();
            var locator = new StructureMapServiceLocator(container);
            var context = new ServiceConfigurationContext(HostType.Installer, container);

            var commerce = new Mediachase.Commerce.Initialization.CommerceInitialization();
            commerce.ConfigureContainer(context);

            context.Container.Configure(ce =>
            {
                ce.Scan(scan =>
                {
                    scan.TheCallingAssembly();
                    scan.WithDefaultConventions();
                });

                ce.For<ICurrentMarket>().Singleton().Use<CurrentMarketImpl>();
                ce.For<IMarketService>().Singleton().Use<MarketServiceCache>();
                ce.For<IPriceService>().Singleton().Use<PriceServiceDatabase>();
                ce.For<IWarehouseRepository>().Singleton().Use<WarehouseRepositoryDatabase>();
                ce.For<IWarehouseInventoryService>().Singleton().Use<WarehouseInventoryServiceDatabase>();
                ce.For<IRequiredMetaFieldCollection>().Singleton().Use<NoRequiredMetaFields>();
                ce.For<ISynchronizedObjectInstanceCache>().Singleton().Use<LocalCacheWrapper>();
                ce.For<ICatalogSystem>().Use(() => CatalogContext.Current);
                ce.For<ITypeScannerLookup>().Singleton().Use<NullTypeScanner>();
                ce.For<IPriceDetailService>().Singleton().Use<PriceDetailDatabase>();
                ce.For<IChangeNotificationQueueFactory>().Singleton().Use<InMemoryQueueFactory>();
                ce.For<IChangeNotificationManager>().Singleton().Use<ChangeNotificationManager>();
                ce.For<IChangeProcessor>().Add<CatalogIndexingChangeNotificationProcessor>();

                ce.For<IConvertUserKey>().Singleton().Use<ConvertGuidUserKey>().Named(typeof(Guid).Name);
                ce.For<IConvertUserKey>().Singleton().Use<ConvertSidUserKey>().Named(typeof(SecurityIdentifier).Name);
                ce.For<IConvertUserKey>().Singleton().Use<ConvertIntUserKey>().Named(typeof(Int32).Name);
                ce.For<IConvertUserKey>().Singleton().Use<ConvertBinaryUserKey>().Named(typeof(byte[]).Name);
            });

            ServiceLocator.SetLocator(locator);

            Logger.Debug("End service locator initialization");
        }

        public static string GetConnectionString()
        {
            if (string.IsNullOrEmpty(_connectionString) && ConfigurationManager.ConnectionStrings["EcfSqlConnection"] != null)
                _connectionString = ConfigurationManager.ConnectionStrings["EcfSqlConnection"].ConnectionString;
            return _connectionString;
        }

    }
}
