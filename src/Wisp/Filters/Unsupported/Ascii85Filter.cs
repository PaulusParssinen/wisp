namespace Wisp.Filters;

public sealed class Ascii85Filter : IFilter
{
    public string Name { get; } = "ASCII85Decode";
    public bool Supported { get; } = false;

    public byte[] Decode(ReadOnlySpan<byte> data, CosDictionary? parameters)
    {
        throw new NotSupportedException();
    }
}