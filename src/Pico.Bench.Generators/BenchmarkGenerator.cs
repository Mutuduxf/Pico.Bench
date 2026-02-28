using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Pico.Bench.Generators;

/// <summary>
/// Incremental source generator that discovers [BenchmarkClass]-attributed types
/// and generates AOT-compatible <see cref="IBenchmarkClass"/> implementations.
/// </summary>
[Generator]
public sealed class BenchmarkGenerator : IIncrementalGenerator
{
    // Fully-qualified attribute names used for matching (no assembly qualification).
    private const string BenchmarkClassAttr = "Pico.Bench.BenchmarkClassAttribute";
    private const string BenchmarkAttr = "Pico.Bench.BenchmarkAttribute";
    private const string GlobalSetupAttr = "Pico.Bench.GlobalSetupAttribute";
    private const string GlobalCleanupAttr = "Pico.Bench.GlobalCleanupAttribute";
    private const string IterationSetupAttr = "Pico.Bench.IterationSetupAttribute";
    private const string IterationCleanupAttr = "Pico.Bench.IterationCleanupAttribute";
    private const string ParamsAttr = "Pico.Bench.ParamsAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context
            .SyntaxProvider
            .ForAttributeWithMetadataName(
                BenchmarkClassAttr,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => ExtractModel(ctx, ct)
            )
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        context.RegisterSourceOutput(
            provider,
            static (spc, model) =>
            {
                var code = Emitter.Generate(model);
                var hintName = model.Namespace is null
                    ? model.ClassName
                    : $"{model.Namespace}.{model.ClassName}";
                hintName = hintName.Replace('.', '_') + ".g.cs";
                spc.AddSource(hintName, code);
            }
        );
    }

    private static BenchmarkClassModel? ExtractModel(
        GeneratorAttributeSyntaxContext ctx,
        System.Threading.CancellationToken ct
    )
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol typeSymbol)
            return null;

        ct.ThrowIfCancellationRequested();

        // Namespace
        var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : typeSymbol.ContainingNamespace.ToDisplayString();

        // Access modifier
        var accessibility = typeSymbol.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            _ => "internal"
        };

        // Description from [BenchmarkClass(Description = "...")]
        string? description = null;
        foreach (var attr in ctx.Attributes)
        {
            foreach (var named in attr.NamedArguments)
            {
                if (named.Key == "Description" && named.Value.Value is string desc)
                    description = desc;
            }
        }

        // Scan members
        string? globalSetup = null;
        string? globalCleanup = null;
        string? iterSetup = null;
        string? iterCleanup = null;
        var methods = ImmutableArray.CreateBuilder<BenchmarkMethodModel>();
        var paramsProps = ImmutableArray.CreateBuilder<ParamsPropertyModel>();

        foreach (var member in typeSymbol.GetMembers())
        {
            ct.ThrowIfCancellationRequested();

            if (member is IMethodSymbol method)
            {
                foreach (var attr in method.GetAttributes())
                {
                    var attrName = attr.AttributeClass?.ToDisplayString();
                    switch (attrName)
                    {
                        case BenchmarkAttr:
                        {
                            var isBaseline = false;
                            string? methodDesc = null;
                            foreach (var named in attr.NamedArguments)
                            {
                                if (named.Key == "Baseline" && named.Value.Value is true)
                                    isBaseline = true;
                                if (named.Key == "Description" && named.Value.Value is string d)
                                    methodDesc = d;
                            }

                            methods.Add(
                                new BenchmarkMethodModel
                                {
                                    Name = method.Name,
                                    IsBaseline = isBaseline,
                                    Description = methodDesc
                                }
                            );
                            break;
                        }
                        case GlobalSetupAttr:
                            globalSetup = method.Name;
                            break;
                        case GlobalCleanupAttr:
                            globalCleanup = method.Name;
                            break;
                        case IterationSetupAttr:
                            iterSetup = method.Name;
                            break;
                        case IterationCleanupAttr:
                            iterCleanup = method.Name;
                            break;
                    }
                }
            }
            else if (member is IPropertySymbol prop)
            {
                var paramAttr = FindAttribute(prop.GetAttributes(), ParamsAttr);
                if (paramAttr != null)
                    paramsProps.Add(BuildParamsModel(prop.Name, prop.Type, paramAttr));
            }
            else if (member is IFieldSymbol field)
            {
                var paramAttr = FindAttribute(field.GetAttributes(), ParamsAttr);
                if (paramAttr != null)
                    paramsProps.Add(BuildParamsModel(field.Name, field.Type, paramAttr));
            }
        }

        if (methods.Count == 0)
            return null; // No benchmark methods found; skip generation.

        return new BenchmarkClassModel
        {
            Namespace = ns,
            ClassName = typeSymbol.Name,
            AccessModifier = accessibility,
            Description = description,
            GlobalSetupMethod = globalSetup,
            GlobalCleanupMethod = globalCleanup,
            IterationSetupMethod = iterSetup,
            IterationCleanupMethod = iterCleanup,
            Methods = methods.ToImmutable(),
            ParamsProperties = paramsProps.ToImmutable()
        };
    }

    private static AttributeData? FindAttribute(
        ImmutableArray<AttributeData> attrs,
        string fullyQualifiedName
    )
    {
        foreach (var attr in attrs)
        {
            if (attr.AttributeClass?.ToDisplayString() == fullyQualifiedName)
                return attr;
        }
        return null;
    }

    private static ParamsPropertyModel BuildParamsModel(
        string memberName,
        ITypeSymbol memberType,
        AttributeData attr
    )
    {
        var typeName = memberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var values = ImmutableArray.CreateBuilder<string>();

        if (attr.ConstructorArguments.Length > 0)
        {
            var arg = attr.ConstructorArguments[0];
            if (arg.Kind == TypedConstantKind.Array)
            {
                foreach (var element in arg.Values)
                    values.Add(FormatConstant(element));
            }
        }

        return new ParamsPropertyModel
        {
            Name = memberName,
            TypeFullName = typeName,
            FormattedValues = values.ToImmutable()
        };
    }

    /// <summary>
    /// Formats a <see cref="TypedConstant"/> as a C# literal string.
    /// </summary>
    internal static string FormatConstant(TypedConstant constant)
    {
        if (constant.IsNull)
            return "null";

        var value = constant.Value;
        return value switch
        {
            string s => FormatStringLiteral(s),
            bool b => b ? "true" : "false",
            char c => FormatCharLiteral(c),
            float f => FormatFloatLiteral(f),
            double d => FormatDoubleLiteral(d),
            decimal m => m.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m",
            long l => l.ToString(System.Globalization.CultureInfo.InvariantCulture) + "L",
            ulong ul => ul.ToString(System.Globalization.CultureInfo.InvariantCulture) + "UL",
            uint ui => ui.ToString(System.Globalization.CultureInfo.InvariantCulture) + "U",
            _ => value?.ToString() ?? "default"
        };
    }

    private static string FormatStringLiteral(string s)
    {
        var sb = new System.Text.StringBuilder("\"");
        foreach (var c in s)
        {
            sb.Append(
                c switch
                {
                    '"' => "\\\"",
                    '\\' => "\\\\",
                    '\n' => "\\n",
                    '\r' => "\\r",
                    '\t' => "\\t",
                    '\0' => "\\0",
                    _ when c < 0x20 => $"\\u{(int)c:X4}",
                    _ => c.ToString()
                }
            );
        }
        sb.Append('"');
        return sb.ToString();
    }

    private static string FormatCharLiteral(char c)
    {
        return c switch
        {
            '\'' => "'\\''",
            '\\' => "'\\\\'",
            '\n' => "'\\n'",
            '\r' => "'\\r'",
            '\t' => "'\\t'",
            '\0' => "'\\0'",
            _ when c < 0x20 || c > 0x7E => $"'\\u{(int)c:X4}'",
            _ => $"'{c}'"
        };
    }

    private static string FormatFloatLiteral(float f)
    {
        if (float.IsNaN(f))
            return "float.NaN";
        if (float.IsPositiveInfinity(f))
            return "float.PositiveInfinity";
        if (float.IsNegativeInfinity(f))
            return "float.NegativeInfinity";
        return f.ToString("G", System.Globalization.CultureInfo.InvariantCulture) + "F";
    }

    private static string FormatDoubleLiteral(double d)
    {
        if (double.IsNaN(d))
            return "double.NaN";
        if (double.IsPositiveInfinity(d))
            return "double.PositiveInfinity";
        if (double.IsNegativeInfinity(d))
            return "double.NegativeInfinity";
        return d.ToString("G", System.Globalization.CultureInfo.InvariantCulture) + "D";
    }
}
