using FlowBroker.Core.Utils.Buffer;
using FlowBroker.Main.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBroker.Test.DataProcessing;

public class DynamicBufferTests
{
    [Fact]
    public void TestDynamicBuffer()
    {
        var dynamicBuffer = new DynamicBuffer();
        var random = new Random();
        var randomData = new List<string>();

        for (var i = 0; i <= 1000; i++)
        {
            var randomStringLength = random.Next(0, 100);
            randomData.Add(RandomGenerator.GenerateString(randomStringLength, random));
        }

        foreach (var randomItem in randomData)
        {
            var size = BitConverter.GetBytes(randomItem.Length);

            dynamicBuffer.Write(size);
            dynamicBuffer.Write(Encoding.UTF8.GetBytes(randomItem));
        }

        foreach (var randomItem in randomData)
        {
            var canRead = dynamicBuffer.CanRead(4);

            Assert.True(canRead);

            var sizeB = dynamicBuffer.ReadAndClear(4);
            var size = BitConverter.ToInt32(sizeB);

            var data = dynamicBuffer.ReadAndClear(size);

            Assert.Equal(randomItem, Encoding.UTF8.GetString(data));
        }
    }
}
