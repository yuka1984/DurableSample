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
        [FunctionName("WebhookOrchestrator")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req
            , TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            string name = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "name", true) == 0)
                .Value;

            // Get request body
            dynamic data = await req.Content.ReadAsAsync<object>();

            // Set name to query string or body data
            name = name ?? data?.name;

            return name == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                : req.CreateResponse(HttpStatusCode.OK, "Hello " + name);
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
                return result;
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
