using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace WebHookApi
{
    public static class DelayHook
    {
        [FunctionName("DelayHook")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequestMessage req
            , [OrchestrationClient] DurableOrchestrationClient client
            , TraceWriter log)
        {
            if (!int.TryParse(req.RequestUri.ParseQueryString()["Value"], out var value))
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            var instanceId = await client.StartNewAsync(nameof(WebHookOrchestrator), value);
            return req.CreateResponse(HttpStatusCode.OK, instanceId);
        }

        [FunctionName(nameof(WebHookOrchestrator))]
        public static async Task WebHookOrchestrator([OrchestrationTrigger]DurableOrchestrationContext context)
        {
            int requestNo = context.GetInput<int>();
            var result = await context.CallActivityAsync<int>(nameof(ProcessActivity), requestNo);

            await context.CallActivityWithRetryAsync(nameof(ProcessActivity),
                new RetryOptions(TimeSpan.FromMinutes(5), 5), requestNo);
        }

        [FunctionName(nameof(ProcessActivity))]
        public static async Task<int> ProcessActivity([ActivityTrigger] int requestNo)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            return requestNo * 2;
        }

        public static async Task HookActivity([ActivityTrigger] DurableActivityContext context)
        {
            var requestNo = context.GetInput<int>();
            ServicePointManager.DefaultConnectionLimit = 100;
            var hookUrl = ConfigurationManager.AppSettings["WebHookUrl"];
            using (var client = new HttpClient())
            {
                var result =
                    await client.PostAsJsonAsync(hookUrl, new ResultModel()
                    {
                        Succeed = requestNo % 2 == 0,
                        Value = requestNo,
                        InstanceId = context.InstanceId
                    });
                if (!result.IsSuccessStatusCode)
                {
                    throw new Exception("WebHook fault");
                }
            }
        }
    }

    public class ResultModel
    {
        public string InstanceId { get; set; }

        public bool Succeed { get; set; }

        public int Value { get; set; }
    }
}
