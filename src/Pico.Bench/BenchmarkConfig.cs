namespace Pico.Bench;

/// <summary>
/// Configuration for benchmark execution.
/// </summary>
public sealed class BenchmarkConfig
{
    private int _warmupIterations = 1000;
    private int _sampleCount = 100;
    private int _iterationsPerSample = 10000;

    private static BenchmarkConfig? _default;
    private static BenchmarkConfig? _quick;
    private static BenchmarkConfig? _precise;

    /// <summary>Number of warmup iterations before measurement.</summary>
    public int WarmupIterations
    {
        get => _warmupIterations;
        init =>
            _warmupIterations =
                value >= 0
                    ? value
                    : throw new ArgumentOutOfRangeException(
                        nameof(WarmupIterations),
                        "WarmupIterations must be non-negative."
                    );
    }

    /// <summary>Number of samples to collect.</summary>
    public int SampleCount
    {
        get => _sampleCount;
        init =>
            _sampleCount =
                value > 0
                    ? value
                    : throw new ArgumentOutOfRangeException(
                        nameof(SampleCount),
                        "SampleCount must be positive."
                    );
    }

    /// <summary>Number of iterations per sample.</summary>
    public int IterationsPerSample
    {
        get => _iterationsPerSample;
        init =>
            _iterationsPerSample =
                value > 0
                    ? value
                    : throw new ArgumentOutOfRangeException(
                        nameof(IterationsPerSample),
                        "IterationsPerSample must be positive."
                    );
    }

    /// <summary>Whether to retain raw samples in the result.</summary>
    public bool RetainSamples { get; init; } = false;

    /// <summary>Default configuration suitable for most benchmarks.</summary>
    public static BenchmarkConfig Default => _default ??= new();

    /// <summary>Quick configuration for faster iteration during development.</summary>
    public static BenchmarkConfig Quick => _quick ??= new()
    {
        WarmupIterations = 100,
        SampleCount = 10,
        IterationsPerSample = 1000
    };

    /// <summary>Precise configuration for final measurements.</summary>
    public static BenchmarkConfig Precise => _precise ??= new()
    {
        WarmupIterations = 5000,
        SampleCount = 200,
        IterationsPerSample = 50000
    };
}
