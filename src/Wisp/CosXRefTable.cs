namespace Wisp;

public sealed class CosXRefTable : IEnumerable<ICosXRef>
{
    private readonly Dictionary<CosObjectId, ICosXRef> _lookup;
    private readonly List<ICosXRef> _references;
    private int _highestId;

    public CosXRefTable()
    {
        _lookup = new Dictionary<CosObjectId, ICosXRef>(CosObjectIdComparer.Shared);
        _references = [];
    }

    public CosObjectId GetNextId() => new CosObjectId(++_highestId, 0);

    public ICosXRef? GetXRef(CosObjectId key) => _lookup.GetValueOrDefault(key);

    public bool Contains(CosObjectId id) => _lookup.ContainsKey(id);

    internal CosXRefTable Clone()
    {
        var result = new CosXRefTable();
        foreach (var entry in this)
        {
            result.Add(entry);
        }

        return result;
    }

    internal CosXRefTable Merge(CosXRefTable other)
    {
        ArgumentNullException.ThrowIfNull(other);

        var result = new CosXRefTable();
        foreach (var entry in this)
        {
            result.Add(entry);
        }

        foreach (var entry in other)
        {
            result.Add(entry);
        }

        return result;
    }

    internal bool Add(ICosXRef reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        if (!_lookup.TryAdd(reference.Id, reference))
        {
            return false;
        }

        // Highest number?
        if (reference.Id.Number > _highestId)
        {
            _highestId = reference.Id.Number;
        }

        _references.Add(reference);
        return true;
    }

    internal bool Remove(CosObjectId id)
    {
        var reference = _references.FirstOrDefault(x => x.Id.Equals(id));
        if (reference is not null)
        {
            _references.Remove(reference);
            _lookup.Remove(id);
            return true;
        }

        return false;
    }

    public IEnumerator<ICosXRef> GetEnumerator() => _references.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}