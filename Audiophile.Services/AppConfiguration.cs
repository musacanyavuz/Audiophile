using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace Audiophile.Services
{

    public static class AppConfiguration
    {
        static IConfigurationRoot cBuilderRoot = null;
        public static IConfiguration GetConfig()
        {
            
            if (cBuilderRoot == null)
            {
                cBuilderRoot = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();
                return cBuilderRoot;
            }
            else
            {
                return cBuilderRoot;
            }

        }
        public static string GetConnectionString()
        {
            return GetConfig().GetConnectionString("Audiophile").Trim();
        }

        public static bool IsDevelopment()
        {
            var settings = GetConfig();

            return (settings["Environment"] != null && settings["Environment"] == "Development") ? true : false;
        }
    }
   
}
