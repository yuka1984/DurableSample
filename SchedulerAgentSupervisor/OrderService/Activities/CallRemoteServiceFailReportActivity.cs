using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using OrderService.Models;

namespace OrderService.Activities
{
    public static class CallRemoteServiceFailReportActivity
    {
        [FunctionName("CallRemoteServiceFailReport")]
        public static async Task CallRemoteServiceFailReport(
            [ActivityTrigger]DurableActivityContext context,
            [Table("FailReport")]CloudTable table)
        {
            var order = context.GetInput<OrderModel>();

            var entity = new FailReportEntity
            {
                PartitionKey = "CallRemoteService",
                RowKey = context.InstanceId,
                Json = JsonConvert.SerializeObject(order)
            };

            var insert = TableOperation.Insert(entity);
            await table.ExecuteAsync(insert);
        }
    }
}
