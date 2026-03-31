namespace PicoBench.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Incremental source generator that discovers [BenchmarkClass]-attributed types
/// and generates AOT-compatible <c>IBenchmarkClass</c> implementations.
/// </summary>
[Generator]
public sealed class BenchmarkGenerator : IIncrementalGenerator
{
    // Fully-qualified attribute names used for matching (no assembly qualification).
    private const string BenchmarkClassAttr = "PicoBench.BenchmarkClassAttribute";
    private const string BenchmarkAttr = "PicoBench.BenchmarkAttribute";
    private const string GlobalSetupAttr = "PicoBench.GlobalSetupAttribute";
    private const string GlobalCleanupAttr = "PicoBench.GlobalCleanupAttribute";
    private const string IterationSetupAttr = "PicoBench.IterationSetupAttribute";
    private const string IterationCleanupAttr = "PicoBench.IterationCleanupAttribute";
    private const string ParamsAttr = "PicoBench.ParamsAttribute";

    /// <summary>
    /// Configures the incremental pipelines that validate benchmark classes and emit source.
    /// </summary>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context
            .SyntaxProvider
            .ForAttributeWithMetadataName(
                BenchmarkClassAttr,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => AnalyzeTarget(ctx, ct)
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
                var hintName = model.Namespace is null
                    ? model.ClassName
                    : $"{model.Namespace}.{model.ClassName}";
                hintName = hintName.Replace('.', '_') + ".g.cs";
                spc.AddSource(hintName, code);
            }
        );
    }

    private static GeneratorAnalysisResult AnalyzeTarget(
        GeneratorAttributeSyntaxContext ctx,
        System.Threading.CancellationToken ct
    )
    {
        var diagnostics = new List<Diagnostic>();

        if (ctx.TargetSymbol is not INamedTypeSymbol typeSymbol)
            return new GeneratorAnalysisResult(null, diagnostics.ToImmutableArray());

        ct.ThrowIfCancellationRequested();

        if (!IsPartial(typeSymbol))
        {
            diagnostics.Add(
                Diagnostic.Create(
                    DiagnosticDescriptors.BenchmarkClassMustBePartial,
                    GetTypeLocation(typeSymbol),
                    typeSymbol.Name
                )
            );
        }

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
        var baselineMethod = default(string);
        var methods = ImmutableArray.CreateBuilder<BenchmarkMethodModel>();
        var paramsProps = ImmutableArray.CreateBuilder<ParamsPropertyModel>();

        foreach (var member in typeSymbol.GetMembers())
        {
            ct.ThrowIfCancellationRequested();

            switch (member)
            {
                case IMethodSymbol method:
                {
                    foreach (var attr in method.GetAttributes())
                    {
                        var attrName = attr.AttributeClass?.ToDisplayString();
                        switch (attrName)
                        {
                            case BenchmarkAttr:
                            {
                                if (!IsValidBenchmarkMethod(method))
                                {
                                    diagnostics.Add(
                                        Diagnostic.Create(
                                            DiagnosticDescriptors.InvalidBenchmarkMethod,
                                            GetAttributeLocation(attr, ct),
                                            method.Name
                                        )
                                    );
                                    break;
                                }

                                var isBaseline = false;
                                string? methodDesc = null;
                                foreach (var named in attr.NamedArguments)
                                {
                                    switch (named.Key)
                                    {
                                        case "Baseline" when named.Value.Value is true:
                                            isBaseline = true;
                                            break;
                                        case "Description" when named.Value.Value is string d:
                                            methodDesc = d;
                                            break;
                                    }
                                }

                                if (isBaseline && baselineMethod is not null)
                                {
                                    diagnostics.Add(
                                        Diagnostic.Create(
                                            DiagnosticDescriptors.DuplicateBaseline,
                                            GetAttributeLocation(attr, ct)
                                        )
                                    );
                                    break;
                                }

                                if (isBaseline)
                                    baselineMethod = method.Name;

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
                                RegisterLifecycleMethod(
                                    method,
                                    attr,
                                    ref globalSetup,
                                    "[GlobalSetup]",
                                    diagnostics,
                                    ct
                                );
                                break;
                            case GlobalCleanupAttr:
                                RegisterLifecycleMethod(
                                    method,
                                    attr,
                                    ref globalCleanup,
                                    "[GlobalCleanup]",
                                    diagnostics,
                                    ct
                                );
                                break;
                            case IterationSetupAttr:
                                RegisterLifecycleMethod(
                                    method,
                                    attr,
                                    ref iterSetup,
                                    "[IterationSetup]",
                                    diagnostics,
                                    ct
                                );
                                break;
                            case IterationCleanupAttr:
                                RegisterLifecycleMethod(
                                    method,
                                    attr,
                                    ref iterCleanup,
                                    "[IterationCleanup]",
                                    diagnostics,
                                    ct
                                );
                                break;
                        }
                    }

                    break;
                }
                case IPropertySymbol prop:
                {
                    var paramAttr = FindAttribute(prop.GetAttributes(), ParamsAttr);
                    if (paramAttr != null)
                    {
                        var paramModel = BuildParamsModel(
                            prop,
                            prop.Name,
                            prop.Type,
                            paramAttr,
                            ctx.SemanticModel.Compilation,
                            diagnostics,
                            ct
                        );
                        if (paramModel is not null)
                            paramsProps.Add(paramModel);
                    }
                    break;
                }
                case IFieldSymbol field:
                {
                    var paramAttr = FindAttribute(field.GetAttributes(), ParamsAttr);
                    if (paramAttr != null)
                    {
                        var paramModel = BuildParamsModel(
                            field,
                            field.Name,
                            field.Type,
                            paramAttr,
                            ctx.SemanticModel.Compilation,
                            diagnostics,
                            ct
                        );
                        if (paramModel is not null)
                            paramsProps.Add(paramModel);
                    }
                    break;
                }
            }
        }

        if (methods.Count == 0)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    DiagnosticDescriptors.NoBenchmarkMethods,
                    GetTypeLocation(typeSymbol),
                    typeSymbol.Name
                )
            );
        }

        if (diagnostics.Any(static d => d.Severity == DiagnosticSeverity.Error))
            return new GeneratorAnalysisResult(null, diagnostics.ToImmutableArray());

        return new GeneratorAnalysisResult(
            new BenchmarkClassModel
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
            },
            diagnostics.ToImmutableArray()
        );
    }

    private static bool IsPartial(INamedTypeSymbol typeSymbol)
    {
        foreach (var syntaxRef in typeSymbol.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is ClassDeclarationSyntax classDecl
                && classDecl.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsValidBenchmarkMethod(IMethodSymbol method)
    {
        return !method.IsStatic && !method.IsGenericMethod && method.Parameters.Length == 0;
    }

    private static bool IsValidLifecycleMethod(IMethodSymbol method)
    {
        return !method.IsStatic
            && !method.IsGenericMethod
            && method.Parameters.Length == 0
            && method.ReturnsVoid;
    }

    private static void RegisterLifecycleMethod(
        IMethodSymbol method,
        AttributeData attr,
        ref string? target,
        string attributeName,
        List<Diagnostic> diagnostics,
        System.Threading.CancellationToken ct
    )
    {
        if (!IsValidLifecycleMethod(method))
        {
            diagnostics.Add(
                Diagnostic.Create(
                    DiagnosticDescriptors.InvalidLifecycleMethod,
                    GetAttributeLocation(attr, ct),
                    attributeName,
                    method.Name
                )
            );
            return;
        }

        if (target is not null)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    DiagnosticDescriptors.DuplicateLifecycleMethod,
                    GetAttributeLocation(attr, ct),
                    attributeName
                )
            );
            return;
        }

        target = method.Name;
    }

    private static AttributeData? FindAttribute(
        ImmutableArray<AttributeData> attrs,
        string fullyQualifiedName
    )
    {
        return Enumerable.FirstOrDefault(
            attrs,
            attr => attr.AttributeClass?.ToDisplayString() == fullyQualifiedName
        );
    }

    private static ParamsPropertyModel BuildParamsModel(
        ISymbol memberSymbol,
        string memberName,
        ITypeSymbol memberType,
        AttributeData attr,
        Compilation compilation,
        List<Diagnostic> diagnostics,
        System.Threading.CancellationToken ct
    )
    {
        if (!IsValidParamsMember(memberSymbol))
        {
            diagnostics.Add(
                Diagnostic.Create(
                    DiagnosticDescriptors.InvalidParamsMember,
                    GetAttributeLocation(attr, ct),
                    memberName
                )
            );
            return null!;
        }

        var typeName = memberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var values = ImmutableArray.CreateBuilder<string>();

        if (attr.ConstructorArguments.Length <= 0)
            return new ParamsPropertyModel
            {
                Name = memberName,
                TypeFullName = typeName,
                FormattedValues = values.ToImmutable()
            };
        var arg = attr.ConstructorArguments[0];
        if (arg.Kind != TypedConstantKind.Array)
            return new ParamsPropertyModel
            {
                Name = memberName,
                TypeFullName = typeName,
                FormattedValues = values.ToImmutable()
            };

        foreach (var element in arg.Values)
        {
            if (!IsCompatibleWithTargetType(element, memberType, compilation))
            {
                diagnostics.Add(
                    Diagnostic.Create(
                        DiagnosticDescriptors.IncompatibleParamsValue,
                        GetAttributeLocation(attr, ct),
                        element.ToCSharpString(),
                        memberName,
                        memberType.ToDisplayString()
                    )
                );
                return null!;
            }

            values.Add(FormatConstant(element, memberType));
        }

        return new ParamsPropertyModel
        {
            Name = memberName,
            TypeFullName = typeName,
            FormattedValues = values.ToImmutable()
        };
    }

    private static bool IsValidParamsMember(ISymbol memberSymbol)
    {
        return memberSymbol switch
        {
            IPropertySymbol property => !property.IsStatic
                && property.SetMethod is not null
                && !property.SetMethod.IsInitOnly,
            IFieldSymbol field => !field.IsStatic && !field.IsReadOnly && !field.IsConst,
            _ => false
        };
    }

    private static bool IsCompatibleWithTargetType(
        TypedConstant constant,
        ITypeSymbol memberType,
        Compilation compilation
    )
    {
        if (constant.IsNull)
            return memberType.IsReferenceType || memberType.NullableAnnotation == NullableAnnotation.Annotated;

        if (constant.Type is null)
            return false;

        var conversion = compilation.ClassifyConversion(constant.Type, memberType);
        return conversion.Exists && (conversion.IsIdentity || conversion.IsImplicit);
    }

    /// <summary>
    /// Formats a <see cref="TypedConstant"/> as a C# literal string.
    /// </summary>
    private static string FormatConstant(TypedConstant constant, ITypeSymbol? targetType)
    {
        if (constant.IsNull)
            return "null";

        if (targetType is INamedTypeSymbol enumType && enumType.TypeKind == TypeKind.Enum)
            return FormatEnumLiteral(enumType, constant.Value);

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

    private static string FormatEnumLiteral(INamedTypeSymbol enumType, object? value)
    {
        var underlyingValue = value is null
            ? "0"
            : FormatPrimitiveNumericLiteral(value);

        return $"({enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){underlyingValue}";
    }

    private static string FormatPrimitiveNumericLiteral(object value)
    {
        return value switch
        {
            byte b => b.ToString(System.Globalization.CultureInfo.InvariantCulture),
            sbyte sb => sb.ToString(System.Globalization.CultureInfo.InvariantCulture),
            short s => s.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ushort us => us.ToString(System.Globalization.CultureInfo.InvariantCulture),
            int i => i.ToString(System.Globalization.CultureInfo.InvariantCulture),
            uint ui => ui.ToString(System.Globalization.CultureInfo.InvariantCulture) + "U",
            long l => l.ToString(System.Globalization.CultureInfo.InvariantCulture) + "L",
            ulong ul => ul.ToString(System.Globalization.CultureInfo.InvariantCulture) + "UL",
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "0"
        };
    }

    private static Location GetTypeLocation(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.Locations.FirstOrDefault(static location => location.IsInSource)
            ?? Location.None;
    }

    private static Location GetAttributeLocation(
        AttributeData attr,
        System.Threading.CancellationToken ct
    )
    {
        return attr.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation() ?? Location.None;
    }

    private sealed class GeneratorAnalysisResult
    {
        public GeneratorAnalysisResult(
            BenchmarkClassModel? model,
            ImmutableArray<Diagnostic> diagnostics
        )
        {
            Model = model;
            Diagnostics = diagnostics;
        }

        public BenchmarkClassModel? Model { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public bool HasErrors => Diagnostics.Any(static d => d.Severity == DiagnosticSeverity.Error);
    }

    private static string FormatStringLiteral(string s)
    {
        var sb = new StringBuilder("\"");
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
