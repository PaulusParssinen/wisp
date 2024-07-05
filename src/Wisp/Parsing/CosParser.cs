namespace Wisp;

public sealed class CosParser
{
    private readonly CosLexer _lexer;
    private readonly bool _isStreamObject;

    public long Position => _lexer.Position;
    public long Length => _lexer.Length;
    public bool CanRead => _lexer.CanRead;

    public CosParser(
        byte[] buffer,
        bool isStreamObject = false)
    {
        _lexer = new CosLexer(buffer);
        _isStreamObject = isStreamObject;
    }

    public long Seek(long offset, SeekOrigin origin)
    {
        return _lexer.Seek(offset, origin);
    }

    public int ReadByte() => _lexer.ReadByte();

    public void ReadBytes(Span<byte> buffer) => _lexer.ReadBytes(buffer);

    public CosToken? PeekToken()
    {
        _lexer.TryPeek(out var token);
        return token;
    }

    public CosToken ReadToken() => _lexer.Read();

    public bool CheckToken(CosTokenKind kind) => _lexer.Check(kind);

    public CosToken ExpectToken(CosTokenKind kind) => _lexer.Expect(kind);

    public ICosPrimitive Parse()
    {
        while (_lexer.Check(CosTokenKind.Comment))
        {
            _lexer.Read();
        }

        if (!_lexer.TryPeek(out var token))
        {
            throw new WispParserException(
                this, "Reached end of stream");
        }

        return token.Kind switch
        {
            CosTokenKind.Null => ParseNull(),
            CosTokenKind.Boolean => ParseBoolean(),
            CosTokenKind.Integer => ParseInteger(),
            CosTokenKind.Real => ParseReal(),
            CosTokenKind.StringLiteral => ParseStringLiteral(),
            CosTokenKind.HexStringLiteral => ParseHexStringLiteral(),
            CosTokenKind.Name => ParseName(),
            CosTokenKind.BeginDictionary => ParseDictionary(),
            CosTokenKind.BeginArray => ParseArray(),
            _ => throw new WispParserException(this, $"Unexpected token {token.Kind} encountered"),
        };
    }

    private ICosPrimitive ParseBoolean()
    {
        var token = _lexer.Expect(CosTokenKind.Boolean);
        return token.Text == "true" ? CosBoolean.True : CosBoolean.False;
    }

    private ICosPrimitive ParseInteger()
    {
        var value = _lexer.Expect(CosTokenKind.Integer).ParseInt32();
        var position = _lexer.Position;

        // Got an integer next?
        if (_lexer.TryPeek(out var token) && token.Kind == CosTokenKind.Integer)
        {
            var generation = _lexer.Expect(CosTokenKind.Integer).ParseInt32();

            if (_lexer.TryPeek(out token))
            {
                switch (token.Kind)
                {
                    case CosTokenKind.Reference:
                        // Reference means object ID
                        _lexer.Expect(CosTokenKind.Reference);
                        return new CosObjectReference(new CosObjectId(value, generation));
                    case CosTokenKind.BeginObject:
                        // Object definition
                        _lexer.Expect(CosTokenKind.BeginObject);
                        return new CosObject(
                            new CosObjectId(value, generation),
                            Parse());
                }
            }

            // Rewind the reader
            _lexer.Seek(position, SeekOrigin.Begin);
        }

        return new CosInteger(value);
    }

    private CosReal ParseReal()
    {
        var value = _lexer.Expect(CosTokenKind.Real).ParseDouble();
        return new CosReal(value);
    }

    private CosNull ParseNull()
    {
        _lexer.Expect(CosTokenKind.Null);
        return CosNull.Shared;
    }

