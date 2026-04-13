# Source Projects

[English](README.md) | [中文](README.zh-CN.md) | [中文 (Traditional)](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

This directory contains the two library projects that make up PicoBench.

## PicoBench

The main benchmarking library targeting **netstandard2.0** with zero external dependencies.

### Key Files

| File | Purpose |
|------|---------|
| `Benchmark.cs` | Imperative API - `Run()`, `Run<TState>()`, `RunScoped<TScope>()`, `Compare()` |
| `BenchmarkRunner.cs` | Attribute-based entry point - `Run<T>()` |
| `Attributes.cs` | Seven attributes: `[BenchmarkClass]`, `[Benchmark]`, `[Params]`, `[GlobalSetup]`, `[GlobalCleanup]`, `[IterationSetup]`, `[IterationCleanup]` |
| `IBenchmarkClass.cs` | Interface implemented by the source generator on decorated classes |
| `BenchmarkConfig.cs` | Configuration with Quick / Default / Precise presets plus optional auto-calibration |
| `Runner.cs` | Low-level timing flow and sample creation |
| `Runner.Gc.cs` | GC baseline and delta tracking |
| `Runner.Cpu.cs` | Platform-specific CPU counter implementation |
| `StatisticsCalculator.cs` | Percentile and statistics computation |
| `Models.cs` | Result types including `Statistics` precision fields and CPU counter metadata in `EnvironmentInfo` |
| `Formatters/` | Four `IFormatter` implementations (Console, Markdown, HTML, CSV) plus `SummaryFormatter` |

### Packaging

The project bundles `PicoBench.Generators` as an analyzer so consumers get the source generator automatically:

```bash
# Add the project reference
dotnet add reference ../PicoBench.Generators/PicoBench.Generators.csproj

# Then manually add the following attributes to the <ProjectReference> element in your .csproj file:
# PrivateAssets="all"
# ReferenceOutputAssembly="false"  
# OutputItemType="Analyzer"
```

## PicoBench.Generators

An **incremental source generator** (`IIncrementalGenerator`) that turns `[BenchmarkClass]`-decorated partial classes into full `IBenchmarkClass` implementations at compile time.

- **Target**: netstandard2.0
- **Dependency**: Microsoft.CodeAnalysis.CSharp 5.0.0
- **Output**: AOT-compatible C# with `global::` qualified calls and no reflection

### Key Files

| File | Purpose |
|------|---------|
| `BenchmarkGenerator.cs` | Generator entry point using `ForAttributeWithMetadataName` |
| `BenchmarkClassAnalyzer.cs` | Roslyn analysis and diagnostics before code emission |
| `CSharpLiteralFormatter.cs` | Formats C# literals for emitted `[Params]` values |
| `DiagnosticDescriptors.cs` | Centralised generator diagnostics for invalid benchmark declarations |
| `Emitter.cs` | C# code emitter - generates `RunBenchmarks()` with parameter iteration, setup/teardown hooks, and comparison logic |
| `Models.cs` | Roslyn analysis models: `BenchmarkClassModel`, `BenchmarkMethodModel`, `ParamsPropertyModel` (all `IEquatable<T>` for caching) |

The generator now validates common mistakes before code emission and reports diagnostics for invalid benchmark methods, lifecycle methods, duplicate baselines, invalid `[Params]` targets, and incompatible parameter values.

### Generated Code

For a class like:

```csharp
[BenchmarkClass]
public partial class MyBench
{
    [Params(10, 100)]
    public int N { get; set; }

    [GlobalSetup]
    public void Setup() { }

    [Benchmark(Baseline = true)]
    public void Baseline() { /* ... */ }

    [Benchmark]
    public void Fast() { /* ... */ }
}
```

The generator emits a `partial class MyBench : IBenchmarkClass` with a `RunBenchmarks()` method that:

1. Iterates each `[Params]` value (Cartesian product for multiple properties)
2. Sets the property, calls `[GlobalSetup]`
3. Runs each `[Benchmark]` method via `Benchmark.Run()` with `[IterationSetup]`/`[IterationCleanup]` as setup/teardown
4. Compares candidates against the baseline
5. Calls `[GlobalCleanup]`
6. Returns a `BenchmarkSuite` with all results and comparisons
