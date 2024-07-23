namespace Wisp;

[DebuggerDisplay("{ToString(),nq}")]
[CosPrimitive]
public sealed partial class CosHexString(byte[] value) : ICosPrimitive
{
    public byte[] Value { get; } = value;

    /// <inheritdoc/>
    public override string ToString() => $"[Hex] {Convert.ToHexString(Value)}";
}