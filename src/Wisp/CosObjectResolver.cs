using System.Reflection.Metadata.Ecma335;

namespace Wisp;

internal sealed class CosObjectResolver : IDisposable
{
    private readonly CosParser _parser;
    private readonly CosXRefTable _xRefTable;

    public CosObjectResolver(CosParser parser, CosXRefTable xRefTable)
    {
        _parser = parser;
        _xRefTable = xRefTable;
    }

    public void Dispose()
    { }

    public bool TryGetObject(ICosObjectCache cache, CosObjectId id, out CosObject? owner, [NotNullWhen(true)] out CosObject? obj)
    {
        owner = obj = null;

        switch (_xRefTable.GetXRef(id))
        {
            case null:
                return false;

            case CosStreamXRef streamXref:
                // Get the xref to the stream object
                if (_xRefTable.GetXRef(streamXref.StreamId) is not CosIndirectXRef streamObjectXRef)
                {
                    throw new WispObjectResolveException(
                        _parser, "Could not get xref to stream object");
                }

                // Ensure that the stream has a position
                if (streamObjectXRef.Position is null)
                {
                    throw new WispObjectResolveException(
                        _parser, "Object in object stream should exist in cache");
                }

                // Is the stream itself in the cache?
                // Don't try to resolve the stream object; that will lead to a stack overflow
                if (!cache.TryGet(streamObjectXRef.Id, CosResolveFlags.NoResolve, out var ownerObject))
                {
                    // Parse the object stream
                    _parser.Seek(streamObjectXRef.Position.Value, SeekOrigin.Begin);
                    ownerObject = _parser.Parse() as CosObject;
                    if (ownerObject is null)
                    {
                        throw new WispObjectResolveException(_parser, "Could not find an object at the stream position");
                    }
                }

                // Ensure the primitive is an object stream
                if (ownerObject.Object is not CosObjectStream objectStream)
                {
                    throw new WispObjectResolveException(_parser, "Object was not an object stream");
                }

                // Get the object within the stream
                var objectStreamItem = objectStream.GetObjectByIndex(cache, streamXref.Index) 
                    ?? throw new WispObjectResolveException(_parser, $"Could not get object in object stream at index {streamXref.Index}");
                
                owner = ownerObject;
                obj = objectStreamItem;
                return true;

            case CosIndirectXRef indirectXRef:
                if (indirectXRef.Position is null)
                {
                    throw new WispObjectResolveException(_parser, "Object should exist in cache (no position)");
                }

                _parser.Seek(indirectXRef.Position.Value, SeekOrigin.Begin);
                if (_parser.Parse() is not CosObject resultObj)
                {
                    return false;
                }
                obj = resultObj;
                return true;
            default:
                return false;
        }
    }
}