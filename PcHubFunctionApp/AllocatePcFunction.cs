using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PcHubFunctionApp.Dao;
using PcHubFunctionApp.Helper;
using PcHubFunctionApp.Model;


namespace PcHubFunctionApp;

public class AllocatePcFunction
{
    [FunctionName(nameof(RunAllocatePcFunction))]
    [ExponentialBackoffRetry(1, "00:01:00", "00:05:00")]
    public async Task RunAllocatePcFunction([QueueTrigger("allocate-pc")] QueueMessage cloudQueueMessage, ExecutionContext context, ILogger log)
    {
        log.LogInformation($"Queue trigger ({cloudQueueMessage.DequeueCount}): {cloudQueueMessage.Body}");

        var sshConnection = SshConnection.FromJson(cloudQueueMessage.Body.ToString(), log);
        var config = new Config(context);
        var sshConnectionDao = new SshConnectionDao(config, log);
        var computerDao = new ComputerDao(config, log);

        var variables = JsonConvert.DeserializeObject<Dictionary<string, string>>(sshConnection!.Variables);
        Computer computer = null;
        if (variables!.ContainsKey("MachineName"))
        {
            computer = computerDao.GetComputerByMachineName(sshConnection!.Location, variables!["MachineName"]);
        }
        else if (variables!.ContainsKey("SeatNumber"))
        {
            computer = computerDao.GetComputerBySeatNumber(sshConnection!.Location, int.Parse(variables!["SeatNumber"]));
        }
        else
        {
            var freeComputers = computerDao.GetFreeComputer(sshConnection!.Location);
            if (freeComputers.Count > 0)
            {
                //Find a free computer in lab.
                var random = new Random();
                computer = freeComputers[random.Next(freeComputers.Count)];
            }
        }

        if (computer is { IsOnline: true, IsReserved: false })
        {
            //If found free pc, update db, and send direct message.
            //If cannot find free pc or send direct message in error, put it in retry queue with delay, and email users.
            computerDao.UpdateReservation(computer, sshConnection.Email);
            var success = await Helper.Azure.ChangeSshConnectionToDevice(config, log, computer.GetIoTDeviceId(), sshConnection);

            if (success)
            {
                sshConnection.Status = "ASSIGNED";
                sshConnection.ETag = ETag.All;
                sshConnection.MacAddress = computer.MacAddress;
                sshConnectionDao.Upsert(sshConnection);
                var email = new Email(config, log);
                var emailMessage = new EmailMessage
                {
                    Subject = $"{sshConnection.Lab}: Your PC in {sshConnection.Location}",
                    To = sshConnection.Email,
                    Body = $@"
Dear Student,

Please run your SSH client and connect to 
IP:             {sshConnection.IpAddress}
Port:           {sshConnection.Port}
User:           {sshConnection.Username}
Password:

{sshConnection.Password}

Regards,
Azure Hybrid Cloud Lab Environment 
"
                };
                email.Send(emailMessage, null);
                return;
            }

            //Rollback action
            computer = computerDao.Get(computer.PartitionKey, computer.RowKey);
            computerDao.UpdateReservation(computer, null);
        }

        // Force Retry
        throw new NoFreePcException(sshConnection);
    }

    [FunctionName(nameof(RunAllocatePcPoisonFunction))]
    public void RunAllocatePcPoisonFunction([QueueTrigger("allocate-pc-poison")] QueueMessage cloudQueueMessage, ExecutionContext context, ILogger log)
    {
        log.LogInformation($"Queue trigger RunAllocatePcPoisonFunction ({cloudQueueMessage.DequeueCount}): {cloudQueueMessage.Body}");

        var sshConnection = SshConnection.FromJson(cloudQueueMessage.Body.ToString(), log);
        var config = new Config(context);
        var sshConnectionDao = new SshConnectionDao(config, log);

        if (sshConnection == null) return;

        sshConnection.Status = "NO_PC_AVAILABLE";
        sshConnection.ETag = ETag.All;
        sshConnection.MacAddress = "";
        sshConnectionDao.Upsert(sshConnection);
        var email = new Email(config, log);
        var emailMessage = new EmailMessage
        {
            Subject = $"{sshConnection.Lab}: Cannot allocate PC in {sshConnection.Location}",
            To = sshConnection.Email,
            Body = $@"
Dear Student,

I am sorry to tell you that there is no free PC in {sshConnection.Location}.

Regards,
Azure Hybrid Cloud Lab Environment 
"
        };
        email.Send(emailMessage, null);

        var computerErrorLogDao = new ComputerErrorLogDao(config, log);
        var computerErrorLog = new ComputerErrorLog()
        {
            Email = sshConnection.Email,
            DeviceId = "",
            MachineName = "",
            MacAddress = "",
            IpAddress = sshConnection.IpAddress,
            IsConnected = false,
            IsOnline = false,
            IsReserved = false,
            ErrorMessage = "Cannot allocate PC",
            Location = sshConnection.Location,
            IoTConnectionString = "",
            PartitionKey = sshConnection.Email,
            RowKey = $"{DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks:D19}"
        };
        computerErrorLogDao.Add(computerErrorLog);
    }
    public class NoFreePcException : Exception
    {
        public NoFreePcException(SshConnection sshConnection)
        {
            Message = "No Free Pc for " + sshConnection;
        }

        public override string Message { get; }
    }
}