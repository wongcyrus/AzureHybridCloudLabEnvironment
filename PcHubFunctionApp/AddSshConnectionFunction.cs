using System;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using PcHubFunctionApp.Dao;
using PcHubFunctionApp.Helper;
using PcHubFunctionApp.Model;
using Session = Common.Model.Session;

namespace PcHubFunctionApp;

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
        sshConnection.Status = "UNASSIGNED";
        sshConnection.ETag = ETag.All;

        var computerDao = new ComputerDao(config, log);

        log.LogInformation(status);
        if (status == "CREATED")
        {
            sshConnectionDao.Upsert(sshConnection);
            log.LogInformation("Done Upsert sshConnection");

            //Find a free computer in lab.
            //If found free pc, update db, and send direct message.
            //If cannot find free pc or send direct message in error, put it in retry queue with delay, and email users.

            var freeComputers = computerDao.GetFreeComputer(sshConnection.Location);

            if (freeComputers.Count > 0)
            {
                var random = new Random();
                var computer = freeComputers[random.Next(freeComputers.Count)];
                computerDao.UpdateReservation(computer, sshConnection.Email);
                var success = await ChangeSshConnectionToDevice(config, log, computer.GetIoTDeviceId(), sshConnection);

                if (success)
                {
                    sshConnection.Status = "ASSIGNED";
                    sshConnection.ETag = ETag.All;
                    sshConnection.MacAddress = computer.MacAddress;
                    sshConnectionDao.Upsert(sshConnection);
                    return new OkObjectResult(sshConnection);
                }

                //Rollback action
                computer = computerDao.Get(computer.PartitionKey, computer.RowKey);
                computerDao.UpdateReservation(computer, null);
                log.LogInformation("IoT Direct message not success!");
            }

            //No free computer or IoT Direct message not success. 
            //QueueClient queueClient = new QueueClient(config.GetConfig(Config.Key.AzureWebJobsStorage), "retry");
            //await queueClient.SendMessageAsync(sshConnection.ToJson());
            //log.LogInformation("No Free computer.");
            return new OkObjectResult(sshConnection);
        }

        if (status == "DELETED" || status == "DELETING")
        {
            var computer = computerDao.GetComputer(sshConnection.Location, sshConnection.Email);

            if (computer == null) return new OkObjectResult(sshConnection);
            computerDao.UpdateReservation(computer, "");
            await ChangeSshConnectionToDevice(config, log, computer.GetIoTDeviceId(), null);
            sshConnection.Status = "COMPLETED";
            sshConnectionDao.Upsert(sshConnection);
            return new OkObjectResult(sshConnection);
        }

        log.LogInformation(sshConnection.ToString());
        return new OkObjectResult($"Status {status} no action.");
    }


    private static async Task<bool> ChangeSshConnectionToDevice(Config config, ILogger log, string deviceId,
        SshConnection sshConnection)
    {
        try
        {
            using var client =
                ServiceClient.CreateFromConnectionString(config.GetConfig(Config.Key.IotHubPrimaryConnectionString));
            using var manager =
                RegistryManager.CreateFromConnectionString(config.GetConfig(Config.Key.IotHubPrimaryConnectionString));
            if (sshConnection == null)
            {
                var twin = await manager.GetTwinAsync(deviceId);
                twin.Properties.Desired["session"] = "";
                await manager.UpdateTwinAsync(twin.DeviceId, twin, twin.ETag);
                log.LogInformation("Set session to empty in Twin.");
                var method = new CloudToDeviceMethod("OnRemoveSshMessage")
                {
                    ResponseTimeout = TimeSpan.FromSeconds(30)
                };
                //May flow exception if devices is offline.
                await client.InvokeDeviceMethodAsync(deviceId, method);
            }
            else
            {
                var method = new CloudToDeviceMethod("OnNewSshMessage")
                {
                    ResponseTimeout = TimeSpan.FromSeconds(30)
                };
                //Let it throw exception if device is offline.
                var session = new Session(sshConnection.IpAddress, sshConnection.Port, sshConnection.Username,
                    sshConnection.Password);
                method.SetPayloadJson(session.ToJson());
                await client.InvokeDeviceMethodAsync(deviceId, method);
                var twin = await manager.GetTwinAsync(deviceId);
                twin.Properties.Desired["session"] = session.ToJson();
                await manager.UpdateTwinAsync(twin.DeviceId, twin, twin.ETag);
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