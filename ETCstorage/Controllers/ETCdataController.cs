using Commons.Enums;
using Commons.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace ETCstorage.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]

    public class ETCdataController : Controller
    {
        private const string requestsDictionaryName = "ETCrequests";
        private const string inProgressDictionaryName = "ETCinProgress";
        private const string statisticsDictionaryName = "ETCstatistics";
        private readonly IReliableStateManager stateManager;

        public ETCdataController(IReliableStateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        [HttpPut("add")]
        public async Task<IActionResult> Put([FromBody] ETCrequest request)
        {
            // add to requests dictionary
            IReliableDictionary<uint, ETCrequest> requestsDictionary = await this.stateManager.GetOrAddAsync<IReliableDictionary<uint, ETCrequest>>(requestsDictionaryName);
            IReliableDictionary<int, uint> statisticsDictionary = await this.stateManager.GetOrAddAsync<IReliableDictionary<int, uint>>(statisticsDictionaryName);
            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                await requestsDictionary.AddAsync(tx, request.ID, request);
                await statisticsDictionary.AddOrUpdateAsync(tx, (int)RequestState.Created, 1, (key, oldvalue) => oldvalue + 1);
                await tx.CommitAsync();
            }

            return new OkResult();
        }


        [HttpPut("change")]
        public async Task<IActionResult> ChangeState([FromBody] ETCrequest request)
        {
            // change in requests dictionary
            // IReliableDictionary<uint, ETCrequest> requestsDictionary = await this.stateManager.GetOrAddAsync<IReliableDictionary<uint, ETCrequest>>(requestsDictionaryName);
            IReliableDictionary<uint, ETCrequest> inProgresDictionary = await this.stateManager.GetOrAddAsync<IReliableDictionary<uint, ETCrequest>>(inProgressDictionaryName);
            IReliableDictionary<int, uint> statisticsDictionary = await this.stateManager.GetOrAddAsync<IReliableDictionary<int, uint>>(statisticsDictionaryName);
            using (ITransaction tx = this.stateManager.CreateTransaction())
            {

                // if ((await requestsDictionary.TryRemoveAsync(tx, request.ID)).HasValue)
                try
                {
                    if ((await inProgresDictionary.TryRemoveAsync(tx, request.ID)).HasValue)
                    {
                        await statisticsDictionary.AddOrUpdateAsync(tx, (int)request.state, 1, (key, oldvalue) => oldvalue + 1);
                        await tx.CommitAsync();
                    }
                    else
                    {
                        // TODO add exception handling
                    }
                }
                catch (Exception ex)
                {
                    // TODO add exception handling
                }
            }

            return new OkResult();
        }


        [HttpGet("get")]
        public async Task<IActionResult> getRequests()
        {
            CancellationToken ct = new CancellationToken();

            IReliableDictionary<uint, ETCrequest> requestsDictionary = await this.stateManager.GetOrAddAsync<IReliableDictionary<uint, ETCrequest>>(requestsDictionaryName);
            IReliableDictionary<uint, ETCrequest> inProgresDictionary = await this.stateManager.GetOrAddAsync<IReliableDictionary<uint, ETCrequest>>(inProgressDictionaryName);

            List<KeyValuePair<uint, ETCrequest>> ActiveRequestsList = new List<KeyValuePair<uint, ETCrequest>>();

            // move from inProgresDictionary to requestsDictionary if timeout has passed
            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                Microsoft.ServiceFabric.Data.IAsyncEnumerator<KeyValuePair<uint, ETCrequest>> enumerator = (await inProgresDictionary.CreateEnumerableAsync(tx)).GetAsyncEnumerator();
                while (await enumerator.MoveNextAsync(ct))
                {
                    ETCrequest request = new ETCrequest();
                    request = enumerator.Current.Value;
                    uint key = enumerator.Current.Key;

                    DateTime timeNow = DateTime.Now;
                    TimeSpan MaxRequestWorkingTime = TimeSpan.FromSeconds(Commons.Constants.Constants.MaxRequestWorkingTimeInSeconds);
                    DateTime MinRequestSubmitTime = timeNow - MaxRequestWorkingTime;
                    if (request.processingStartTime < MinRequestSubmitTime)
                    {
                        using (ITransaction txMove = this.stateManager.CreateTransaction())
                        {
                            try
                            {
                                if ((await inProgresDictionary.TryRemoveAsync(txMove, key)).HasValue)
                                {
                                    request.state = Commons.Enums.RequestState.Created;
                                    await requestsDictionary.AddAsync(txMove, key, request);
                                    await txMove.CommitAsync();
                                }
                            }
                            catch (Exception ex)
                            {
                                // TODO add exception handling
                            }
                        }
                    }
                }
            }



            // get a number of created requests from requestsDictionary
            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                Microsoft.ServiceFabric.Data.IAsyncEnumerator<KeyValuePair<uint, ETCrequest>> enumerator = (await requestsDictionary.CreateEnumerableAsync(tx)).GetAsyncEnumerator();

                int nRequersts = 0;

                while (await enumerator.MoveNextAsync(ct) && nRequersts < Commons.Constants.Constants.NumberOfRequestsInSingleTurn)
                {
                    ETCrequest request = new ETCrequest();
                    request = enumerator.Current.Value;
                    uint key = enumerator.Current.Key;

                    if (request.state == Commons.Enums.RequestState.Created)
                    {
                        using (ITransaction txMove = this.stateManager.CreateTransaction())
                        {
                            try
                            {
                                if ((await requestsDictionary.TryRemoveAsync(txMove, key)).HasValue)
                                {
                                    request.state = Commons.Enums.RequestState.InProgress;
                                    request.processingStartTime = DateTime.Now;
                                    await inProgresDictionary.AddAsync(txMove, key, request);
                                    await txMove.CommitAsync();
                                    //
                                    KeyValuePair<uint, ETCrequest> kv = new KeyValuePair<uint, ETCrequest>(enumerator.Current.Key, request);
                                    ActiveRequestsList.Add(kv);
                                    nRequersts++;
                                }
                            }
                            catch (Exception ex)
                            {
                                // TODO add exception handling
                            }
                        }
                    }
                }
            }

            return this.Json(ActiveRequestsList);
        }


        [HttpGet("statistics")]
        public async Task<IActionResult> getStatistics()
        {
            CancellationToken ct = new CancellationToken();

            List<KeyValuePair<int, uint>> statisticsList = new List<KeyValuePair<int, uint>>();
            IReliableDictionary<int, uint> statisticsDictionary = await this.stateManager.GetOrAddAsync<IReliableDictionary<int, uint>>(statisticsDictionaryName);

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                Microsoft.ServiceFabric.Data.IAsyncEnumerator<KeyValuePair<int, uint>> enumerator = (await statisticsDictionary.CreateEnumerableAsync(tx)).GetAsyncEnumerator();
                while (await enumerator.MoveNextAsync(ct))
                {
                    KeyValuePair<int, uint> kv = new KeyValuePair<int, uint>(enumerator.Current.Key, enumerator.Current.Value);
                    statisticsList.Add(kv);
                }
            }
            return this.Json(statisticsList);
        }

    }
}
