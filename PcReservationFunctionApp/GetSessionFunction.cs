using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
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
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
        HttpRequest req,
        ExecutionContext context,
        ILogger log)
    {
        log.LogInformation("GetSessionFunction HTTP trigger function processed a request.");

        var config = new Config(context);
        var computerDao = new ComputerDao(config, log);

        var computer = new Computer
        {
            Location = req.Query["Location"],
            IpAddress = req.Query["IpAddress"],
            MachineName = req.Query["MachineName"],
            DeviceId = req.Query["DeviceId"],
            MacAddress = req.Query["MacAddress"],
            IsConnected = false,
            IsOnline = false,
            IsReserved = false,
            LastErrorMessage = req.Query["LastErrorMessage"],
            PartitionKey = req.Query["Location"],
            RowKey = req.Query["MacAddress"]
        };

        var registryManager = RegistryManager.CreateFromConnectionString(config.GetConfig(Config.Key.IotHubPrimaryConnectionString));
        if (computerDao.IsNew(computer))
        {
            log.LogInformation("New computer.");
          
            await RegisterDevice(log, computer, registryManager, config);
            computerDao.Add(computer);
        }
        else
        {
            log.LogInformation("Get computer.");
            computer = computerDao.Get(computer.PartitionKey, computer.RowKey);
            var device = await registryManager.GetDeviceAsync(computer.GetIoTDeviceId());

            if (device == null) //Take care the case of deleting IoT device out of the system.
                await RegisterDevice(log, computer, registryManager, config);
            computerDao.Update(computer);
        }

        return new OkObjectResult(computer.IoTConnectionString);
    }

    private static async Task RegisterDevice(ILogger log, Computer computer, RegistryManager registryManager, Config config)
    {
        var device = new Device(computer.GetIoTDeviceId());
        var deviceWithKeys = await registryManager.AddDeviceAsync(device);
        computer.IoTConnectionString =
            $"HostName={config.GetConfig(Config.Key.IotHubName)}.azure-devices.net;DeviceId={device.Id};SharedAccessKey={deviceWithKeys.Authentication.SymmetricKey.PrimaryKey}";

        var twin = await registryManager.GetTwinAsync(device.Id);
        var patch =
            $@"{{
                    tags:{computer.ToJson()}                   
                }}";
        log.LogInformation(patch);
        await registryManager.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag);
    }
}