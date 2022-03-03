// See https://aka.ms/new-console-template for more information
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;


Console.WriteLine("Create Local Azure Storage Account resources.");

CloudStorageAccount storageAcc = CloudStorageAccount.Parse("UseDevelopmentStorage=true");

CloudTableClient cloudTableClient = storageAcc.CreateCloudTableClient();
var tables = new[] { "Computer", "SshConnection", "Session" };
foreach (var tableName in tables)
{
    var table = cloudTableClient.GetTableReference(tableName);
    await table.CreateIfNotExistsAsync();
}

var cloudQueueClient = storageAcc.CreateCloudQueueClient();
var queues = new[] { "retry" };
foreach (var queueName in queues)
{
    var queue = cloudQueueClient.GetQueueReference(queueName);
    await queue.CreateIfNotExistsAsync();
}

Console.WriteLine("Created Local Azure Storage Account resources.");

//CloudBlobClient cloudBlobClient = storageAcc.CreateCloudBlobClient();
//var blobs = new[] {  };

//foreach (var containerName in blobs)
//{
//    var container = cloudBlobClient.GetContainerReference(containerName);
//    await container.CreateIfNotExistsAsync();
//}


