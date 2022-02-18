using Common.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace PcReservationFunctionApp
{
    // ReSharper disable once UnusedMember.Global
    public static class GetSessionFunction
    {
        [FunctionName(nameof(GetSessionFunction))]
        // ReSharper disable once UnusedMember.Global
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetSessionFunction HTTP trigger function processed a request.");

            var computer = new Computer
            {
                IpAddress = req.Query["IpAddress"],
                MachineName = req.Query["MachineName"],
                DeviceId = req.Query["DeviceId"],
                MacAddress = req.Query["MacAddress"]
            };



            var session = new Session(ipAddress: "20.24.124.48", port: 22, username: "bastion", password: "q1Hf82IasE4TlsOkncT&");

            return new OkObjectResult(session.ToJson());
        }
    }
}
