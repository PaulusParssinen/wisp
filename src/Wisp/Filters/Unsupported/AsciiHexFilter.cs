namespace Wisp.Filters;

public sealed class AsciiHexFilter : IFilter
{
    public string Name { get; } = "ASCIIHexDecode";
    public bool Supported { get; } = false;

    public byte[] Decode(ReadOnlySpan<byte> data, CosDictionary? parameters)
    {
        throw new NotSupportedException();
    }
}