using Common.Model;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

RegistryManager registryManager;
string connString = "HostName=k2ga-lab-pc-ioTHub.azure-devices.net;SharedAccessKeyName=lab-pc;SharedAccessKey=yp3lAZHr8JIztlPmJKYJmLeoi2qNUzG7jiC7yl1YEz8=";
ServiceClient client;
string targetDevice = "JKC99PHHWCV9TSKDQPAV6KCMVPTD56F98NJ87WGQN25Y9HQQ25R0";

async Task QueryTwinRebootReported()
{
    Twin twin = await registryManager.GetTwinAsync(targetDevice);
    Console.WriteLine(twin.Properties.Reported.ToJson());
}

async Task StartReboot()
{
    client = ServiceClient.CreateFromConnectionString(connString);
    CloudToDeviceMethod method = new CloudToDeviceMethod("OnNewSshMessage");
    method.ResponseTimeout = TimeSpan.FromSeconds(30);
    var session = new Session("124.0.1.463", 22, "usus", "sasas");
    method.SetPayloadJson(session.ToJson());

    CloudToDeviceMethodResult result = await
    client.InvokeDeviceMethodAsync(targetDevice, method);

    Console.WriteLine("Invoked firmware update on device.");
}

registryManager = RegistryManager.CreateFromConnectionString(connString);
StartReboot().Wait();
StartReboot().Wait();
StartReboot().Wait();
QueryTwinRebootReported().Wait();
Console.WriteLine("Press ENTER to exit.");
Console.ReadLine();
