using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace WebHookOrchestration
{
    public static class HookRecieve
    {
        public class WebHookResult
        {
            public string InstanceId { get; set; }

            public bool Succeed { get; set; }

            public int Value { get; set; }
        }

        [FunctionName("HookRecieve")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequestMessage req
            , [Table("KeySaveTable")] CloudTable table
            , [OrchestrationClient] DurableOrchestrationClient client
            , TraceWriter log)
        {
            var json = await req.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<WebHookResult>(json);
            var query = new TableQuery<TableEntity>()
                .Where(TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey),
                QueryComparisons.Equal, result.InstanceId));

            var entity =  table.ExecuteQuery(query).FirstOrDefault();

            if (entity == null)
            {
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }

            await client.RaiseEventAsync(entity.RowKey, WebhookOrchestrator.HookWaitEventName,
                new ResultModel {Succeed = result.Succeed, Value = result.Value});

            return req.CreateResponse(HttpStatusCode.OK);

        }
    }
}
