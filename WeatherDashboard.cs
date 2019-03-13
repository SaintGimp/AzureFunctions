using System;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Particle.SDK;

namespace SaintGimp
{
    public static class WeatherDashboard
    {
        [FunctionName("WeatherDashboard")]
        public static async Task RunAsync([TimerTrigger("0 */15 * * * *")]TimerInfo myTimer, ILogger log)
        {
            var forecast = await GetForecast(log);
            await SendForecastToDevice(forecast, log);

            log.LogInformation($"Done.");

        }

        public static async Task<string> GetForecast(ILogger log)
        {
            // Weather Underground shut down free access. Open Weather Map seems to be fairly inaccurate.

            log.LogInformation($"Getting current weather...");

            var darkSkyApiKey = Environment.GetEnvironmentVariable("DarkSkyApiKey", EnvironmentVariableTarget.Process);
            var forecastLocation = GetEnvironmentVariable("ForecastLocation");
            var weatherUri = new Uri($"https://api.darksky.net/forecast/{darkSkyApiKey}/{forecastLocation}?exclude=currently,minutely,daily,alerts,flags");
            
            HttpClientHandler handler = new HttpClientHandler();
            handler.AutomaticDecompression = System.Net.DecompressionMethods.GZip;
            var httpClient = new HttpClient(handler);
            
            var response = await httpClient.GetAsync(weatherUri);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(responseContent);

            var hourly = data.hourly;
            log.LogInformation($"The hourly summary is: {hourly.summary}");
            log.LogInformation($"The hourly summary icon is: {hourly.icon}");
            log.LogInformation($"The hourly + 4 icon is: {hourly.data[3].icon}");

            // TODO: Not sure which we want to actually show - hourly summary, hour + N forecast, or daily summary
            var forecast = ConvertIconToForecast(hourly.icon.ToString());
            log.LogInformation($"The forecast to send is: {forecast}");

            return forecast;
        }

        public static string ConvertIconToForecast(string icon)
        {
            switch (icon)
            {
                case "clear-day":
                case "clear-night":
                {
                    return "sunny";
                }
                case "partly-cloudy-day":
                case "partly-cloudy-night":
                {
                    return "partlycloudy";
                }
                case "cloudy":
                case "fog":
                {
                    return "cloudy";
                }
                case "rain":
                case "snow":
                case "sleet":
                {
                    return "rain";
                }
                default:
                {
                    return "rain";
                }

            }
        }

        public static async Task SendForecastToDevice(string forecast, ILogger log)
        {
            log.LogInformation($"Sending forecast to device...");

            var weatherDashboardDeviceAccessKey = Environment.GetEnvironmentVariable("WeatherDashboardDeviceAccessKey", EnvironmentVariableTarget.Process);
            var weatherDashboardDeviceId = Environment.GetEnvironmentVariable("WeatherDashboardDeviceId", EnvironmentVariableTarget.Process);

            await ParticleCloud.SharedCloud.TokenLoginAsync(weatherDashboardDeviceAccessKey);
            var device = await ParticleCloud.SharedCloud.GetDeviceAsync(weatherDashboardDeviceId);
            await device.RunFunctionAsync("display", forecast);
        }

        public static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }
}
