namespace Wisp.Filters;

public sealed class Jbig2Filter : IFilter
{
    public string Name { get; } = "JBIG2Decode";
    public bool Supported { get; } = false;

    public byte[] Decode(ReadOnlySpan<byte> data, CosDictionary? parameters)
    {
        throw new NotSupportedException();
    }
}