using System.Runtime.Serialization;
using Common.Model;

namespace PcReservationFunctionApp.Model;

[DataContract]
public class SshProxy : JsonBase<SshProxy>
{
    public SshProxy(string email, string lab, string ipAddress, int port, string username, string password)
    {
        Email = email;
        Lab = lab;
        IpAddress = ipAddress;
        Port = port;
        Username = username;
        Password = password;
    }

    [DataMember(Name = "EMAIL", EmitDefaultValue = false)] public string Email { get; set; }
    [DataMember(Name = "LAB", EmitDefaultValue = false)] public string Lab { get; set; }
    [DataMember] public string IpAddress { get; set; }
    [DataMember] public int Port { get; set; }
    [DataMember] public string Username { get; set; }
    [DataMember] public string Password { get; set; }

    public override bool Equals(object? obj)
    {
        return obj != null && ((obj as SshProxy)!).ToString().Equals(ToString());
    }

    public override int GetHashCode()
    {
        return (Email + Lab + IpAddress + Port + Username + Password).GetHashCode();
    }

    public override string ToString()
    {
        return $"{Email} {Lab} {IpAddress}:{Port} => {Username} : {Password}";
    }
}