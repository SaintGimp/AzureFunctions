using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace SaintGimp.Functions;

record EmailRequest(string Subject, string Message);

public class SendEmail(EmailService emailService, IConfiguration configuration, ILogger<SendEmail> logger)
{
    private readonly EmailService emailService = emailService;
    private readonly IConfiguration configuration = configuration;
    private readonly ILogger<SendEmail> _logger = logger;

    [Function("SendEmail")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("Executed at: {executionTime}", DateTime.Now);

        var recipient = configuration["EmailRecipient"] ?? "";
        
        try
        {
            var request = await System.Text.Json.JsonSerializer.DeserializeAsync<EmailRequest>(req.Body,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var subject = request?.Subject ?? "No subject";
            var message = request?.Message ?? "No message";

            emailService.SendEmailNotification(subject, message, recipient);

            return new OkObjectResult("Email sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            return new BadRequestObjectResult("Failed to send email.");
        }
    }
}