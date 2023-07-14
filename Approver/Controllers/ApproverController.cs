using Microsoft.AspNetCore.Mvc;
using System.Fabric;

namespace Approver.Controllers
{

    [Route("api/[controller]")]
    public class ApproverController : ControllerBase
    {
        private readonly StatelessServiceContext context;
        private readonly HttpClient httpClient;

        public ApproverController(StatelessServiceContext context, HttpClient httpClient)
        {
            this.context = context;
            this.httpClient = httpClient;
        }


        [HttpGet(Name = "GetWeatherForecast")]
        public void Get()
        {

        }
    }
}