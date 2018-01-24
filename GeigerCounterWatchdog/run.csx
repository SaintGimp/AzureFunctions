#r "Newtonsoft.Json"

using System;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TweetSharp;

public static async Task Run(TimerInfo myTimer, TraceWriter log)
{
    log.Info($"Loading data from ElasticSearch...");
    
    try
    {
        var elasticSearchCredentials = GetEnvironmentVariable("ELASTICSEARCH_CREDENTIALS");
      
        var httpClient = new HttpClient();
        var byteArray = Encoding.ASCII.GetBytes(elasticSearchCredentials);
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

        var uri = $"http://elasticsearch.saintgimp.org/logstash-geiger/_search";
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
        log.Info($"Timestamp is {timestamp}, difference is {difference}");
        
        if (difference > TimeSpan.FromMinutes(30))
        {
            throw new ApplicationException("No new data");
        }
        
        log.Info("Everything's fine here, we're all fine, how are you?");
    }
    catch (Exception)
    {
        await SendNotification(log);
    }
}

static async Task SendNotification(TraceWriter log)
{
    var message = "Hey, I think the geiger counter is offline!";
    log.Info(message);

    var consumerKey = GetEnvironmentVariable("TWITTER_CONSUMER_KEY");
    var consumerSecret = GetEnvironmentVariable("TWITTER_CONSUMER_SECRET");
    var accessToken = GetEnvironmentVariable("TWITTER_ACCESS_TOKEN_KEY");
    var accessTokenSecret = GetEnvironmentVariable("TWITTER_ACCESS_TOKEN_SECRET");

    var service = new TwitterService(consumerKey, consumerSecret);
    service.AuthenticateWith(accessToken, accessTokenSecret);

    await service.SendDirectMessageAsync(new SendDirectMessageOptions()
    {
        ScreenName = "saintgimp",
        Text = message
    });
}

public static string GetEnvironmentVariable(string name)
{
    return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
}
