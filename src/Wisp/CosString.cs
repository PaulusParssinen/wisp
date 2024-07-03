namespace Wisp;

[DebuggerDisplay("{ToString(),nq}")]
[CosPrimitive]
public sealed partial class CosString : ICosPrimitive
{
    public string Value { get; }
    public CosStringEncoding Encoding { get; set; }

    public CosString(string value)
    {
        Value = value;
        Encoding = Ascii.IsValid(value) ? CosStringEncoding.Ascii : CosStringEncoding.Unicode;
    }

    internal CosString(string value, CosStringEncoding encoding)
    {
        Value = value;
        Encoding = encoding;
    }

    /// <inheritdoc />
    public override string ToString() => $"[String] {Value} ({Encoding})";
}