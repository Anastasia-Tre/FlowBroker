﻿using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBroker.Core.Utils.BinarySerialization;

public class MessagePackHelper
{
    public static byte[] ObjectToByteArray<T>(T obj)
    {
        return MessagePackSerializer.Serialize(obj);
    }

    public static T ByteArrayToObject<T>(byte[] arrBytes)
    {
        return MessagePackSerializer.Deserialize<T>(arrBytes);
    }

    public static object ByteArrayToObject(byte[] arrBytes, Type type)
    {
        return MessagePackSerializer.Deserialize(type, arrBytes);
    }

}