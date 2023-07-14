using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Fabric;
using System.Net.Http;
using System.Threading.Tasks;

namespace TrafficGenerator.Controllers
{

    [Route("api/[controller]")]
    public class LoadDataController : Controller
    {
        private readonly StatelessServiceContext context;
        private readonly HttpClient httpClient;
        private readonly SyncValue frequency;

        public LoadDataController(StatelessServiceContext context, HttpClient httpClient, SyncValue frequency)
        {
            this.context = context;
            this.frequency = frequency;
            this.httpClient = httpClient;
        }

        [HttpGet("")]
        public async Task<IActionResult> Get()
        {
            return Json(new KeyValuePair<string, double>("frequency", this.frequency.Value));
        }

        [HttpPut("{name}")]
        public async Task<IActionResult> Put(string name)
        {
            this.frequency.Value = double.Parse(name);

            return new OkResult();
        }
    }
}
