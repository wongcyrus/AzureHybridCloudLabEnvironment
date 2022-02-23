using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using PcReservationFunctionApp.Dao;
using PcReservationFunctionApp.Helper;
using PcReservationFunctionApp.Model;

namespace PcReservationFunctionApp;

// ReSharper disable once UnusedMember.Global
public static class GetSessionFunction
{
    [FunctionName(nameof(GetSessionFunction))]
    // ReSharper disable once UnusedMember.Global
    public static IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
        HttpRequest req,
        ExecutionContext context,
        ILogger log)
    {
        log.LogInformation("GetSessionFunction HTTP trigger function processed a request.");

        var computer = new Computer
        {
            Location = req.Query["Location"],
            IpAddress = req.Query["IpAddress"],
            MachineName = req.Query["MachineName"],
            DeviceId = req.Query["DeviceId"],
            MacAddress = req.Query["MacAddress"],
            IsConnected = Convert.ToBoolean(req.Query["IsConnected"]),
            LastErrorMessage = req.Query["LastErrorMessage"],
            PartitionKey = req.Query["Location"],
            RowKey = req.Query["MacAddress"]
        };

        var config = new Config(context);
        var computerDao = new ComputerDao(config, log);
        computerDao.Upsert(computer);

        var sessionDao = new SessionDao(config, log);

        var connectedSession = new Session
        {
            PartitionKey = computer.Location,
            RowKey = computer.MacAddress,
            Location = computer.Location,
            MacAddress = computer.MacAddress
        };

        var sshConnectionDao = new SshConnectionDao(config, log);
        Common.Model.Session sessionPoco;
        SshConnection sshConnection;
        if (sessionDao.IsNew(connectedSession))
        {
            //Random get a unassigned SshConnection, change status to assigned (Optimistic concurrency) and save new Session.
            var allUnassignedForLab = sshConnectionDao.GetAllUnassignedByLab(computer.Location);

            if (allUnassignedForLab.Count == 0)
            {
                log.LogInformation("No Unassigned SSH connection.");
                return new OkObjectResult("");
            }
            var random = new Random();
            sshConnection = allUnassignedForLab[random.Next(allUnassignedForLab.Count)];
            var isSuccess = sshConnectionDao.ChangeStatusToAssigned(sshConnection);
            if (isSuccess)
            {
                connectedSession.Email = sshConnection.Email;
                sessionDao.Upsert(connectedSession);
                log.LogInformation("Assigned SSH connection to Session.");
            }
        }
        else
        {
            //Get back previous connectedSession, and then get back Assigned sshConnection;
            connectedSession = sessionDao.Get(connectedSession.PartitionKey, connectedSession.RowKey);
            sshConnection = sshConnectionDao.Get(computer.Location, connectedSession.Email);
            if (sshConnection == null)
            {
                //No sshConnection, then delete connectedSession
                sessionDao.Delete(connectedSession);
            }
        }
        if (sshConnection == null) return new OkObjectResult("");
        sessionPoco = new Common.Model.Session(sshConnection.IpAddress, sshConnection.Port, sshConnection.Username,
            sshConnection.Password);
        log.LogInformation(computer.ToString());
        log.LogInformation(sessionPoco.ToString());
        return new OkObjectResult(sessionPoco.ToJson());
    }
}