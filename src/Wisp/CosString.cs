namespace Wisp;

[DebuggerDisplay("{ToString(),nq}")]
[CosVisitable]
public sealed partial class CosString
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

    public override string ToString() => $"[String] {Value} ({Encoding})";
}