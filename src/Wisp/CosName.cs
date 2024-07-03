namespace Wisp;

[DebuggerDisplay("{ToString(),nq}")]
[CosPrimitive]
public sealed partial class CosName : ICosPrimitive, IEquatable<CosName>
{
    public string Value { get; }

    public static CosNameComparer Comparer => CosNameComparer.Shared;

    public CosName(string value)
    {
        Value = value.TrimStart('/');
    }

    public bool Equals(CosName? other) => CosNameComparer.Shared.Equals(this, other);

    public override int GetHashCode() => CosNameComparer.Shared.GetHashCode(this);
    
    /// <inheritdoc/>
    public override string ToString() => $"[Name] {Value}";
}

public sealed class CosNameComparer : IEqualityComparer<CosName>
{
    public static CosNameComparer Shared { get; } = new();

    public bool Equals(CosName? x, CosName? y)
    {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;

        return x.Value.Equals(y.Value, StringComparison.Ordinal);
    }

    public int GetHashCode(CosName obj) => obj.Value.GetHashCode(StringComparison.Ordinal);
}