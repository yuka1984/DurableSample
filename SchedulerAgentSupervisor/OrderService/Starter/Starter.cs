using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using OrderService.Models;

namespace OrderService.Starter
{
    public static class Starter
    {
        [FunctionName("OrderRequest")]
        public static async Task<HttpResponseMessage> StarterApi(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "order")]HttpRequestMessage request,
        [OrchestrationClient]DurableOrchestrationClientBase client
            )
        {
            var json = await request.Content.ReadAsStringAsync();
            var order = JsonConvert.DeserializeObject<OrderModel>(json);

            var instanceId = await client.StartNewAsync("OrderOrchestrator", order);

            return await client.WaitForCompletionOrCreateCheckStatusResponseAsync(request, instanceId, TimeSpan.FromSeconds(10));
        }
    }
}
