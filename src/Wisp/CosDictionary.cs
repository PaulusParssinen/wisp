namespace Wisp;

[DebuggerDisplay("{ToString(),nq}")]
[CosPrimitive]
public sealed partial class CosDictionary : ICosPrimitive, IEnumerable<KeyValuePair<CosName, ICosPrimitive>>
{
    private readonly Dictionary<CosName, ICosPrimitive> _dictionary;

    public int Count => _dictionary.Count;

    public ICosPrimitive? this[string key]
    {
        get => this[new CosName(key)];
        set => this[new CosName(key)] = value;
    }

    public ICosPrimitive? this[CosName key]
    {
        get => Get(key);
        set => Set(key, value);
    }

    public CosDictionary()
    {
        _dictionary = new Dictionary<CosName, ICosPrimitive>(CosNameComparer.Shared);
    }

    public CosDictionary(CosDictionary dictionary)
        : this()
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        foreach (var (key, value) in dictionary)
        {
            Set(key, value);
        }
    }

    public bool ContainsKey(CosName key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _dictionary.ContainsKey(key);
    }

    public void Add(CosName key, ICosPrimitive value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        if (!_dictionary.TryAdd(key, value))
        {
            throw new WispException(
                "Another item with the same key already exist in the dictionary");
        }
    }

    public ICosPrimitive? Get(CosName key)
    {
        _dictionary.TryGetValue(key, out var value);
        return value;
    }

    public void Set(CosName key, ICosPrimitive? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        // Setting the value to null
        // removes the pair from the dictionary
        if (value is null)
        {
            _dictionary.Remove(key);
            return;
        }

        _dictionary[key] = value;
    }

    public bool Remove(CosName key) => _dictionary.Remove(key);

    public void Combine(CosDictionary other)
    {
        foreach (var kvp in other)
        {
            if (!ContainsKey(kvp.Key))
            {
                Set(kvp.Key, kvp.Value);
            }
        }
    }

    public bool TryGetValue(CosName key, [NotNullWhen(true)] out ICosPrimitive? obj)
    {
        return _dictionary.TryGetValue(key, out obj);
    }

    public IEnumerator<KeyValuePair<CosName, ICosPrimitive>> GetEnumerator() => _dictionary.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => $"[Dictionary] Count = {_dictionary.Count}";
}

public static class PdfDictionaryExtensions
{
    // TODO: where T : ICosPrimitive
    public static T? Get<T>(this CosDictionary dictionary, CosName key)
    {
        if (!dictionary.TryGetValue(key, out var obj))
        {
            return default;
        }

        if (obj is not T item)
        {
#if DEBUG
            throw new WispException(
                $"Expected key '{key.Value}' to be of type '{typeof(T).Name}', " +
                $"but it was of type '{obj.GetType().Name}'");
#else
            return default;
#endif
        }

        return item;
    }

    // TODO: where T : ICosPrimitive
    public static T GetRequired<T>(this CosDictionary dictionary, CosName key)
    {
        if (!dictionary.TryGetValue(key, out var obj))
        {
            throw new WispException($"The key /{key} does not exist in the dictionary");
        }

        if (obj is not T item)
        {
            throw new WispException(
                $"Expected required key '{key.Value}' to be of type '{typeof(T).Name}', " +
                $"but it was of type '{obj.GetType().Name}'");
        }

        return item;
    }

    public static void SetRequired(this CosDictionary dictionary, CosName key, ICosPrimitive value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (value is null)
        {
            throw new WispException(
                "Cannot set required key '{key}' to null");
        }

        dictionary[key] = value;
    }
}