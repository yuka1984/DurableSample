using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IdempotencyKey.Activity;
using Microsoft.Azure.WebJobs;

namespace IdempotencyKey.Orchestrator
{
    public static class SampleOrchestrator
    {
        public const string Name = "Orchestrator_Sample";

        [FunctionName(Name)]
        public static async Task<string> Run([OrchestrationTrigger]DurableOrchestrationContextBase durable)
        {
            var input = durable.GetInput<SampleOrchestratorRequest>();
            var result = await durable.CallActivityAsync<string>(EmptyActivity.Name, input);
            return result;
        }

        public class SampleOrchestratorRequest
        {
            public string UserId { get; set; }

            public string ProductId { get; set; }

            public long Amount { get; set; }
        }
    }
}
