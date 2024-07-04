using System.Buffers;

namespace Wisp;

public sealed class CosLexer
{
    private static readonly SearchValues<byte> _lowerAsciiLetters = SearchValues.Create("abcdefghijklmnopqrstuvwxyz"u8);

    private readonly byte[] _buffer;
    private int _position;

    private ReadOnlySpan<byte> CurrentSpan => _buffer.AsSpan(_position);

    public bool CanRead => _position < _buffer.Length;
    public long Position => _position;
    public long Length => _buffer.Length;

    public CosLexer(byte[] buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    public int PeekByte() => CanRead ? _buffer[_position] : -1;

    public char PeekChar() => (char)PeekByte();

    public int ReadByte() => CanRead ? _buffer[_position++] : -1;

    public char ReadChar() => (char)ReadByte();

    public void ReadBytes(Span<byte> buffer)
    {
        if (_position + buffer.Length > _buffer.Length)
        {
            throw new WispException("Exceeded stream end");
        }

        _buffer.AsSpan(_position, buffer.Length).CopyTo(buffer);
        _position += _buffer.Length;
    }

    public long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                _position = (int)offset;
                break;
            case SeekOrigin.Current:
                _position += (int)offset;
                break;
            case SeekOrigin.End:
                _position = _buffer.Length + (int)offset;
                break;
            default:
                throw new WispException("Unknown seek origin");
        }

