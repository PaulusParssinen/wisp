namespace Wisp;

[DebuggerDisplay("{ToString(),nq}")]
[CosPrimitive]
public sealed partial class CosHexString : ICosPrimitive
{
    public byte[] Value { get; }

    public CosHexString(byte[] value)
    {
        Value = value;
    }
    
    /// <inheritdoc/>
    public override string ToString() => $"[Hex] {Convert.ToHexString(Value)}";
}