namespace Wisp;

[DebuggerDisplay("{ToString(),nq}")]
[CosPrimitive]
public sealed partial class CosInteger : ICosPrimitive
{
    public long Value { get; }
    public int IntValue => (int)Value;

    public CosInteger(long value)
    {
        Value = value;
    }

    public override string ToString() => $"[Integer] {Value}";
}