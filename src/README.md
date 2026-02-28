# Source Projects

This directory contains the two library projects that make up Pico.Bench.

## Pico.Bench

The main benchmarking library targeting **netstandard2.0** with zero external dependencies.

### Key Files

| File | Purpose |
|------|---------|
| `Benchmark.cs` | Imperative API - `Run()`, `Run<TState>()`, `RunScoped<TScope>()`, `Compare()` |
| `BenchmarkRunner.cs` | Attribute-based entry point - `Run<T>()` |
| `Attributes.cs` | Seven attributes: `[BenchmarkClass]`, `[Benchmark]`, `[Params]`, `[GlobalSetup]`, `[GlobalCleanup]`, `[IterationSetup]`, `[IterationCleanup]` |
| `IBenchmarkClass.cs` | Interface implemented by the source generator on decorated classes |
| `BenchmarkConfig.cs` | Configuration with Quick / Default / Precise presets |
| `Runner.cs` | Low-level timing engine with platform-specific CPU cycle counting |
| `StatisticsCalculator.cs` | Percentile and statistics computation |
| `Models.cs` | Result types: `BenchmarkResult`, `ComparisonResult`, `BenchmarkSuite`, `Statistics`, `TimingSample`, `GcInfo`, `EnvironmentInfo` |
| `Formatters/` | Five formatters: Console, Markdown, HTML, CSV, Summary |

### Packaging

The project bundles `Pico.Bench.Generators` as an analyzer so consumers get the source generator automatically:

```xml
<ProjectReference Include="..\Pico.Bench.Generators\Pico.Bench.Generators.csproj"
                  PrivateAssets="all"
                  ReferenceOutputAssembly="false"
                  OutputItemType="Analyzer" />
```

## Pico.Bench.Generators

An **incremental source generator** (`IIncrementalGenerator`) that turns `[BenchmarkClass]`-decorated partial classes into full `IBenchmarkClass` implementations at compile time.

- **Target**: netstandard2.0
- **Dependency**: Microsoft.CodeAnalysis.CSharp 4.3.1
- **Output**: AOT-compatible C# with `global::` qualified calls and no reflection

### Key Files

| File | Purpose |
|------|---------|
| `BenchmarkGenerator.cs` | Generator entry point using `ForAttributeWithMetadataName` |
| `Emitter.cs` | C# code emitter - generates `RunBenchmarks()` with parameter iteration, setup/teardown hooks, and comparison logic |
| `Models.cs` | Roslyn analysis models: `BenchmarkClassModel`, `BenchmarkMethodModel`, `ParamsPropertyModel` (all `IEquatable<T>` for caching) |

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
