using System;
using Azure;
using Azure.Data.Tables;
using Common.Model;
using PcReservationFunctionApp.Helper;

namespace PcReservationFunctionApp.Model;

public class Computer : JsonBase<Computer>, ITableEntity
{
    public string Location { get; set; } //PartitionKey
    public string MacAddress { get; set; }//RowKey
    public string DeviceId { get; set; }
    public string IpAddress { get; set; }
    public string MachineName { get; set; }
    public bool IsOnline { get; set; }
    public bool IsConnected { get; set; }
    public string LastErrorMessage { get; set; }
    public string IoTConnectionString { get; set; }
    public string Email { get; set; }
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public override string ToString()
    {
        return this.Dump();
    }
}