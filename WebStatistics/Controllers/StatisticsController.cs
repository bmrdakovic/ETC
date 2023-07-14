using Microsoft.AspNetCore.Mvc;
using System.Fabric;
using WebStatisticsM.Models;

namespace WebStatistics.Controllers
{
    [ApiController]
    [Route("[controller]")]

    public class StatisticsController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly StatelessServiceContext context;
        private List<KeyValuePair<int, uint>> listStateCount;

        public StatisticsController(HttpClient httpClient, StatelessServiceContext context)
        {
            this.httpClient = httpClient;
            this.context = context;
            listStateCount = new List<KeyValuePair<int, uint>>();
        }

        // 
        [HttpGet]
        [Route ("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            Dictionary<int, uint>? dictStatistic = WebStatistics.dictStateCount;
            List<StatisticModel> listOfStatistics = new List<StatisticModel>();

            if (dictStatistic != null)
            {
                foreach (var item in dictStatistic)
                {
                    string strState;
                    uint value = 0;
                    switch (item.Key)
                    {
                        case (int)Commons.Enums.RequestState.Created:
                            strState = "Number of created requests: ";
                            value = item.Value;
                            break;
                        case (int)Commons.Enums.RequestState.InProgress:
                            strState = "Currently processed: ";
                            value = item.Value;
                            break;
                        case (int)Commons.Enums.RequestState.Approved:
                            strState = "Approved: ";
                            value = item.Value;
                            break;
                        case (int)Commons.Enums.RequestState.Rejected:
                            strState = "Rejected: ";
                            value = item.Value;
                            break;
                        case (int)Commons.Enums.RequestState.ApprovedPlus:
                            strState = "ApprovedPlus: ";
                            value = item.Value;
                            break;
                        case (int)Commons.Enums.RequestState.RejectedPlus:
                            strState = "RejectedPlus: ";
                            value = item.Value;
                            break;
                        default:
                            strState = "Created";
                            value = item.Value;
                            break;
                    }
                    listOfStatistics.Add(new StatisticModel
                    {
                        Name = strState,
                        Count = value
                    });
                }
            }

            return View(listOfStatistics);
        }
    }
}


