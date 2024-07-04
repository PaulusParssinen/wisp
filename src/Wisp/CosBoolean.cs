namespace Wisp;

[DebuggerDisplay("{ToString(),nq}")]
[CosPrimitive]
public sealed partial class CosBoolean : ICosPrimitive
{
    public static CosBoolean True { get; } = new CosBoolean(true);
    public static CosBoolean False { get; } = new CosBoolean(false);

    public bool Value { get; }

    private CosBoolean(bool value) => Value = value;

    public override string ToString() => $"[Boolean] {Value}";
}