using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace PipesAndFilters
{
    public class PipesTable : TableEntity
    {
        public int Value { get; set; }
    }


    public static class Func1
    {
        [FunctionName(nameof(DataSource1))]
        public static async Task<HttpResponseMessage> DataSource1(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "DataSource1/{value}")] HttpRequestMessage req
            , [OrchestrationClient] DurableOrchestrationClient client
            , string value
            , TraceWriter log)
        {
            var instanceId = await client.StartNewAsync(nameof(DataSource1Orchestrator), value);
            return client.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(DataSource2))]
        public static async Task<HttpResponseMessage> DataSource2(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "DataSource2/{value}")] HttpRequestMessage req
            , [OrchestrationClient] DurableOrchestrationClient client
            , string value
            , TraceWriter log)
        {
            var instanceId = await client.StartNewAsync(nameof(DataSource2Orchestrator), value);
            return client.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(DataSource1Orchestrator))]
        public static async Task<List<string>> DataSource1Orchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context
            )
        {
            var input = context.GetInput<string>();
            var a = await context.CallActivityAsync<List<string>>(nameof(TaskAActivity), new List<string>() {input});
            var b = await context.CallActivityAsync<List<string>>(nameof(TaskBActivity), a);
            var c = await context.CallActivityAsync<List<string>>(nameof(TaskCActivity), b);
            var d = await context.CallActivityAsync<List<string>>(nameof(TaskDActivity), c);
            return d;
        }

        [FunctionName(nameof(DataSource2Orchestrator))]
        public static async Task<List<string>> DataSource2Orchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context
            )
        {
            var input = context.GetInput<string>();
            var a = await context.CallActivityAsync<List<string>>(nameof(TaskAActivity), new List<string>() { input });
            var b = await context.CallActivityAsync<List<string>>(nameof(TaskBActivity), a);
            var e = await context.CallActivityAsync<List<string>>(nameof(TaskEActivity), b);
            var f = await context.CallActivityAsync<List<string>>(nameof(TaskFActivity), e);
            return f;
        }

        [FunctionName(nameof(TaskAActivity))]
        public static async Task<List<string>> TaskAActivity(
            [ActivityTrigger] List<string> results,
            TraceWriter logger
        )
        {
            logger.Info("Task A start");
            results.Add("TaskA Complete");
            return results;
        }

        [FunctionName(nameof(TaskBActivity))]
        public static async Task<List<string>> TaskBActivity(
            [ActivityTrigger] List<string> results,
            TraceWriter logger
        )
        {
            logger.Info("Task B start");
            results.Add("TaskB Complete");
            return results;
        }

        [FunctionName(nameof(TaskCActivity))]
        public static async Task<List<string>> TaskCActivity(
            [ActivityTrigger] List<string> results,
            TraceWriter logger
        )
        {
            logger.Info("Task C start");
            results.Add("TaskC Complete");
            return results;
        }

        [FunctionName(nameof(TaskDActivity))]
        public static async Task<List<string>> TaskDActivity(
            [ActivityTrigger] List<string> results,
            TraceWriter logger
        )
        {
            logger.Info("Task D start");
            results.Add("TaskD Complete");
            return results;
        }

        [FunctionName(nameof(TaskEActivity))]
        public static async Task<List<string>> TaskEActivity(
            [ActivityTrigger] List<string> results,
            TraceWriter logger
        )
        {
            logger.Info("Task E start");
            results.Add("TaskE Complete");
            return results;
        }

        [FunctionName(nameof(TaskFActivity))]
        public static async Task<List<string>> TaskFActivity(
            [ActivityTrigger] List<string> results,
            TraceWriter logger
        )
        {
            logger.Info("Task F start");
            results.Add("TaskF Complete");
            return results;
        }

    }
}
