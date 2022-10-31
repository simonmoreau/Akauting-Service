using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Akaunting
{
    public class AkauntingReccord
    {
                private readonly AkauntingClient _akauntingClient;

        public AkauntingReccord(AkauntingClient akauntingClient)
        {
            this._akauntingClient = akauntingClient;
        }

        [FunctionName("AkauntingReccord")]
        public async Task RunAsync([QueueTrigger("akaunting-queue", Connection = "")]Status myQueueItem, 
        ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem.ToString()}");
            CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
            Status status = await _akauntingClient.Ping(_cancellationTokenSource.Token);
        }
    }
}
