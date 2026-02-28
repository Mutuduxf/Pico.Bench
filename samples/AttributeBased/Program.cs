using System.Text;
using Pico.Bench;
using Pico.Bench.Formatters;

// ─── Attribute-based benchmark (AOT-compatible, source-generated) ───
// Just annotate a partial class with [BenchmarkClass] and methods with [Benchmark].
// The source generator emits an IBenchmarkClass implementation — zero reflection.

var suite = BenchmarkRunner.Run<StringBenchmarks>(BenchmarkConfig.Quick);

// Console output
Console.WriteLine(new ConsoleFormatter().Format(suite));

// Summary
Console.WriteLine(SummaryFormatter.Format(suite.Comparisons!));

// Save results
var outputDir = Path.Combine(AppContext.BaseDirectory, "results");
Directory.CreateDirectory(outputDir);
File.WriteAllText(Path.Combine(outputDir, "results.md"), new MarkdownFormatter().Format(suite));
Console.WriteLine($"\nResults saved to: {outputDir}");

// ─────────────────────────────────────────────────────────────────────
// Benchmark class — the source generator turns this into a full runner.
// ─────────────────────────────────────────────────────────────────────

[BenchmarkClass(Description = "Comparing string concatenation strategies")]
public partial class StringBenchmarks
{
    [Params(10, 100, 1000)]
    public int N { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Any per-parameter-combination setup logic goes here.
    }

    /// <summary>
    /// Baseline: naive string concatenation (creates a new string each time).
    /// </summary>
    [Benchmark(Baseline = true)]
    public void StringConcat()
    {
        var s = string.Empty;
        for (var i = 0; i < N; i++)
            s += "a";
    }

    /// <summary>
    /// Candidate: StringBuilder with default capacity.
    /// </summary>
    [Benchmark]
    public void StringBuilder()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < N; i++)
            sb.Append('a');
        _ = sb.ToString();
    }

    /// <summary>
    /// Candidate: StringBuilder with pre-allocated capacity.
    /// </summary>
    [Benchmark]
    public void StringBuilderWithCapacity()
    {
        var sb = new StringBuilder(N);
        for (var i = 0; i < N; i++)
            sb.Append('a');
        _ = sb.ToString();
    }
}
