using System.Diagnostics;

namespace Wisp.Tests.Infrastructure;

public static class EmbeddedResourceReader
{
    public static string Read(string resourceName)
    {
        ArgumentNullException.ThrowIfNull(resourceName);

        var frame = new StackFrame(1);
        var assembly = (frame.GetMethod()?.DeclaringType?.Assembly) ?? throw new InvalidOperationException("Could not resolve caller.");

        resourceName = resourceName.Replace("/", ".", StringComparison.Ordinal);

        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream is null)
            {
                throw new InvalidOperationException("Could not load manifest resource stream.");
            }

            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    public static Stream GetStream(string resourceName)
    {
        ArgumentNullException.ThrowIfNull(resourceName);

        var frame = new StackFrame(1);
        var assembly = (frame.GetMethod()?.DeclaringType?.Assembly) ?? throw new InvalidOperationException("Could not resolve caller.");

        resourceName = resourceName.Replace("/", ".", StringComparison.Ordinal);

        return assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"Could not resolve manifest stream '{resourceName}'");
    }
}