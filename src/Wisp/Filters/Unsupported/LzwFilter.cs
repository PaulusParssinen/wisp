namespace Wisp.Filters;

public sealed class LzwFilter : IFilter
{
    public string Name { get; } = "LZWDecode";
    public bool Supported { get; } = false;

    public byte[] Decode(ReadOnlySpan<byte> data, CosDictionary? parameters)
    {
        throw new NotSupportedException();
    }
}