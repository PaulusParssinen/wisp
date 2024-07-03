using System.Collections.Immutable;

using Wisp.SourceGeneration.Models;
using Wisp.SourceGeneration.Helpers;
using Wisp.SourceGeneration.Extensions;

namespace Wisp.SourceGeneration;

public sealed partial class VisitorGenerator
{
    /// <summary>
    /// Writes syntax for a class definition of the given <paramref name="cosPrimitive"/>.
    /// </summary>
    internal static void WriteVisitableCosPrimitiveSyntax(IndentedTextWriter writer,
       CosPrimitiveInfo cosPrimitive)
    {
        string primitiveNameWithoutPrefix = cosPrimitive.Name[3..];

        writer.WriteGeneratedAttributes(nameof(VisitorGenerator));

        writer.WriteLine($"public sealed partial class {cosPrimitive.Name} : global::Wisp.ICosPrimitive");

        using (writer.WriteBlock())
        {
            writer.WriteLine($$"""
                [DebuggerStepThrough]
                public void Accept<TContext>(ICosVisitor<TContext> visitor, TContext context)
                {
                    visitor.Visit{{primitiveNameWithoutPrefix}}(this, context);
                }

                [DebuggerStepThrough]
                public TResult Accept<TContext, TResult>(ICosVisitor<TContext, TResult> visitor, TContext context)
                {
                    return visitor.Visit{{primitiveNameWithoutPrefix}}(this, context);
                }
                """, true);
        }
    }

    /// <summary>
    /// Writes syntax for a the visitor definition.
    /// </summary>
    internal static void WriteVisitorSyntax(IndentedTextWriter writer, ImmutableArray<CosPrimitiveInfo> primitives)
    {
        throw new NotImplementedException();
    }
}
