using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CodigosQRComprasCEDIS_2._0.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using System.Security.Claims;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.Http;
using System.Net.Http;

namespace CodigosQRComprasCEDIS_2._0.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private ILogger<HomeController> _logger;
        public String tenantid = "avanceytec.onmicrosoft.com", authorityBaseUrl = "https://login.microsoftonline.com", username = "sistemas12@avanceytec.com.mx", password = "Avance6", clientId = "d8e2e311-b455-4e00-be57-4006ae7d9adc", clientSecret = "6i2lBY~~Ay5818lVIQhfx6XtTp.o7-mZF0", resourceBaseUrl = "https://graph.windows.net";

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
      
        public async Task<IActionResult> LogOut()
        {
            ClaimsPrincipal principal = HttpContext.User as ClaimsPrincipal;
            var client = new HttpClient();
            var requestUri = new Uri($"{authorityBaseUrl}/{tenantid}/oauth2/token");
            var authenticationBody = CreatePasswordGrantContext(username,password);
            var result = await client.PostAsync(requestUri, authenticationBody);

            return SignOut(new AuthenticationProperties()
            { RedirectUri = "CapturadeOC/Index" },
                 AzureADDefaults.AuthenticationScheme,
                 AzureADDefaults.CookieScheme,
                 AzureADDefaults.OpenIdScheme);
        }

        private HttpContent CreatePasswordGrantContext(String username,String password)
        {
            var authenticationForm = new Dictionary<String, String>
            {
                { "grant_type","password" },
                { "client_id",clientId},
                { "client_secret",clientSecret},
                { "resource", resourceBaseUrl},
                {"username", username },
                {"password",password }
            };

            return new FormUrlEncodedContent(authenticationForm);
        }
    }
}
