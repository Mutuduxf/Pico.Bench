namespace PicoBench.Generators;

/// <summary>
/// Incremental source generator that discovers [BenchmarkClass]-attributed types
/// and generates AOT-compatible <c>IBenchmarkClass</c> implementations.
/// </summary>
[Generator]
public sealed class BenchmarkGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Configures the incremental pipelines that validate benchmark classes and emit source.
    /// </summary>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context
            .SyntaxProvider
            .ForAttributeWithMetadataName(
                BenchmarkClassAnalyzer.BenchmarkClassAttributeName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => BenchmarkClassAnalyzer.AnalyzeTarget(ctx, ct)
            )
            .Where(static result => result is not null)
            .Select(static (result, _) => result!);

        context.RegisterSourceOutput(
            provider,
            static (spc, result) =>
            {
                foreach (var diagnostic in result.Diagnostics)
                    spc.ReportDiagnostic(diagnostic);

                if (result.Model is null || result.HasErrors)
                    return;

                var model = result.Model;
                var code = Emitter.Generate(model);
                spc.AddSource(BenchmarkGenerator.CreateHintName(model), code);
            }
        );
    }

    private static string CreateHintName(BenchmarkClassModel model)
    {
        var qualifiedName = model.Namespace is null
            ? model.ClassName
            : $"{model.Namespace}.{model.ClassName}";

        return qualifiedName.Replace('.', '_') + ".g.cs";
    }
}
