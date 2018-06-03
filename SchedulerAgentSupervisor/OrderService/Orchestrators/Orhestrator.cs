using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using OrderService.Models;

namespace OrderService
{
    public static class Orhestrator
    {
        private static readonly RetryOptions RetryOptions = new RetryOptions(TimeSpan.FromSeconds(5), 10);

        [FunctionName("OrderOrchestrator")]
        public static async Task Order([OrchestrationTrigger]DurableOrchestrationContextBase context)
        {
            var order = context.GetInput<OrderModel>();

            await context.CallActivityAsync("InsertToRepository" , order);

            while (true)
            {
                try
                {
                    await context.CallActivityWithRetryAsync("CallRemoteService", RetryOptions, order);
                }
                catch
                {
                    await context.CallActivityAsync("CallRemoteServiceFailReport", order);

                    var limitTime = context.CurrentUtcDateTime.AddDays(5);
                    var timer = context.CreateTimer<bool>(limitTime, false, CancellationToken.None);
                    var instract = context.WaitForExternalEvent<bool>("RetryCallRemoteServiceInstract");

                    var retry = await Task.WhenAny(timer, instract);

                    if (retry.Result)
                    {
                        continue;
                    }
                    else
                    {
                        await context.CallActivityAsync("RollBackRepository", order);
                        // await context.CallActivityAsync("SendFailedMail", order);
                    }
                }
                break;
            }
        }
    }
}