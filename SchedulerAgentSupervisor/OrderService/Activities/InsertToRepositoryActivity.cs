using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Table;
using OrderService.Models;

namespace OrderService
{
    public static class InsertToRepositoryActivity
    {
        [FunctionName("InsertToRepository")]
        public static async Task InsertToRepository(
            [ActivityTrigger]OrderModel order
            ,[Table("Order")] CloudTable table)
        {
            var entity = new OrderEntity
            {
                PartitionKey = order.OrderId,
                RowKey = "",
                OrderCount = order.OrderCount
            };

            var insert = TableOperation.Insert(entity);
            await table.ExecuteAsync(insert);
        }

        [FunctionName("RollBackRepository")]
        public static async Task RollBackRepository(
            [ActivityTrigger]OrderModel order
            , [Table("Order")] CloudTable table)
        {
            var entity = new OrderEntity
            {
                PartitionKey = order.OrderId,
                RowKey = "",
                OrderCount = order.OrderCount
            };

            var delete = TableOperation.Delete(entity);
            await table.ExecuteAsync(delete);
        }
    }
}
