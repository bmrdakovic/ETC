using Commons.Models;
using Commons.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TrafficGenerator
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class TrafficGenerator : StatelessService
    {
        private static readonly int MinFrequency = 1;
        private static readonly int MaxFrequency = 1000;

        public SyncValue Frequency { get; }

        public TrafficGenerator(StatelessServiceContext context)
            : base(context)
        {
            Frequency = new SyncValue(MinFrequency, MaxFrequency, MinFrequency * 10);

            // var workCancellation = new CancellationTokenSource();
            // RunTrafficGeneration(workCancellation.Token);
        }

        protected override async Task RunAsync(CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();

                double delaySec = 60.0 / this.Frequency.Value;
                var clock = Task.Delay(TimeSpan.FromSeconds(delaySec), token);

                ETCrequest App = new ETCrequest();
                Commons.Utilities.Request.GetRandomETCrequest(App);

                Uri proxyAddress = RevProxies.GetETCstorageProxyAddress(this.Context);
                long partitionKey = Partitions.GetPartitionKey(App.ID);
                string proxyUrl = $"{proxyAddress}/api/ETCdata/add/?PartitionKey={partitionKey}&PartitionKind=Int64Range";

                HttpClient httpClient = new HttpClient();
                StringContent putContent = new StringContent(JsonConvert.SerializeObject(App), Encoding.UTF8, "application/json");
                // introduce a delay of 30 secconds for the first app, as statefull server is still not up
                if (App.ID == 0)
                {
                    var clockFirst = Task.Delay(TimeSpan.FromSeconds(30), token);
                    await clockFirst;
                }

                HttpResponseMessage response = await httpClient.PutAsync(proxyUrl, putContent);
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        // wait for 0.1 seconda, and then try again
                        var clockFirst = Task.Delay(TimeSpan.FromSeconds(0.1), token);
                        await clockFirst;

                        response = await httpClient.PutAsync(proxyUrl, putContent);
                    }
                }

                await clock;
            }
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

                        return new WebHostBuilder()
                            .UseKestrel()
                            .ConfigureServices(
                                services => services
                                    .AddSingleton<SyncValue>(this.Frequency)
                                    .AddSingleton(serviceContext)
                                    .AddSingleton(new HttpClient()))
                            .UseContentRoot(Directory.GetCurrentDirectory())
                            .UseStartup<Startup>()
                            .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                            .UseUrls(url)
                            .Build();
                    }))
            };
        }

        /* private async Task RunTrafficGeneration(CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();

                double delaySec = 60.0 / this.Frequency.Value;
                var clock = Task.Delay(TimeSpan.FromSeconds(delaySec), token);

                ETCrequest App = new ETCrequest();
                Commons.Utilities.Request.GetRandomETCrequest(App);

                Uri proxyAddress = RevProxies.GetETCstorageProxyAddress(this.Context);
                long partitionKey = Partitions.GetPartitionKey(App.ID);
                string proxyUrl = $"{proxyAddress}/api/ETCdata/add/?PartitionKey={partitionKey}&PartitionKind=Int64Range";

                StringContent content = new StringContent(JsonConvert.SerializeObject(App), Encoding.UTF8, "application/json");

                HttpClient httpClient = new HttpClient();

                var putContent = new StringContent($"{{ 'mrdak' : '{"abc"}' }}", Encoding.UTF8, "application/json");
                putContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                using (HttpResponseMessage response = await httpClient.PutAsync(proxyUrl, putContent))
                {
                    
                }



                await clock;
            }
        } */

    }
}
