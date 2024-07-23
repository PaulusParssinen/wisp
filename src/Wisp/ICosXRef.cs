namespace Wisp;

public interface ICosXRef
{
    CosObjectId Id { get; }

    ICosXRef CreateCopy();
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed class CosIndirectXRef(CosObjectId id) : ICosXRef
{
    public CosObjectId Id { get; } = id;
    public long? Position { get; set; }

    public CosIndirectXRef(CosObjectId id, long position)
        : this(id)
    {
        Position = position;
    }

    public ICosXRef CreateCopy()
    {
        return Position is not null ? 
            new CosIndirectXRef(Id, Position.Value) : new CosIndirectXRef(Id);
    }

    public override string ToString() => $"[XRef] {Id.Number}:{Id.Generation} Position = {Position}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed class CosStreamXRef(CosObjectId id, CosObjectId streamId, int index) : ICosXRef
{
    public CosObjectId Id { get; } = id;
    public CosObjectId StreamId { get; } = streamId;
    public int Index { get; } = index;

    public ICosXRef CreateCopy() => new CosStreamXRef(Id, StreamId, Index);

    public override string ToString()
    {
        return $"[XRef] {Id.Number}:{Id.Generation} Stream = {StreamId.Number}:{StreamId.Generation}, Index = {Index}";
    }
}