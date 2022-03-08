using System;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
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

        //Find a free computer in lab.
        //If found free pc, update db, and send direct message.
        //If cannot find free pc or send direct message in error, put it in retry queue with delay, and email users.
        var freeComputers = computerDao.GetFreeComputer(sshConnection!.Location);

        if (freeComputers.Count > 0)
        {
            var random = new Random();
            var computer = freeComputers[random.Next(freeComputers.Count)];
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
Password:       {sshConnection.Password}

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
            log.LogInformation("IoT Direct message not success!");
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
    }
    public class NoFreePcException : Exception
    {
        private SshConnection _sshConnection;
        public NoFreePcException(SshConnection sshConnection)
        {
            _sshConnection = sshConnection;
            Message = "No Free Pc for " + sshConnection;
        }

        public override string Message { get; }
    }
}