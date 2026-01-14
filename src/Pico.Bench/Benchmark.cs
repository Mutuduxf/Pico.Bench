namespace Pico.Bench;

/// <summary>
/// Configuration for benchmark execution.
/// </summary>
public sealed record BenchmarkConfig
{
    /// <summary>Number of warmup iterations before measurement.</summary>
    public int WarmupIterations { get; init; } = 1000;

    /// <summary>Number of samples to collect.</summary>
    public int SampleCount { get; init; } = 100;

    /// <summary>Number of iterations per sample.</summary>
    public int IterationsPerSample { get; init; } = 10000;

    /// <summary>Whether to retain raw samples in the result.</summary>
    public bool RetainSamples { get; init; } = false;

    /// <summary>Default configuration suitable for most benchmarks.</summary>
    public static BenchmarkConfig Default { get; } = new();

    /// <summary>Quick configuration for faster iteration during development.</summary>
    public static BenchmarkConfig Quick { get; } =
        new()
        {
            WarmupIterations = 100,
            SampleCount = 10,
            IterationsPerSample = 1000
        };

    /// <summary>Precise configuration for final measurements.</summary>
    public static BenchmarkConfig Precise { get; } =
        new()
        {
            WarmupIterations = 5000,
            SampleCount = 200,
            IterationsPerSample = 50000
        };
}

/// <summary>
/// High-level benchmark orchestrator that runs measurements and computes statistics.
/// </summary>
public static class Benchmark
{
    /// <summary>
    /// Run a benchmark with the given action.
    /// </summary>
    /// <param name="name">Name of the benchmark.</param>
    /// <param name="action">The action to measure.</param>
    /// <param name="config">Optional configuration (uses <see cref="BenchmarkConfig.Default"/> if null).</param>
    /// <returns>A <see cref="BenchmarkResult"/> containing statistics.</returns>
    public static BenchmarkResult Run(
        string name,
        Action action,
        BenchmarkConfig? config = null
    ) => Run(name, action, warmup: action, config);

    /// <summary>
    /// Run a benchmark with separate warmup and measured actions.
    /// </summary>
    public static BenchmarkResult Run(
        string name,
        Action action,
        Action? warmup,
        BenchmarkConfig? config = null
    )
    {
        config ??= BenchmarkConfig.Default;

        // Initialize runner if not already done
        Runner.Initialize();

        // Warmup phase
        if (warmup != null && config.WarmupIterations > 0)
        {
            for (int i = 0; i < config.WarmupIterations; i++)
                warmup();
        }

        // Collection phase
        var samples = new TimingSample[config.SampleCount];
        var perOpTimes = new double[config.SampleCount];
        var perOpCycles = new double[config.SampleCount];

        for (int s = 0; s < config.SampleCount; s++)
        {
            var sample = Runner.Time(config.IterationsPerSample, action);
            samples[s] = sample;
            perOpTimes[s] = sample.ElapsedNanoseconds / config.IterationsPerSample;
            perOpCycles[s] = (double)sample.CpuCycles / config.IterationsPerSample;
        }

        // Compute statistics
        var stats = ComputeStatistics(perOpTimes, perOpCycles, samples);

        return new BenchmarkResult
        {
            Name = name,
            Statistics = stats,
            IterationsPerSample = config.IterationsPerSample,
            SampleCount = config.SampleCount,
            Samples = config.RetainSamples ? samples : null
        };
    }

    /// <summary>
    /// Run a benchmark with state passed to avoid closure allocation.
    /// </summary>
    public static BenchmarkResult Run<TState>(
        string name,
        TState state,
        Action<TState> action,
        Action<TState>? warmup = null,
        BenchmarkConfig? config = null
    )
        where TState : allows ref struct
    {
        config ??= BenchmarkConfig.Default;

        Runner.Initialize();

        // Warmup phase
        if (warmup != null && config.WarmupIterations > 0)
        {
            for (int i = 0; i < config.WarmupIterations; i++)
                warmup(state);
        }
        else if (config.WarmupIterations > 0)
        {
            for (int i = 0; i < config.WarmupIterations; i++)
                action(state);
        }

        // Collection phase
        var samples = new TimingSample[config.SampleCount];
        var perOpTimes = new double[config.SampleCount];
        var perOpCycles = new double[config.SampleCount];

        for (int s = 0; s < config.SampleCount; s++)
        {
            var sample = Runner.Time(config.IterationsPerSample, state, action);
            samples[s] = sample;
            perOpTimes[s] = sample.ElapsedNanoseconds / config.IterationsPerSample;
            perOpCycles[s] = (double)sample.CpuCycles / config.IterationsPerSample;
        }

        var stats = ComputeStatistics(perOpTimes, perOpCycles, samples);

        return new BenchmarkResult
        {
            Name = name,
            Statistics = stats,
            IterationsPerSample = config.IterationsPerSample,
            SampleCount = config.SampleCount,
            Samples = config.RetainSamples ? samples : null
        };
    }

