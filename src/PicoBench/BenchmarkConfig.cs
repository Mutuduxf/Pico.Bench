namespace PicoBench;

/// <summary>
/// Configuration for benchmark execution.
/// </summary>
public sealed class BenchmarkConfig
{
    private static BenchmarkConfig? _default;
    private static BenchmarkConfig? _quick;
    private static BenchmarkConfig? _precise;

    /// <summary>Number of warmup iterations before measurement.</summary>
    public int WarmupIterations
    {
        get;
        init =>
            field =
                value >= 0
                    ? value
                    : throw new ArgumentOutOfRangeException(
                        nameof(WarmupIterations),
                        "WarmupIterations must be non-negative."
                    );
    } = 1000;

    /// <summary>Number of samples to collect.</summary>
    public int SampleCount
    {
        get;
        init =>
            field =
                value > 0
                    ? value
                    : throw new ArgumentOutOfRangeException(
                        nameof(SampleCount),
                        "SampleCount must be positive."
                    );
    } = 100;

    /// <summary>Number of iterations per sample.</summary>
    public int IterationsPerSample
    {
        get;
        init =>
            field =
                value > 0
                    ? value
                    : throw new ArgumentOutOfRangeException(
                        nameof(IterationsPerSample),
                        "IterationsPerSample must be positive."
                    );
    } = 10000;

    /// <summary>Whether to retain raw samples in the result.</summary>
    public bool RetainSamples { get; init; } = false;

    /// <summary>
    /// When enabled, PicoBench automatically increases iterations per sample until
    /// a minimum timing budget is reached for more stable measurements.
    /// </summary>
    public bool AutoCalibrateIterations { get; init; } = false;

    /// <summary>
    /// Minimum elapsed time per sample targeted by auto-calibration.
    /// </summary>
    public TimeSpan MinSampleTime { get; init; } = TimeSpan.FromMilliseconds(0.25);

    /// <summary>
    /// Upper bound for iterations per sample when auto-calibration is enabled.
    /// </summary>
    public int MaxAutoIterationsPerSample
    {
        get;
        init =>
            field =
                value > 0
                    ? value
                    : throw new ArgumentOutOfRangeException(
                        nameof(MaxAutoIterationsPerSample),
                        "MaxAutoIterationsPerSample must be positive."
                    );
    } = 1_000_000_000;

    /// <summary>Default configuration suitable for most benchmarks.</summary>
    public static BenchmarkConfig Default => _default ??= new BenchmarkConfig();

    /// <summary>Quick configuration for faster iteration during development.</summary>
    public static BenchmarkConfig Quick =>
        _quick ??= new BenchmarkConfig
        {
            WarmupIterations = 100,
            SampleCount = 10,
            IterationsPerSample = 1000,
            AutoCalibrateIterations = true
        };

    /// <summary>Precise configuration for final measurements.</summary>
    public static BenchmarkConfig Precise =>
        _precise ??= new BenchmarkConfig
        {
            WarmupIterations = 5000,
            SampleCount = 200,
            IterationsPerSample = 50000,
            AutoCalibrateIterations = true,
            MinSampleTime = TimeSpan.FromMilliseconds(1)
        };
}
