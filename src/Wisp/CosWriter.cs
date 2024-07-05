namespace Wisp;

public sealed class CosWriter : IDisposable
{
    private readonly Stream _stream;
    private readonly CosWriterSettings _settings;

    public CosWriterSettings Settings => _settings;
    public long Position => _stream.Position;

    public CosWriter(Stream stream, CosWriterSettings? settings)
    {
        _stream = stream;
        _settings = settings ?? new CosWriterSettings();
    }

    public void Dispose()
    {
        _stream.Flush();

        if (!_settings.LeaveStreamOpen)
        {
            _stream.Dispose();
        }
    }

    public void WriteByte(byte value) => _stream.WriteByte(value);

    public void WriteByte(char value) => _stream.WriteByte((byte)value);

    public void WriteBytes(ReadOnlySpan<byte> value) => _stream.Write(value);

    [SkipLocalsInit]
    public void WriteLiteral<T>(T value, ReadOnlySpan<char> format = default) where T : unmanaged, IUtf8SpanFormattable
    {
        // TODO: ..
        Span<byte> buffer = stackalloc byte[128];
        if (!value.TryFormat(buffer, out int bytesWritten, format, CultureInfo.InvariantCulture))
            throw new WispException("Buffer too small for UTF8 formatting");

        _stream.Write(buffer.Slice(0, bytesWritten));
    }

    public void WriteLiteral(string value)
    {
        _stream.Write(Encoding.UTF8.GetBytes(value));
    }

    public void Write(CosDocument owner, ICosPrimitive value)
    {
        var context = new Visitor.Context(owner, this, _settings);
        value.Accept(Visitor.Shared, context);
    }

    private sealed class Visitor : CosVisitor<Visitor.Context>
    {
        public static Visitor Shared { get; } = new();

        public sealed class Context(CosDocument document, CosWriter writer, CosWriterSettings settings)
        {
            public CosDocument Document { get; } = document;
            public CosWriter Writer { get; } = writer;
            public CosWriterSettings Settings { get; set; } = settings;
        }

        public override void VisitArray(CosArray obj, Context context)
        {
            context.Writer.WriteByte('[');

            foreach (var (_, _, last, item) in obj.Enumerate())
            {
                item.Accept(this, context);

                if (!last)
                {
                    context.Writer.WriteByte(' ');
                }
            }

            context.Writer.WriteByte(']');
        }

        public override void VisitBoolean(CosBoolean obj, Context context)
        {
            context.Writer.WriteBytes(obj.Value ? "true"u8 : "false"u8);
        }

        public override void VisitDate(CosDate obj, Context context)
        {
            var timestamp = obj.Value.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            var offset = obj.Value.ToString("zzz", CultureInfo.InvariantCulture);
            context.Writer.WriteBytes("(D:"u8);
            context.Writer.WriteLiteral(timestamp + offset.Replace(':', '\''));
            context.Writer.WriteByte(')');
        }

        public override void VisitDictionary(CosDictionary obj, Context context)
        {
            context.Writer.WriteBytes("<<"u8);
            context.Writer.WriteByte(' ');

            foreach (var (key, value) in obj)
            {
                key.Accept(this, context);
                context.Writer.WriteByte(' ');
                value.Accept(this, context);
                context.Writer.WriteByte(' ');
            }

            context.Writer.WriteBytes(">>"u8);
        }

        public override void VisitInteger(CosInteger obj, Context context)
        {
            context.Writer.WriteLiteral(obj.Value);
        }

        public override void VisitName(CosName obj, Context context)
        {
            context.Writer.WriteByte('/');
            context.Writer.WriteLiteral(obj.Value);
        }

        public override void VisitNull(CosNull obj, Context context)
        {
            context.Writer.WriteBytes("null"u8);
        }

        public override void VisitReal(CosReal obj, Context context)
        {
            context.Writer.WriteLiteral(obj.Value);
        }

