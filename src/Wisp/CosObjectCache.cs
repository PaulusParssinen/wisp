namespace Wisp;

internal sealed class CosObjectCache(CosXRefTable table, CosObjectResolver? resolver) : ICosObjectCache
{
    private readonly CosXRefTable _table = table;
    private readonly CosObjectResolver? _resolver = resolver;
    private readonly Dictionary<CosObjectId, CosObject> _objects = new(CosObjectIdComparer.Shared);

    public bool Contains(CosObjectId id) => _objects.ContainsKey(id);

    public bool TryGet(CosObjectId id, [NotNullWhen(true)] out CosObject? obj) => TryGet(id, CosResolveFlags.None, out obj);
    public bool TryGet(CosObjectId id, CosResolveFlags flags, [NotNullWhen(true)] out CosObject? obj)
    {
        obj = null;

        var shouldInvalidate = flags.HasFlag(CosResolveFlags.Invalidate);
        if (!shouldInvalidate)
        {
            // Try get the object from caches
            if (!_objects.TryGetValue(id, out obj))
                return false;
        }

        // Should we try to resolve the object from the PDF document stream?
        var shouldResolve = !flags.HasFlag(CosResolveFlags.NoResolve);
        if (shouldResolve && _resolver is not null)
        {
            if (!_resolver.TryGetObject(this, id, out var owner, out obj))
                return false;

            // Should we add the resolved object to the cache?
            var shouldCache = !flags.HasFlag(CosResolveFlags.NoCache);
            if (shouldCache)
            {
                _objects.TryAdd(id, obj);

                if (owner is not null)
                {
                    _objects.TryAdd(owner.Id, owner);
                }
            }
            return true;
        }

        return false;
    }

    public void Set(CosObject obj)
    {
        _objects[obj.Id] = obj;

        if (!_table.Contains(obj.Id))
        {
            _table.Add(new CosIndirectXRef(obj.Id));
        }
    }

    public IEnumerator<CosObject> GetEnumerator() => new Enumerator(this, _table);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class Enumerator : IEnumerator<CosObject>
    {
        private readonly CosObjectCache _collection;
        private readonly CosXRefTable _table;
        private IEnumerator<ICosXRef> _source;
        private CosObject? _current;

        public CosObject Current => _current ?? throw new InvalidOperationException("Current cannot be used in the current state");

        object IEnumerator.Current => Current;

        public Enumerator(
            CosObjectCache collection,
            CosXRefTable table)
        {
            _collection = collection;
            _table = table;
            _source = _table.GetEnumerator();
        }

        public void Dispose() => _source.Dispose();

        public void Reset()
        {
            _source.Dispose();
            _source = _table.GetEnumerator();
        }

        public bool MoveNext()
        {
            while (true)
            {
                if (!_source.MoveNext()) return false;

                var current = _source.Current;
                if (current is CosIndirectXRef xref)
                {
                    if (_collection.TryGet(xref.Id, CosResolveFlags.NoCache, out var obj))
                    {
                        _current = obj;
                        return true;
                    }
                }
            }
        }
    }
}

[Flags]
public enum CosResolveFlags
{
    None = 0,

    /// <summary>
    /// If an objects is cached, skip the cache
    /// completely when retrieving it.
    /// Can only be used when working with a loaded document.
    /// </summary>
    Invalidate = 1 << 0,

    /// <summary>
    /// An resolved objects will not be added
    /// to the object cache.
    /// Can only be used when working with a loaded document.
    /// </summary>
    NoCache = 1 << 1,

    /// <summary>
    /// If the document is loaded, the object will not
    /// be resolved from the document stream.
    /// </summary>
    NoResolve = 1 << 2,
}

public interface ICosObjectCache : IEnumerable<CosObject>
{
    bool Contains(CosObjectId id);
    bool TryGet(CosObjectId id, CosResolveFlags flags, [NotNullWhen(true)] out CosObject? obj);
    void Set(CosObject obj);
}

public static class ICosObjectCacheExtensions
{
    public static bool TryGet(this ICosObjectCache collection, int number, int generation, [NotNullWhen(true)] out CosObject? obj, CosResolveFlags flags = CosResolveFlags.None)
    {
        return collection.TryGet(new CosObjectId(number, generation), flags, out obj);
    }

    public static bool TryGet(this ICosObjectCache collection, CosObjectReference reference, [NotNullWhen(true)] out CosObject? obj, CosResolveFlags flags = CosResolveFlags.None)
    {
        return collection.TryGet(new CosObjectId(reference.Id.Number, reference.Id.Generation), flags, out obj);
    }
}