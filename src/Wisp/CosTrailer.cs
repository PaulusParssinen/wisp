namespace Wisp;

public sealed class CosTrailer
{
    /// <summary>
    /// The underlying trailer dictionary.
    /// </summary>
    public CosDictionary Dictionary { get; }

    /// <summary>
    /// Gets or sets the total number of entries in the file's cross-reference table,
    /// as defined by the combination of the original section and all update
    /// sections. Equivalently, this value shall be 1 greater than the
    /// highest object number defined in the file. Any object in a cross-reference
    /// section whose number is greater than this value shall be ignored and
    /// defined to be missing by a conforming reader.
    /// </summary>
    public CosInteger Size
    {
        get => Dictionary.GetRequired<CosInteger>(CosNames.Size);
        set => Dictionary.Set(CosNames.Size, value);
    }

    /// <summary>
    /// Gets or sets the byte offset in the decoded stream from the
    /// beginning of the file to the beginning of the previous
    /// cross-reference section.
    /// </summary>
    public long? Prev
    {
        get => Dictionary.Get<CosInteger>(CosNames.Prev)?.Value;
        set => Dictionary.Set(CosNames.Prev, new CosInteger(value ?? 0));
    }

    /// <summary>
    /// Gets or sets the catalog dictionary for the document
    /// contained in the file.
    /// </summary>
    public CosObjectReference Root
    {
        get => Dictionary.GetRequired<CosObjectReference>(CosNames.Root);
        set => Dictionary.Set(CosNames.Root, value);
    }

    /// <summary>
    /// Gets or sets the document's encryption dictionary.
    /// </summary>
    public CosDictionary? Encrypt
    {
        get => Dictionary.Get<CosDictionary>(CosNames.Encrypt);
        set => Dictionary.Set(CosNames.Encrypt, value);
    }

    /// <summary>
    /// Gets or sets the document's information dictionary.
    /// </summary>
    public CosObjectReference? Info
    {
        get => Dictionary.Get<CosObjectReference>(CosNames.Info);
        set => Dictionary.Set(CosNames.Info, value);
    }

    /// <summary>
    /// Gets or sets an array of two byte-strings
    /// constituting a file identifier.
    /// </summary>
    public CosArray? Id
    {
        get => Dictionary.Get<CosArray>(CosNames.Id);
        set => Dictionary.Set(CosNames.Id, value);
    }

    public CosTrailer(CosDictionary dictionary)
    {
        Dictionary = dictionary;
    }
}