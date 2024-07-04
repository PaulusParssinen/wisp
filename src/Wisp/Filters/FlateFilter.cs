using System.IO.Compression;

namespace Wisp.Filters;

public sealed class FlateFilter : IFilter
{
    public string Name { get; } = "FlateDecode";

    public bool Supported => true;

    public byte[] Decode(ReadOnlySpan<byte> data, CosDictionary? parameters)
    {
        // Run the deflate algorithm
        var bytes = Deflate(data);

        var settings = GetPredictorSettings(parameters);
        if (settings.Predictor == 1)
        {
            return bytes;
        }

        if (settings.Predictor == 2)
        {
            throw new WispException("TIFF predictor not supported");
        }

        return PngDecoder.Decode(bytes, settings.Columns, settings.Colors, settings.BitsPerComponent);
    }

    public static byte[] Encode(byte[] data, CosCompression compression)
    {
        using (var original = new MemoryStream(data))
        using (var output = new MemoryStream())
        {
            // Write the flate header
            output.Write([120, 156]); // TODO: I think we may be able to ZLibStream directly.

            var level = compression switch
            {
                CosCompression.None => CompressionLevel.NoCompression,
                CosCompression.Fastest => CompressionLevel.Fastest,
                CosCompression.Optimal => CompressionLevel.Optimal,
                CosCompression.Smallest => CompressionLevel.SmallestSize,
                _ => throw new ArgumentOutOfRangeException(nameof(compression), compression, null),
            };

            using (var compressor = new DeflateStream(output, level))
            {
                original.CopyTo(compressor);
                compressor.Flush();
                return output.ToArray();
            }
        }
    }

    private static unsafe byte[] Deflate(ReadOnlySpan<byte> data)
    {
        if (data.Length < 2)
        {
            throw new WispException("Invalid flate stream");
        }

        var output = new MemoryStream();
        fixed (byte* dataPtr = data)
        {
            using var inputStream = new UnmanagedMemoryStream(dataPtr, data.Length);
            using var zlibStream = new ZLibStream(inputStream, CompressionMode.Decompress);

            zlibStream.CopyTo(output);
        }

        return output.ToArray();
    }

    private static (int Predictor, int Columns, int Colors, int BitsPerComponent)
        GetPredictorSettings(CosDictionary? parameters)
    {
        var predictor = parameters?.Get<CosInteger>(CosNames.Predictor)?.IntValue ?? 1;
        var columns = parameters?.Get<CosInteger>(CosNames.Columns)?.IntValue ?? 1;
        var colors = parameters?.Get<CosInteger>(CosNames.Colors)?.IntValue ?? 1;
        var bits = parameters?.Get<CosInteger>(CosNames.BitsPerComponent)?.IntValue ?? 8;

        return (predictor, columns, colors, bits);
    }

    private static class PngDecoder
    {
        public static unsafe byte[] Decode(ReadOnlySpan<byte> bytes, int columns, int colors, int bitsPerComponent)
        {
            var bytesPerRow = ((colors * columns * bitsPerComponent) + 7) / 8;

            fixed (byte* bytesPtr = bytes)
            {
                using var inputStream = new UnmanagedMemoryStream(bytesPtr, bytes.Length);
                using var reader = new BinaryReader(inputStream);
                var writer = new MemoryStream(bytes.Length);

                var previous = default(byte[]);

                while (true)
                {
                    var filter = reader.Read();
                    if (filter < 0)
                    {
                        return writer.ToArray();
                    }

                    var current = new byte[bytesPerRow];
                    ReadBytes(reader, current, bytesPerRow);

                    if (filter == 0)
                    {
                        // NONE
                    }
                    else if (filter == 1)
                    {
                        // SUB
                        throw new WispException("Unsupported filter: PngSub");
                    }
                    else if (filter == 2)
                    {
                        // UP
                        if (previous is not null)
                        {
                            for (var i = 0; i < bytesPerRow; i++)
                            {
                                current[i] += previous[i];
                            }
                        }
                    }
                    else if (filter == 3)
                    {
                        // AVERAGE
                        throw new WispException("Unsupported filter: PngAverage");
                    }
                    else if (filter == 4)
                    {
                        // PAETH
                        throw new WispException("Unsupported filter: PngPaeth");
                    }
                    else if (filter == 5)
                    {
                        // PAETH
                        throw new WispException("Unsupported filter: PngOptimum");
                    }
                    else
                    {
                        // UNKNOWN
                        throw new WispException("Encountered unknown PNG filter during decoding");
                    }

                    // Write the current row to the stream
                    writer.Write(current);

                    // Swap streams
                    previous = current;
                }
            }
        }

        // TODO: Rewrite
        private static void ReadBytes(BinaryReader reader, byte[] buffer, int count)
        {
            if (count < 0)
            {
                throw new IndexOutOfRangeException();
            }

            var off = 0;
            var n = 0;
            while (n < count)
            {
                int read = reader.Read(buffer, off + n, count - n);
                if (read <= 0)
                {
                    throw new EndOfStreamException();
                }

                n += read;
            }
        }
    }
}