namespace Wisp;

public sealed class CosHeaderReader
{
    public static Version Read(CosParser parser)
    {
        var previousPosition = parser.Position;

        try
        {
            parser.Seek(0, SeekOrigin.Begin);

            Span<byte> headerBuffer = stackalloc byte[8];
            parser.ReadBytes(headerBuffer);

            if (!headerBuffer.StartsWith("%PDF-"u8))
            {
                throw new WispException("PDF file is missing header");
            }

            return new Version(headerBuffer[5], headerBuffer[7]);
        }
        finally
        {
            parser.Seek(previousPosition, SeekOrigin.Begin);
        }
    }
}