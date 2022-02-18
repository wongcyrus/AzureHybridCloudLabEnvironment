using System;
using Azure;
using Azure.Data.Tables;

namespace PcReservationFunctionApp.Model;

internal class SshConnection : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}