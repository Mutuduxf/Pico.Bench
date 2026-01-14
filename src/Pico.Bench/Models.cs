namespace Pico.Bench;

/// <summary>
/// GC generation counts captured during a benchmark run.
/// </summary>
public sealed class GcInfo
{
    /// <summary>Gen0 collection count delta.</summary>
    public int Gen0 { get; init; }

    /// <summary>Gen1 collection count delta.</summary>
    public int Gen1 { get; init; }

    /// <summary>Gen2 collection count delta.</summary>
    public int Gen2 { get; init; }

    /// <summary>Total GC collections across all generations.</summary>
    public int Total => Gen0 + Gen1 + Gen2;

    /// <summary>Returns true if no GC occurred during the benchmark.</summary>
    public bool IsZero => Gen0 == 0 && Gen1 == 0 && Gen2 == 0;

    public override string ToString() => $"{Gen0}/{Gen1}/{Gen2}";
}

/// <summary>
/// Raw timing data from a single sample run.
/// </summary>
public sealed class TimingSample
{
    /// <summary>Elapsed time in nanoseconds.</summary>
    public double ElapsedNanoseconds { get; init; }

    /// <summary>Elapsed time in milliseconds.</summary>
    public double ElapsedMilliseconds { get; init; }

    /// <summary>Elapsed Stopwatch ticks.</summary>
    public long ElapsedTicks { get; init; }

    /// <summary>CPU cycles consumed (Windows only, 0 on other platforms).</summary>
    public ulong CpuCycles { get; init; }

    /// <summary>GC collection counts during this sample.</summary>
    public required GcInfo GcInfo { get; init; }
}

/// <summary>
/// Statistical summary of multiple timing samples.
/// </summary>
public sealed class Statistics
{
    /// <summary>Average time in nanoseconds per operation.</summary>
    public double Avg { get; init; }

    /// <summary>Median (50th percentile) in nanoseconds.</summary>
    public double P50 { get; init; }

    /// <summary>90th percentile in nanoseconds.</summary>
    public double P90 { get; init; }

    /// <summary>95th percentile in nanoseconds.</summary>
    public double P95 { get; init; }

    /// <summary>99th percentile in nanoseconds.</summary>
    public double P99 { get; init; }

    /// <summary>Minimum time in nanoseconds.</summary>
    public double Min { get; init; }

    /// <summary>Maximum time in nanoseconds.</summary>
    public double Max { get; init; }

    /// <summary>Standard deviation in nanoseconds.</summary>
    public double StdDev { get; init; }

    /// <summary>Average CPU cycles per operation.</summary>
    public double CpuCyclesPerOp { get; init; }

    /// <summary>Aggregated GC info across all samples.</summary>
    public required GcInfo GcInfo { get; init; }
}

/// <summary>
/// Complete benchmark result for a single test case.
/// </summary>
public sealed class BenchmarkResult
{
    /// <summary>Name/identifier of the benchmark.</summary>
    public required string Name { get; init; }

    /// <summary>Optional category/group for organizing results.</summary>
    public string? Category { get; init; }

    /// <summary>Optional tags for filtering and grouping.</summary>
    public IReadOnlyDictionary<string, string>? Tags { get; init; }

    /// <summary>Statistical summary of the benchmark.</summary>
    public required Statistics Statistics { get; init; }

    /// <summary>Raw timing samples (optional, for detailed analysis).</summary>
    public IReadOnlyList<TimingSample>? Samples { get; init; }

    /// <summary>Number of iterations per sample.</summary>
    public int IterationsPerSample { get; init; }

    /// <summary>Total number of samples collected.</summary>
    public int SampleCount { get; init; }

    /// <summary>When the benchmark was run.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Comparison result between two benchmark runs.
/// </summary>
public sealed class ComparisonResult
{
    /// <summary>Name of the comparison.</summary>
    public required string Name { get; init; }

    /// <summary>Optional category/group for organizing results.</summary>
    public string? Category { get; init; }

    /// <summary>Optional tags for filtering and grouping.</summary>
    public IReadOnlyDictionary<string, string>? Tags { get; init; }

    /// <summary>Baseline benchmark result.</summary>
    public required BenchmarkResult Baseline { get; init; }

    /// <summary>Candidate benchmark result (the one being compared).</summary>
    public required BenchmarkResult Candidate { get; init; }

    /// <summary>Speedup ratio (Baseline.Avg / Candidate.Avg). >1 means candidate is faster.</summary>
    public double Speedup => Baseline.Statistics.Avg / Candidate.Statistics.Avg;

    /// <summary>Returns true if candidate is faster than baseline.</summary>
    public bool IsFaster => Speedup > 1.0;

    /// <summary>Percentage improvement. Positive means candidate is faster.</summary>
    public double ImprovementPercent => (Speedup - 1) * 100;
}

/// <summary>
/// A suite of benchmark results, typically from a single test run.
/// </summary>
public sealed class BenchmarkSuite
{
    /// <summary>Name of the benchmark suite.</summary>
    public required string Name { get; init; }

    /// <summary>Optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Environment information.</summary>
    public required EnvironmentInfo Environment { get; init; }

    /// <summary>All benchmark results in this suite.</summary>
    public required IReadOnlyList<BenchmarkResult> Results { get; init; }

    /// <summary>All comparison results in this suite.</summary>
    public IReadOnlyList<ComparisonResult>? Comparisons { get; init; }

    /// <summary>When the suite was run.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Total duration of the benchmark run.</summary>
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Information about the benchmark execution environment.
/// </summary>
public sealed class EnvironmentInfo
{
    /// <summary>Operating system description.</summary>
    public string Os { get; init; } = RuntimeInformation.OSDescription;

    /// <summary>Process architecture.</summary>
    public string Architecture { get; init; } = RuntimeInformation.ProcessArchitecture.ToString();

    /// <summary>.NET runtime version.</summary>
    public string RuntimeVersion { get; init; } = RuntimeInformation.FrameworkDescription;

    /// <summary>Number of logical processors.</summary>
    public int ProcessorCount { get; init; } = Environment.ProcessorCount;

    /// <summary>Whether running with Native AOT.</summary>
    public bool IsNativeAot { get; init; } = !RuntimeFeature.IsDynamicCodeCompiled;

    /// <summary>Build configuration (Debug/Release).</summary>
    public string Configuration { get; init; } =
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    /// <summary>Custom environment tags.</summary>
    public IReadOnlyDictionary<string, string>? CustomTags { get; init; }

    public override string ToString() =>
        $"{RuntimeVersion} | {Os} | {Architecture} | {(IsNativeAot ? "AOT" : "JIT")} | {Configuration}";
}
