using Commons.Utilities;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json;
using System.Fabric;
using System.Fabric.Description;

namespace WebStatistics
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance.
    /// </summary>
    internal sealed class WebStatistics : StatelessService
    {
        private readonly HttpClient httpClient;
        private readonly FabricClient fabricClient;
        public static Dictionary<int, uint>? dictStateCount;

        private static readonly string TotalNumberOfRequestsMetricName = "1 - Number of Requests";
        private static readonly string InProgressMetricName = "2 - InProgress";
        private static readonly string ApprovedMetricName = "3 - Approved";
        private static readonly string RejectedMetricName = "4 - Rejected";
        private static readonly string ApprovedPlusMetricName = "5 - ApprovedPlus";
        private static readonly string RejectedPlusMetricName = "6 - RejectedPlus";

        public WebStatistics(StatelessServiceContext context)
            : base(context)
        {
            httpClient = new HttpClient();
            fabricClient = new FabricClient();
            dictStateCount = new Dictionary<int, uint>();
            for (int i = 0; i <= 5; i++)
                dictStateCount[i] = 0;
        }

        private async Task PollETCrequestContainer(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            foreach (var item in dictStateCount.Keys)
            {
                dictStateCount[item] = 0;
            }

            Uri proxyAddress = RevProxies.GetETCstorageProxyAddress(this.Context);
            System.Fabric.Query.ServicePartitionList partitionList = await this.fabricClient.QueryManager.GetPartitionListAsync(RevProxies.GetETCstorageServiceName(this.Context));

            foreach (System.Fabric.Query.Partition p in partitionList)
            {
                string proxyUrl = $"{proxyAddress}/api/ETCdata/statistics/?PartitionKey={((Int64RangePartitionInformation)p.PartitionInformation).LowKey}&PartitionKind=Int64Range";
                using (HttpResponseMessage response = await httpClient.GetAsync(proxyUrl))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        if (!(response.Content is null))
                        {
                            List<KeyValuePair<int, uint>> listStateCountPart = new List<KeyValuePair<int, uint>>();
                            CancellationToken ct1 = ct;
                            listStateCountPart.AddRange(JsonConvert.DeserializeObject<List<KeyValuePair<int, uint>>>(await response.Content.ReadAsStringAsync(ct1)));
                            //
                            foreach (var item in listStateCountPart)
                            {
                                dictStateCount[item.Key] += item.Value;
                            }
                        }
                    }
                }
            }
            uint nProcessed = dictStateCount[(int)Commons.Enums.RequestState.Approved] + dictStateCount[(int)Commons.Enums.RequestState.Rejected] + dictStateCount[(int)Commons.Enums.RequestState.ApprovedPlus] + dictStateCount[(int)Commons.Enums.RequestState.RejectedPlus];
            dictStateCount[(int)Commons.Enums.RequestState.InProgress] = dictStateCount[(int)Commons.Enums.RequestState.Created] - nProcessed;
        }


        protected override async Task RunAsync(CancellationToken ct)
        {
            DefineMetrics(ct);

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                await PollETCrequestContainer(ct);
                await Task.Delay(TimeSpan.FromSeconds(5), ct);

                // update metrics
                Partition.ReportLoad(new List<LoadMetric> { new LoadMetric(TotalNumberOfRequestsMetricName, (int)dictStateCount[(int)Commons.Enums.RequestState.Created]) });
                Partition.ReportLoad(new List<LoadMetric> { new LoadMetric(InProgressMetricName, (int)dictStateCount[(int)Commons.Enums.RequestState.InProgress]) });
                Partition.ReportLoad(new List<LoadMetric> { new LoadMetric(ApprovedMetricName, (int)dictStateCount[(int)Commons.Enums.RequestState.Approved]) });
                Partition.ReportLoad(new List<LoadMetric> { new LoadMetric(RejectedMetricName, (int)dictStateCount[(int)Commons.Enums.RequestState.Rejected]) });
                Partition.ReportLoad(new List<LoadMetric> { new LoadMetric(ApprovedPlusMetricName, (int)dictStateCount[(int)Commons.Enums.RequestState.ApprovedPlus]) });
                Partition.ReportLoad(new List<LoadMetric> { new LoadMetric(RejectedPlusMetricName, (int)dictStateCount[(int)Commons.Enums.RequestState.RejectedPlus]) });
            }
        }


        private async void DefineMetrics(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            FabricClient fabricClient = new FabricClient();

            StatelessServiceLoadMetricDescription TotalNumberOfRequestsMetric = new StatelessServiceLoadMetricDescription
            {
                Name = TotalNumberOfRequestsMetricName,
                DefaultLoad = 0,
                Weight = ServiceLoadMetricWeight.High
            };
            StatelessServiceLoadMetricDescription InProgressRequestsMetric = new StatelessServiceLoadMetricDescription
            {
                Name = InProgressMetricName,
                DefaultLoad = 0,
                Weight = ServiceLoadMetricWeight.High
            };
            StatelessServiceLoadMetricDescription ApprovedRequestsMetric = new StatelessServiceLoadMetricDescription
            {
                Name = ApprovedMetricName,
                DefaultLoad = 0,
                Weight = ServiceLoadMetricWeight.High
            };
            StatelessServiceLoadMetricDescription RejectedRequestsMetric = new StatelessServiceLoadMetricDescription
            {
                Name = RejectedMetricName,
                DefaultLoad = 0,
                Weight = ServiceLoadMetricWeight.High
            };
            StatelessServiceLoadMetricDescription ApprovedPlusRequestsMetric = new StatelessServiceLoadMetricDescription
            {
                Name = ApprovedPlusMetricName,
                DefaultLoad = 0,
                Weight = ServiceLoadMetricWeight.High
            };
            StatelessServiceLoadMetricDescription RejectedPlusRequestsMetric = new StatelessServiceLoadMetricDescription
            {
                Name = RejectedPlusMetricName,
                DefaultLoad = 0,
                Weight = ServiceLoadMetricWeight.High
            };


            StatelessServiceUpdateDescription updateDescription = new StatelessServiceUpdateDescription();
            if (updateDescription.Metrics == null)
            {
                updateDescription.Metrics = new MetricsCollection();
            }
            updateDescription.Metrics.Add(TotalNumberOfRequestsMetric);
            updateDescription.Metrics.Add(InProgressRequestsMetric);
            updateDescription.Metrics.Add(ApprovedRequestsMetric);
            updateDescription.Metrics.Add(RejectedRequestsMetric);
            updateDescription.Metrics.Add(ApprovedPlusRequestsMetric);
            updateDescription.Metrics.Add(RejectedPlusRequestsMetric);

            await fabricClient.ServiceManager.UpdateServiceAsync(RevProxies.GetWebStatisticsServiceName(Context), updateDescription);
        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[]
            {
                new ServiceInstanceListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, "ServiceEndpoint", (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        var builder = WebApplication.CreateBuilder();

                        builder.Services.AddSingleton<StatelessServiceContext>(serviceContext);
                        builder.Services.AddSingleton<HttpClient>(new HttpClient());
                        builder.WebHost
                                    .UseKestrel()
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                                    .UseUrls(url);
                        
                        // Add services to the container.
                        builder.Services.AddControllersWithViews();

                        var app = builder.Build();
                        
                        // Configure the HTTP request pipeline.
                        if (!app.Environment.IsDevelopment())
                        {
                        app.UseExceptionHandler("/Home/Error");
                        }
                        app.UseStaticFiles();

                        app.UseRouting();

                        app.UseAuthorization();

                        app.MapControllerRoute(
                        name: "default",
                        pattern: "{controller=Home}/{action=Index}/{id?}");


                        return app;

                    }))
            };
        }
    }
}
