using System;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SaintGimp
{
    public class TwitterTest : FunctionBase
    {
        [FunctionName("TwitterTest")]
        public static void Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] 
    HttpRequest req, ILogger log)
        {
            log.LogInformation($"Testing Twitter notifications...");
            SendTwitterNotification("This is a test of the emergency broadcasting system!", log);
        }
    }
}