    private ICosPrimitive ParseStringLiteral()
    {
        static bool TryDecodeString(
            ReadOnlySpan<byte> input,
            [NotNullWhen(true)] out string? value,
            [NotNullWhen(true)] out CosStringEncoding? encoding)
        {
            if (input.StartsWith([(byte)0xFE, (byte)0xFF])) // Big-endian
            {
                value = Encoding.BigEndianUnicode.GetString(input.Slice(2));
                encoding = CosStringEncoding.BigEndianUnicode;
            }
            else if (input.StartsWith([(byte)0xFF, (byte)0xFE])) // Little-endian
            {
                value = Encoding.Unicode.GetString(input.Slice(2));
                encoding = CosStringEncoding.Unicode;
            }
            else
            {
                // Treat everything else as ASCII.
                value = Encoding.ASCII.GetString(input);
                encoding = CosStringEncoding.Ascii;
            }

            return true;
        }

        var token = _lexer.Expect(CosTokenKind.StringLiteral);
        if (token.Lexeme is null)
        {
            throw new WispParserException(this, "String literal token had no byte content");
        }

        if (!TryDecodeString(token.Lexeme, out var decoded, out var encoding))
        {
            throw new WispParserException(this, "Could not decode PDF string");
        }

        // TODO: outline D: prefix check or make date parsing explicit.
        if (CosDate.TryParse(decoded, out var date))
        {
            return new CosDate(date.Value);
        }

        return new CosString(decoded, encoding.Value);
    }

    private CosHexString ParseHexStringLiteral()
    {
        var token = _lexer.Expect(CosTokenKind.HexStringLiteral);
        return new CosHexString(token.Lexeme!);
    }

    private CosName ParseName()
    {
        var token = _lexer.Expect(CosTokenKind.Name);
        return new CosName(token.Text!);
    }

    private ICosPrimitive ParseDictionary()
    {
        _lexer.Expect(CosTokenKind.BeginDictionary);

        var result = new CosDictionary();
        while (_lexer.TryPeek(out var token))
        {
            if (token.Kind == CosTokenKind.EndDictionary)
            {
                break;
            }

            var temp = Parse();
            if (temp is not CosName key)
            {
                throw new WispParserException(
                    this, "Encountered dictionary key that was not a PDF name");
            }

            var value = Parse();
            result.Set(key, value);
        }

        if (_isStreamObject)
        {
            if (_lexer.CanRead)
            {
                _lexer.Expect(CosTokenKind.EndDictionary);
            }
        }
        else
        {
            _lexer.Expect(CosTokenKind.EndDictionary);
        }

        // Is there a stream as well?
        var stream = ParseStream(result);
        if (stream is not null)
        {
            var type = result.Get<CosName>(CosNames.Type);
            if (type?.Equals(CosNames.ObjStm) == true)
            {
                return new CosObjectStream(stream);
            }

            return stream;
        }

        return result;
    }

    private CosArray ParseArray()
    {
        _lexer.Expect(CosTokenKind.BeginArray);

        var result = new CosArray();
        while (_lexer.TryPeek(out var token))
        {
            if (token.Kind == CosTokenKind.EndArray)
            {
                break;
            }

            result.Add(Parse());
        }

        _lexer.Expect(CosTokenKind.EndArray);

        return result;
    }

    private CosStream? ParseStream(CosDictionary metadata)
    {
        // Not a stream?
        if (!_lexer.TryPeek(out var streamToken) || streamToken.Kind != CosTokenKind.BeginStream)
        {
            return null;
        }

        var length = metadata.Get<CosInteger>(CosNames.Length) ??
            throw new WispParserException(this, "Stream did not have a specified length");

        // Read the stream data
        _lexer.Expect(CosTokenKind.BeginStream);

        var current = _lexer.ReadByte();
        if (current == '\r')
        {
            if (_lexer.ReadByte() != '\n')
            {
                throw new WispParserException(this, $"Invalid end-of-line marker. Expected LF, instead got '{current}'");
            }
        }
        else if (current != '\n')
        {
            throw new WispParserException(this, "Expected an end-of-line marker consisting either of CRLF or a single LF.");
        }

        var data = new byte[length.Value];

        _lexer.ReadBytes(data);
        _lexer.Expect(CosTokenKind.EndStream);

        return new CosStream(metadata, data);
    }
}