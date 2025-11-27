using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace SaintGimp.Functions;

public class TemperatureWatchdog(ElasticService elasticService, EmailService emailService, IConfiguration configuration, ILogger<TemperatureWatchdog> logger)
{
    private readonly ElasticService elasticService = elasticService;
    private readonly EmailService emailService = emailService;
    private readonly IConfiguration configuration = configuration;
    private readonly ILogger _logger = logger;

    [Function("TemperatureWatchdog")]
    public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("Executed at: {executionTime}", DateTime.Now);
        
        var recipient = configuration["EmailRecipient"] ?? "";

        try
        {
            var mostRecentTemperature = await elasticService.GetMostRecentDocument("logstash-temperatures/_search");
            if (mostRecentTemperature.Age > TimeSpan.FromMinutes(30))
            {
                emailService.SendEmailNotification(recipient, "Hey, I think the temperature sensors are offline!");
            }
            else
            {
                _logger.LogInformation("Everything's fine here, we're all fine, how are you?");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            emailService.SendEmailNotification(recipient, "I couldn't check on the temperature sensors!");
        }
    }
}