        return _position;
    }

    public void Consume() => ReadByte();

    public void Consume(char expected)
    {
        var read = ReadByte();
        if (read != expected)
        {
            throw new WispException($"Expected '{expected}' but got '{read}'.");
        }
    }

    public bool TryPeek([NotNullWhen(true)] out CosToken? token)
    {
        var position = _position;

        try
        {
            return TryRead(out token, out _);
        }
        finally
        {
            // Move the cursor back to where we were
            Seek(position, SeekOrigin.Begin);
        }
    }

    public bool Check(CosTokenKind kind)
    {
        if (TryPeek(out var token))
        {
            return token.Kind == kind;
        }

        return false;
    }

    public CosToken Expect(CosTokenKind kind)
    {
        try
        {
            var token = Read();
            if (token.Kind != kind)
            {
                throw new WispLexerException(
                    this, $"Expected '{kind}' token in stream but found '{token.Kind}'");
            }

            return token;
        }
        catch (Exception ex)
        {
            throw new WispLexerException(
                this,
                $"Expected token '{kind}', but lexer returned an error",
                ex);
        }
    }

    public CosToken Read()
    {
        if (!TryRead(out var token, out var error))
        {
            throw new WispLexerException(this, $"Could not read next token from stream. Reason: {error}");
        }

        return token;
    }

    private bool TryRead([NotNullWhen(true)] out CosToken? token, [NotNullWhen(false)] out string? error)
    {
        error = null;

        EatWhitespace();

        if (!CanRead)
        {
            token = null;
            error = "Reached end of stream";
            return false;
        }

        var current = PeekChar();

        if (current == '%')
        {
            token = ReadComment();
            return true;
        }
        else if (current == '/')
        {
            token = ReadName();
            return true;
        }
        else if (current == '(')
        {
            token = ReadStringLiteral();
            return true;
        }
        else if (current == '<')
        {
            token = ReadBeginDictionaryOrHexStringLiteral();
            return true;
        }
        else if (current == '>')
        {
            token = ReadEndDictionary();
            return true;
        }
        else if (current == '[')
        {
            token = ReadBeginArray();
            return true;
        }
        else if (current == ']')
        {
            token = ReadEndArray();
            return true;
        }
        else if (char.IsDigit(current) || current == '-' || current == '+' || current == '.')
        {
            token = ReadNumber();
            return true;
        }
        else if (char.IsLetter(current))
        {
            token = ReadKeyword();
            return true;
        }

        token = null;
        error = $"Encountered invalid token '{current}' ({Uri.HexEscape(current)})";
        return false;
    }

    private void EatWhitespace()
    {
        while (CanRead)
        {
            var current = PeekChar();
            if (!current.IsPdfWhitespace())
            {
                return;
            }

            ReadByte();
        }
    }

    private CosToken ReadComment()
    {
        Consume('%');

        while (CanRead)
        {
            var current = PeekChar();
            if (current.IsPdfLineBreak())
            {
                break;
            }

            ReadByte();
        }

        return new CosToken(
            CosTokenKind.Comment);
    }

    private CosToken ReadName()
    {
        Consume('/');

        var accumulator = new StringBuilder();
        Span<byte> hexBuffer = stackalloc byte[2];

        while (CanRead)
        {
            var current = PeekChar();
            if (!current.IsPdfName() && !current.IsPdfSolidus())
            {
                break;
            }

            // Not part of spec but...
            if (current is '<' or '>' or '/' or '[' or ']' or '(' or ')')
            {
                break;
            }

            if (current == '#')
            {
                Consume('#');

                ReadBytes(hexBuffer);
                accumulator.Append(HexUtility.FromHex((char)hexBuffer[0], (char)hexBuffer[1]));
            }
            else
            {
                accumulator.Append(ReadChar());
            }
        }

        return new CosToken(
            CosTokenKind.Name,
            accumulator.ToString());
    }

    private CosToken ReadStringLiteral()
    {
        Consume('(');

        var level = 0;
        var escaped = false;
        var accumulator = new List<byte>();

        while (CanRead)
        {
            var current = ReadByte();

            // Escaped new line?
            var character = (char)current;
            if ((character == '\r' || character == '\n') && escaped)
            {
                continue;
            }

            // Escape?
            if (character == '\\' && !escaped)
            {
                escaped = true;
            }
            else
            {
                if (character == '(')
                {
                    if (!escaped)
                    {
                        level++;
                    }
                }
                else if (character == ')')
                {
                    if (!escaped)
                    {
                        if (level == 0)
                        {
                            break;
                        }

                        level--;
                    }
                }

                accumulator.Add((byte)current);
                escaped = false;
            }
        }

        return new CosToken(
            CosTokenKind.StringLiteral,
            text: null,
            lexeme: accumulator.ToArray());
    }

    private CosToken ReadBeginDictionaryOrHexStringLiteral()
    {
        Consume('<');

        if (PeekChar() == '<')
        {
            Consume('<');
            return new CosToken(CosTokenKind.BeginDictionary);
        }

        return ReadHexStringLiteral();
    }

    private CosToken ReadHexStringLiteral()
    {
        var accumulator = new StringBuilder();
        while (true)
        {
            if (!CanRead)
            {
                throw new WispLexerException(
                    this, "Hex string literal is missing trailing '>'.");
            }

            var current = PeekChar();
            if (!char.IsLetter(current) && !char.IsDigit(current))
            {
                if (current == '>')
                {
                    Consume('>');
                    break;
                }

                throw new WispLexerException(
                    this, $"Malformed hexadecimal literal. Invalid character '{current}'.");
            }

            accumulator.Append(ReadChar());
        }

        if (accumulator.Length % 2 != 0)
        {
            accumulator.Append('0');
        }

        return new CosToken(
            CosTokenKind.HexStringLiteral,
            lexeme: Convert.FromHexString(accumulator.ToString()));
    }

    private CosToken ReadBeginArray()
    {
        Consume('[');
        return new CosToken(CosTokenKind.BeginArray);
    }

    private CosToken ReadEndArray()
    {
        Consume(']');
        return new CosToken(CosTokenKind.EndArray);
    }

    private CosToken ReadEndDictionary()
    {
        Consume('>');
        Consume('>');
        return new CosToken(CosTokenKind.EndDictionary);
    }

    private CosToken ReadNumber()
    {
        var accumulator = new StringBuilder();
        var encounteredPeriod = false;

        while (CanRead)
        {
            var current = PeekChar();

            if (char.IsDigit(current))
            {
                accumulator.Append(ReadChar());
            }
            else if (current == '-' || current == '+')
            {
                if (accumulator.Length > 0)
                {
                    throw new WispLexerException(
                        this, "Encountered malformed integer");
                }

                Consume();
                if (current == '-')
                {
                    accumulator.Append('-');
                }
            }
            else if (current == '.')
            {
                if (encounteredPeriod)
                {
                    throw new WispLexerException(
                        this, "Encountered more than one period");
                }

                encounteredPeriod = true;
                accumulator.Append(ReadChar());
            }
            else
            {
                break;
            }
        }

        var number = accumulator.ToString();

        if (number.StartsWith('.'))
        {
            number = "0" + number;
        }
        else if (number.StartsWith("-."))
        {
            number = "-0." + number.TrimStart('-', '.');
        }

        return new CosToken(
            encounteredPeriod ? CosTokenKind.Real : CosTokenKind.Integer,
            number);
    }

    private CosToken ReadKeyword()
    {
        int endOfKeywordIndex = CurrentSpan.IndexOfAnyExceptInRange((byte)'a', (byte)'z');

        var keywordSpan = CurrentSpan.Slice(0, endOfKeywordIndex);
        _position += endOfKeywordIndex;

        return keywordSpan.Length switch
        {
            4 when keywordSpan.SequenceEqual("true"u8) => new CosToken(CosTokenKind.Boolean, "true"),
            5 when keywordSpan.SequenceEqual("false"u8) => new CosToken(CosTokenKind.Boolean, "false"),
            7 when keywordSpan.SequenceEqual("trailer"u8) => new CosToken(CosTokenKind.Trailer),
            3 when keywordSpan.SequenceEqual("obj"u8) => new CosToken(CosTokenKind.BeginObject),
            6 when keywordSpan.SequenceEqual("endobj"u8) => new CosToken(CosTokenKind.EndObject),
            6 when keywordSpan.SequenceEqual("stream"u8) => new CosToken(CosTokenKind.BeginStream),
            9 when keywordSpan.SequenceEqual("endstream"u8) => new CosToken(CosTokenKind.EndStream),
            4 when keywordSpan.SequenceEqual("null"u8) => new CosToken(CosTokenKind.Null),
            1 when keywordSpan.SequenceEqual("R"u8) => new CosToken(CosTokenKind.Reference),
            9 when keywordSpan.SequenceEqual("startxref"u8) => new CosToken(CosTokenKind.StartXRef),
            4 when keywordSpan.SequenceEqual("xref"u8) => new CosToken(CosTokenKind.XRef),
            1 when keywordSpan.SequenceEqual("f"u8) => new CosToken(CosTokenKind.XRefFree),
            1 when keywordSpan.SequenceEqual("n"u8) => new CosToken(CosTokenKind.XRefIndirect),
            
            _ => throw new WispLexerException(this, $"Unknown token"),
        };
    }
}