using System.Threading.Tasks;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using PcHubFunctionApp.Dao;
using PcHubFunctionApp.Helper;
using PcHubFunctionApp.Model;


namespace PcHubFunctionApp;

public static class AddSshConnectionFunction
{
    [FunctionName(nameof(AddSshConnectionFunction))]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        [Queue("allocate-pc")] ICollector<SshConnection> allocatePcQueue,
        ExecutionContext context,
        ILogger log)
    {
        log.LogInformation("AddSshConnectionFunction HTTP trigger function processed a request.");
        var status = req.Form["Status"];
        var output = req.Form["Output"];

        log.LogInformation(output);
        var sshConnection = SshConnection.FromJson(output, log);
        if (sshConnection == null) return new OkObjectResult("sshConnection null.");

        var config = new Config(context);
        var sshConnectionDao = new SshConnectionDao(config, log);
        sshConnection.PartitionKey = sshConnection.Location;
        sshConnection.RowKey = sshConnection.Email;
        sshConnection.Status = "UNASSIGNED";
        sshConnection.ETag = ETag.All;

        var computerDao = new ComputerDao(config, log);

        log.LogInformation(status);
        if (status == "CREATED")
        {
            sshConnectionDao.Upsert(sshConnection);
            allocatePcQueue.Add(sshConnection);
            log.LogInformation("Added to allocate pc queue.");
            return new OkObjectResult(sshConnection);
        }

        if (status == "DELETED" || status == "DELETING")
        {
            var computer = computerDao.GetComputer(sshConnection.Location, sshConnection.Email);

            if (computer == null) return new OkObjectResult(sshConnection);
            computerDao.UpdateReservation(computer, "");
            await Helper.Azure.ChangeSshConnectionToDevice(config, log, computer.GetIoTDeviceId(), null);
            sshConnection.Status = "COMPLETED";
            sshConnectionDao.Upsert(sshConnection);

            if (status == "DELETING")
            {
                var email = new Email(config, log);
                var emailMessage = new EmailMessage
                {
                    Subject = $"{sshConnection.Lab}: PC in {sshConnection.Location} session has ended",
                    To = sshConnection.Email,
                    Body = $@"
Dear Student,

Your PC session in {sshConnection.Location} has ended and please disconnect your SSH client..

Regards,
Azure Hybrid Cloud Lab Environment 
"
                };
                email.Send(emailMessage, null);
            }


            return new OkObjectResult(sshConnection);
        }

        log.LogInformation(sshConnection.ToString());
        return new OkObjectResult($"Status {status} no action.");
    }



}