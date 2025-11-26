using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
﻿using Particle.SDK;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

namespace SaintGimp.Functions;

public class WeatherDashboard(IConfiguration configuration, ILogger<WeatherDashboard> logger)
{
    private readonly IConfiguration configuration = configuration;
    private readonly ILogger _logger = logger;

    [Function("WeatherDashboard")]
    public async Task Run([TimerTrigger("0 5/15 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);
        
        var openWeatherApiKey = configuration["OpenWeatherApiKey"] ?? "";
        var forecastLocationLat = configuration["ForecastLocationLat"] ?? "";
        var forecastLocationLon = configuration["ForecastLocationLon"] ?? "";
        var deviceId = configuration["DeviceId"] ?? "";
        var deviceAccessKey = configuration["DeviceAccessKey"] ?? "";
        
        var forecast = await GetForecast(forecastLocationLat, forecastLocationLon, openWeatherApiKey);
        await SendForecastToDevice(forecast, deviceId, deviceAccessKey);

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
        }
    }

    private async Task<string> GetForecast(string forecastLocationLat, string forecastLocationLon, string openWeatherApiKey)
    {
        // Weather Underground shut down free access. Dark Sky closed down entirely.
        // Open Weather Map seems to be fairly inaccurate, but it's the one we can use for free, so here we are.
        // https://medium.com/@Ari_n/8-weather-api-alternatives-now-that-darksky-is-shutting-down-42a5ac395f93

        Console.WriteLine($"Getting current weather...");

        var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip
        };
        var httpClient = new HttpClient(handler);
        var weatherUri = new Uri($"https://api.openweathermap.org/data/3.0/onecall?lat={forecastLocationLat}&lon={forecastLocationLon}&exclude=currently,minutely,alerts&appid={openWeatherApiKey}");

        var response = await httpClient.GetAsync(weatherUri);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        dynamic data = JObject.Parse(responseContent);

        var thisHour = data.hourly[0];
        var futureHour = data.hourly[4];
        var today = data.daily[0];
        Console.WriteLine($"Daily summary for today is: {today.summary}");
        Console.WriteLine($"Daily summary icon for today is: {today.weather[0].icon}");
        Console.WriteLine($"The hourly description is: {thisHour.weather[0].description}");
        Console.WriteLine($"The hourly icon is: {thisHour.weather[0].icon}");
        Console.WriteLine($"The hourly + 4 description is: {futureHour.weather[0].description}");
        Console.WriteLine($"The hourly + 4 icon is: {futureHour.weather[0].icon}");

        // TODO: Not sure which we want to actually show - hourly summary, hour + N forecast, or daily summary
        // If the icon is too pessimistic, we could also key off of other properties like .clouds, .pop
        var forecast = ConvertIconToForecast(futureHour.weather[0].icon.ToString());
        Console.WriteLine($"The forecast to send is: {forecast}");

        return forecast;
    }

    private string ConvertIconToForecast(string icon) =>
        // https://openweathermap.org/weather-conditions
        icon switch
        {
            "01d" or "01n" => "sunny",
            "02d" or "02n" or "03d" or "03n" => "partlycloudy",
            "04d" or "04n" or "50d" or "50n" => "cloudy",
            _ => "rain"
        };

    private async Task SendForecastToDevice(string forecast, string deviceId, string deviceAccessKey)
    {
        Console.WriteLine($"Sending forecast to device...");

        await ParticleCloud.SharedCloud.TokenLoginAsync(deviceAccessKey);
        var device = await ParticleCloud.SharedCloud.GetDeviceAsync(deviceId);
        var functionResponse = await device.RunFunctionAsync("display", forecast);
        
        Console.WriteLine($"Response from device was {functionResponse.ReturnValue}");
    }
}