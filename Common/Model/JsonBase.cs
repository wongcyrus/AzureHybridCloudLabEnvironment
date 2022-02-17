using Microsoft.Extensions.Logging;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Common.Model;

[DataContract]
public abstract class JsonBase<T> where T : class
{
    public string ToJson()
    {
        var serializer = new DataContractJsonSerializer(GetType());
        using var ms = new MemoryStream();
        serializer.WriteObject(ms, this);
        return Encoding.Default.GetString(ms.ToArray());
    }

    public static T? FromJson(string content, ILogger logger)
    {
        try
        {
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var ser = new DataContractJsonSerializer(typeof(T));
            var obj = ser.ReadObject(ms) as T;
            ms.Close();
            return obj;
        }
        catch (Exception e)
        {
            logger.LogError(message: e.ToString());
            return null;
        }
    }
}