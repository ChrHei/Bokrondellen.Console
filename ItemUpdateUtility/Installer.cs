using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Configuration;
using System.IO;


namespace EVRY.One.Varnamo.ItemUpdateUtility
{
    [RunInstaller(true)]
    public partial class Installer : System.Configuration.Install.Installer
    {
        public Installer()
        {
            InitializeComponent();
        }

        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);

            System.Diagnostics.Debugger.Break();

            bool saveConfig = false;
            string targetDirectory = Context.Parameters["TargetDir"];

            string connectionString = Context.Parameters["ConnectionString"];
            string searchProviderUrl = Context.Parameters["SearchProviderUrl"];

            Configuration config = ConfigurationManager.OpenExeConfiguration(typeof(Program).Assembly.Location);

            if (!string.IsNullOrEmpty(connectionString))
            {
                config.ConnectionStrings.ConnectionStrings["EcfSqlConnection"].ConnectionString = connectionString;
                saveConfig = true;
            }

            if (!string.IsNullOrEmpty(searchProviderUrl))
            {
                Mediachase.Search.SearchConfiguration searchConfig = config.GetSection("Mediachase.Search") as Mediachase.Search.SearchConfiguration;
                searchConfig.SearchProviders.Providers[searchConfig.SearchProviders.DefaultProvider].Parameters["url"] = searchProviderUrl;
                saveConfig = true;
            }

            if (saveConfig)
                config.Save();

        }
    }
}
