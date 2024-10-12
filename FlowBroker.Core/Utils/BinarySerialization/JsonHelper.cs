using System.Text;
using System.Text.Json;

namespace FlowBroker.Core.Utils.BinarySerialization;

public class JsonHelper
{
    public static byte[] ObjectToByteArray<T>(T obj)
    {
        return JsonSerializer.SerializeToUtf8Bytes(obj);
    }

    public static byte[] ObjectToByteArray(object obj, Type type)
    {
        return JsonSerializer.SerializeToUtf8Bytes(obj, type);
    }

    public static T ByteArrayToObject<T>(byte[] arrBytes)
    {
        return JsonSerializer.Deserialize<T>(arrBytes);
    }

    public static object ByteArrayToObject(byte[] arrBytes, Type type)
    {
        var bytesAsString = Encoding.UTF8.GetString(arrBytes);
        //return JsonSerializer.Deserialize(arrBytes, type);
        return JsonSerializer.Deserialize(bytesAsString, type);
    }
}
