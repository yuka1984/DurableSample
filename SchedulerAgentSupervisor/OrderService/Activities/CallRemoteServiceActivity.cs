using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using OrderService.Models;

namespace OrderService
{
    public static class CallRemoteServiceActivity
    {
        private static HttpClient httpClient = new HttpClient();
        private const string ApiUrl = "";

        [FunctionName("CallRemoteService")]
        public static async Task CallRemoteService([ActivityTrigger]OrderModel order)
        {
            var json = JsonConvert.SerializeObject(order);
            var result =  await httpClient.PostAsync(ConfigurationManager.AppSettings["ApiUrl"], new StringContent(json));
            if (!result.IsSuccessStatusCode)
            {
                throw new Exception($"RemoteService returned Badstatus {result.StatusCode}");
            }
        }
    }
}
