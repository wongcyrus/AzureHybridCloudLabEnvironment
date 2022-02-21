using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PcReservationFunctionApp.Dao;
using PcReservationFunctionApp.Helper;
using PcReservationFunctionApp.Model;

namespace PcReservationFunctionApp;

public static class AddSshConnectionFunction
{
    [FunctionName(nameof(AddSshConnectionFunction))]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ExecutionContext context,
        ILogger log)
    {
        log.LogInformation("AddSshProxyFunction HTTP trigger function processed a request.");

        string status = req.Query["Status"];
        string output = req.Query["Output"];

        var sshConnection = SshConnection.FromJson(output, log);

        var config = new Config(context);
        var connectionDao = new SshConnectionDao(config, log);

        if (sshConnection != null)
        {
            sshConnection.PartitionKey = sshConnection.Location;
            sshConnection.RowKey = sshConnection.Email;
            connectionDao.Upsert(sshConnection);

            return new OkObjectResult(sshConnection);
        }
        return new OkObjectResult("sshConnection null.");
    }
}