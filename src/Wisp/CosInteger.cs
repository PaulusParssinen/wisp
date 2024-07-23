namespace Wisp;

[DebuggerDisplay("{ToString(),nq}")]
[CosPrimitive]
public sealed partial class CosInteger(long value) : ICosPrimitive
{
    public long Value { get; } = value;
    public int IntValue => (int)Value;

    public override string ToString() => $"[Integer] {Value}";
}