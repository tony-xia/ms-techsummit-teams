using System.Collections.Generic;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace CosmosDB2Teams
{
    public static class SendFeedbackFunction
    {
        [FunctionName("SendFeedbackToTeams")]
        public static void Run(
            [CosmosDBTrigger(
            databaseName: "demo",
            collectionName: "userfeedback",
            ConnectionStringSetting = "CosmosDbConnectionString",
            LeaseCollectionName = "leases")]IReadOnlyList<Document> input,
            ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                log.LogInformation("Documents modified " + input.Count);
                log.LogInformation("First document Id " + input[0].Id);
            }
        }
    }
}
