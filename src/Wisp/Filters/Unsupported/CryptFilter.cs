namespace Wisp.Filters;

public sealed class CryptFilter : IFilter
{
    public string Name { get; } = "Crypt";
    public bool Supported { get; } = false;

    public byte[] Decode(ReadOnlySpan<byte> data, CosDictionary? parameters)
    {
        throw new NotSupportedException();
    }
}