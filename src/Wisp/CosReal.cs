namespace Wisp;

[DebuggerDisplay("{ToString(),nq}")]
[CosPrimitive]
public sealed partial class CosReal(double value) : ICosPrimitive
{
    public double Value { get; } = value;

    public override string ToString() => $"[Real] {Value.ToString(CultureInfo.InvariantCulture)}";
}