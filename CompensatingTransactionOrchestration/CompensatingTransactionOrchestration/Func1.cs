using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CompensatingTransactionOrchestration
{
    public static class Functions
    {        
        [FunctionName("HttpStartSingle")]
        public static async Task<HttpResponseMessage> RunSingle(
            [HttpTrigger(AuthorizationLevel.Function, methods: "post",
                Route = "orchestrators/HttpStartSingle/{requestId}/{point}")] HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClient starter,
            string requestId,
            int point,
            TraceWriter log)
        {
            var instanceId = await starter.StartNewAsync("CompensatingTransactOrchestrator",
                new PointRequest() {RequestId = requestId, Point = point});

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("CompensatingTransactOrchestrator")]
        public static async Task<string> CompensatingTransactOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            [OrchestrationClient] DurableOrchestrationClient client
        )
        {
            var pointRequest = context.GetInput<PointRequest>();
            var compensatingStack = new Stack<ValueTuple<string, RepositoryResult>>();

            try
            {


                var result1 = await context.CallActivityAsync<RepositoryResult>(nameof(Repository1Activity), pointRequest);
                if (result1.Succeed)
                {
                    compensatingStack.Push((nameof(Repository1CompensatingActivity), result1));
                }
                else
                {
                    return "repository1 Failure";
                }

                var result2 = await context.CallActivityAsync<RepositoryResult>(nameof(Repository2Activity), pointRequest);
                if (result2.Succeed)
                {
                    compensatingStack.Push((nameof(Repository2CompensatingActivity), result2));
                }
                else
                {
                    return "repository2 Failure";
                }

                var result3 = await context.CallActivityAsync<RepositoryResult>(nameof(Repository3Activity), pointRequest);
                if (result3.Succeed)
                {
                    compensatingStack.Push((nameof(Repository3CompensatingActivity), result3));
                }
                else
                {
                    return "repository3 Failure";
                }

                compensatingStack.Clear();
            }
            finally
            {
                while (compensatingStack.Any())
                {
                    var item = compensatingStack.Pop();
                    await context.CallActivityWithRetryAsync(item.Item1, GetRetryOprion(), item.Item2);
                }
            }

            return "Success";
        }

        private static RetryOptions GetRetryOprion()
        {

            return new RetryOptions(TimeSpan.FromSeconds(10), 10);

        }

        [FunctionName("Repository1Activity")]
        public static async Task<RepositoryResult> Repository1Activity(
            [ActivityTrigger] PointRequest context
            , [Table("table1")] CloudTable table
            )
        {
            var query =
                new TableQuery<PointTable>().Where(TableQuery.GenerateFilterCondition(nameof(PointTable.PartitionKey),
                    QueryComparisons.Equal, context.RequestId));

            if (!table.ExecuteQuery(query).Any())
            {
                var processId = Guid.NewGuid().ToString();
                var insertOperation =
                    TableOperation.Insert(new PointTable()
                    {
                        PartitionKey = context.RequestId,
                        RowKey = processId,
                        Point = context.Point
                    });

                var result = await table.ExecuteAsync(insertOperation);
                return new RepositoryResult()
                {
                    ProcessId = processId,
                    Request = context,
                    Succeed = true,
                };
            }

            return new RepositoryResult()
            {
                Request = context,
                Succeed = false
            };
        }

        [FunctionName("Repository1CompensatingActivity")]
        public static async Task Repository1CompensatingActivity(
            [ActivityTrigger] RepositoryResult repositoryResult
            , [Table("table1")] CloudTable table
            )
        {
            var query =
                    new TableQuery<PointTable>()
                        .Where(TableQuery.GenerateFilterCondition(nameof(PointTable.PartitionKey),
                            QueryComparisons.Equal,
                            repositoryResult.Request.RequestId))
                        .Where(TableQuery.GenerateFilterCondition(nameof(PointTable.RowKey), QueryComparisons.Equal,
                            repositoryResult.ProcessId))
                ;

            var targets = table.ExecuteQuery(query).ToArray();

            if (targets.Any())
            {
                foreach (var target in targets)
                {
                    var deleteOperation = TableOperation.Delete(target);
                    await table.ExecuteAsync(deleteOperation);
                }
            }
        }

        [FunctionName("Repository2Activity")]
        public static async Task<RepositoryResult> Repository2Activity(
            [ActivityTrigger] PointRequest context
            ,[Table("table2")] CloudTable table
            )
        {
            var query =
                new TableQuery<PointTable>().Where(TableQuery.GenerateFilterCondition(nameof(PointTable.PartitionKey),
                    QueryComparisons.Equal, context.RequestId));

            if (!table.ExecuteQuery(query).Any())
            {
                var processId = Guid.NewGuid().ToString();
                var insertOperation =
                    TableOperation.Insert(new PointTable()
                    {
                        PartitionKey = context.RequestId,
                        RowKey = processId,
                        Point = context.Point
                    });

                var result = await table.ExecuteAsync(insertOperation);
                return new RepositoryResult()
                {
                    ProcessId = processId,
                    Request = context,
                    Succeed = true,
                };
            }

            return new RepositoryResult()
            {
                Request = context,
                Succeed = false
            };
        }

        [FunctionName("Repository2CompensatingActivity")]
        public static async Task Repository2CompensatingActivity(
            [ActivityTrigger] RepositoryResult repositoryResult
            , [Table("table2")] CloudTable table
            )
        {
            var query =
                    new TableQuery<PointTable>()
                        .Where(TableQuery.GenerateFilterCondition(nameof(PointTable.PartitionKey),
                            QueryComparisons.Equal,
                            repositoryResult.Request.RequestId))
                        .Where(TableQuery.GenerateFilterCondition(nameof(PointTable.RowKey), QueryComparisons.Equal,
                            repositoryResult.ProcessId))
                ;

            var targets = table.ExecuteQuery(query).ToArray();

            if (targets.Any())
            {
                foreach (var target in targets)
                {
                    var deleteOperation = TableOperation.Delete(target);
                    await table.ExecuteAsync(deleteOperation);
                }
            }
        }

        [FunctionName("Repository3Activity")]
        public static async Task<RepositoryResult> Repository3Activity(
    [ActivityTrigger] PointRequest context
    , [Table("table3")] CloudTable table
    )
        {
            var query =
                new TableQuery<PointTable>().Where(TableQuery.GenerateFilterCondition(nameof(PointTable.PartitionKey),
                    QueryComparisons.Equal, context.RequestId));

            if (!table.ExecuteQuery(query).Any())
            {
                var processId = Guid.NewGuid().ToString();
                var insertOperation =
                    TableOperation.Insert(new PointTable()
                    {
                        PartitionKey = context.RequestId,
                        RowKey = processId,
                        Point = context.Point
                    });

                var result = await table.ExecuteAsync(insertOperation);
                return new RepositoryResult()
                {
                    ProcessId = processId,
                    Request = context,
                    Succeed = true,
                };
            }

            return new RepositoryResult()
            {
                Request = context,
                Succeed = false
            };
        }

        [FunctionName("Repository3CompensatingActivity")]
        public static async Task Repository3CompensatingActivity(
            [ActivityTrigger] RepositoryResult repositoryResult
            , [Table("table3")] CloudTable table
        )
        {
            var query =
                    new TableQuery<PointTable>()
                        .Where(TableQuery.GenerateFilterCondition(nameof(PointTable.PartitionKey),
                            QueryComparisons.Equal,
                            repositoryResult.Request.RequestId))
                        .Where(TableQuery.GenerateFilterCondition(nameof(PointTable.RowKey), QueryComparisons.Equal,
                            repositoryResult.ProcessId))
                ;

            var targets = table.ExecuteQuery(query).ToArray();

            if (targets.Any())
            {
                foreach (var target in targets)
                {
                    var deleteOperation = TableOperation.Delete(target);
                    await table.ExecuteAsync(deleteOperation);
                }
            }
        }



        public class PointRequest
        {
            public string RequestId { get; set; }

            public int Point { get; set; }
        }

        public class RepositoryResult
        {
            public bool Succeed { get; set; }

            public string ProcessId { get; set; }

            public PointRequest Request { get; set; }
        }

        public class PointTable : TableEntity
        {
            public int Point { get; set; }
        }

        [FunctionName("HttpStartWait")]
        public static async Task<HttpResponseMessage> RunWait(
            [HttpTrigger(AuthorizationLevel.Function, methods: "post",
                Route = "orchestrators/HttpStartWait/{requestId}/{point}")] HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClient starter,
            string requestId,
            int point,
            TraceWriter log)
        {
            var instanceId = await starter.StartNewAsync("CompensatingTransactOrchestrator",
                new PointRequest() { RequestId = requestId, Point = point });

            while (true)
            {
                var status = await starter.GetStatusAsync(instanceId);
                if (status?.RuntimeStatus > OrchestrationRuntimeStatus.Running)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(status))
                    };
                }
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }
}
