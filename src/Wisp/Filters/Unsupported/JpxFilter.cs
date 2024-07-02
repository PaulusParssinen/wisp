namespace Wisp.Filters;

public sealed class JpxFilter : Filter
{
    public override string Name { get; } = "JPXDecode";
    public override bool Supported { get; } = false;

    public override byte[] Decode(byte[] data, CosDictionary? parameters)
    {
        throw new NotSupportedException();
    }
}