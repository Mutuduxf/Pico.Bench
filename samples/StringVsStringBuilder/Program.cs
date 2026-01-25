using System.Text;
using Pico.Bench;
using Pico.Bench.Formatters;

Console.WriteLine("=== String vs StringBuilder Benchmark ===\n");

// Test configurations
int[] appendCounts = [10, 100, 1000];
var allResults = new List<BenchmarkResult>();
var allComparisons = new List<ComparisonResult>();

foreach (var count in appendCounts)
{
    Console.WriteLine($"Testing with {count} appends...");

    // Benchmark: String concatenation (creates new string each time)
    var stringResult = Benchmark.Run(
        $"String ({count} appends)",
        () =>
        {
            var s = string.Empty;
            for (var i = 0; i < count; i++)
            {
                s += "a";
            }
        },
        BenchmarkConfig.Quick
    );

    // Benchmark: StringBuilder (reuses buffer)
    var sbResult = Benchmark.Run(
        $"StringBuilder ({count} appends)",
        () =>
        {
            var sb = new StringBuilder();
            for (var i = 0; i < count; i++)
            {
                sb.Append('a');
            }
            _ = sb.ToString();
        },
        BenchmarkConfig.Quick
    );

    // Benchmark: StringBuilder with capacity (pre-allocated)
    var sbCapacityResult = Benchmark.Run(
        $"StringBuilder+Capacity ({count} appends)",
        () =>
        {
            var sb = new StringBuilder(count);
            for (var i = 0; i < count; i++)
            {
                sb.Append('a');
            }
            _ = sb.ToString();
        },
        BenchmarkConfig.Quick
    );

    allResults.AddRange([stringResult, sbResult, sbCapacityResult]);

    // Create comparisons
    var comparison1 = new ComparisonResult(
        name: $"String vs StringBuilder ({count})",
        baseline: stringResult,
        candidate: sbResult,
        category: $"{count} appends"
    );

    var comparison2 = new ComparisonResult(
        name: $"String vs StringBuilder+Capacity ({count})",
        baseline: stringResult,
        candidate: sbCapacityResult,
        category: $"{count} appends"
    );

    allComparisons.AddRange([comparison1, comparison2]);
}

// Additional test: String.Join vs StringBuilder for joining arrays
Console.WriteLine("\nTesting array joining...");

var words = Enumerable.Range(0, 100).Select(i => $"word{i}").ToArray();

var joinResult = Benchmark.Run(
    "String.Join",
    words,
    static w => _ = string.Join(", ", w),
    config: BenchmarkConfig.Quick
);

var sbJoinResult = Benchmark.Run(
    "StringBuilder Join",
    words,
    static w =>
    {
        var sb = new StringBuilder();
        for (var i = 0; i < w.Length; i++)
        {
            if (i > 0)
                sb.Append(", ");
            sb.Append(w[i]);
        }
        _ = sb.ToString();
    },
    config: BenchmarkConfig.Quick
);

allResults.AddRange([joinResult, sbJoinResult]);

var joinComparison = new ComparisonResult(
    name: "String.Join vs StringBuilder",
    baseline: sbJoinResult,
    candidate: joinResult,
    category: "Array Join"
);
allComparisons.Add(joinComparison);

// Create suite
var suite = new BenchmarkSuite
{
    Name = "String vs StringBuilder Benchmark",
    Description = "Comparing string concatenation performance between String and StringBuilder",
    Environment = new EnvironmentInfo(),
    Results = allResults,
    Comparisons = allComparisons
};

// Output results
Console.WriteLine("\n");

// Use custom labels for this benchmark
var options = new FormatterOptions { BaselineLabel = "String", CandidateLabel = "StringBuilder" };

// Console output
var consoleFormatter = new ConsoleFormatter(options);
Console.WriteLine(consoleFormatter.Format(suite));

// Summary
Console.WriteLine("\n" + SummaryFormatter.Format(allComparisons));

// Save to files with custom labels
var markdownFormatter = new MarkdownFormatter(options);
var htmlFormatter = new HtmlFormatter(options);
var csvFormatter = new CsvFormatter(options);

var outputDir = Path.Combine(AppContext.BaseDirectory, "results");
Directory.CreateDirectory(outputDir);

File.WriteAllText(Path.Combine(outputDir, "results.md"), markdownFormatter.Format(suite));
File.WriteAllText(Path.Combine(outputDir, "results.html"), htmlFormatter.Format(suite));
File.WriteAllText(Path.Combine(outputDir, "results.csv"), csvFormatter.Format(suite));

Console.WriteLine($"\nResults saved to: {outputDir}");
