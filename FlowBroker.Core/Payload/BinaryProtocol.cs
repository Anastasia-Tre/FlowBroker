namespace FlowBroker.Core.Payload;

public static class BinaryProtocolConfiguration
{
    public const int PayloadHeaderSize = 4;

    public const int ReceiveDataSize = 1024;

    public const int SizeForInt = 4;

    public const int SizeForGuid = 16;

    public const int SizeForNewLine = 1;
}
