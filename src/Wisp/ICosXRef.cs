namespace Wisp;

public interface ICosXRef
{
    CosObjectId Id { get; }

    ICosXRef CreateCopy();
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed class CosIndirectXRef : ICosXRef
{
    public CosObjectId Id { get; }
    public long? Position { get; set; }


    public CosIndirectXRef(CosObjectId id) => Id = id;

    public CosIndirectXRef(CosObjectId id, long position)
        : this(id)
    {
        Position = position;
    }

    public ICosXRef CreateCopy()
    {
        if (Position is not null)
        {
            return new CosIndirectXRef(Id, Position.Value);
        }
        else
        {
            return new CosIndirectXRef(Id);
        }
    }

    public override string ToString() => $"[XRef] {Id.Number}:{Id.Generation} Position = {Position}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed class CosStreamXRef : ICosXRef
{
    public CosObjectId Id { get; }
    public CosObjectId StreamId { get; }
    public int Index { get; }

    public CosStreamXRef(CosObjectId id, CosObjectId streamId, int index)
    {
        Id = id;
        StreamId = streamId;
        Index = index;
    }

    public ICosXRef CreateCopy() => new CosStreamXRef(Id, StreamId, Index);

    public override string ToString()
    {
        var streamId = $"{StreamId.Number}:{StreamId.Generation}";
        return $"[XRef] {Id.Number}:{Id.Generation} Stream = {streamId}, Index = {Index}";
    }
}