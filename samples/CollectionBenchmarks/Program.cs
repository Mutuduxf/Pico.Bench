using Pico.Bench;
using Pico.Bench.Formatters;

// ═══════════════════════════════════════════════════════════════════════
//  Collection Benchmarks — Source-Generator Showcase
// ═══════════════════════════════════════════════════════════════════════
//
//  This sample demonstrates every attribute provided by Pico.Bench:
//
//    [BenchmarkClass]     – marks the partial class for code generation
//    [Params]             – parameterised benchmarks (automatic Cartesian product)
//    [GlobalSetup]        – runs once per parameter combination (prepare data)
//    [GlobalCleanup]      – runs once per parameter combination (release data)
//    [IterationSetup]     – runs before each sample (not timed)
//    [Benchmark]          – the method under test
//    [Benchmark(Baseline)] – the baseline for speedup comparisons
//
//  The source generator emits a full IBenchmarkClass implementation at
//  compile time — zero reflection, fully AOT-compatible.
// ═══════════════════════════════════════════════════════════════════════

Console.WriteLine("=== Collection Lookup Benchmarks ===\n");

// One-liner: run all benchmarks declared in the class.
var suite = BenchmarkRunner.Run<LookupBenchmarks>(BenchmarkConfig.Quick);

// ── Console output ─────────────────────────────────────────────────
Console.WriteLine(new ConsoleFormatter().Format(suite));

// ── Summary (wins / speedup) ───────────────────────────────────────
if (suite.Comparisons is { Count: > 0 })
    Console.WriteLine("\n" + SummaryFormatter.Format(suite.Comparisons));

// ── Save to multiple formats ───────────────────────────────────────
var outputDir = Path.Combine(AppContext.BaseDirectory, "results");
Directory.CreateDirectory(outputDir);

File.WriteAllText(Path.Combine(outputDir, "results.md"), new MarkdownFormatter().Format(suite));
File.WriteAllText(Path.Combine(outputDir, "results.html"), new HtmlFormatter().Format(suite));
File.WriteAllText(Path.Combine(outputDir, "results.csv"), new CsvFormatter().Format(suite));

Console.WriteLine($"\nResults saved to: {outputDir}");
