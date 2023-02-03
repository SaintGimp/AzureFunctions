using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Azure.Communication.Email;
using Azure.Communication.Email.Models;

namespace SaintGimp
{
    public class FunctionBase
    {
        public static void SendEmailNotification(string message, ILogger log)
        {
            log.LogInformation(message);

            string connectionString = Environment.GetEnvironmentVariable("EmailConnectionString");
            EmailClient emailClient = new EmailClient(connectionString);
            EmailContent emailContent = new EmailContent(message);
            emailContent.PlainText = message;
            List<EmailAddress> emailAddresses = new List<EmailAddress> { new EmailAddress("saintgimp@hotmail.com") { DisplayName = "Eric Lee" } };
            EmailRecipients emailRecipients = new EmailRecipients(emailAddresses);
            EmailMessage emailMessage = new EmailMessage("AzureFunctions@72b83dc5-34f6-4670-91cf-3e9a4ef26bab.azurecomm.net", emailContent, emailRecipients);
            emailClient.Send(emailMessage, CancellationToken.None);
        }
    }
}
