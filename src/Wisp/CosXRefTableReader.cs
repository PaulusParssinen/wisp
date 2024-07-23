using System.Buffers.Binary;

namespace Wisp;

public static class CosXRefTableReader
{
    public static (CosXRefTable XRefTable, CosDictionary Trailer) Read(CosParser parser)
    {
        parser.ExpectToken(CosTokenKind.XRef);

        var table = new CosXRefTable();

        while (parser.CanRead)
        {
            if (!parser.CheckToken(CosTokenKind.Integer))
            {
                break;
            }

            var startId = parser.ExpectToken(CosTokenKind.Integer).ParseInt32();
            var count = parser.ExpectToken(CosTokenKind.Integer).ParseInt32();

            foreach (var id in Enumerable.Range(startId, count))
            {
                var position = parser.ExpectToken(CosTokenKind.Integer).ParseInt32();
                var generation = parser.ExpectToken(CosTokenKind.Integer).ParseInt32();
                var kind = parser.ReadToken().Kind;

                if (position == 0)
                {
                    // Microsoft Word adds empty rows in the xref table sometimes
                    // For now, just ignore since it doesn't point to an actual object
                    continue;
                }

                switch (kind)
                {
                    case CosTokenKind.XRefFree:
                        // We don't care of free objects
                        break;
                    case CosTokenKind.XRefIndirect:
                        table.Add(new CosIndirectXRef(
                            new CosObjectId(id, generation),
                            position));
                        break;
                    default:
                        throw new WispException("Unknown xref kind encountered");
                }
            }
        }

        // Now find the trailer
        var trailer = new CosDictionary();
        while (parser.CanRead)
        {
            var current = parser.ReadToken();
            if (current.Kind == CosTokenKind.Trailer)
            {
                if (parser.Parse() is CosDictionary trailerDictionary)
                {
                    trailer = trailerDictionary;
                    break;
                }
            }
        }

        return (table, trailer);
    }

    public static (CosXRefTable XRefTable, CosDictionary Trailer) ParseXRefStream(CosParser parser)
    {
        var start = parser.Position;
        var primitive = parser.Parse();
        if (primitive is not CosObject obj || obj.Object is not CosStream stream)
        {
            throw new WispException("Expected COS stream");
        }

        var table = new CosXRefTable();

        var sizes = GetFieldSizes(stream);
        var ids = GetObjectIds(stream);

        var entries = ReadEntries(stream, sizes);
        foreach (var entry in entries)
        {
            if (ids.Count == 0)
            {
                throw new WispException("Cannot read xref stream (no more index)");
            }

            // Get the next object ID.
            var id = ids.Dequeue();

            if (entry.First == 0)
            {
                // We don't care of free objects
            }
            else if (entry.First == 1)
            {
                // Indirect object
                var generation = (int)entry.Third;
                var offset = entry.Second;
                table.Add(new CosIndirectXRef(
                    new CosObjectId(id, generation),
                    offset));
            }
            else if (entry.First == 2)
            {
                // Indirect object in stream
                var streamId = (int)entry.Second;
                var streamIndex = (int)entry.Third;
                table.Add(new CosStreamXRef(
                    new CosObjectId(id, 0),
                    new CosObjectId(streamId, 0),
                    streamIndex));
            }
            else
            {
                // PDF32000-1:2008, 7.5.8.3. ("Cross-Reference Stream Data")
                // Any other value shall be interpreted as a reference to the null object, ..
                throw new WispException("Unknown xref stream object type");
            }
        }

        return (table, stream.Dictionary);
    }

    private static (int, int, int) GetFieldSizes(CosStream stream)
    {
        var sizes = stream.Dictionary.Get<CosArray>(CosNames.W) ?? throw new WispException("XRef Stream is missing /W array");
        if (sizes.Count != 3)
        {
            throw new WispException($"Expected 3 items in /W array in XRef stream. Found {sizes.Count}");
        }

        (int, int, int) w = new(
            sizes.GetAt<CosInteger>(0)?.IntValue ?? 1,
            sizes.GetAt<CosInteger>(1)?.IntValue ?? 0,
            sizes.GetAt<CosInteger>(2)?.IntValue ?? 0);

        if (w.Item1 < 0 || w.Item2 < 0 || w.Item3 < 0)
        {
            throw new WispException("/W array in XRef stream is invalid");
        }

        return w;
    }

    private static Queue<int> GetObjectIds(CosStream stream)
    {
        var size = stream.Dictionary.Get<CosInteger>(CosNames.Size)?.Value ?? throw new WispException("Stream xref table did not have size");
        var indexArray = stream.Dictionary.Get<CosArray>(CosNames.Index) ??
            [
                new CosInteger(0),
                new CosInteger(size),
            ];

        var indices = new List<int>();
        foreach (var item in indexArray)
        {
            if (item is not CosInteger arrayInteger)
            {
                throw new WispException(
                    "Encountered malformed index array (not an integer)");
            }

            indices.Add((int)arrayInteger.Value);
        }

        if (int.IsOddInteger(indices.Count))
        {
            throw new WispException(
                "Encountered malformed index array (unbalanced)");
        }

        var result = new List<int>(indices.Count);
        for (var i = 0; i < indices.Count; i += 2)
        {
            var start = indices[i];
            var count = indices[i + 1];

            result.AddRange(Enumerable.Range(start, count));
        }

        return new Queue<int>(result);
    }

    private static List<(uint First, uint Second, uint Third)> ReadEntries(CosStream stream, (int, int, int) fieldSizes)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var entries = new List<(uint First, uint Second, uint Third)>();
        var data = new ReadOnlySpan<byte>(stream.GetUnfilteredData());

        while (!data.IsEmpty)
        {
            var first = ReadVariableLengthUInt32(ref data, fieldSizes.Item1);
            var second = ReadVariableLengthUInt32(ref data, fieldSizes.Item2);
            var third = ReadVariableLengthUInt32(ref data, fieldSizes.Item3);

            entries.Add((first, second, third));
        }

        return entries;

        // Decodes an unsigned integer consisting of n bytes in big-endian format as an unsigned 32-bit integer.
        static uint ReadVariableLengthUInt32(ref ReadOnlySpan<byte> data, int length)
        {
            uint value = length switch
            {
                1 => data[0],
                2 => BinaryPrimitives.ReadUInt16BigEndian(data),
                // TODO: Is 24-bit spec. compliant?
                4 => BinaryPrimitives.ReadUInt32BigEndian(data),
                _ => throw new WispException("Encountered unexpected field size")
            };

            data = data.Slice(length);
            return value;
        }
    }
}