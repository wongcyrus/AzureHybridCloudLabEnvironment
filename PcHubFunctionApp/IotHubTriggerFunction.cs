using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using PcReservationFunctionApp.Dao;
using PcReservationFunctionApp.Helper;

namespace PcReservationFunctionApp;

public class IotHubTriggerFunction
{
    [FunctionName("IotHubTriggerFunction")]
    public async Task Run(
        [EventHubTrigger(nameof(Config.Key.EventHubName),
            Connection = nameof(Config.Key.EventHubPrimaryConnectionString))]
        EventData myEventHubMessage,
        DateTime enqueuedTimeUtc,
        long sequenceNumber,
        string offset,
        ExecutionContext context,
        ILogger log)
    {
        log.LogInformation($"Event: {Encoding.UTF8.GetString(myEventHubMessage.Body)}");
        var config = new Config(context);
        var registryManager =
            RegistryManager.CreateFromConnectionString(config.GetConfig(Config.Key.IotHubPrimaryConnectionString));
        var computerDao = new ComputerDao(config, log);
        if (myEventHubMessage.Properties.ContainsKey("opType"))
        {
            var deviceId = myEventHubMessage.SystemProperties["iothub-connection-device-id"] as string;
            var opType = myEventHubMessage.Properties["opType"] as string;
            log.LogInformation(opType!);

            var twin = await registryManager.GetTwinAsync(deviceId);
            if (twin == null)
            {
                log.LogInformation($"Device {deviceId} does not exist.");
                return;
            }

            var location = twin.Tags["Location"].Value as string;
            var macAddress = twin.Tags["MacAddress"].Value as string;
            var computer = computerDao.Get(location, macAddress);

            if (computer == null)
            {
                log.LogInformation($"Cannot get computer in {location} with {macAddress}");
                return;
            }

            if (opType!.Equals("deviceConnected") || opType!.Equals("deviceDisconnected"))
            {
                computer.IsOnline = opType!.Equals("deviceConnected");
                //Offline imply ssh disconnect.
                if (opType!.Equals("deviceDisconnected")) computer.IsConnected = false;
                computerDao.Update(computer);
            }
            else if (opType!.Equals("updateTwin"))
            {
                computer.IsOnline = true;
                if (twin.Properties.Reported.Contains("isSshConnected") &&
                    twin.Properties.Reported["isSshConnected"] != null)
                    computer.IsConnected = Convert.ToBoolean(twin.Properties.Reported!["isSshConnected"].Value);
                if (twin.Properties.Reported.Contains("lastErrorMessage") &&
                    twin.Properties.Reported["lastErrorMessage"] != null)
                    computer.LastErrorMessage = twin.Properties.Reported!["lastErrorMessage"].Value;
                computerDao.Update(computer);
            }
        }
    }
}