using Commons.Enums;
using Commons.Models;
using Commons.Utilities;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json;
using System.Fabric;
using System.Fabric.Description;
using System.Text;

namespace Approver
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance.
    /// </summary>
    internal sealed class Approver : StatelessService
    {
        private HttpClient httpClient;
        private FabricClient fabricClient;

        private static readonly string ETCrequestFrequencyMetricName = "ETCrequestFrequency";
        private static readonly TimeSpan ReportingIntervalInSeconds = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan ScaleIntervalInSeconds = TimeSpan.FromSeconds(90);
        private static int NumberOfRequestsPerPrevInterval = 0;
        private static int NumberOfRequestsPerCurrentInterval = 0;
        private static int LowerLoadThreshold = 10;
        private static int UpperLoadThreshold = 100;
        private static DateTime ScaleIntervalStartTime = DateTime.Now;

        private List<KeyValuePair<uint, ETCrequest>> listRequestsInProgress;

        public Approver(StatelessServiceContext context)
            : base(context)
        {
            httpClient = new HttpClient();
            fabricClient = new FabricClient();
            listRequestsInProgress = new List<KeyValuePair<uint, ETCrequest>>();
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
                        
                        builder.Services.AddControllers();
                        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
                        builder.Services.AddEndpointsApiExplorer();
                        builder.Services.AddSwaggerGen();

                        var app = builder.Build();
                        
                        // Configure the HTTP request pipeline.
                        if (app.Environment.IsDevelopment())
                        {
                        app.UseSwagger();
                        app.UseSwaggerUI();
                        }

                        app.UseAuthorization();

                        app.MapControllers();


                        return app;

                    }))
            };
        }

        private async Task GetRequestsFromStorage(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            // TODO add call to ETCstorage to pick a number of requests, and push them to listRequestsInProgress
            Uri proxyAddress = RevProxies.GetETCstorageProxyAddress(this.Context);
            System.Fabric.Query.ServicePartitionList partitionList = await this.fabricClient.QueryManager.GetPartitionListAsync(RevProxies.GetETCstorageServiceName(this.Context));

            foreach (System.Fabric.Query.Partition p in partitionList)
            {
                string proxyUrl = $"{proxyAddress}/api/ETCdata/get/?PartitionKey={((Int64RangePartitionInformation)p.PartitionInformation).LowKey}&PartitionKind=Int64Range";
                using (HttpResponseMessage response = await httpClient.GetAsync(proxyUrl))
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        continue;
                        //TODO throw exception
                    }
                    if (response.Content != null)
                    {
                        CancellationToken ct1 = ct;
                        this.listRequestsInProgress.AddRange(JsonConvert.DeserializeObject<List<KeyValuePair<uint, ETCrequest>>>(await response.Content.ReadAsStringAsync(ct1)));
                    }
                }
            }
        }

        private async Task ProcessRequests(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            foreach (var iter in listRequestsInProgress)
            {
                ct.ThrowIfCancellationRequested();

                ETCrequest request = iter.Value;

                double speed = Request.GetSpeed(request);
                double amount = Request.GetPaymentAmount(request);
                if (Request.GetSpeed(request) < Commons.Constants.Constants.speedLimit) // 05.07.2023. add multiplication constant (<1) to produce more "...Plus" states
                {
                    if (amount <= request.credit)
                    {
                        request.credit -= amount;
                        request.state = RequestState.Approved;
                    }
                    else
                        request.state = RequestState.Rejected;
                }
                else
                {
                    if (amount <= request.credit)
                    {
                        request.credit -= amount;
                        request.state = RequestState.ApprovedPlus;
                    }
                    else
                        request.state = RequestState.RejectedPlus;
                }

                // 01.07.2023.
                await Task.Delay(TimeSpan.FromSeconds(0.05), ct);

                Uri proxyAddress = RevProxies.GetETCstorageProxyAddress(this.Context);
                long partitionKey = Partitions.GetPartitionKey(request.ID);
                string proxyUrl = $"{proxyAddress}/api/ETCdata/change/?PartitionKey={partitionKey}&PartitionKind=Int64Range";

                StringContent changeContent = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                using (HttpResponseMessage response = await httpClient.PutAsync(proxyUrl, changeContent))
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        // TODO add exception handling
                    }
                }
            }
            // requests processing is finalized - clear the list
            listRequestsInProgress.Clear();
        }


        protected override async Task RunAsync(CancellationToken ct)
        {
            // Metrics
            DefineMetricsAndScalingPolicies(ct);

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                // if there is no requests in progress, get them from ETCstorage
                int nNewRequests = 0;
                bool flagEmptyList = false;
                if (!listRequestsInProgress.Any())
                {
                    flagEmptyList = true;
                    await GetRequestsFromStorage(ct); // the call must be synchronous
                    nNewRequests = listRequestsInProgress.Count;
                    if (nNewRequests > 0)
                    {
                        ProcessRequests(ct);  // asynchronous call
                    }
                }

                DateTime timeNow = DateTime.Now;
                TimeSpan duration = timeNow - ScaleIntervalStartTime;
                if (duration >= ReportingIntervalInSeconds)
                {
                    ScaleIntervalStartTime = DateTime.Now;
                    // autoscale procedure
                    Partition.ReportLoad(new List<LoadMetric> { new LoadMetric(ETCrequestFrequencyMetricName, NumberOfRequestsPerPrevInterval + NumberOfRequestsPerCurrentInterval) });

                    NumberOfRequestsPerPrevInterval = NumberOfRequestsPerCurrentInterval;
                    NumberOfRequestsPerCurrentInterval = 0;
                }
                else if (flagEmptyList)
                {
                    NumberOfRequestsPerCurrentInterval += nNewRequests;
                }
            }
        }

        private async void DefineMetricsAndScalingPolicies(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            StatelessServiceLoadMetricDescription NumberOfRequestsPerIntervalMetric = new StatelessServiceLoadMetricDescription();
            NumberOfRequestsPerIntervalMetric.Name = ETCrequestFrequencyMetricName;
            NumberOfRequestsPerIntervalMetric.DefaultLoad = 0;
            NumberOfRequestsPerIntervalMetric.Weight = ServiceLoadMetricWeight.High;

            StatelessServiceUpdateDescription updateDescription = new StatelessServiceUpdateDescription();
            //updateDescription.PlacementConstraints = "(ExternalAccess == false)";
            if (updateDescription.Metrics == null)
            {
                updateDescription.Metrics = new MetricsCollection();
            }
            updateDescription.Metrics.Add(NumberOfRequestsPerIntervalMetric);

            AveragePartitionLoadScalingTrigger scalingTrigger = new AveragePartitionLoadScalingTrigger();
            scalingTrigger.MetricName = ETCrequestFrequencyMetricName;
            scalingTrigger.ScaleInterval = ScaleIntervalInSeconds;
            scalingTrigger.LowerLoadThreshold = LowerLoadThreshold;
            scalingTrigger.UpperLoadThreshold = UpperLoadThreshold;

            PartitionInstanceCountScaleMechanism scalingMechanism = new PartitionInstanceCountScaleMechanism();
            scalingMechanism.MaxInstanceCount = 5;
            scalingMechanism.MinInstanceCount = 1;
            scalingMechanism.ScaleIncrement = 1;

            ScalingPolicyDescription scalingPolicy = new ScalingPolicyDescription(scalingMechanism, scalingTrigger);
            if (updateDescription.ScalingPolicies == null)
            {
                updateDescription.ScalingPolicies = new List<ScalingPolicyDescription>();
            }
            updateDescription.ScalingPolicies.Add(scalingPolicy);

            await this.fabricClient.ServiceManager.UpdateServiceAsync(RevProxies.GetApproverServiceName(Context), updateDescription);
        }
    }

}
