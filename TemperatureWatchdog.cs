using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace SaintGimp
{
    public class TemperatureWatchdog : TemperatureFunctionBase
    {
        [FunctionName("TemperatureWatchdog")]
        public static async Task RunAsync([TimerTrigger("45 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {
            try
            {
                var mostRecentRecord = await GetTemperatureData(log);

                DateTime timestamp = mostRecentRecord["@timestamp"];
                var timeSinceLastUpdate = DateTime.UtcNow - timestamp;

                if (timeSinceLastUpdate > TimeSpan.FromMinutes(30))
                {
                    throw new ApplicationException($"No new data for {timeSinceLastUpdate}");
                }

                log.LogInformation("Everything's fine here, we're all fine, how are you?");
            }
            catch (Exception e)
            {
                log.LogInformation(e.ToString());
                SendEmailNotification("Hey, I think the temperature sensors are offline!", log);
            }
        }
    }
}