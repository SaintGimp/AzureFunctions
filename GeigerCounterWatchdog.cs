using System;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SaintGimp
{
    public class GeigerCounterWatchdog : FunctionBase
    {
        [FunctionName("GeigerCounterWatchdog")]
        public static async Task RunAsync([TimerTrigger("0 */30 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Loading data from ElasticSearch...");
            
            try
            {
                var elasticSearchCredentials = Environment.GetEnvironmentVariable("ElasticSearchCredentials");
            
                var httpClient = new HttpClient();
                var byteArray = Encoding.ASCII.GetBytes(elasticSearchCredentials);
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var uri = $"https://elasticsearch.saintgimp.org/logstash-geiger/_search";
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
                dynamic data = JObject.Parse(responseContent);

                DateTime timestamp = data.hits.hits[0]._source["@timestamp"];
                var difference = DateTime.UtcNow - timestamp;
                log.LogInformation($"Timestamp is {timestamp}, difference is {difference}");

                int cpm = data.hits.hits[0]._source["cpm"];                
                log.LogInformation($"cpm is {cpm}");

                if (difference > TimeSpan.FromMinutes(30))
                {
                    SendEmailNotification("Hey, I think the geiger counter is offline!", log);
                }
                else if (cpm > 256)
                {
                    SendEmailNotification("Hey, I think the geiger counter is logging bad data!", log);
                }
                else
                {
                    log.LogInformation("Everything's fine here, we're all fine, how are you?");
                }
            }
            catch (Exception)
            {
                SendEmailNotification("I couldn't check on the geiger counter!", log);
            }
        }
    }
}
