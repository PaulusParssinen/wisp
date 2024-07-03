namespace Wisp;

[DebuggerDisplay("{ToString(),nq}")]
[CosPrimitive]
public sealed partial class CosBoolean : ICosPrimitive
{
    public bool Value { get; }

    public CosBoolean(bool value)
    {
        Value = value;
    }

    public override string ToString() => $"[Boolean] {Value}";
}