namespace Wisp.Internal;

internal sealed class ByteStreamReader
{
    private readonly byte[] _buffer;
    private int _position;

    public bool CanRead => _position < _buffer.Length;
    public long Position => _position;
    public long Length => _buffer.Length;

    public ByteStreamReader(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        _buffer = ReadAllBytes(stream);
        _position = 0;
    }

    public int PeekByte()
    {
        if (_position >= _buffer.Length)
        {
            return -1;
        }

        return _buffer[_position];
    }

    public char PeekChar() => (char)PeekByte();

    public int ReadByte()
    {
        var result = PeekByte();
        if (result != -1)
        {
            _position++;
        }

        return result;
    }

    public char ReadChar() => (char)ReadByte();

    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        if (_position + count > _buffer.Length)
        {
            throw new WispException("Exceeded stream end");
        }

        var result = _buffer.AsSpan(_position, count);
        _position += count;

        return result;
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

    private static byte[] ReadAllBytes(Stream stream)
    {
        if (stream is MemoryStream memoryStream)
        {
            return memoryStream.ToArray();
        }

        // TODO: Read using buffer
        using var output = new MemoryStream();
        stream.CopyTo(output);
        return output.ToArray();
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
}