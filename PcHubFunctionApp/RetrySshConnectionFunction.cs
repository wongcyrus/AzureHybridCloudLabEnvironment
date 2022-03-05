using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using PcHubFunctionApp.Model;

namespace PcHubFunctionApp;

public class RetrySshConnectionFunction
{
    [FunctionName(nameof(RetrySshConnectionFunction))]
    public void Run([QueueTrigger("retry")] string myQueueItem, ILogger log)
    {
        log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
    }
}