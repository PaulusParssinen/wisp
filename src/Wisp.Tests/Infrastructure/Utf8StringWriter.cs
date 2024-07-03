using System.Text;

namespace Wisp.Tests.Infrastructure;

internal sealed class Utf8StringWriter : StringWriter
{
    public override Encoding Encoding => Encoding.UTF8;
}