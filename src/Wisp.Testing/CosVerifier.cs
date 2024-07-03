namespace Wisp.Testing;

public static class CosVerifier
{
    public static SettingsTask Verify(
        CosDocument model,
        CosSerializerSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        settings ??= new CosSerializerSettings();
        var output = CosSerializer.Serialize(model, settings);
        return Verifier.Verify(output, extension: "xml");
    }
}