    /// <summary>
    /// Run a benchmark with a scope factory (creates new scope per sample).
    /// Useful for DI container benchmarks.
    /// </summary>
    public static BenchmarkResult RunScoped<TScope>(
        string name,
        Func<TScope> scopeFactory,
        Action<TScope> action,
        BenchmarkConfig? config = null
    )
        where TScope : IDisposable
    {
        config ??= BenchmarkConfig.Default;

        Runner.Initialize();

        // Warmup phase - use a single scope
        if (config.WarmupIterations > 0)
        {
            using var warmupScope = scopeFactory();
            for (int i = 0; i < config.WarmupIterations; i++)
                action(warmupScope);
        }

        // Collection phase - one scope per sample
        var samples = new TimingSample[config.SampleCount];
        var perOpTimes = new double[config.SampleCount];
        var perOpCycles = new double[config.SampleCount];

        for (int s = 0; s < config.SampleCount; s++)
        {
            using var scope = scopeFactory();
            var sample = Runner.Time(config.IterationsPerSample, scope, action);
            samples[s] = sample;
            perOpTimes[s] = sample.ElapsedNanoseconds / config.IterationsPerSample;
            perOpCycles[s] = (double)sample.CpuCycles / config.IterationsPerSample;
        }

        var stats = ComputeStatistics(perOpTimes, perOpCycles, samples);

        return new BenchmarkResult
        {
            Name = name,
            Statistics = stats,
            IterationsPerSample = config.IterationsPerSample,
            SampleCount = config.SampleCount,
            Samples = config.RetainSamples ? samples : null
        };
    }

    /// <summary>
    /// Compare two benchmarks and return a comparison result.
    /// </summary>
    public static ComparisonResult Compare(
        string name,
        BenchmarkResult baseline,
        BenchmarkResult candidate
    )
    {
        return new ComparisonResult
        {
            Name = name,
            Baseline = baseline,
            Candidate = candidate
        };
    }

    /// <summary>
    /// Compare two actions directly and return a comparison result.
    /// </summary>
    public static ComparisonResult Compare(
        string name,
        string baselineName,
        Action baselineAction,
        string candidateName,
        Action candidateAction,
        BenchmarkConfig? config = null
    )
    {
        var baseline = Run(baselineName, baselineAction, config);
        var candidate = Run(candidateName, candidateAction, config);

        return new ComparisonResult
        {
            Name = name,
            Baseline = baseline,
            Candidate = candidate
        };
    }

    #region Statistics Computation

    private static Statistics ComputeStatistics(
        double[] perOpTimes,
        double[] perOpCycles,
        TimingSample[] samples
    )
    {
        // Sort for percentile calculation
        var sorted = (double[])perOpTimes.Clone();
        Array.Sort(sorted);

        // Aggregate GC info
        int gen0 = 0, gen1 = 0, gen2 = 0;
        foreach (var sample in samples)
        {
            gen0 += sample.GcInfo.Gen0;
            gen1 += sample.GcInfo.Gen1;
            gen2 += sample.GcInfo.Gen2;
        }

        var avg = perOpTimes.Average();
        var variance = perOpTimes.Select(t => (t - avg) * (t - avg)).Average();
        var stdDev = Math.Sqrt(variance);

        return new Statistics
        {
            Avg = avg,
            P50 = GetPercentile(sorted, 50),
            P90 = GetPercentile(sorted, 90),
            P95 = GetPercentile(sorted, 95),
            P99 = GetPercentile(sorted, 99),
            Min = sorted[0],
            Max = sorted[^1],
            StdDev = stdDev,
            CpuCyclesPerOp = perOpCycles.Average(),
            GcInfo = new GcInfo { Gen0 = gen0, Gen1 = gen1, Gen2 = gen2 }
        };
    }

    private static double GetPercentile(double[] sortedData, int percentile)
    {
        var index = (int)Math.Ceiling(percentile / 100.0 * sortedData.Length) - 1;
        return sortedData[Math.Clamp(index, 0, sortedData.Length - 1)];
    }

    #endregion
}
