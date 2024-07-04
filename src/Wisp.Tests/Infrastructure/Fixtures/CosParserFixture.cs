namespace Wisp.Tests.Infrastructure.Fixtures;

public static class CosParserFixture
{
    public static ICosPrimitive WriteAndParse(ICosPrimitive primitive)
    {
        var doc = new CosDocument();
        doc.Objects.Set(new CosObject(new CosObjectId(1, 0), primitive));
        using var stream = new MemoryStream();
        doc.Save(stream, new CosWriterSettings
        {
            LeaveStreamOpen = true,
        });

        // When
        var newDocument = CosDocument.Open(stream);
        bool success = newDocument.Objects.TryGet(new CosObjectId(1, 0), CosResolveFlags.None, out var obj);

        Assert.True(success);
        Assert.NotNull(obj);
        return obj.Object;
    }
}