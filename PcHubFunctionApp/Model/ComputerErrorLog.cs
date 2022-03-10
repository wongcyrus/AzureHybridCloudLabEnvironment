using System;
using Azure;
using Azure.Data.Tables;

namespace PcHubFunctionApp.Model
{
    internal class ComputerErrorLog: ITableEntity
    {
        public string Location { get; set; } 
        public string MacAddress { get; set; }
        public string DeviceId { get; set; } //PartitionKey
        public string IpAddress { get; set; }
        public string MachineName { get; set; }
        public bool IsOnline { get; set; }
        public bool IsReserved { get; set; }
        public bool IsConnected { get; set; }
        public string ErrorMessage { get; set; }
        public string IoTConnectionString { get; set; }
        public string Email { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }  //RowKey
        public ETag ETag { get; set; }
    }
}
