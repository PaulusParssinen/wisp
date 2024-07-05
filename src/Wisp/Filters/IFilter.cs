namespace Wisp.Filters;

public interface IFilter
{
    string Name { get; }
    bool Supported { get; }

    byte[] Decode(ReadOnlySpan<byte> data, CosDictionary? parameters);

    public static byte[] Decode(CosStream stream, byte[] data)
    {
        var pipeline = FilterPipeline.Factory.Create(stream);
        return pipeline.Decode(data, stream.DecodeParms);
    }
}