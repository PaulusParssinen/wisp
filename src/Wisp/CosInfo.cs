namespace Wisp;

public sealed class CosInfo : CosObjectReference<CosDictionary>
{
    /// <summary>
    /// Gets or sets the document's title.
    /// </summary>
    public CosString? Title
    {
        get => Object.Get<CosString>(CosNames.Title);
        set => Object.Set(CosNames.Title, value);
    }

    /// <summary>
    /// Gets or sets the name of the person who created the document.
    /// </summary>
    public CosString? Author
    {
        get => Object.Get<CosString>(CosNames.Author);
        set => Object.Set(CosNames.Author, value);
    }

    /// <summary>
    /// Gets or sets the subject of the document.
    /// </summary>
    public CosString? Subject
    {
        get => Object.Get<CosString>(CosNames.Subject);
        set => Object.Set(CosNames.Subject, value);
    }

    /// <summary>
    /// Gets or sets the keywords associated with the document.
    /// </summary>
    public CosString? Keywords
    {
        get => Object.Get<CosString>(CosNames.Keywords);
        set => Object.Set(CosNames.Keywords, value);
    }

    /// <summary>
    /// Gets or sets the name of the conforming product that created the
    /// original document, if the document was converted to PDF from
    /// another format.
    /// </summary>
    public CosString? Creator
    {
        get => Object.Get<CosString>(CosNames.Creator);
        set => Object.Set(CosNames.Creator, value);
    }

    /// <summary>
    /// Gets or sets the name of the conforming product that converted
    /// it to PDF, if the document was converted to PDF from another format.
    /// </summary>
    public CosString? Producer
    {
        get => Object.Get<CosString>(CosNames.Producer);
        set => Object.Set(CosNames.Producer, value);
    }

    /// <summary>
    /// Gets or sets the date and time the document was created.
    /// </summary>
    public CosDate? CreationDate
    {
        get => Object.Get<CosDate>(CosNames.CreationDate);
        set => Object.Set(CosNames.CreationDate, value);
    }

    /// <summary>
    /// Gets or sets the date and time the document
    /// was most recently modified.
    /// </summary>
    public CosDate? ModDate
    {
        get => Object.Get<CosDate>(CosNames.ModDate);
        set => Object.Set(CosNames.ModDate, value);
    }

    public CosInfo(CosObject obj)
        : base(obj)
    { }

    internal CosInfo(CosObjectReference id, CosDictionary dictionary)
        : base(id, dictionary)
    { }
}