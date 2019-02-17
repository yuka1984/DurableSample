using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IdempotencyKey.Orchestrator;
using Microsoft.Azure.WebJobs;

namespace IdempotencyKey.Activity
{
    public static class EmptyActivity
    {
        public const string Name = "Activity_Empty";

        [FunctionName(Name)]
        public static async Task<string> Run([ActivityTrigger]SampleOrchestrator.SampleOrchestratorRequest input)
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            return $"EmptyActivityReslt-{input.ProductId}を{input.Amount}コ買いました";
        }
    }
}
