using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
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
                var intent = document.GetPropertyValue<string>("intent");
                await PostMessageToTeams(intent, customerName, customerPhone, category, description);

                log.LogInformation("Post a message to teams successfully.");
            }
        }

        private static async Task PostMessageToTeams(string intent, string customerName, string customerPhone, string category, string description)
        {
            var teamsChannelUrl = System.Environment.GetEnvironmentVariable(
                intent == "维修" ? "FixChannelUrl" : "ReplaceChannelUrl"
                );
            var text = $"客户\"{customerName}\"报修{category}, 故障描述: {description}";
            var payload = JsonConvert.SerializeObject(new { text });
            using (var client = new HttpClient())
            {
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                await client.PostAsync(teamsChannelUrl, content);
            }
        }
    }
}
