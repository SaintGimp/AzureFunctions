using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public class EmailTest(EmailService emailService, ILogger<EmailTest> logger)
{
    private readonly EmailService emailService = emailService;
    private readonly ILogger<EmailTest> _logger = logger;

    [Function("EmailTest")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        emailService.SendEmailNotification("This is a test of the emergency broadcasting system!", "saintgimp@hotmail.com");

        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Email sent successfully.");
    }
}