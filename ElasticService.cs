using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

public class ElasticService(IConfiguration configuration, ILogger<ElasticService> logger)
{
    private readonly IConfiguration configuration = configuration;
    private readonly ILogger _logger = logger;

    public async Task<ElasticDocument> GetMostRecentDocument(string relativeSearchUri)
    {
        _logger.LogInformation("Loading data from ElasticSearch...");

        var elasticSearchCredentials = configuration["ElasticSearchCredentials"] ?? "";
        var uri = new Uri($"{configuration["ElasticSearchUrl"]}/{relativeSearchUri}");

        var httpClient = new HttpClient();
        var byteArray = Encoding.ASCII.GetBytes(elasticSearchCredentials);
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

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
        _logger.LogInformation(responseContent);

        dynamic data = JObject.Parse(responseContent);
        var document = new ElasticDocument(data.hits.hits[0]._source);
        _logger.LogInformation($"Most recent timestamp is {document.Timestamp}, {document.Age} old");

        return document;
    }
}

public class ElasticDocument(dynamic data)
{
    public dynamic Data { get; } = data;
    public DateTime Timestamp => Data["@timestamp"];
    public TimeSpan Age => DateTime.UtcNow - Timestamp;
}