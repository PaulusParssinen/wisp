namespace Wisp;

[DebuggerDisplay("{ToString(),nq}")]
[CosPrimitive]
public sealed partial class CosObjectStream : ICosPrimitive
{
    private readonly CosStream _stream;
    private readonly List<(int Id, long Offset)> _offsetsByIndex = [];
    private readonly Dictionary<int, long> _offsetsById = new();
    private readonly HashSet<int> _unpackedObjects = [];
    private bool _unpacked;

    /// <summary>
    /// Gets the number of indirect objects stored in the stream.
    /// </summary>
    public int N => _stream.Dictionary.Get<CosInteger>(CosNames.N)?.IntValue ?? 0;

    public CosDictionary Dictionary => _stream.Dictionary;

    public CosObjectStream(CosStream stream)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }

    public CosObject GetObject(ICosObjectCache cache, CosObjectId id)
    {
        if (_unpacked)
        {
            if (!_offsetsById.ContainsKey(id.Number))
            {
                throw new WispException($"Object #{id.Number} does not exist within object stream");
            }

            if (_unpackedObjects.Contains(id.Number))
            {
                // Ok, we know the ID, does this object exist in the cache?
                // Try to get it without resolving (since we don't want a stack overflow).
                if (cache.TryGet(id, CosResolveFlags.NoResolve, out var cachedObj))
                {
                    return cachedObj;
                }
            }
        }

        var bytes = _stream.GetUnfilteredData() ?? throw new WispException("Stream contained no data");
        
        var parser = new CosParser(bytes, isStreamObject: true);
        EnsureOffsetsHaveBeenPopulated(parser);

        // Ensure the object exist within the stream
        if (!_offsetsById.TryGetValue(id.Number, out var offset))
        {
            throw new WispException($"Object #{id.Number} does not exist within object stream");
        }

        // Read the object at the correct offset
        parser.Seek(offset, SeekOrigin.Begin);
        var primitive = parser.Parse();
        var obj = new CosObject(id, primitive);
        _unpackedObjects.Add(id.Number);

        return obj;
    }

    public CosObject GetObjectByIndex(ICosObjectCache cache, int index)
    {
        var bytes = _stream.GetUnfilteredData() ?? throw new WispException("Stream contained no data");

        var parser = new CosParser(bytes, isStreamObject: true);
        EnsureOffsetsHaveBeenPopulated(parser);

        if (index >= _offsetsByIndex.Count)
        {
            throw new WispException(
                $"Object with index {index} does not exist within object stream");
        }

        // Read the object at the correct offset
        var (number, offset) = _offsetsByIndex[index];
        var id = new CosObjectId(number, 0);

        // Consider this object to be unpacked
        // Just in case it's already been cached.
        _unpackedObjects.Add(id.Number);

        // Ok, we know the ID, does this object exist in the cache?
        // Try to get it without resolving (since we don't want a stack overflow).
        if (cache.TryGet(id, CosResolveFlags.NoResolve, out var obj))
        {
            return obj;
        }

        // Find the object and parse it
        parser.Seek(offset, SeekOrigin.Begin);
        var primitive = parser.Parse();
        obj = new CosObject(id, primitive);

        return obj;
    }

    public CosObjectId GetObjectIdByIndex(int index)
    {
        var bytes = _stream.GetUnfilteredData() ?? throw new WispException("Stream contained no data");
        if (!_unpacked)
        {
            var parser = new CosParser(bytes, isStreamObject: true);
            EnsureOffsetsHaveBeenPopulated(parser);

            return new CosObjectId(_offsetsByIndex[index].Id, 0);
        }

        return new CosObjectId(_offsetsByIndex[index].Id, 0);
    }

    internal List<int> GetObjectIds()
    {
        var bytes = _stream.GetUnfilteredData() ?? throw new WispException("Object stream contained no data");
        if (!_unpacked)
        {
            var parser = new CosParser(bytes, isStreamObject: true);
            EnsureOffsetsHaveBeenPopulated(parser);

            return _offsetsById.Keys.ToList();
        }

        return _offsetsById.Keys.ToList();
    }

    private void EnsureOffsetsHaveBeenPopulated(CosParser parser)
    {
        if (_unpacked) return;

        var objectOffset = _stream.Dictionary.Get<CosInteger>(CosNames.First) 
            ?? throw new WispException("Object stream is missing /First parameter");
        
        for (var i = 0; i < N; i++)
        {
            if (!parser.CanRead)
            {
                throw new WispException("Encountered premature end of object stream");
            }

            var id = (int)((CosInteger)parser.Parse()).Value;
            var offset = objectOffset.Value + ((CosInteger)parser.Parse()).Value;

            _offsetsById.Add(id, offset);
            _offsetsByIndex.Add((id, offset));
        }

        _unpacked = true;
    }

    public override string ToString() => $"[ObjectStream] Objects = {N}, Size = {_stream.Length}";
}