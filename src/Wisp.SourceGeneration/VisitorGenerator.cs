using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Wisp.SourceGeneration.Models;
using Wisp.SourceGeneration.Helpers;
using Wisp.SourceGeneration.Extensions;

namespace Wisp.SourceGeneration;

/// <inheritdoc />
[Generator(LanguageNames.CSharp)]
public sealed partial class VisitorGenerator : IIncrementalGenerator
{
    private const string CosVisitableAttributeFullName = "Wisp.CosVisitableAttribute";
    private const string CosPrimitiveNamespace = "Wisp";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Gather info for all the annotated visitable primitives
        IncrementalValuesProvider<Result<CosPrimitiveInfo?>> cosPrimitiveInfoResults =
            context.SyntaxProvider.ForAttributeWithMetadataName(CosVisitableAttributeFullName,
                predicate: static (node, token) => node is ClassDeclarationSyntax,
                transform: static (context, token) =>
                {
                    var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;
                    var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

                    _ = TryGetInfo(
                        classDeclaration,
                        typeSymbol,
                        context.SemanticModel,
                        token,
                        out CosPrimitiveInfo? instructionInfo,
                        out ImmutableArray<DiagnosticInfo> diagnostics);

                    return new Result<CosPrimitiveInfo?>(instructionInfo, diagnostics);
                })
            .WithTrackingName(nameof(cosPrimitiveInfoResults));

        context.ReportDiagnostics(cosPrimitiveInfoResults.Select(static (item, _) => item.Errors));

        // Drop the nulls
        IncrementalValuesProvider<CosPrimitiveInfo> instructions = cosPrimitiveInfoResults
            .Where(static (result) => result.Value is not null)
            .Select(static (result, token) => result.Value!);

        // Generate instruction definitions
        context.RegisterSourceOutput(instructions, static (context, instruction) =>
        {
            using IndentedTextWriter writer = new();

            writer.WriteLine($"namespace {CosPrimitiveNamespace};");
            writer.WriteLine();

            WriteVisitableCosPrimitiveSyntax(writer, instruction);

            context.AddSource($"{CosPrimitiveNamespace}.{instruction.Name}.ICosPrimitive.g.cs", writer.ToString());
        });

        // Gather all primitive information and generate factory method
        //var allInstructions = instructions.Collect();
        //
        //context.RegisterSourceOutput(allInstructions, static (context, instructions) =>
        //{
        //    using IndentedTextWriter writer = new();
        //
        //    writer.WriteLine($"namespace {CosPrimitiveNamespace};");
        //    writer.WriteLine();
        //
        //    WriteInstructionReadSyntax(writer, instructions);
        //
        //    context.AddSource($"{CosPrimitiveNamespace}.IInstruction.Read.g.cs", writer.ToString());
        //});
    }

    // TODO: Overkill currently
    internal static bool TryGetInfo(
        ClassDeclarationSyntax classSyntax,
        INamedTypeSymbol typeSymbol,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out CosPrimitiveInfo? cosPrimitiveInfo,
        out ImmutableArray<DiagnosticInfo> diagnostics)
    {
        cosPrimitiveInfo = new CosPrimitiveInfo(typeSymbol.Name);
        diagnostics = [];

        return true;
    }
}
