namespace Wisp.Filters;

public sealed class JpxFilter : IFilter
{
    public string Name { get; } = "JPXDecode";
    public bool Supported { get; } = false;

    public byte[] Decode(ReadOnlySpan<byte> data, CosDictionary? parameters)
    {
        throw new NotSupportedException();
    }
}