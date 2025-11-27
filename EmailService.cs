using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SaintGimp.Functions;

public class EmailService(IConfiguration configuration, ILogger<EmailService> logger)
{
    private readonly IConfiguration configuration = configuration;
    private readonly ILogger<EmailService> _logger = logger;

    public void SendEmailNotification(string message, string recipient)
    {
        _logger.LogInformation($"Sending email to {recipient} with message: {message}");

        string connectionString = configuration["EmailConnectionString"] ?? "";

        var emailClient = new EmailClient(connectionString);
        var emailContent = new EmailContent(message)
        {
            PlainText = message
        };
        var emailAddresses = new List<EmailAddress> { new(recipient) };
        var emailRecipients = new EmailRecipients(emailAddresses);
        var emailMessage = new EmailMessage("DoNotReply@a036e2cc-5e5b-4ece-bf78-dcbd50ba6554.azurecomm.net", emailRecipients, emailContent);
        emailClient.Send(WaitUntil.Started, emailMessage, CancellationToken.None);
    }
}