# Pico.Bench

[English](README.md) | [中文](README.zh-CN.md) | [中文 (Traditional)](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

![CI](https://github.com/Mutuduxf/Pico.Bench/actions/workflows/ci.yml/badge.svg)
[![NuGet](https://img.shields.io/nuget/v/Pico.Bench.svg)](https://www.nuget.org/packages/Pico.Bench)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A lightweight, zero-dependency benchmarking library for .NET with **two complementary APIs**: an imperative fluent API and an attribute-based, source-generated API that is fully **AOT-compatible**.

## Features

- **Zero Dependencies** - Pure .NET implementation, no external packages required
- **Two APIs** - Imperative (`Benchmark.Run`) for ad-hoc tests; attribute-based (`[Benchmark]` + source generator) for structured suites
- **AOT-Compatible Source Generator** - The incremental generator emits direct method calls with zero reflection at runtime
- **Cross-Platform** - Full support for Windows, Linux, and macOS
- **High-Precision Timing** - Uses `Stopwatch` with nanosecond-level granularity
- **GC Tracking** - Monitors Gen0/Gen1/Gen2 collection counts during benchmarks
- **CPU Cycle Counting** - Hardware-level cycle counting (Windows via `QueryThreadCycleTime`, Linux via `perf_event`, macOS via `mach_absolute_time`)
- **Statistical Analysis** - Mean, Median, P90, P95, P99, Min, Max, StdDev
- **Multiple Output Formats** - Console, Markdown, HTML, CSV and programmatic summary
- **Parameterised Benchmarks** - `[Params]` attribute with automatic Cartesian product iteration
- **Comparison Support** - Baseline vs candidate with speedup calculations
- **Configurable** - Quick, Default, and Precise presets or fully custom configuration
- **netstandard2.0** - Compatible with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+

## Installation

Reference the **Pico.Bench** NuGet package. The source generator (`Pico.Bench.Generators`) is bundled automatically as an analyzer - no extra reference needed.

```bash
dotnet add package Pico.Bench
```

## Quick Start

### Imperative API

```csharp
using Pico.Bench;

var result = Benchmark.Run("My Benchmark", () =>
{
    Thread.SpinWait(100);
});

Console.WriteLine($"Average: {result.Statistics.Avg:F1} ns/op");
```

### Attribute-Based API (Source-Generated)

```csharp
using Pico.Bench;

var suite = BenchmarkRunner.Run<MyBenchmarks>();
Console.WriteLine(new Pico.Bench.Formatters.ConsoleFormatter().Format(suite));

[BenchmarkClass]
public partial class MyBenchmarks
{
    [Benchmark(Baseline = true)]
    public void Baseline() { /* ... */ }

    [Benchmark]
    public void Candidate() { /* ... */ }
}
```

> The class **must** be `partial`. The source generator emits an `IBenchmarkClass` implementation at compile time - no reflection, fully AOT-safe.

---

## Imperative API Reference

### Basic Benchmark

```csharp
using Pico.Bench;
using Pico.Bench.Formatters;

var result = Benchmark.Run("SpinWait", () => Thread.SpinWait(100));
Console.WriteLine(new ConsoleFormatter().Format(result));
```

### Benchmark with State (Avoid Closures)

```csharp
var data = new byte[1024];
var result = Benchmark.Run("ArrayCopy", data, static d =>
{
    var copy = new byte[d.Length];
    Buffer.BlockCopy(d, 0, copy, 0, d.Length);
});
```

### Scoped Benchmarks (DI-Friendly)

```csharp
var result = Benchmark.RunScoped("DbQuery",
    () => new MyDbContext(),
    static ctx => ctx.Users.FirstOrDefault()
);
// A new scope is created per sample; the scope is disposed after each sample.
```

### Comparing Two Implementations

```csharp
var comparison = Benchmark.Compare(
    "String vs StringBuilder",
    "String Concat",  () => { var s = ""; for (int i = 0; i < 100; i++) s += "a"; },
    "StringBuilder",  () => { var sb = new StringBuilder(); for (int i = 0; i < 100; i++) sb.Append('a'); _ = sb.ToString(); }
);

Console.WriteLine($"Speedup: {comparison.Speedup:F2}x ({comparison.ImprovementPercent:F1}%)");
```

### Advanced: Separate Warmup, Setup & Teardown

```csharp
var result = Benchmark.Run(
    name:     "Custom",
    action:   () => DoWork(),
    warmup:   () => DoWork(),      // null to skip warmup
    config:   BenchmarkConfig.Precise,
    setup:    () => PrepareState(), // called before each sample (not timed)
    teardown: () => CleanUp()       // called after each sample (not timed)
);
```

---

## Attribute-Based API Reference

Decorate a **partial** class with `[BenchmarkClass]` and its methods/properties with the attributes below. The source generator emits all wiring code at compile time.

### Attributes

| Attribute | Target | Description |
|-----------|--------|-------------|
| `[BenchmarkClass]` | Class | Marks the class for code generation. Optional `Description` property. |
| `[Benchmark]` | Method | Marks a parameterless method as a benchmark. Set `Baseline = true` for the reference method. Optional `Description`. |
| `[Params(values)]` | Property / Field | Iterates the given compile-time constant values. Multiple `[Params]` properties produce a Cartesian product. |
| `[GlobalSetup]` | Method | Called **once** per parameter combination, before benchmarks run. |
| `[GlobalCleanup]` | Method | Called **once** per parameter combination, after benchmarks run. |
| `[IterationSetup]` | Method | Called before **each sample** (not timed). |
| `[IterationCleanup]` | Method | Called after **each sample** (not timed). |

### Full Example

```csharp
using Pico.Bench;

[BenchmarkClass(Description = "Comparing string concatenation strategies")]
public partial class StringBenchmarks
{
    [Params(10, 100, 1000)]
    public int N { get; set; }

    [GlobalSetup]
    public void Setup() { /* prepare data for current N */ }

    [GlobalCleanup]
    public void Cleanup() { /* release resources */ }

    [IterationSetup]
    public void BeforeSample() { /* per-sample preparation */ }

    [Benchmark(Baseline = true)]
    public void StringConcat()
    {
        var s = string.Empty;
        for (var i = 0; i < N; i++) s += "a";
    }

    [Benchmark]
    public void StringBuilder()
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < N; i++) sb.Append('a');
        _ = sb.ToString();
    }
}
```

### Running

```csharp
// Create instance internally:
var suite = BenchmarkRunner.Run<StringBenchmarks>(BenchmarkConfig.Quick);

// Or with a pre-configured instance:
var instance = new StringBenchmarks();
var suite2 = BenchmarkRunner.Run(instance, BenchmarkConfig.Quick);
```

---

## Configuration

### Presets

| Preset | Warmup | Samples | Iters/Sample | Use Case |
|--------|--------|---------|--------------|----------|
| `Quick` | 100 | 10 | 1,000 | Fast iteration / CI |
| `Default` | 1,000 | 100 | 10,000 | General benchmarking |
| `Precise` | 5,000 | 200 | 50,000 | Final measurements |

### Custom Configuration

```csharp
var config = new BenchmarkConfig
{
    WarmupIterations    = 500,
    SampleCount         = 50,
    IterationsPerSample = 5000,
    RetainSamples       = true   // Keep raw TimingSample data
};

var result = Benchmark.Run("Test", action, config);
```

---

## Output Formatters

Five built-in formatters implement `IFormatter`:

```csharp
using Pico.Bench.Formatters;

var console  = new ConsoleFormatter();     // Box-drawing console tables
var markdown = new MarkdownFormatter();    // GitHub-friendly Markdown
var html     = new HtmlFormatter();        // Styled HTML report
var csv      = new CsvFormatter();         // CSV for data analysis

// Static helper for comparison summaries:
Console.WriteLine(SummaryFormatter.Format(suite.Comparisons));
```

### Formatting Targets

```csharp
formatter.Format(result);               // Single BenchmarkResult
formatter.Format(results);              // IEnumerable<BenchmarkResult>
formatter.Format(comparison);           // Single ComparisonResult
formatter.Format(comparisons);          // IEnumerable<ComparisonResult>
formatter.Format(suite);                // Complete BenchmarkSuite
```

### Formatter Options

```csharp
var options = new FormatterOptions
{
    IncludeEnvironment   = true,
    IncludeTimestamp      = true,
    IncludeGcInfo         = true,
    IncludeCpuCycles      = true,
    IncludePercentiles    = true,
    TimeDecimalPlaces     = 1,
    SpeedupDecimalPlaces  = 2,
    BaselineLabel         = "Old",
    CandidateLabel        = "New"
};

var formatter = new ConsoleFormatter(options);
// Also available: FormatterOptions.Default, .Compact, .Minimal
```

### Saving Results

```csharp
var dir = Path.Combine(AppContext.BaseDirectory, "results");
Directory.CreateDirectory(dir);

File.WriteAllText(Path.Combine(dir, "results.md"),   new MarkdownFormatter().Format(suite));
File.WriteAllText(Path.Combine(dir, "results.html"), new HtmlFormatter().Format(suite));
File.WriteAllText(Path.Combine(dir, "results.csv"),  new CsvFormatter().Format(suite));
```

---

## Result Model

| Type | Description |
|------|-------------|
| `BenchmarkResult` | Name, Statistics, Samples, IterationsPerSample, SampleCount, Tags, Category |
| `ComparisonResult` | Baseline, Candidate, Speedup, IsFaster, ImprovementPercent |
| `BenchmarkSuite` | Name, Description, Results, Comparisons, Environment, Duration |
| `Statistics` | Avg, P50, P90, P95, P99, Min, Max, StdDev, CpuCyclesPerOp, GcInfo |
| `TimingSample` | ElapsedNanoseconds, ElapsedMilliseconds, ElapsedTicks, CpuCycles, GcInfo |
| `GcInfo` | Gen0, Gen1, Gen2, Total, IsZero |
| `EnvironmentInfo` | Os, Architecture, RuntimeVersion, ProcessorCount, Configuration |

---

## Architecture

```
src/
+-- Pico.Bench/                        # Main library (netstandard2.0)
|   +-- Benchmark.cs                   # Imperative API (Run, Compare, RunScoped)
|   +-- BenchmarkRunner.cs             # Attribute-based entry point (Run<T>)
|   +-- BenchmarkConfig.cs             # Configuration with presets
|   +-- Attributes.cs                  # 7 benchmark attributes
|   +-- IBenchmarkClass.cs             # Interface emitted by the generator
|   +-- Runner.cs                      # Low-level timing engine
|   +-- StatisticsCalculator.cs        # Percentile / stats computation
|   +-- Models.cs                      # Result types
|   +-- Formatters/
|       +-- IFormatter.cs              # IFormatter, FormatterOptions & FormatterBase
|       +-- ConsoleFormatter.cs        # Box-drawing console tables
|       +-- MarkdownFormatter.cs       # GitHub Markdown tables
|       +-- HtmlFormatter.cs           # Styled HTML reports
|       +-- CsvFormatter.cs            # CSV export
|       +-- SummaryFormatter.cs        # Win/loss summary
|
+-- Pico.Bench.Generators/            # Source generator (netstandard2.0)
    +-- BenchmarkGenerator.cs          # IIncrementalGenerator entry point
    +-- Emitter.cs                     # C# code emitter (AOT-safe)
    +-- Models.cs                      # Roslyn analysis models
```

---

## Platform-Specific Features

| Feature | Windows | Linux | macOS |
|---------|---------|-------|-------|
| High-precision timing | Stopwatch | Stopwatch | Stopwatch |
| GC tracking (Gen0/1/2) | Yes | Yes | Yes |
| CPU cycle counting | `QueryThreadCycleTime` | `perf_event_open` | `mach_absolute_time` |
| Process priority boost | Yes | Yes | Yes |

---

## Samples

| Sample | API Style | Description |
|--------|-----------|-------------|
| `StringVsStringBuilder` | Imperative | Compares `string +=`, `StringBuilder`, and `StringBuilder` with capacity |
| `AttributeBased` | Attribute | Same comparison using `[Benchmark]`, `[Params]`, and the source generator |
| `CollectionBenchmarks` | Attribute | List vs Dictionary vs HashSet lookup - showcases every attribute |

```bash
dotnet run --project samples/StringVsStringBuilder -c Release
dotnet run --project samples/AttributeBased -c Release
dotnet run --project samples/CollectionBenchmarks -c Release
```

---

## Comparison with BenchmarkDotNet

| Feature | Pico.Bench | BenchmarkDotNet |
|---------|-----------|----------------|
| Dependencies | 0 | Many |
| Package size | Tiny | Large |
| Target framework | netstandard2.0 | net6.0+ |
| AOT support | Source generator | Reflection-based |
| Attribute API | `[Benchmark]`, `[Params]` | `[Benchmark]`, `[Params]` |
| Setup time | Instant | Seconds |
| Output formats | 5 | 10+ |
| Statistical depth | Good | Extensive |
| Use case | Quick A/B tests, CI, AOT apps | Detailed analysis, publications |

---

## License

MIT License - see [LICENSE](LICENSE) file for details.

## Building and Publishing

```bash
dotnet build --configuration Release
dotnet test --configuration Release
dotnet pack src/Pico.Bench/Pico.Bench.csproj --configuration Release --include-symbols --output ./nupkg
```

Releases are **tag-driven** — push a version tag (e.g. `git tag v2026.2.0 && git push origin v2026.2.0`) and the GitHub Actions pipeline will test, pack, and publish to [NuGet.org](https://www.nuget.org/packages/Pico.Bench) automatically.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make changes with tests
4. Submit a pull request
