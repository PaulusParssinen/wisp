namespace Wisp.Filters;

public sealed class RunLengthFilter : IFilter
{
    public string Name { get; } = "RunLengthDecode";
    public bool Supported { get; } = false;

    public byte[] Decode(ReadOnlySpan<byte> data, CosDictionary? parameters)
    {
        throw new NotSupportedException();
    }
}