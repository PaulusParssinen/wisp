namespace Wisp;

[DebuggerDisplay("{ToString(),nq}")]
[CosPrimitive]
public sealed partial class CosNull : ICosPrimitive
{
    public static CosNull Shared { get; } = new();

    public override string ToString() => "[Null]";
}