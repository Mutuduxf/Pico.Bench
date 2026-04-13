namespace PicoBench.Generators;

internal static class BenchmarkClassAnalyzer
{
    internal const string BenchmarkClassAttributeName = "PicoBench.BenchmarkClassAttribute";

    private const string BenchmarkAttributeName = "PicoBench.BenchmarkAttribute";
    private const string GlobalSetupAttributeName = "PicoBench.GlobalSetupAttribute";
    private const string GlobalCleanupAttributeName = "PicoBench.GlobalCleanupAttribute";
    private const string IterationSetupAttributeName = "PicoBench.IterationSetupAttribute";
    private const string IterationCleanupAttributeName = "PicoBench.IterationCleanupAttribute";
    private const string ParamsAttributeName = "PicoBench.ParamsAttribute";

    internal static GeneratorAnalysisResult AnalyzeTarget(
        GeneratorAttributeSyntaxContext ctx,
        CancellationToken ct
    )
    {
        var diagnostics = new List<Diagnostic>();

        if (ctx.TargetSymbol is not INamedTypeSymbol typeSymbol)
            return new GeneratorAnalysisResult(null, [..diagnostics]);

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

        var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : typeSymbol.ContainingNamespace.ToDisplayString();

        var accessibility = typeSymbol.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            _ => "internal"
        };

        string? description = null;
        foreach (var named in ctx.Attributes.SelectMany(attr => attr.NamedArguments))
        {
            if (named is { Key: "Description", Value.Value: string desc })
                description = desc;
        }

        string? globalSetup = null;
        string? globalCleanup = null;
        string? iterSetup = null;
        string? iterCleanup = null;
        string? baselineMethod = null;
        var hasBenchmarkDeclarations = false;
        var methods = ImmutableArray.CreateBuilder<BenchmarkMethodModel>();
        var paramsProps = ImmutableArray.CreateBuilder<ParamsPropertyModel>();

        foreach (var member in typeSymbol.GetMembers())
        {
            ct.ThrowIfCancellationRequested();

            switch (member)
            {
                case IMethodSymbol method:
                    AnalyzeMethod(
                        method,
                        diagnostics,
                        ref globalSetup,
                        ref globalCleanup,
                        ref iterSetup,
                        ref iterCleanup,
                        ref baselineMethod,
                        ref hasBenchmarkDeclarations,
                        methods,
                        ct
                    );
                    break;
                case IPropertySymbol property:
                    TryAddParamsModel(
                        property,
                        property.Name,
                        property.Type,
                        property.GetAttributes(),
                        ctx.SemanticModel.Compilation,
                        diagnostics,
                        paramsProps,
                        ct
                    );
                    break;
                case IFieldSymbol field:
                    TryAddParamsModel(
                        field,
                        field.Name,
                        field.Type,
                        field.GetAttributes(),
                        ctx.SemanticModel.Compilation,
                        diagnostics,
                        paramsProps,
                        ct
                    );
                    break;
            }
        }

