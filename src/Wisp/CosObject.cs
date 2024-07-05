namespace Wisp;

[DebuggerDisplay("{ToString(),nq}")]
[CosPrimitive]
public sealed partial class CosObject(CosObjectId id, ICosPrimitive obj) : ICosPrimitive
{
    public CosObjectId Id { get; } = id;
    public ICosPrimitive Object { get; } = obj;

    public override string ToString() => $"[Object] {Id.Number}:{Id.Generation} ({Object})";
}