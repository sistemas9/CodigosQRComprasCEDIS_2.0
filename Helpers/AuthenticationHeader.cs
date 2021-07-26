using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CodigosQRComprasCEDIS_2._0.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;

namespace CodigosQRComprasCEDIS_2._0.TestHelper
{
    public class AuthenticationHeader
    {
        public static String clientId;
        public static String clientSecretId;
        public static String urlBase;

        public async Task<String> getAuthenticationHeader()
        {
            ////////////Obtener datos de configuracion////////////////////////////////////////////////////////////////////////////////////
            var configs = new GetConfigsData();
            String config = await configs.GetConfigurationData("Config");
            String company = await configs.GetConfigurationData("Company");
            String urlToken = "";
            if ( company == "atp" )
            {
                if ( config == "DESARROLLO")
                    urlToken = await configs.GetConfigurationData("URL_TOKEN_ATP_TES");
                else
                    urlToken = await configs.GetConfigurationData("URL_TOKEN_ATP_PROD");
            }
            /////////////////////////////////////generar token/////////////////////////////////////////////////////////////////////////////
            token authenticationHeader = new token();
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls;
            HttpClient token = new HttpClient();
            HttpResponseMessage response = await token.GetAsync(urlToken);
            response.EnsureSuccessStatusCode();
            String responseBody = await response.Content.ReadAsStringAsync();
            responseBody = responseBody.Substring(1, responseBody.Length - 2);
            authenticationHeader = JsonConvert.DeserializeObject<token>(responseBody);
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            return authenticationHeader.Token;
        }
    }
    public class token
    {
        public String Token { get; set; }
    }
}