using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace SaintGimp.Functions;

public class GeigerCounterWatchdog(ElasticService elasticService, EmailService emailService, IConfiguration configuration, ILogger<GeigerCounterWatchdog> logger)
{
    private readonly ElasticService elasticService = elasticService;
    private readonly EmailService emailService = emailService;
    private readonly IConfiguration configuration = configuration;
    private readonly ILogger _logger = logger;

    [Function("GeigerCounterWatchdog")]
    public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("Executed at: {executionTime}", DateTime.Now);
        
        var recipient = configuration["EmailRecipient"] ?? "";

        try
        {
            var mostRecentReading = await elasticService.GetMostRecentDocument("logstash-geiger/_search");
            if (mostRecentReading.Age > TimeSpan.FromMinutes(30))
            {
                emailService.SendEmailNotification("Geiger Counter Alert", "Hey, I think the geiger counter is offline!", recipient);
            }
            else if (mostRecentReading.Data["cpm"] > 256)
            {
                _logger.LogInformation($"cpm is {mostRecentReading.Data["cpm"]}");
                emailService.SendEmailNotification("Geiger Counter Alert", "Hey, I think the geiger counter is logging bad data!", recipient);
            }
            else
            {
                _logger.LogInformation("Everything's fine here, we're all fine, how are you?");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            emailService.SendEmailNotification("Geiger Counter Alert", "I couldn't check on the geiger counter!", recipient);
        }
    }
}