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
    public static class CWOPGateway
    {
        [FunctionName("CWOPGateway")]
        public static async Task RunAsync([TimerTrigger("45 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Loading data from ElasticSearch...");
            
            try
            {
                var elasticSearchCredentials = GetEnvironmentVariable("ElasticSearchCredentials");
            
                var httpClient = new HttpClient();
                var byteArray = Encoding.ASCII.GetBytes(elasticSearchCredentials);
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var uri = $"http://elasticsearch.saintgimp.org/logstash-temperatures/_search";
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
                log.LogInformation($"Timestamp is {timestamp}, difference is {timeSinceLastUpdate}");
            
                if (timeSinceLastUpdate > TimeSpan.FromMinutes(30))
                {
                    throw new ApplicationException("No new data");
                }
                
                var temperature = (int)Math.Round((double)mostRecentRecord.t3 * 9.0 / 5.0 + 32.0);
                log.LogInformation($"Temperature is {temperature} F");
                
                var packet = new AprsWeatherDataPacket(
                    accountNumber: "EW9714",
                    equipmentIdentifier: "custom",
                    latitudeInDegrees: 47.697201f,
                    longitudeInDegrees: -122.063844f,
                    temperatureInFahrenheit: temperature);
                
                await SendDataToCwop(packet, log);
                
                log.LogInformation("Everything's fine here, we're all fine, how are you?");
            }
            catch (Exception e)
            {
                log.LogInformation(e.ToString());
                SendNotification(log);
            }
        }

        private static async Task SendDataToCwop(AprsWeatherDataPacket packet, ILogger log)
        {
            using (var client = new TcpClient("cwop.aprs.net", 14580))
            using (var stream = client.GetStream())
            {
                await Send("user EW9714 pass -1 vers custom 1.00\r\n", stream, log);
                
                await ReceiveResponse(stream, log);
                await Task.Delay(TimeSpan.FromSeconds(3));

                await Send(packet.ToString(), stream, log);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        private static async Task Send(string message, NetworkStream stream, ILogger log)
        {
            log.LogInformation($"Sending: {message}");
            Byte[] data = Encoding.ASCII.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);
        }

        private static async Task ReceiveResponse(NetworkStream stream, ILogger log)
        {
            var data = new byte[256];
            int numberOfBytes = await stream.ReadAsync(data, 0, data.Length);
            var responseData = Encoding.ASCII.GetString(data, 0, numberOfBytes);
            log.LogInformation($"Received: {responseData}");
        }

        static void SendNotification(ILogger log)
        {
            var message = "Hey, I think the temperature sensor is offline!";
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

        public class AprsWeatherDataPacket
        {
            // http://weather.gladstonefamily.net/aprswxnet.html
            public string AccountNumber { get; private set; }
            public float LatitudeInDegrees { get; private set; }
            public float LongitudeInDegrees { get; private set; }
            public int? WindDirectionInDegrees { get; private set; }
            public int? WindSpeedInMph { get; private set; }
            public int? MaximumGustSpeedInMph { get; private set; }
            public int? TemperatureInFahrenheit { get; private set; }
            public float? Rainfall1HourInInches { get; private set; }
            public float? Rainfall24HoursInInches { get; private set; }
            public float? RainfallSinceMidnightInInches { get; private set; }
            public int? PercentHumidity { get; private set; }
            public float? AdjustedPressureInMillibars { get; private set; }
            public string EquipmentIdentifier { get; private set; }

            public AprsWeatherDataPacket(
                string accountNumber,
                string equipmentIdentifier,
                float latitudeInDegrees,
                float longitudeInDegrees,
                int? windDirectionInDegrees = null,
                int? windSpeedInMph = null,
                int? maximumGustSpeedInMph = null,
                int? temperatureInFahrenheit = null,
                float? rainfall1HourInInches = null,
                float? rainfall24HoursInInches = null,
                float? rainfallSinceMidnightInInches = null,
                int? percentHumidity = null,
                float? adjustedPressureInMillibars = null
                )
            {
                AccountNumber = accountNumber;
                EquipmentIdentifier = equipmentIdentifier;
                LatitudeInDegrees = latitudeInDegrees;
                LongitudeInDegrees = longitudeInDegrees;
                WindDirectionInDegrees = windDirectionInDegrees;
                WindSpeedInMph = windSpeedInMph;
                MaximumGustSpeedInMph = maximumGustSpeedInMph;
                TemperatureInFahrenheit = temperatureInFahrenheit;
                Rainfall1HourInInches = rainfall1HourInInches;
                Rainfall24HoursInInches = rainfall24HoursInInches;
                RainfallSinceMidnightInInches = rainfallSinceMidnightInInches;
                PercentHumidity = percentHumidity;
                AdjustedPressureInMillibars = adjustedPressureInMillibars;
            }

            public override string ToString()
            {
                return $"{AccountNumber}>APRS,TCPIP*:/{TimeString()}{LocationString()}{WindDirectionString()}{WindSpeedString()}{GustWindSpeedString()}{TemperatureString()}{OneHourRainString()}{TwentyFourRainString()}{RainSinceMidnightString()}{HumidityString()}{PressureString()}e{EquipmentIdentifier}\r\n";
            }

            private string TimeString()
            {
                var now = DateTime.UtcNow;
                return $"{now.Day:D2}{now.Hour:D2}{now.Minute:D2}z";
            }

            private string LocationString()
            {
                return $"{LatitudeString()}/{LongitudeString()}";
            }
            private string LatitudeString()
            {
                var absoluteLatitude = Math.Abs(LatitudeInDegrees);
                var degrees = (int)absoluteLatitude;
                var minutes = DecimalPart(absoluteLatitude) * 60m;
                var direction = LatitudeInDegrees > 0 ? "N" : "S";
                return $"{degrees:D2}{minutes:00.00}{direction}";
            }

            private string LongitudeString()
            {
                var absoluteLongitude = Math.Abs(LongitudeInDegrees);
                var degrees = (int)absoluteLongitude;
                var minutes = DecimalPart(absoluteLongitude) * 60m;
                var direction = LongitudeInDegrees > 0 ? "E" : "W";
                return $"{degrees:D3}{minutes:00.00}{direction}";
            }

            private decimal DecimalPart(float number)
            {
                return (decimal)number % 1;
            }

            private string WindDirectionString()
            {
                if (!WindDirectionInDegrees.HasValue)
                {
                    return "_...";
                }

                var safeDirection = WindDirectionInDegrees % 360;
                if (safeDirection < 0)
                {
                    safeDirection += 360;
                }
                return $"_{safeDirection:000}";
            }

            private string WindSpeedString()
            {
                return $"/{WindString(WindSpeedInMph)}";
            }

            private string WindString(int? speedInMph)
            {
                if (!WindSpeedInMph.HasValue)
                {
                    return "...";
                }

                var safeSpeed = Math.Min(WindSpeedInMph.Value, 999);
                safeSpeed = Math.Max(safeSpeed, 0);
                return $"{safeSpeed:000}";
            }

            private string GustWindSpeedString()
            {
                return $"g{WindString(MaximumGustSpeedInMph)}";
            }

            private string TemperatureString()
            {
                if (!TemperatureInFahrenheit.HasValue)
                {
                    return "t...";
                }

                var safeTemperature = Math.Max(TemperatureInFahrenheit.Value, -99);
                safeTemperature = Math.Min(safeTemperature, 999);
                return $"t{safeTemperature:000;-00}";
            }

            private string OneHourRainString()
            {
                return Rainfall1HourInInches.HasValue ? $"r{RainString(Rainfall1HourInInches)}" : "";
            }

            private string TwentyFourRainString()
            {
                return Rainfall24HoursInInches.HasValue ? $"p{RainString(Rainfall24HoursInInches)}" : "";
            }

            private string RainSinceMidnightString()
            {
                return RainfallSinceMidnightInInches.HasValue ? $"P{RainString(RainfallSinceMidnightInInches)}" : "";
            }

            private string RainString(float? rainInInches)
            {
                if (!rainInInches.HasValue)
                {
                    return "...";
                }

                var rainInHundredths = (int)(rainInInches * 100);
                rainInHundredths = Math.Min(rainInHundredths, 999);
                return $"{rainInHundredths:000}";
            }

            private string HumidityString()
            {
                if (!PercentHumidity.HasValue)
                {
                    return "";
                }

                var safeHumidity = Math.Max(PercentHumidity.Value, 1);
                safeHumidity = safeHumidity <= 99 ? safeHumidity : 0;
                return $"h{safeHumidity:00}";
            }

            private string PressureString()
            {
                if (!AdjustedPressureInMillibars.HasValue)
                {
                    return "";
                }

                var tenthsOfMillibars = (int)(AdjustedPressureInMillibars * 10);
                var safeValue = Math.Max(0, tenthsOfMillibars);
                safeValue = Math.Min(safeValue, 99999);
                return $"b{safeValue:00000}";
            }
        }
    }
}