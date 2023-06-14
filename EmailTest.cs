using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace SaintGimp
{
    public class EmailTest : FunctionBase
    {
        [FunctionName("EmailTest")]
        public static void Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation($"Testing email notifications...");

            SendEmailNotification("This is a test of the emergency broadcasting system!", log);

        }
    }
}