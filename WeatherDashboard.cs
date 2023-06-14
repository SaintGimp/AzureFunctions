using System;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Particle.SDK;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;

namespace SaintGimp
{
    public class WeatherDashboard : FunctionBase
    {
        [FunctionName("WeatherDashboard")]
        public static async Task RunAsync([TimerTrigger("0 */15 * * * *")] TimerInfo myTimer, ILogger log)
        {
            var forecast = await GetForecast(log);
            await SendForecastToDevice(forecast, log);

            log.LogInformation($"Done.");

        }

        public static async Task<string> GetForecast(ILogger log)
        {
            // Weather Underground shut down free access. Dark Sky closed down entirely.
            // Open Weather Map seems to be fairly inaccurate, but it's the one we can use for free, so here we are.

            log.LogInformation($"Getting current weather...");

            var openWeatherApiKey = Environment.GetEnvironmentVariable("OpenWeatherApiKey", EnvironmentVariableTarget.Process);
            var forecastLocationLat = Environment.GetEnvironmentVariable("ForecastLocationLat");
            var forecastLocationLon = Environment.GetEnvironmentVariable("ForecastLocationLon");
            var weatherUri = new Uri($"https://api.openweathermap.org/data/3.0/onecall?lat={forecastLocationLat}&lon={forecastLocationLon}&exclude=currently,minutely,alerts&appid={openWeatherApiKey}");

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip
            };
            var httpClient = new HttpClient(handler);
            
            var response = await httpClient.GetAsync(weatherUri);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(responseContent);

            var thisHour = data.hourly[0];
            var futureHour = data.hourly[4];
            var today = data.daily[0];
            log.LogInformation($"Daily summary for today is: {today.summary}");
            log.LogInformation($"Daily summary icon for today is: {today.weather[0].icon}");
            log.LogInformation($"The hourly description is: {thisHour.weather[0].description}");
            log.LogInformation($"The hourly icon is: {thisHour.weather[0].icon}");
            log.LogInformation($"The hourly + 4 description is: {futureHour.weather[0].description}");
            log.LogInformation($"The hourly + 4 icon is: {futureHour.weather[0].icon}");

            // TODO: Not sure which we want to actually show - hourly summary, hour + N forecast, or daily summary
            // If the icon is too pessimistic, we could also key off of other properties like .clouds, .pop
            var forecast = ConvertIconToForecast(futureHour.weather[0].icon.ToString());
            log.LogInformation($"The forecast to send is: {forecast}");

            return forecast;
        }

        public static string ConvertIconToForecast(string icon) =>
            // https://openweathermap.org/weather-conditions
            icon switch
            {
                "01d" or "01n" => "sunny",
                "02d" or "02n" or "03d" or "03n" => "partlycloudy",
                "04d" or "04n" or "50d" or "50n" => "cloudy",
                _ => "rain"
            };

        public static async Task SendForecastToDevice(string forecast, ILogger log)
        {
            log.LogInformation($"Sending forecast to device...");

            var weatherDashboardDeviceAccessKey = Environment.GetEnvironmentVariable("WeatherDashboardDeviceAccessKey", EnvironmentVariableTarget.Process);
            var weatherDashboardDeviceId = Environment.GetEnvironmentVariable("WeatherDashboardDeviceId", EnvironmentVariableTarget.Process);

            await ParticleCloud.SharedCloud.TokenLoginAsync(weatherDashboardDeviceAccessKey);
            var device = await ParticleCloud.SharedCloud.GetDeviceAsync(weatherDashboardDeviceId);
            await device.RunFunctionAsync("display", forecast);
        }
    }
}
