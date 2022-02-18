using System.Runtime.Serialization;

namespace Common.Model
{
    [DataContract]
    public class SshConnection : JsonBase<SshConnection>
    {
        public SshConnection(string email, string lab, string ipAddress, int port, string username, string password)
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
            return obj != null && ((obj as Session)!).ToString().Equals(ToString());
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
}
