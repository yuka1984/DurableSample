using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;

namespace WebHookOrchestration
{
    public static class WebhookOrchestrator
    {
        [FunctionName("request")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "api/request/{value}")]HttpRequestMessage req
            , [OrchestrationClient]DurableOrchestrationClient client
            , int value
            , TraceWriter log)
        {
            var instanceId = await client.StartNewAsync(nameof(WebHookOrchestratir), value);
            return client.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(WebHookOrchestratir))]
        public static async Task<ResultModel> WebHookOrchestratir(
            [OrchestrationTrigger]DurableOrchestrationContext context
            )
        {
            var requestVaue = context.GetInput<int>();
            var hookKey = await context.CallActivityAsync<string>(nameof(HookApiRequestActivity), requestVaue);
            await context.CallActivityAsync(nameof(SaveRequestKeyActivity), hookKey);
            var result = await context.WaitForExternalEvent<ResultModel>(HookWaitEventName);
            return result;
        }

        [FunctionName(nameof(HookApiRequestActivity))]
        public static async Task<string> HookApiRequestActivity(
            [ActivityTrigger] int requestValue
            )
        {
            var url = $"{ConfigurationManager.AppSettings["HookUrl"]}?Value={requestValue}";
            ServicePointManager.DefaultConnectionLimit = 100;
            using (var client = new HttpClient())
            {
                var result = await client.GetStringAsync(url);
                return result.Replace("\"", "");
            }
        }

        [FunctionName(nameof(SaveRequestKeyActivity))]
        public static async Task SaveRequestKeyActivity(
            [ActivityTrigger] DurableActivityContext context,
            [Table("KeySaveTable")] CloudTable table
            )
        {
            var key = context.GetInput<string>();
            var insertOperation = TableOperation.Insert(new TableEntity(key, context.InstanceId));
            await table.ExecuteAsync(insertOperation);
        }

        public const string HookWaitEventName = "HookWait";
    }

    public class ResultModel
    {
        public bool Succeed { get; set; }

        public int Value { get; set; }
    }
}
