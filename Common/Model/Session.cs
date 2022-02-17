using System.Runtime.Serialization;

namespace Common.Model
{
    [DataContract]
    public class Session : JsonBase<Session>
    {
        public Session(string ipAddress, int port, string username, string password, DateTime startTime, DateTime endTime)
        {
            IpAddress = ipAddress;
            Port = port;
            Username = username;
            Password = password;
            StartTime = startTime;
            EndTime = endTime;
        }

        [DataMember] public string IpAddress { get; set; }
        [DataMember] public int Port { get; set; }
        [DataMember] public string Username { get; set; }
        [DataMember] public string Password { get; set; }

        [DataMember] public DateTime StartTime { get; set; }
        [DataMember] public DateTime EndTime { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            return ((obj as Session)!).ToString().Equals(ToString());
        }

        public override int GetHashCode()
        {
            return (IpAddress + Port + Username + Password).GetHashCode();
        }

        public override string ToString()
        {
            return $"{IpAddress}:{Port} => {Username} : {Password}";
        }
    }
}
