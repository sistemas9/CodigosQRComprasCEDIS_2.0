using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CodigosQRComprasCEDIS_2._0.Helpers
{
    public class GetConfigsData
    {
        public async Task<String> GetConfigurationData(String key)
        {
            var AppSettings = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

            return AppSettings[key];
        } 
    }
}
