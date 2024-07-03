namespace Wisp.Filters;

public sealed class DctFilter : IFilter
{
    public string Name { get; } = "DCTDecode";
    public bool Supported { get; } = false;

    public byte[] Decode(ReadOnlySpan<byte> data, CosDictionary? parameters)
    {
        throw new NotSupportedException();
    }
}