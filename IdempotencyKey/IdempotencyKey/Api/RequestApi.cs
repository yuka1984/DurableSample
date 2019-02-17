using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IdempotencyKey.Orchestrator;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace IdempotencyKey.Api
{
    public static class RequestApi
    {
        public const string Name = "Api_Request";
        public const int RetryCount = 3;
        public static readonly TimeSpan ExpirationTime = TimeSpan.FromDays(30);

        [FunctionName(Name)]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "regist")]HttpRequestMessage requestMessage,
            [Table("IdempotencyKey")]CloudTable idempotencyKeytable,
            [OrchestrationClient]DurableOrchestrationClientBase durable,
            Binder binder)
        {
            var requestJson = await requestMessage.Content.ReadAsStringAsync();
            var request = default(RequestModel);


            for (int i = 0; i < RetryCount; i++)
            {
                try
                {
                    request = JsonConvert.DeserializeObject<RequestModel>(requestJson);
                }
                catch (Exception e)
                {
                    return requestMessage.CreateErrorResponse(HttpStatusCode.BadRequest, "Request json is bad.");
                }

                if (!request.GetValidate())
                {
                    return requestMessage.CreateErrorResponse(HttpStatusCode.BadRequest, "Request is bad.");
                }                

                var idempotencyKeyEntity = await idempotencyKeytable.Get<IdempotencyKeyTableEntity>(request.UserId, request.IdempotencyKey);
                if (idempotencyKeyEntity != null && !string.IsNullOrEmpty(idempotencyKeyEntity.InstanceId))
                {
                    var elapsedTime = DateTimeOffset.Now - idempotencyKeyEntity.Timestamp;
                    if (elapsedTime <= ExpirationTime)
                    {
                        var taskDurable = durable;
                        if (idempotencyKeyEntity.TaskHubName != durable.TaskHubName)
                        {
                            taskDurable = await binder.BindAsync<DurableOrchestrationClientBase>(new OrchestrationClientAttribute { TaskHub = idempotencyKeyEntity.TaskHubName });
                        }
                        var status = await taskDurable.GetStatusAsync(idempotencyKeyEntity.InstanceId);
                        if (status != null)
                        {
                            return taskDurable.CreateCheckStatusResponse(requestMessage, idempotencyKeyEntity.InstanceId);
                        }
                    }
                    else
                    {
                        await idempotencyKeytable.Delete(request.UserId, request.IdempotencyKey);
                    }                    
                }

                idempotencyKeyEntity = new IdempotencyKeyTableEntity
                {
                    PartitionKey = request.UserId,
                    RowKey = request.IdempotencyKey,
                    TaskHubName = durable.TaskHubName,
                    InstanceId = string.Empty,
                };

                try
                {
                    await idempotencyKeytable.Insert(idempotencyKeyEntity);
                }
                catch (StorageException e)
                {
                    if (e.RequestInformation.HttpStatusCode == 409)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        continue;
                    }

                    throw;
                }

                idempotencyKeyEntity.InstanceId = await durable.StartNewAsync(
                    SampleOrchestrator.Name,
                    new SampleOrchestrator.SampleOrchestratorRequest
                    {
                        UserId = request.UserId,
                        ProductId = request.ProductId,
                        Amount = request.Amount
                    });

                await idempotencyKeytable.InsertOrMerge(idempotencyKeyEntity);

                return durable.CreateCheckStatusResponse(requestMessage, idempotencyKeyEntity.InstanceId);
            }

            throw new Exception("なんかうまくいかなかった(´・ω・｀)");
        }

        public class RequestModel
        {
            public string UserId { get; set; }

            public string IdempotencyKey { get; set; }

            public string ProductId { get; set; }

            public long Amount { get; set; }

            public bool GetValidate()
            {
                return
                    !string.IsNullOrEmpty(UserId)
                    &&
                    !string.IsNullOrEmpty(IdempotencyKey)
                    &&
                    !string.IsNullOrEmpty(ProductId)
                    &&
                    Amount > 0;
            }
        }

        public class IdempotencyKeyTableEntity : TableEntity
        {
            public string TaskHubName { get; set; }

            public string InstanceId { get; set; }
        }
    }
}
