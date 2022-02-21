using System;
using System.Runtime.Serialization;
using Azure;
using Azure.Data.Tables;
using Common.Model;

namespace PcReservationFunctionApp.Model;

[DataContract]
public class SshConnection : JsonBase<SshConnection> , ITableEntity
{

    [DataMember(Name = "EMAIL", EmitDefaultValue = false)] public string Email { get; set; }
    [DataMember(Name = "LAB", EmitDefaultValue = false)] public string Lab { get; set; }
    [DataMember(Name = "LOCATION", EmitDefaultValue = false)] public string Location { get; set; }
    [DataMember] public string IpAddress { get; set; }
    [DataMember] public int Port { get; set; }
    [DataMember] public string Username { get; set; }
    [DataMember] public string Password { get; set; }

    public override bool Equals(object obj)
    {
        return obj != null && ((obj as SshConnection)!).ToString().Equals(ToString());
    }

    public override int GetHashCode()
    {
        return (Email + Lab + IpAddress + Port + Username + Password).GetHashCode();
    }

    public override string ToString()
    {
        return $"{Email} {Lab} {IpAddress}:{Port} => {Username} : {Password}";
    }

    [IgnoreDataMember] public string PartitionKey { get; set; }
    [IgnoreDataMember] public string RowKey { get; set; }
    [IgnoreDataMember] public DateTimeOffset? Timestamp { get; set; }
    [IgnoreDataMember] public ETag ETag { get; set; }
}