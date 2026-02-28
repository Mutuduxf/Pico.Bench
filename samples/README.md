# Samples

Three sample projects demonstrate the two APIs provided by Pico.Bench.

## StringVsStringBuilder (Imperative API)

Uses the imperative `Benchmark.Run()` and `Benchmark.Compare()` API to measure string concatenation strategies at various sizes.

**Highlights:**

- `Benchmark.Run()` with closures and `BenchmarkConfig.Quick`
- `Benchmark.Run<TState>()` with state to avoid closure allocation
- Manual `ComparisonResult` creation for custom grouping
- `BenchmarkSuite` construction with all results and comparisons
- Outputs to Console, Markdown, HTML, and CSV via `FormatterOptions` (custom labels)

```bash
dotnet run --project samples/StringVsStringBuilder -c Release
```

## AttributeBased (Attribute API + Source Generator)

Rewrites the same string benchmark using the attribute-based API.

**Highlights:**

- `[BenchmarkClass]` with `Description`
- `[Params(10, 100, 1000)]` for parameterised runs
- `[Benchmark(Baseline = true)]` to mark the reference method
- `[GlobalSetup]` for per-parameter-combination preparation
- One-liner execution via `BenchmarkRunner.Run<T>()`
- `SummaryFormatter` for quick win/loss overview

```csharp
[BenchmarkClass(Description = "Comparing string concatenation strategies")]
public partial class StringBenchmarks
{
    [Params(10, 100, 1000)]
    public int N { get; set; }

    [GlobalSetup]
    public void Setup() { }

    [Benchmark(Baseline = true)]
    public void StringConcat() { /* ... */ }

    [Benchmark]
    public void StringBuilder() { /* ... */ }

    [Benchmark]
    public void StringBuilderWithCapacity() { /* ... */ }
}
```

```bash
dotnet run --project samples/AttributeBased -c Release
```

## CollectionBenchmarks (Full Attribute Showcase)

Demonstrates **every** attribute by comparing List, Dictionary, and HashSet lookup performance.

**Highlights:**

- `[BenchmarkClass]` with `Description`
- `[Params(100, 1_000, 10_000)]` - three collection sizes
- `[GlobalSetup]` - populates all three collections with randomised data
- `[GlobalCleanup]` - releases the collections
- `[IterationSetup]` - shuffles the lookup target before each sample
- `[Benchmark(Baseline = true, Description = "...")]` - `List.Contains()` as baseline
- `[Benchmark(Description = "...")]` - `Dictionary.ContainsKey()` and `HashSet.Contains()`
- Multi-format output: Console, Markdown, HTML, CSV

```csharp
[BenchmarkClass(Description = "Lookup performance: List vs Dictionary vs HashSet")]
public partial class LookupBenchmarks
{
    [Params(100, 1_000, 10_000)]
    public int N { get; set; }

    [GlobalSetup]   public void Setup()         { /* populate collections */ }
    [GlobalCleanup] public void Cleanup()       { /* release collections */ }
    [IterationSetup] public void ShuffleTarget() { /* vary lookup target */ }

    [Benchmark(Baseline = true, Description = "Linear scan O(n)")]
    public void ListContains() { _ = _list.Contains(_target); }

    [Benchmark(Description = "Hash lookup O(1)")]
    public void DictionaryContainsKey() { _ = _dictionary.ContainsKey(_target); }

    [Benchmark(Description = "Hash lookup O(1), set-optimised")]
    public void HashSetContains() { _ = _hashSet.Contains(_target); }
}
```

```bash
dotnet run --project samples/CollectionBenchmarks -c Release
```

## Output

All samples save results to a `results/` subdirectory under the output folder in Markdown, HTML, and CSV formats.
