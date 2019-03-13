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
using Tweetinvi;

namespace SaintGimp
{
    public static class EnergyMonitorWatchdog
    {
        [FunctionName("EnergyMonitorWatchdog")]
        public static async Task RunAsync([TimerTrigger("0 */30 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Loading data from ElasticSearch...");
            
            try
            {
                var elasticSearchCredentials = GetEnvironmentVariable("ElasticSearchCredentials");
            
                var httpClient = new HttpClient();
                var byteArray = Encoding.ASCII.GetBytes(elasticSearchCredentials);
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var uri = $"http://elasticsearch.saintgimp.org/logstash-energy/_search";
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
                
                if (difference > TimeSpan.FromMinutes(30))
                {
                    throw new ApplicationException("No new data");
                }
                
                log.LogInformation("Everything's fine here, we're all fine, how are you?");
            }
            catch (Exception)
            {
                SendNotification(log);
            }
        }

        static void SendNotification(ILogger log)
        {
            var message = "Hey, I think the energy monitor is offline!";
            log.LogInformation(message);

            // These are retrieved from https://developer.twitter.com/en/apps/8049320
            var consumerKey = GetEnvironmentVariable("TwitterConsumerKey");
            var consumerSecret = GetEnvironmentVariable("TwitterConsumerSecret");
            var accessToken = GetEnvironmentVariable("TwitterAccessTokenKey");
            var accessTokenSecret = GetEnvironmentVariable("TwitterAccessTokenSecret");

            Auth.SetUserCredentials(consumerKey, consumerSecret, accessToken, accessTokenSecret);
            var user = User.GetUserFromScreenName("saintgimp");
            Message.PublishMessage(message, user.Id);
        }

        public static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        } 
    }
}
