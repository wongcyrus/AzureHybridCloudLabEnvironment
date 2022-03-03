using System;
using System.Threading.Tasks;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using PcReservationFunctionApp.Dao;
using PcReservationFunctionApp.Helper;
using PcReservationFunctionApp.Model;
using Session = Common.Model.Session;

namespace PcReservationFunctionApp;

public static class AddSshConnectionFunction
{
    [FunctionName(nameof(AddSshConnectionFunction))]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
        HttpRequest req,
        ExecutionContext context,
        ILogger log)
    {
        log.LogInformation("AddSshProxyFunction HTTP trigger function processed a request.");

        var status = req.Form["Status"];
        var output = req.Form["Output"];

        log.LogInformation(output);
        var sshConnection = SshConnection.FromJson(output, log);
        if (sshConnection == null) return new OkObjectResult("sshConnection null.");

        var config = new Config(context);
        var sshConnectionDao = new SshConnectionDao(config, log);
        sshConnection.PartitionKey = sshConnection.Location;
        sshConnection.RowKey = sshConnection.Email;
        sshConnection.Status = "Unassigned";
        sshConnection.ETag = ETag.All;

        var computerDao = new ComputerDao(config, log);

        log.LogInformation(status);
        if (status == "CREATED")
        {
            sshConnectionDao.Upsert(sshConnection);
            log.LogInformation("Done Upsert. sshConnection");

            //Find a free computer in lab.
            //If found free pc, update db, and send direct message.
            //If cannot find free pc or send direct message in error, put it in retry queue with delay, and email users.

            var freeComputers = computerDao.GetFreeComputer(sshConnection.Location);

            if (freeComputers.Count > 0)
            {
                var random = new Random();
                var computer = freeComputers[random.Next(freeComputers.Count)];
                computerDao.UpdateConnection(computer, sshConnection.Email);
                var success = await ChangeSshConnectionToDevice(config, log, computer.RowKey, sshConnection);

                if (success) return new OkObjectResult(sshConnection);

                computer = computerDao.Get(computer.PartitionKey, computer.RowKey);
                computerDao.UpdateConnection(computer, null);
                log.LogInformation("IoT Direct message not success!");
            }
            //No free computer or IoT Direct message not success. 
            log.LogInformation("No Free computer.");
            return new OkObjectResult(sshConnection);
        }
        if (status == "DELETED" || status == "DELETING")
        {
            var computer = computerDao.GetComputer(sshConnection.Location, sshConnection.Email);

            if (computer == null) return new OkObjectResult(sshConnection);
            computerDao.UpdateConnection(computer, "");
            await ChangeSshConnectionToDevice(config, log, computer.RowKey, null);
            sshConnectionDao.Delete(sshConnection);
            log.LogInformation("Deleted.");
            return new OkObjectResult(sshConnection);
        }
        log.LogInformation(sshConnection.ToString());
        return new OkObjectResult($"Status {status} no action.");
    }


    static async Task<bool> ChangeSshConnectionToDevice(Config config, ILogger log, string deviceId, SshConnection sshConnection)
    {
        try
        {
            var client = ServiceClient.CreateFromConnectionString((config.GetConfig(Config.Key.IotHubPrimaryConnectionString)));
            if (sshConnection == null)
            {
                var method = new CloudToDeviceMethod("OnRemoveSshMessage")
                {
                    ResponseTimeout = TimeSpan.FromSeconds(30)
                };
                await client.InvokeDeviceMethodAsync(deviceId, method);
            }
            else
            {
                var method = new CloudToDeviceMethod("OnNewSshMessage")
                {
                    ResponseTimeout = TimeSpan.FromSeconds(30)
                };
                var session = new Session(sshConnection.IpAddress, sshConnection.Port, sshConnection.Username, sshConnection.Password);
                method.SetPayloadJson(session.ToJson());
                await client.InvokeDeviceMethodAsync(deviceId, method);
            }
            return true;
        }
        catch (Exception ex)
        {
            log.LogInformation(ex.Message);
            return false;
        }
    }
}