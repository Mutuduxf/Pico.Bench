namespace PicoBench;

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

    /// <inheritdoc />
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

    /// <summary>
    /// CPU cycles consumed (Windows/Linux). On macOS this is a monotonic timestamp
    /// and should not be compared across platforms. 0 on unsupported platforms.
    /// </summary>
    public ulong CpuCycles { get; init; }

    /// <summary>GC collection counts during this sample.</summary>
    public GcInfo GcInfo { get; init; } = new();
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

    /// <summary>Standard error of the mean in nanoseconds.</summary>
    public double StandardError { get; init; }

    /// <summary>Relative standard deviation as a percentage of the mean.</summary>
    public double RelativeStdDevPercent { get; init; }

    /// <summary>Average CPU cycles per operation.</summary>
    public double CpuCyclesPerOp { get; init; }

    /// <summary>Aggregated GC info across all samples.</summary>
    public GcInfo GcInfo { get; init; } = new GcInfo();

    /// <summary>Returns true when sample variation is high enough to question result stability.</summary>
    public bool HasHighVariance => RelativeStdDevPercent >= 10.0;
}

/// <summary>
/// Describes how the CPU cycle metric is collected on the current platform.
/// </summary>
public enum CpuCycleMeasurementKind
{
    /// <summary>No cycle-like measurement is available.</summary>
    Unsupported,

    /// <summary>Native thread cycle counts are used.</summary>
    ThreadCycles,

    /// <summary>Linux perf hardware CPU cycle events are used.</summary>
    PerfEventCpuCycles,

    /// <summary>A monotonic clock proxy is used instead of true CPU cycles.</summary>
    MonotonicClockProxy
}

/// <summary>
/// Complete benchmark result for a single test case.
/// </summary>
public sealed class BenchmarkResult
{
    /// <summary>Name/identifier of the benchmark.</summary>
    public string Name { get; }

    /// <summary>Optional category/group for organizing results.</summary>
    public string? Category { get; }

    /// <summary>Optional tags for filtering and grouping.</summary>
    public IReadOnlyDictionary<string, string>? Tags { get; }

    /// <summary>Statistical summary of the benchmark.</summary>
    public Statistics Statistics { get; }

    /// <summary>Raw timing samples (optional, for detailed analysis).</summary>
    public IReadOnlyList<TimingSample>? Samples { get; }

    /// <summary>Number of iterations per sample.</summary>
    public int IterationsPerSample { get; }

    /// <summary>Total number of samples collected.</summary>
    public int SampleCount { get; }

    /// <summary>When the benchmark was run.</summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a benchmark result.
    /// </summary>
    public BenchmarkResult(
        string name,
        Statistics statistics,
        int iterationsPerSample,
        int sampleCount,
        string? category = null,
        IReadOnlyDictionary<string, string>? tags = null,
        IReadOnlyList<TimingSample>? samples = null,
        DateTime? timestamp = null
    )
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));

        if (iterationsPerSample <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(iterationsPerSample),
                "IterationsPerSample must be positive."
            );
        IterationsPerSample = iterationsPerSample;

        if (sampleCount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(sampleCount),
                "SampleCount must be positive."
            );
        SampleCount = sampleCount;

        Category = category;
        Tags = tags;
        Samples = samples;

        if (timestamp.HasValue)
            Timestamp = timestamp.Value;
    }
}

