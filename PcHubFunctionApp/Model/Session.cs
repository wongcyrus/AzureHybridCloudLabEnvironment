using System;
using Azure;
using Azure.Data.Tables;

namespace PcHubFunctionApp.Model;

internal class Session : ITableEntity
{
    public string Location { get; set; } //PartitionKey
    public string MacAddress { get; set; } //RowKey
    public string Email { get; set; }

    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}