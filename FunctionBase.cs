using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Azure.Communication.Email;
using Azure;

namespace SaintGimp
{
    public class FunctionBase
    {
        public static void SendEmailNotification(string message, ILogger log)
        {
            log.LogInformation(message);

            string connectionString = Environment.GetEnvironmentVariable("EmailConnectionString");
            var emailClient = new EmailClient(connectionString);
            var emailContent = new EmailContent(message)
            {
                PlainText = message
            };
            var emailAddresses = new List<EmailAddress> { new EmailAddress("saintgimp@hotmail.com", "Eric Lee") };
            var emailRecipients = new EmailRecipients(emailAddresses);
            var emailMessage = new EmailMessage("AzureFunctions@72b83dc5-34f6-4670-91cf-3e9a4ef26bab.azurecomm.net", emailRecipients, emailContent);
            emailClient.Send(WaitUntil.Started, emailMessage, CancellationToken.None);
        }
    }
}
