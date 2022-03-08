using System;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using PcHubFunctionApp.Model;
using Session = Common.Model.Session;

namespace PcHubFunctionApp.Helper;

internal static class Azure
{
    public static async Task<IAzure> Get()
    {
        var defaultCredential = new DefaultAzureCredential();
        var defaultToken = (await defaultCredential
            .GetTokenAsync(new TokenRequestContext(new[] {"https://management.azure.com/.default"}))).Token;
        var defaultTokenCredentials = new TokenCredentials(defaultToken);
        var azureCredentials = new AzureCredentials(defaultTokenCredentials, defaultTokenCredentials, null,
            AzureEnvironment.AzureGlobalCloud);

        var azure = await Microsoft.Azure.Management.Fluent.Azure.Authenticate(azureCredentials)
            .WithDefaultSubscriptionAsync();
        return azure;
    }

    public static async Task<bool> ChangeSshConnectionToDevice(Config config, ILogger log, string deviceId,
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
    public static string Base64Encode(string plainText)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }
}