        public override void VisitHexString(CosHexString obj, Context context)
        {
            context.Writer.WriteByte('<');
            context.Writer.WriteLiteral(Convert.ToHexString(obj.Value));
            context.Writer.WriteByte('>');
        }

        public override void VisitObjectId(CosObjectId obj, Context context)
        {
            context.Writer.WriteLiteral(obj.Number.ToString(CultureInfo.InvariantCulture));
            context.Writer.WriteByte(' ');
            context.Writer.WriteLiteral(obj.Generation.ToString(CultureInfo.InvariantCulture));
        }

        public override void VisitObjectReference(CosObjectReference obj, Context context)
        {
            obj.Id.Accept(this, context);
            context.Writer.WriteBytes(" R"u8);
        }

        public override void VisitString(CosString obj, Context context)
        {
            var text = obj.Value
                .Replace("\\(", "(")
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)");

            context.Writer.WriteByte('(');
            context.Writer.WriteBytes(obj.Encoding switch
            {
                CosStringEncoding.Ascii => Encoding.ASCII.GetBytes(text),
                CosStringEncoding.Unicode => [.. (byte[])[0xFF, 0xFE], .. Encoding.Unicode.GetBytes(text)],
                CosStringEncoding.BigEndianUnicode => [.. (byte[])[0xFE, 0xFF], .. Encoding.BigEndianUnicode.GetBytes(text)],
                _ => throw new WispException("Unknown string encoding"),
            });
            context.Writer.WriteByte(')');
        }

        public override void VisitObject(CosObject obj, Context context)
        {
            obj.Id.Accept(this, context);
            context.Writer.WriteBytes(" obj\n"u8);
            obj.Object.Accept(this, context);
            context.Writer.WriteBytes("\nendobj"u8);
        }

        public override void VisitStream(CosStream obj, Context context)
        {
            if (context.Settings.Compression == CosCompression.None)
            {
                obj.Decompress();
            }
            else
            {
                obj.Compress(context.Settings.Compression);
            }

            obj.Dictionary.Accept(this, context);
            context.Writer.WriteByte('\n');
            context.Writer.WriteBytes("stream\n"u8);
            context.Writer.WriteBytes(obj.GetData());
            context.Writer.WriteBytes("\nendstream"u8);
        }

        public override void VisitObjectStream(CosObjectStream obj, Context context)
        {
            var headerStream = new MemoryStream();
            var header = new CosWriter(headerStream, CosWriterSettings.WithoutCompression());

            var bodyStream = new MemoryStream();
            var body = new CosWriter(bodyStream, CosWriterSettings.WithoutCompression());

            var numbers = obj.GetObjectIds().GroupConsecutive().Flatten().ToArray();
            foreach (var (_, _, last, number) in numbers.Enumerate())
            {
                var embedded = obj.GetObject(context.Document.Objects, new CosObjectId(number, 0));
                if (embedded is null)
                {
                    throw new WispException("Could not get object stream object during write");
                }

                // Write to the body
                var start = body.Position;
                body.Write(context.Document, embedded.Object);

                if (!last)
                {
                    body.WriteByte('\n');
                }

                // Write to the header
                header.WriteLiteral(number);
                header.WriteByte(' ');
                header.WriteLiteral(start);

                if (!last)
                {
                    header.WriteByte(' ');
                }
            }

            // Write the object dictionary
            var metadata = new CosDictionary()
            {
                { CosNames.Type, CosNames.ObjStm },
                { CosNames.N, new CosInteger(numbers.Length) },
                { CosNames.First, new CosInteger(headerStream.Length + 1) },
            };

            var buffer = new MemoryStream();
            buffer.Append(headerStream);
            buffer.WriteByte((byte)'\n');
            buffer.Append(bodyStream);

            // Write everything as a stream
            context.Writer.Write(
                context.Document,
                new CosStream(
                    metadata,
                    buffer.ToArray()));
        }
    }
}