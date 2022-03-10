using System.Runtime.Serialization;

namespace Common.Model
{
    [DataContract]
    public class Session : JsonBase<Session>
    {
        public Session(string ipAddress, int port, string username, string password, string email, string lab)
        {
            IpAddress = ipAddress;
            Port = port;
            Username = username;
            Password = password;
            Email = email;
            Lab = lab;
        }

        [DataMember] public string IpAddress { get; set; }
        [DataMember] public int Port { get; set; }
        [DataMember] public string Username { get; set; }
        [DataMember] public string Password { get; set; }
        [DataMember] public string Email { get; set; }
        [DataMember] public string Lab { get; set; }

        public override bool Equals(object? obj)
        {
            return obj != null && ((obj as Session)!).ToString().Equals(ToString());
        }

        public override int GetHashCode()
        {
            return $"{Lab} {Email} {IpAddress}:{Port} => {Username} : {Password}".GetHashCode();
        }

        public override string ToString()
        {
            return $"{Lab} {Email} {IpAddress}:{Port} => {Username} : {Password}";
        }
    }
}
