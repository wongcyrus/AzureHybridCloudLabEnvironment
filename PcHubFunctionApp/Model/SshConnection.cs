using System;
using System.Runtime.Serialization;
using Azure;
using Azure.Data.Tables;
using Common.Model;

namespace PcHubFunctionApp.Model;

[DataContract]
public class SshConnection : JsonBase<SshConnection>, ITableEntity
{
    [DataMember(Name = "LOCATION", EmitDefaultValue = false)]
    public string Location { get; set; } //PartitionKey

    [DataMember(Name = "EMAIL", EmitDefaultValue = false)]
    public string Email { get; set; } //RowKey

    [DataMember(Name = "LAB", EmitDefaultValue = false)]
    public string Lab { get; set; }

    [DataMember] public string IpAddress { get; set; }
    [DataMember] public int Port { get; set; }
    [DataMember] public string Username { get; set; }
    [DataMember] public string Password { get; set; }
    [DataMember] public string Status { get; set; }
    [DataMember] public string Variables { get; set; }

    [DataMember] public string MacAddress { get; set; }
    [DataMember] public string PartitionKey { get; set; }
    [DataMember] public string RowKey { get; set; }
    [DataMember] public DateTimeOffset? Timestamp { get; set; }
    [DataMember] public ETag ETag { get; set; }

    public override string ToString()
    {
        return $"{Email} {Lab} {Location} {IpAddress}:{Port} => {Username} : {Password}";
    }
}