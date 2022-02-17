using Common.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PcReservationFunctionApp
{
    // ReSharper disable once UnusedMember.Global
    public static class GetReservationFunction
    {
        [FunctionName("GetReservationFunction")]
        // ReSharper disable once UnusedMember.Global
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";


            var session = new Session(ipAddress: "34.217.111.228", port: 2222, username: "bastion", password: "Abcd1234",
                startTime: DateTime.UtcNow, endTime: DateTime.UtcNow.AddHours(1));

            return new OkObjectResult(session.ToJson());
        }
    }
}
