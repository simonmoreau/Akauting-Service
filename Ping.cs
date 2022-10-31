using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Azure.Cosmos.Table;

namespace Akaunting
{
    public static class Ping
    {
        [FunctionName("Ping")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [Table("webhooks")] CloudTable webhookCloudTable,
            [Table("webhooks")] IAsyncCollector<WebhookBody> webhookTable,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                WebhookBody body = JsonSerializer.Deserialize<WebhookBody>(requestBody);

                body.PartitionKey = "webhook";
                body.RowKey = body.id;
                body.json = requestBody;

                TableQuery<WebhookBody> rangeQuery = new TableQuery<WebhookBody>().Where(
        TableQuery.CombineFilters(
            TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, body.PartitionKey),
            TableOperators.And,
            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, body.RowKey)));

                TableQuerySegment<WebhookBody> querySegment = await webhookCloudTable.ExecuteQuerySegmentedAsync(rangeQuery, null);

                if (querySegment.Results.Count == 0)
                {
                    await webhookTable.AddAsync(body);
                }

                string name = "Ok";

                string responseMessage = string.IsNullOrEmpty(name)
                    ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                    : $"Hello, {name}. This HTTP triggered function executed successfully.";

                return new OkObjectResult(responseMessage);
            }
            catch (System.Exception ex)
            {
                string responseMessage = $"Error: {ex.Message}";
                log.LogError(responseMessage);
                return new OkObjectResult(responseMessage);
            }

        }
    }
}
