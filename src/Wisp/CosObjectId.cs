namespace Wisp;

[DebuggerDisplay("{ToString(),nq}")]
[CosPrimitive]
public sealed partial class CosObjectId(int number, int generation) : ICosPrimitive, IEquatable<CosObjectId>, IComparable<CosObjectId>
{
    public int Number { get; set; } = number;
    public int Generation { get; set; } = generation;

    public static CosObjectIdComparer Comparer => CosObjectIdComparer.Shared;

    public static CosObjectId Parse(ReadOnlySpan<char> text)
    {
        Span<Range> ranges = stackalloc Range[2];

        var parts = text.Split(ranges, ':', StringSplitOptions.RemoveEmptyEntries);
        if (parts == 2)
        {
            var invariant = CultureInfo.InvariantCulture;
            return new CosObjectId(
                int.Parse(text[ranges[0]].Trim(), invariant),
                int.Parse(text[ranges[1]].Trim(), invariant));
        }

        throw new WispException("Could not parse object ID.");
    }

    public int CompareTo(CosObjectId? other)
    {
        if (other is null) return 1;

        return Number.CompareTo(other.Number);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;

        if (obj is CosObjectId objectId)
        {
            return Equals(objectId);
        }

        return false;
    }

    public bool Equals(CosObjectId? other) => CosObjectIdComparer.Shared.Equals(this, other);

    public override int GetHashCode() => CosObjectIdComparer.Shared.GetHashCode(this);

    public override string ToString() => $"[ObjectID] {Number}:{Generation}";
}

public sealed class CosObjectIdComparer : IEqualityComparer<CosObjectId>
{
    public static CosObjectIdComparer Shared { get; } = new();

    public bool Equals(CosObjectId? x, CosObjectId? y)
    {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;

        return x.Number == y.Number &&
               x.Generation == y.Generation;
    }

    public int GetHashCode(CosObjectId obj) => HashCode.Combine(obj.Number, obj.Generation);
}