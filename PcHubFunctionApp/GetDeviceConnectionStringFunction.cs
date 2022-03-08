using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using PcHubFunctionApp.Model;
using PcHubFunctionApp.Dao;
using PcHubFunctionApp.Helper;

namespace PcHubFunctionApp;

// ReSharper disable once UnusedMember.Global
public static class GetDeviceConnectionStringFunction
{
    [FunctionName(nameof(GetDeviceConnectionStringFunction))]
    // ReSharper disable once UnusedMember.Global
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
        HttpRequest req,
        ExecutionContext context,
        ILogger log)
    {
        log.LogInformation("GetDeviceConnectionString HTTP trigger function processed a request.");

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

        var registryManager =
            RegistryManager.CreateFromConnectionString(config.GetConfig(Config.Key.IotHubPrimaryConnectionString));

        string ioTConnectionString;
        var device = await registryManager.GetDeviceAsync(computer.GetIoTDeviceId());
        if (device == null) //Take care the case of deleting IoT device out of the system.
            ioTConnectionString = await SetupIoTDevice(computer, registryManager, config, log);
        else
            ioTConnectionString =
                $"HostName={config.GetConfig(Config.Key.IotHubName)}.azure-devices.net;DeviceId={computer.GetIoTDeviceId()};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}";

        if (computerDao.IsNew(computer))
        {
            log.LogInformation("New computer.");
            computer.IoTConnectionString = ioTConnectionString;
            computerDao.Add(computer);
        }
        else
        {
            if (ioTConnectionString != null)
            {
                log.LogInformation("Get and update ioTConnectionString.");
                var previousComputer = computerDao.Get(computer.PartitionKey, computer.RowKey);
                previousComputer.IoTConnectionString = ioTConnectionString;
                computerDao.Update(previousComputer);
            }
        }

        return new OkObjectResult(ioTConnectionString);
    }

    private static async Task<string> SetupIoTDevice(Computer computer, RegistryManager registryManager, Config config,
        ILogger log)
    {
        var device = new Device(computer.GetIoTDeviceId());
        var deviceWithKeys = await registryManager.AddDeviceAsync(device);

        var twin = await registryManager.GetTwinAsync(computer.GetIoTDeviceId());
        var patch =
            $@"{{
                    tags:{computer.ToJson()}                   
                }}";
        log.LogInformation(patch);
        await registryManager.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag);

        return
            $"HostName={config.GetConfig(Config.Key.IotHubName)}.azure-devices.net;DeviceId={computer.GetIoTDeviceId()};SharedAccessKey={deviceWithKeys.Authentication.SymmetricKey.PrimaryKey}";
    }
}