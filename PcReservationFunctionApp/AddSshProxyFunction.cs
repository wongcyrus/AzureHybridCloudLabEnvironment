using System.Threading.Tasks;
using Common.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PcReservationFunctionApp.Model;

namespace PcReservationFunctionApp
{
    public static class AddSshProxyFunction
    {
        [FunctionName(nameof(AddSshProxyFunction))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("AddSshProxyFunction HTTP trigger function processed a request.");

            string output = req.Query["output"];

            var sshConnection =SshProxy.FromJson(output, log);

            return new OkObjectResult(sshConnection);
        }
    }
}
