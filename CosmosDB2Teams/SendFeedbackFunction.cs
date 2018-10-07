using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CosmosDB2Teams
{
    public static class SendFeedbackFunction
    {
        [FunctionName("SendFeedbackToTeams")]
        public static async Task Run(
            [CosmosDBTrigger(
            databaseName: "demo",
            collectionName: "userfeedback",
            ConnectionStringSetting = "CosmosDbConnectionString",
            LeaseCollectionName = "leases")]IReadOnlyList<Document> input,
            ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                var document = input[0];

                log.LogInformation("New document Id " + document.Id);

                var customerName = document.GetPropertyValue<string>("customerName");
                var customerPhone = document.GetPropertyValue<string>("customerPhone");
                var category = document.GetPropertyValue<string>("category");
                var description = document.GetPropertyValue<string>("description");
                await PostMessageToTeams(customerName, customerPhone, category, description);

                log.LogInformation("Post a message to teams successfully.");
            }
        }

        private static async Task PostMessageToTeams(string customerName, string customerPhone, string category, string description)
        {
            var teamsChannelUrl = ConfigurationManager.AppSettings["TeamsChannelUrl"];
            var payload = JsonConvert.SerializeObject(new { text = customerName });
            using (var client = new HttpClient())
            {
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                await client.PostAsync(teamsChannelUrl, content);
            }
        }
    }
}
