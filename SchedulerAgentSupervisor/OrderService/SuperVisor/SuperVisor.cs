using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.Protocol;
using OrderService.Models;
using SampleExtension;

namespace OrderService.SuperVisor
{
    public static class SuperVisor
    {
        [Disable("FailReportWatcherDisable")]
        [FunctionName("FailReportWatcher")]
        public static async Task FailReportWatcher(
            [TimerTrigger("%FailReportWatcherCRON%")]TimerInfo timerInfo,
            [Table("FailReport")]CloudTable table,
            [Slack(WebHookUrl = "SlackWebHookUrl")]IAsyncCollector<SlackMessage> slackCollector)
        {
            var query =
                new TableQuery<FailReportEntity>().Where(
                    TableQuery.GenerateFilterCondition(nameof(FailReportEntity.PartitionKey), QueryComparisons.Equal, "CallRemoteService"));

            var failReport = table.ExecuteQuery(query).ToList();

            if (failReport.Any())
            {
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("CallRemoteService Fail Alert");
                messageBuilder.AppendLine();
                messageBuilder.AppendLine($"FailReportCount : {failReport.Count}");
                messageBuilder.AppendLine();
                messageBuilder.AppendLine("All fail instance retrt");
                messageBuilder.AppendLine(ConfigurationManager.AppSettings["CallRemoteServiceFailRetryUrl"]);

                var message = new SlackMessage
                {
                    Username = "SuperVisor",
                    Text = messageBuilder.ToString()
                };
                await slackCollector.AddAsync(message);
            }
        }

        [FunctionName("FailReportRetry")]
        public static async Task<HttpResponseMessage> FailReportRetry(
            [HttpTrigger(AuthorizationLevel.Function, "get")]HttpRequestMessage request,
            [Table("FailReport")]CloudTable table,
            [Slack(WebHookUrl = "SlackWebHookUrl")]IAsyncCollector<SlackMessage> slackCollector,
            [OrchestrationClient]DurableOrchestrationClientBase durableCtx
            )
        {
            var query =
                new TableQuery<FailReportEntity>().Where(
                    TableQuery.GenerateFilterCondition(nameof(FailReportEntity.PartitionKey), QueryComparisons.Equal, "CallRemoteService"));

            var failReports = table.ExecuteQuery(query).ToList();

            foreach (var report in failReports)
            {
                await durableCtx.RaiseEventAsync(report.RowKey, "RetryCallRemoteServiceInstract", true);
                var delete = TableOperation.Delete(report);
                await table.ExecuteAsync(delete);
            }

            var message = new SlackMessage
            {
                Username = "SuperVisor",
                Text = "All fal report executed retry."
            };

            await slackCollector.AddAsync(message);

            return request.CreateResponse(HttpStatusCode.OK);
        }
    }
}
