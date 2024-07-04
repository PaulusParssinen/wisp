namespace Wisp;

[DebuggerDisplay("{ToString(),nq}")]
[CosPrimitive]
public partial class CosObjectReference : ICosPrimitive, IEquatable<CosObjectReference>
{
    public CosObjectId Id { get; set; }

    public static CosObjectReferenceComparer Comparer => CosObjectReferenceComparer.Shared;

    public CosObjectReference(int number, int generation) => Id = new CosObjectId(number, generation);
    public CosObjectReference(CosObjectId id) => Id = id;

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || (obj is CosObjectReference other && Equals(other));

    public bool Equals(CosObjectReference? other) => CosObjectReferenceComparer.Shared.Equals(this, other);
    public override int GetHashCode() => CosObjectReferenceComparer.Shared.GetHashCode(this);

    public override string ToString() => $"[ObjectReference] {Id.Number}:{Id.Generation}";
}

public class CosObjectReference<T> : CosObjectReference
    where T : class, ICosPrimitive
{
    public T Object { get; set; }

    public CosObjectReference(CosObject obj)
        : base(obj.Id)
    {
        Object = obj.Object as T ?? throw new WispException("Typed object reference was not of the expected type");
    }

    internal CosObjectReference(CosObjectReference id, T obj)
        : base(id.Id.Number, id.Id.Generation)
    {
        Object = obj;
    }
}

public sealed class CosObjectReferenceComparer : IEqualityComparer<CosObjectReference>
{
    public static CosObjectReferenceComparer Shared { get; } = new();

    public bool Equals(CosObjectReference? x, CosObjectReference? y)
    {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;

        return CosObjectIdComparer.Shared.Equals(x.Id, y.Id);
    }

    public int GetHashCode(CosObjectReference obj)
    {
        return CosObjectIdComparer.Shared.GetHashCode(obj.Id);
    }
}