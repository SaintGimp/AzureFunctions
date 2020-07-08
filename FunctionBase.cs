using System;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tweetinvi;

namespace SaintGimp
{
    public class FunctionBase
    {
        public static void SendTwitterNotification(string message, ILogger log)
        {
            log.LogInformation(message);

            // These are retrieved from https://developer.twitter.com/en/apps/8049320
            var consumerKey = GetEnvironmentVariable("TwitterConsumerKey");
            var consumerSecret = GetEnvironmentVariable("TwitterConsumerSecret");
            var accessToken = GetEnvironmentVariable("TwitterAccessTokenKey");
            var accessTokenSecret = GetEnvironmentVariable("TwitterAccessTokenSecret");

            Auth.SetUserCredentials(consumerKey, consumerSecret, accessToken, accessTokenSecret);
            var user = User.GetUserFromScreenName("saintgimp");
            var result = Message.PublishMessage(message, user.Id);
            log.LogInformation(result.SenderId.ToString());
            log.LogInformation(result.RecipientId.ToString());
        }

        public static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        } 
    }
}
