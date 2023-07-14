using Commons.Utilities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;


namespace ETCWeb.Controllers
{
   
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class LoadController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly StatelessServiceContext context;

        public LoadController(HttpClient httpClient, StatelessServiceContext context)
        {
            this.httpClient = httpClient;
            this.context = context;
        }

        // GET: api/Load
        [HttpGet("")]
        public async Task<IActionResult> Get()
        {
            Uri proxyAddress = RevProxies.GetTrafficGeneratorProxyAddress(context);
            var proxyUrl = RevProxies.GetTrafficGeneratorProxyUrl(proxyAddress);

            using var response = await httpClient.GetAsync(proxyUrl);

            return Json(
                response.StatusCode == System.Net.HttpStatusCode.OK
                    ? JsonConvert.DeserializeObject<KeyValuePair<string, int>>(
                        await response.Content.ReadAsStringAsync())
                    : new KeyValuePair<string, int>("frequency", 10));
        }

        [HttpPut("{value}")]
        public async Task<IActionResult> SetFrequency(string value)
        {
            Uri proxyAddress = RevProxies.GetTrafficGeneratorProxyAddress(context);

            var proxyUrl = $"{RevProxies.GetTrafficGeneratorProxyUrl(proxyAddress)}/{value}";

            var putContent = new StringContent($"{{ 'name' : '{value}' }}", Encoding.UTF8, "application/json");
            putContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var response = await httpClient.PutAsync(proxyUrl, putContent);

            return new ContentResult()
            {
                StatusCode = (int)response.StatusCode,
                Content = await response.Content.ReadAsStringAsync()
            };
        }


        /// <summary>
        /// Constructs a specific proxy URL.
        /// </summary>
        private string GetTrafficGeneratorProxyUrl(Uri proxyAddress)
        {
            return ETCWeb.GetApiName(proxyAddress, "LoadData");
        }
    }
}
