namespace Wisp;

[DebuggerDisplay("{ToString(),nq}")]
[CosPrimitive]
public sealed partial class CosArray : ICosPrimitive, IEnumerable<ICosPrimitive>
{
    private readonly List<ICosPrimitive> _items;

    public int Count => _items.Count;

    public ICosPrimitive this[int index] => _items[index];

    public CosArray()
    {
        _items = [];
    }

    public CosArray(IEnumerable<ICosPrimitive> items)
    {
        _items = new List<ICosPrimitive>(items);
    }

    public void Add(ICosPrimitive item)
    {
        _items.Add(item);
    }

    public ICosPrimitive? GetAt(int index)
    {
        if (index >= _items.Count)
        {
            return null;
        }

        return _items[index];
    }

    public IEnumerator<ICosPrimitive> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => $"[Array] Count = {_items.Count}";
}

public static class CosArrayExtensions
{
    public static T? GetAt<T>(this CosArray array, int index)
        where T : ICosPrimitive
    {
        var obj = array.GetAt(index);
        if (obj is null)
        {
            return default;
        }

        if (obj is not T item)
        {
#if DEBUG
            throw new WispException(
                $"Expected object at #{index} to be of type '{typeof(T).Name}', " +
                $"but it was of type '{obj.GetType().Name}'");
#else
            return default;
#endif
        }

        return item;
    }
}