using MadWolfTwitchBot.Authentication.Models;
using MadWolfTwitchBot.Services;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Authentication.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApiSettingsModel _settings;

        public HomeController(ILogger<HomeController> logger, IOptions<ApiSettingsModel> settings)
        {
            _logger = logger;
            _settings = settings.Value;

            WolfAPIService.SetApiEndpoint(_settings.Endpoint);
        }

        public async Task<IActionResult> Index(string code, string scope, string state)
        {
            var cleanedUrl = Request.GetEncodedUrl().Replace(Request.QueryString.Value, "");
            var response = await WolfAPIService.GetTwitchTokenAsync(_settings.ClientId, _settings.ClientSecret, code, cleanedUrl);

            if (response != null)
                ViewData.Add("TokenString", $"{response.Access_Token};{response.Refresh_Token};{DateTime.UtcNow}");

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
