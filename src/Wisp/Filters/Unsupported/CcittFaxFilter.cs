namespace Wisp.Filters;

public sealed class CcittFaxFilter : IFilter
{
    public string Name { get; } = "CCITTFaxDecode";
    public bool Supported { get; } = false;

    public byte[] Decode(ReadOnlySpan<byte> data, CosDictionary? parameters)
    {
        throw new NotSupportedException();
    }
}