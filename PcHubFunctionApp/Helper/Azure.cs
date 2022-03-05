using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Rest;

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
}