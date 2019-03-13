using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace SaintGimp
{
    public static class GOESImport
    {
        [FunctionName("GOESImport")]
        public static async Task RunAsync([TimerTrigger("0 */6 * * * *")]TimerInfo myTimer, ILogger log)
        {
            var mostRecentTimestamp = await GetMostRecentElasticSearchTimestamp();
            var samples = await GetGoesSamples();
            var samplesToLoad = samples.Where(s => s.Timestamp > mostRecentTimestamp).OrderBy(s => s.Timestamp).ToList();

            log.LogInformation($"Most recent timestamp in GOES is {samples.Last().Timestamp}");
            log.LogInformation($"Most recent timestamp in ElasticSearch is {mostRecentTimestamp}");
            log.LogInformation($"Saving {samplesToLoad.Count()} new samples");

            foreach (var sample in samplesToLoad)
            {
                await SaveSample(sample);
            }
        }

        private static async Task SaveSample(Sample sample)
        {
            var elasticSearchCredentials = GetEnvironmentVariable("ElasticSearchCredentials");

            var httpClient = new HttpClient();
            var byteArray = Encoding.ASCII.GetBytes(elasticSearchCredentials);
            httpClient.DefaultRequestHeaders.Add("SaintGimp-Private-Key", elasticSearchCredentials.Split(':')[1]);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            var uri = "http://logstash.saintgimp.org/goes";
            var payload = JsonConvert.SerializeObject(sample, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
            });
            //Console.WriteLine(payload);

            var response = await httpClient.PostAsync(uri, new StringContent(payload, Encoding.ASCII, "application/json"));
            response.EnsureSuccessStatusCode();
        }

        private static async Task<IEnumerable<Sample>> GetGoesSamples()
        {
            var httpClient = new HttpClient();

            var goesUri = "http://services.swpc.noaa.gov/text/goes-xray-flux-primary.txt";

            var goesResponse = await httpClient.GetAsync(goesUri);
            goesResponse.EnsureSuccessStatusCode();
            var goesResponseContent = await goesResponse.Content.ReadAsStringAsync();

            var lines = goesResponseContent.Split('\n');
            var samples = lines.Where(line => line.StartsWith("20")).Select(Sample.FromText);

            return samples;
        }

        static async Task<DateTime> GetMostRecentElasticSearchTimestamp()
        {
            var elasticSearchCredentials = GetEnvironmentVariable("ElasticSearchCredentials");

            var httpClient = new HttpClient();
            var byteArray = Encoding.ASCII.GetBytes(elasticSearchCredentials);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            var uri = $"http://elasticsearch.saintgimp.org/logstash-goes/_search";
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
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return DateTime.MinValue;
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            dynamic data = JObject.Parse(responseContent);
            DateTime timestamp = data.hits.hits[0]._source["@timestamp"];

            return timestamp;
        }

        public static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

        public class Sample
        {
            public DateTime Timestamp { get; private set; }
            public double ShortFlux { get; private set; }
            public double LongFlux { get; private set; }
            public double ShortFluxScaled { get; private set; }
            public double LongFluxScaled { get; private set; }

            public static Sample FromText(string logLine)
            {
                var parts = logLine.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

                var year = int.Parse(parts[0]);
                var month = int.Parse(parts[1]);
                var day = int.Parse(parts[2]);
                var hour = int.Parse(parts[3].Substring(0, 2));
                var minute = int.Parse(parts[3].Substring(2, 2));
                DateTime timestamp = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);

                var shortFlux = double.Parse(parts[6]);
                var longFlux = double.Parse(parts[7]);

                const double scalingFactor = 1000000000.0;
                return new Sample
                {
                    Timestamp = timestamp,
                    ShortFlux = shortFlux,
                    ShortFluxScaled = Math.Log(shortFlux * scalingFactor, 10),
                    LongFlux = longFlux,
                    LongFluxScaled = Math.Log(longFlux * scalingFactor, 10),

                };
            }

            public override string ToString()
            {
                return $"{Timestamp} Short: {ShortFlux}, Long: {LongFlux}";
            }
        }
    }
}
