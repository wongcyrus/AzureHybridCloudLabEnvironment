import { Construct } from "constructs";
import { App, TerraformOutput, TerraformStack } from "cdktf";
import { AzurermProvider, ResourceGroup, StorageAccount, StorageQueue, StorageTable } from "cdktf-azure-providers/.gen/providers/azurerm";
import { StringResource } from 'cdktf-azure-providers/.gen/providers/random'
import { AzureFunctionLinuxConstruct, PublishMode } from "azure-common-construct/patterns/AzureFunctionLinuxConstruct";
import { AzureIotEventHubConstruct } from "azure-common-construct/patterns/AzureIotEventHubConstruct";
import { AzureStaticConstainerConstruct } from "azure-common-construct/patterns/AzureStaticConstainerConstruct";

import * as path from "path";
import * as dotenv from 'dotenv';
dotenv.config({ path: __dirname + '/.env' });

class AzureHybridCloudLabEnvironmentStack extends TerraformStack {
  constructor(scope: Construct, name: string) {
    super(scope, name);

    new AzurermProvider(this, "AzureRm", {
      features: {
        resourceGroup: {
          preventDeletionIfContainsResources: false
        }
      }
    })

    const prefix = "AzureHybridLab"
    const environment = "dev"

    const resourceGroup = new ResourceGroup(this, "ResourceGroup", {
      location: "EastAsia",
      name: prefix + "ResourceGroup"
    })

    const azureIotConstruct = new AzureIotEventHubConstruct(this, "AzureIotEventHubConstruct", {
      environment,
      prefix,
      resourceGroup,
    })

    const azureStaticConstainerConstruct = new AzureStaticConstainerConstruct(this, "AzureStaticConstainerConstruct", {
      environment,
      prefix,
      resourceGroup,
      gitHubUserName: "wongcyrus",
      gitHubRepo: "ssh-tunneling-bastion",
      gitAccessToken: "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"
    })

    const suffix = new StringResource(this, "Random", {
      length: 5,
      special: false,
      lower: true,
      upper: false,
    })
    const storageAccount = new StorageAccount(this, "StorageAccount", {
      name: prefix.toLocaleLowerCase() + environment.toLocaleLowerCase() + suffix.result,
      location: resourceGroup.location,
      resourceGroupName: resourceGroup.name,
      accountTier: "Standard",
      accountReplicationType: "LRS"
    })

    const tables = ["Computer", "SshConnection", "ComputerErrorLog"];
    tables.map(t => new StorageTable(this, t + "StorageTable", {
      name: t,
      storageAccountName: storageAccount.name
    }))
    new StorageQueue(this, "StorageQueue", {
      name: "allocate-pc",
      storageAccountName: storageAccount.name
    })

    const appSettings = {
      "IotHubPrimaryConnectionString": azureIotConstruct.iothubPrimaryConnectionString,
      "EventHubPrimaryConnectionString": azureIotConstruct.eventhubPrimaryConnectionString,
      "EventHubName": azureIotConstruct.eventhub.name,
      "IotHubName": azureIotConstruct.iothub.name,
      "BastionArcAdminUsername": azureStaticConstainerConstruct.containerRegistry.adminUsername,
      "BastionArcAdminPassword": azureStaticConstainerConstruct.containerRegistry.adminPassword,
      "BastionArcLoginServer": azureStaticConstainerConstruct.containerRegistry.loginServer,
      "CommunicationServiceConnectionString":process.env.COMMUNICATION_SERVICE_CONNECTION_STRING!,
      "EmailSmtp": process.env.EMAIL_SMTP!,
      "EmailUserName": process.env.EMAIL_USERNAME!,
      "EmailPassword": process.env.EMAIL_PASSWORD!,
      "EmailFromAddress": process.env.EMAIL_FROM_ADDRESS!,
      "AdminEmail": process.env.ADMIN_EMAIL!,
      "Salt": prefix,
      "StorageAccountName": storageAccount.name,
      "StorageAccountKey": storageAccount.primaryAccessKey,
      "StorageAccountConnectionString": storageAccount.primaryConnectionString
    }

    const azureFunctionConstruct = new AzureFunctionLinuxConstruct(this, "AzureFunctionConstruct", {
      functionAppName: `ive-lab`,
      environment,
      prefix,
      resourceGroup,
      appSettings,
      vsProjectPath: path.join(__dirname, "..", "PcHubFunctionApp/"),
      publishMode: PublishMode.Always,
      functionNames:["GetDeviceConnectionStringFunction","AddSshConnectionFunction"]
    })   

    new TerraformOutput(this, "GetDeviceConnectionStringFunctionKey", {
      value: azureFunctionConstruct.functionKeys!["GetDeviceConnectionStringFunction"]
    });
    new TerraformOutput(this, "AddSshConnectionFunctionKey", {
      value: azureFunctionConstruct.functionKeys!["AddSshConnectionFunction"]
    });

    new TerraformOutput(this, "FunctionAppHostname", {
      value: azureFunctionConstruct.functionApp.name
    });

    new TerraformOutput(this, "AzureFunctionBaseUrl", {
      value: `https://${azureFunctionConstruct.functionApp.name}.azurewebsites.net`
    });

    new TerraformOutput(this, "LifeCycleHookUrl", {
      value: azureFunctionConstruct.functionUrls!["AddSshConnectionFunction"]
    });

    new TerraformOutput(this, "Environment", {
      value: environment
    });

    new TerraformOutput(this, "AzureWebJobsStorage", {
      sensitive: true,
      value: azureFunctionConstruct.storageAccount.primaryConnectionString
    });

    for (let [key, value] of Object.entries(appSettings)) {
      new TerraformOutput(this, key, {
        sensitive: true,
        value: value
      });
    }

  }
}

const app = new App({ skipValidation: true });
new AzureHybridCloudLabEnvironmentStack(app, "infrastructure");
app.synth();