        if (!hasBenchmarkDeclarations)
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
            return new GeneratorAnalysisResult(null, [..diagnostics]);

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
            [..diagnostics]
        );
    }

    private static void AnalyzeMethod(
        IMethodSymbol method,
        List<Diagnostic> diagnostics,
        ref string? globalSetup,
        ref string? globalCleanup,
        ref string? iterSetup,
        ref string? iterCleanup,
        ref string? baselineMethod,
        ref bool hasBenchmarkDeclarations,
        ImmutableArray<BenchmarkMethodModel>.Builder methods,
        CancellationToken ct
    )
    {
        foreach (var attr in method.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString();
            switch (attrName)
            {
                case BenchmarkAttributeName:
                    hasBenchmarkDeclarations = true;
                    RegisterBenchmarkMethod(
                        method,
                        attr,
                        diagnostics,
                        ref baselineMethod,
                        methods,
                        ct
                    );
                    break;
                case GlobalSetupAttributeName:
                    RegisterLifecycleMethod(
                        method,
                        attr,
                        ref globalSetup,
                        "[GlobalSetup]",
                        diagnostics,
                        ct
                    );
                    break;
                case GlobalCleanupAttributeName:
                    RegisterLifecycleMethod(
                        method,
                        attr,
                        ref globalCleanup,
                        "[GlobalCleanup]",
                        diagnostics,
                        ct
                    );
                    break;
                case IterationSetupAttributeName:
                    RegisterLifecycleMethod(
                        method,
                        attr,
                        ref iterSetup,
                        "[IterationSetup]",
                        diagnostics,
                        ct
                    );
                    break;
                case IterationCleanupAttributeName:
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
    }

    private static void RegisterBenchmarkMethod(
        IMethodSymbol method,
        AttributeData attr,
        List<Diagnostic> diagnostics,
        ref string? baselineMethod,
        ImmutableArray<BenchmarkMethodModel>.Builder methods,
        CancellationToken ct
    )
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
            return;
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
                case "Description" when named.Value.Value is string description:
                    methodDesc = description;
                    break;
            }
        }

        switch (isBaseline)
        {
            case true when baselineMethod is not null:
                diagnostics.Add(
                    Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateBaseline,
                        GetAttributeLocation(attr, ct)
                    )
                );
                return;
            case true:
                baselineMethod = method.Name;
                break;
        }

        methods.Add(
            new BenchmarkMethodModel
            {
                Name = method.Name,
                IsBaseline = isBaseline,
                Description = methodDesc
            }
        );
    }

    private static void TryAddParamsModel(
        ISymbol memberSymbol,
        string memberName,
        ITypeSymbol memberType,
        ImmutableArray<AttributeData> attrs,
        Compilation compilation,
        List<Diagnostic> diagnostics,
        ImmutableArray<ParamsPropertyModel>.Builder paramsProps,
        CancellationToken ct
    )
    {
        var paramAttr = FindAttribute(attrs, ParamsAttributeName);
        if (paramAttr is null)
            return;

        var paramModel = BuildParamsModel(
            memberSymbol,
            memberName,
            memberType,
            paramAttr,
            compilation,
            diagnostics,
            ct
        );

        if (paramModel is not null)
            paramsProps.Add(paramModel);
    }

    private static bool IsPartial(INamedTypeSymbol typeSymbol)
    {
        foreach (var syntaxRef in typeSymbol.DeclaringSyntaxReferences)
        {
            if (
                syntaxRef.GetSyntax() is ClassDeclarationSyntax classDecl
                && classDecl
                    .Modifiers
                    .Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword))
            )
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsValidBenchmarkMethod(IMethodSymbol method)
    {
        return method is { IsStatic: false, IsGenericMethod: false, Parameters.Length: 0 };
    }

    private static bool IsValidLifecycleMethod(IMethodSymbol method)
    {
        return method
            is { IsStatic: false, IsGenericMethod: false, Parameters.Length: 0, ReturnsVoid: true };
    }

    private static void RegisterLifecycleMethod(
        IMethodSymbol method,
        AttributeData attr,
        ref string? target,
        string attributeName,
        List<Diagnostic> diagnostics,
        CancellationToken ct
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
        return attrs.FirstOrDefault(
            attr => attr.AttributeClass?.ToDisplayString() == fullyQualifiedName
        );
    }

    private static ParamsPropertyModel? BuildParamsModel(
        ISymbol memberSymbol,
        string memberName,
        ITypeSymbol memberType,
        AttributeData attr,
        Compilation compilation,
        List<Diagnostic> diagnostics,
        CancellationToken ct
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
            return null;
        }

        var typeName = memberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var values = ImmutableArray.CreateBuilder<string>();

        if (attr.ConstructorArguments.Length <= 0)
        {
            return new ParamsPropertyModel
            {
                Name = memberName,
                TypeFullName = typeName,
                FormattedValues = values.ToImmutable()
            };
        }

        var arg = attr.ConstructorArguments[0];
        if (arg.Kind != TypedConstantKind.Array)
        {
            return new ParamsPropertyModel
            {
                Name = memberName,
                TypeFullName = typeName,
                FormattedValues = values.ToImmutable()
            };
        }

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
                return null;
            }

            values.Add(CSharpLiteralFormatter.FormatConstant(element, memberType));
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
            IPropertySymbol property
                => property is { IsStatic: false, SetMethod.IsInitOnly: false },
            IFieldSymbol field => field is { IsStatic: false, IsReadOnly: false, IsConst: false },
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
            return memberType.IsReferenceType
                || memberType.NullableAnnotation == NullableAnnotation.Annotated;

        if (constant.Type is null)
            return false;

        var conversion = compilation.ClassifyConversion(constant.Type, memberType);
        return conversion.Exists && (conversion.IsIdentity || conversion.IsImplicit);
    }

    private static Location GetTypeLocation(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.Locations.FirstOrDefault(static location => location.IsInSource)
            ?? Location.None;
    }

    private static Location GetAttributeLocation(AttributeData attr, CancellationToken ct)
    {
        return attr.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation() ?? Location.None;
    }
}

internal sealed class GeneratorAnalysisResult(
    BenchmarkClassModel? model,
    ImmutableArray<Diagnostic> diagnostics
)
{
    public BenchmarkClassModel? Model { get; } = model;

    public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;

    public bool HasErrors => Diagnostics.Any(static d => d.Severity == DiagnosticSeverity.Error);
}
