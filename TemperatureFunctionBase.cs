using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace SaintGimp
{
    public class TemperatureFunctionBase : FunctionBase
    {
        public static async Task<dynamic> GetTemperatureData(ILogger log)
        {
            log.LogInformation($"Loading data from ElasticSearch...");

            var elasticSearchCredentials = Environment.GetEnvironmentVariable("ElasticSearchCredentials");

            var httpClient = new HttpClient();
            var byteArray = Encoding.ASCII.GetBytes(elasticSearchCredentials);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            var uri = $"https://elasticsearch.saintgimp.org/logstash-temperatures/_search";
            var query = @"{
                ""query"": {
                    ""match_all"": {}
                },
                ""size"": ""1"",
                ""sort"": [
                    {
                    ""@timestamp"": {
                        ""order"": ""desc""
                    }
                    }
                ]
                }";

            var response = await httpClient.PostAsync(uri, new StringContent(query, Encoding.ASCII, "application/json"));
            var responseContent = await response.Content.ReadAsStringAsync();
            log.LogInformation(responseContent);

            dynamic data = JObject.Parse(responseContent);
            var mostRecentRecord = data.hits.hits[0]._source;
            DateTime timestamp = mostRecentRecord["@timestamp"];
            var timeSinceLastUpdate = DateTime.UtcNow - timestamp;
            log.LogInformation($"Most recent timestamp is {timestamp}, {timeSinceLastUpdate} old");

            return mostRecentRecord;
        }
    }
}