namespace PicoBench.Generators;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor BenchmarkClassMustBePartial =
        new(
            id: "PBGEN001",
            title: "Benchmark class must be partial",
            messageFormat: "Benchmark class '{0}' must be declared partial",
            category: "PicoBench.Generators",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

    public static readonly DiagnosticDescriptor NoBenchmarkMethods =
        new(
            id: "PBGEN002",
            title: "No benchmark methods found",
            messageFormat: "Benchmark class '{0}' must declare at least one valid [Benchmark] method",
            category: "PicoBench.Generators",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

    public static readonly DiagnosticDescriptor InvalidBenchmarkMethod =
        new(
            id: "PBGEN003",
            title: "Invalid benchmark method",
            messageFormat: "Benchmark method '{0}' must be an instance, non-generic method with no parameters",
            category: "PicoBench.Generators",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

    public static readonly DiagnosticDescriptor InvalidLifecycleMethod =
        new(
            id: "PBGEN004",
            title: "Invalid lifecycle method",
            messageFormat: "{0} method '{1}' must be an instance, non-generic, parameterless void method",
            category: "PicoBench.Generators",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

    public static readonly DiagnosticDescriptor DuplicateBaseline =
        new(
            id: "PBGEN005",
            title: "Duplicate baseline benchmark",
            messageFormat: "Only one [Benchmark(Baseline = true)] method is allowed per benchmark class",
            category: "PicoBench.Generators",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

    public static readonly DiagnosticDescriptor DuplicateLifecycleMethod =
        new(
            id: "PBGEN006",
            title: "Duplicate lifecycle method",
            messageFormat: "Only one {0} method is allowed per benchmark class",
            category: "PicoBench.Generators",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

    public static readonly DiagnosticDescriptor InvalidParamsMember =
        new(
            id: "PBGEN007",
            title: "Invalid [Params] member",
            messageFormat: "[Params] member '{0}' must be an instance writable property or a non-readonly field",
            category: "PicoBench.Generators",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

    public static readonly DiagnosticDescriptor IncompatibleParamsValue =
        new(
            id: "PBGEN008",
            title: "Incompatible [Params] value",
            messageFormat: "[Params] value '{0}' is not compatible with member '{1}' of type '{2}'",
            category: "PicoBench.Generators",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
}