/// <summary>
/// Comparison result between two benchmark runs.
/// </summary>
public sealed class ComparisonResult(
    string name,
    BenchmarkResult baseline,
    BenchmarkResult candidate,
    string? category = null,
    IReadOnlyDictionary<string, string>? tags = null
)
{
    /// <summary>Name of the comparison.</summary>
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

    /// <summary>Optional category/group for organizing results.</summary>
    public string? Category { get; } = category;

    /// <summary>Optional tags for filtering and grouping.</summary>
    public IReadOnlyDictionary<string, string>? Tags { get; } = tags;

    /// <summary>Baseline benchmark result.</summary>
    public BenchmarkResult Baseline { get; } =
        baseline ?? throw new ArgumentNullException(nameof(baseline));

    /// <summary>Candidate benchmark result (the one being compared).</summary>
    public BenchmarkResult Candidate { get; } =
        candidate ?? throw new ArgumentNullException(nameof(candidate));

    /// <summary>Speedup ratio (Baseline.Avg / Candidate.Avg). >1 means candidate is faster.</summary>
    public double Speedup
    {
        get
        {
            var candidateAvg = Candidate.Statistics.Avg;
            var baselineAvg = Baseline.Statistics.Avg;

            // Handle near-zero candidate average
            if (!(Math.Abs(candidateAvg) < 1e-12))
                return baselineAvg / candidateAvg;
            // If both are near-zero, treat as equal (speedup = 1)
            return Math.Abs(baselineAvg) < 1e-12
                ? 1.0
                :
                // Candidate is extremely fast (near-zero time)
                double.PositiveInfinity;
        }
    }

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
    public string Name { get; }

    /// <summary>Optional description.</summary>
    public string? Description { get; }

    /// <summary>Environment information.</summary>
    public EnvironmentInfo Environment { get; }

    /// <summary>All benchmark results in this suite.</summary>
    public IReadOnlyList<BenchmarkResult> Results { get; }

    /// <summary>All comparison results in this suite.</summary>
    public IReadOnlyList<ComparisonResult>? Comparisons { get; }

    /// <summary>When the suite was run.</summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>Total duration of the benchmark run.</summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Creates a benchmark suite.
    /// </summary>
    public BenchmarkSuite(
        string name,
        EnvironmentInfo environment,
        IReadOnlyList<BenchmarkResult> results,
        TimeSpan duration,
        string? description = null,
        IReadOnlyList<ComparisonResult>? comparisons = null,
        DateTime? timestamp = null
    )
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
        Results = results ?? throw new ArgumentNullException(nameof(results));
        Duration = duration;
        Description = description;
        Comparisons = comparisons;

        if (timestamp.HasValue)
            Timestamp = timestamp.Value;
    }
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
    public bool IsNativeAot { get; init; } = false; // Native AOT detection not available in netstandard2.0

    /// <summary>Build configuration (Debug/Release).</summary>
    public string Configuration { get; init; } =
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    /// <summary>How the CPU cycle metric is collected on this runtime.</summary>
    public CpuCycleMeasurementKind CpuCycleMeasurement { get; init; } =
        Runner.GetCpuCycleMeasurementKind();

    /// <summary>Whether a cycle-like metric is available at all.</summary>
    public bool CpuCyclesAvailable { get; init; } = Runner.AreCpuCyclesAvailable();

    /// <summary>Whether the cycle metric represents meaningful CPU cycles rather than a proxy timer.</summary>
    public bool CpuCyclesAreMeaningful { get; init; } = Runner.HasMeaningfulCpuCycles();

    /// <summary>Custom environment tags.</summary>
    public IReadOnlyDictionary<string, string>? CustomTags { get; init; }

    /// <inheritdoc />
    public override string ToString() =>
        $"{RuntimeVersion} | {Os} | {Architecture} | {(IsNativeAot ? "AOT" : "JIT")} | {Configuration} | Cycles: {DescribeCpuCycleMeasurement(CpuCycleMeasurement)}";

    internal static string DescribeCpuCycleMeasurement(CpuCycleMeasurementKind kind)
    {
        return kind switch
        {
            CpuCycleMeasurementKind.ThreadCycles => "ThreadCycles",
            CpuCycleMeasurementKind.PerfEventCpuCycles => "PerfEventCpuCycles",
            CpuCycleMeasurementKind.MonotonicClockProxy => "MonotonicClockProxy",
            _ => "Unsupported"
        };
    }
}
