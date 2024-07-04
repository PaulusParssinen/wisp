namespace Wisp;

public sealed class CosToken(CosTokenKind kind, string? text = null, byte[]? lexeme = null)
{
    public CosTokenKind Kind { get; } = kind;
    public string? Text { get; } = text;
    public byte[]? Lexeme { get; } = lexeme;
}

public static class CosTokenExtensions
{
    public static int ParseInt32(this CosToken token)
    {
        if (token.Kind is not CosTokenKind.Integer)
        {
            throw new WispException("Cannot parse token since it's not an integer.");
        }

        return token.Text is null ? 0 : int.Parse(token.Text, CultureInfo.InvariantCulture);
    }

    public static double ParseDouble(this CosToken token)
    {
        if (token.Kind is not CosTokenKind.Real)
        {
            throw new WispException("Cannot parse token since it's not a real number.");
        }

        return token.Text is null ? 0 : double.Parse(token.Text, CultureInfo.InvariantCulture);
    }
}