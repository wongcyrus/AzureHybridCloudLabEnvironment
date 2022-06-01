import { Construct } from "constructs";
import { App, TerraformOutput, TerraformStack } from "cdktf";
import { AzurermProvider, ResourceGroup, StorageAccount, StorageQueue, StorageTable } from "azure-common-construct/.gen/providers/azurerm";
import { StringResource } from 'azure-common-construct/.gen/providers/random'
// import { DataExternal } from "azure-common-construct/.gen/providers/external";
import { AzureFunctionLinuxConstruct } from "azure-common-construct/patterns/AzureFunctionLinuxConstruct";
import { AzureIotEventHubConstruct } from "azure-common-construct/patterns/AzureIotEventHubConstruct";
import { AzureStaticConstainerConstruct } from "azure-common-construct/patterns/AzureStaticConstainerConstruct";

import * as path from "path";
import * as dotenv from 'dotenv';
dotenv.config({ path: __dirname + '/.env' });

class AzureHybridCloudLabEnvironmentStack extends TerraformStack {
  constructor(scope: Construct, name: string) {
    super(scope, name);

    new AzurermProvider(this, "AzureRm", {
      features: {}
    })

    const prefix = "AzureHybridCloudLab"
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
      name: suffix.result,
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
      "EmailSmtp": process.env.EMAIL_SMTP!,
      "EmailUserName": process.env.EMAIL_USERNAME!,
      "EmailPassword": process.env.EMAIL_PASSWORD!,
      "EmailFromAddress": process.env.EMAIL_FROM_ADDRESS!,
      "AdminEmail": process.env.ADMIN_EMAIL!,
      "Salt": prefix,
      "StorageAccountName": storageAccount.name,
      "StorageAccountKey": storageAccount.primaryAccessKey
    }

    const azureFunctionConstruct = new AzureFunctionLinuxConstruct(this, "AzureFunctionConstruct", {
      environment,
      prefix,
      resourceGroup,
      appSettings,
      vsProjectPath: path.join(__dirname, "../", "PcHubFunctionApp")
    })

    // const psScriptPath = path.join(__dirname, "GetFunctionKey.ps1");
    // const getDeviceConnectionStringFunctionKeyExternal = new DataExternal(this, "GetDeviceConnectionStringFunctionKeyExternal", {
    //   program: ["PowerShell", psScriptPath],
    //   query: {
    //     resourceGroup: resourceGroup.name,
    //     functionAppName: azureFunctionConstruct.functionApp.name,
    //     functionName: "GetDeviceConnectionStringFunction"
    //   }
    // })
    // const getDeviceConnectionStringFunctionKey = getDeviceConnectionStringFunctionKeyExternal.result.lookup("default")

    // const addSshConnectionFunctionKeyExternal = new DataExternal(this, "AddSshConnectionFunctionKeyExternal", {
    //   program: ["PowerShell", psScriptPath],
    //   query: {
    //     resourceGroup: resourceGroup.name,
    //     functionAppName: azureFunctionConstruct.functionApp.name,
    //     functionName: "GetDeviceConnectionStringFunction"
    //   }
    // })
    // const addSshConnectionFunctionKey = addSshConnectionFunctionKeyExternal.result.lookup("default")

    // new TerraformOutput(this, "GetDeviceConnectionStringFunctionKey", {
    //   value: getDeviceConnectionStringFunctionKey
    // });
    // new TerraformOutput(this, "AddSshConnectionFunctionKey", {
    //   value: addSshConnectionFunctionKey
    // });

    new TerraformOutput(this, "FunctionAppHostname", {
      value: azureFunctionConstruct.functionApp.name
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
