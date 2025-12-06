using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace SaintGimp.Functions;

public class EnergyMonitorWatchdog(ElasticService elasticService, EmailService emailService, IConfiguration configuration, ILogger<EnergyMonitorWatchdog> logger)
{
    private readonly ElasticService elasticService = elasticService;
    private readonly EmailService emailService = emailService;
    private readonly IConfiguration configuration = configuration;
    private readonly ILogger _logger = logger;

    [Function("EnergyMonitorWatchdog")]
    public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("Executed at: {executionTime}", DateTime.Now);
        
        var recipient = configuration["EmailRecipient"] ?? "";

        try
        {
            var mostRecentReading = await elasticService.GetMostRecentDocument("logstash-energy/_search");
            if (mostRecentReading.Age > TimeSpan.FromMinutes(30))
            {
                emailService.SendEmailNotification("Energy Monitor Alert", "Hey, I think the energy monitor is offline!", recipient);
            }
            else
            {
                _logger.LogInformation("Everything's fine here, we're all fine, how are you?");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            emailService.SendEmailNotification("Energy Monitor Alert", "I couldn't check on the energy monitor!", recipient);
        }
    }
}