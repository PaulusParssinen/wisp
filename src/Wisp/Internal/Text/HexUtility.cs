namespace Wisp.Internal;

internal static class HexUtility
{
    public static char FromHex(char first, char second)
    {
        return (char)int.Parse(stackalloc char[] { first, second }, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }
}