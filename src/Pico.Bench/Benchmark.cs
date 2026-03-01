namespace Pico.Bench;

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
    public static BenchmarkResult Run(string name, Action action, BenchmarkConfig? config = null)
    {
        ValidateName(name, nameof(name));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        return Run(name, action, warmup: action, config);
    }

    /// <summary>
    /// Run a benchmark with separate warmup and measured actions.
    /// </summary>
    /// <param name="name">Name of the benchmark.</param>
    /// <param name="action">The action to measure.</param>
    /// <param name="warmup">Optional warmup action. If null, no warmup is performed.</param>
    /// <param name="config">Optional configuration (uses <see cref="BenchmarkConfig.Default"/> if null).</param>
    /// <param name="setup">Optional per-sample setup action (not timed).</param>
    /// <param name="teardown">Optional per-sample teardown action (not timed).</param>
    /// <returns>A <see cref="BenchmarkResult"/> containing statistics.</returns>
    public static BenchmarkResult Run(
        string name,
        Action action,
        Action? warmup,
        BenchmarkConfig? config = null,
        Action? setup = null,
        Action? teardown = null
    )
    {
        ValidateName(name, nameof(name));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        config ??= BenchmarkConfig.Default;
        Runner.Initialize();

        // Warmup phase
        if (warmup != null && config.WarmupIterations > 0)
        {
            for (var i = 0; i < config.WarmupIterations; i++)
                warmup();
        }

        return CollectAndBuild(
            name,
            config,
            () => Runner.Time(config.IterationsPerSample, action, setup, teardown)
        );
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
    {
        ValidateName(name, nameof(name));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

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
            for (var i = 0; i < config.WarmupIterations; i++)
                action(state);
        }

        return CollectAndBuild(
            name,
            config,
            () => Runner.Time(config.IterationsPerSample, state, action)
        );
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
        ValidateName(name, nameof(name));
        if (scopeFactory == null)
            throw new ArgumentNullException(nameof(scopeFactory));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        config ??= BenchmarkConfig.Default;
        Runner.Initialize();

        // Warmup phase - use a single scope
        if (config.WarmupIterations > 0)
        {
            using var warmupScope = scopeFactory();
            for (var i = 0; i < config.WarmupIterations; i++)
                action(warmupScope);
        }

        return CollectAndBuild(
            name,
            config,
            () =>
            {
                using var scope = scopeFactory();
                return Runner.Time(config.IterationsPerSample, scope, action);
            }
        );
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
        ValidateName(name, nameof(name));
        return new ComparisonResult(name: name, baseline: baseline, candidate: candidate);
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
        ValidateName(name, nameof(name));
        ValidateName(baselineName, nameof(baselineName));
        if (baselineAction == null)
            throw new ArgumentNullException(nameof(baselineAction));
        ValidateName(candidateName, nameof(candidateName));
        if (candidateAction == null)
            throw new ArgumentNullException(nameof(candidateAction));

        var baseline = Run(baselineName, baselineAction, config);
        var candidate = Run(candidateName, candidateAction, config);

        return new ComparisonResult(name: name, baseline: baseline, candidate: candidate);
    }

    #region Private Helpers

    /// <summary>
    /// Validate that a name parameter is not null or whitespace.
    /// </summary>
    private static void ValidateName(string name, string paramName)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"{paramName} cannot be null or whitespace.", paramName);
    }

    /// <summary>
    /// Force a full GC to establish a clean baseline before the collection phase.
    /// </summary>
    private static void ForceGc()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
    }

    /// <summary>
    /// Run the collection phase, compute statistics, and build the result.
    /// </summary>
    private static BenchmarkResult CollectAndBuild(
        string name,
        BenchmarkConfig config,
        Func<TimingSample> sampleFunc
    )
    {
        ForceGc();

        var samples = new TimingSample[config.SampleCount];
        var perOpTimes = new double[config.SampleCount];
        var perOpCycles = new double[config.SampleCount];

        for (var s = 0; s < config.SampleCount; s++)
        {
            var sample = sampleFunc();
            samples[s] = sample;
            perOpTimes[s] = sample.ElapsedNanoseconds / config.IterationsPerSample;
            perOpCycles[s] = (double)sample.CpuCycles / config.IterationsPerSample;
        }

        var stats = StatisticsCalculator.Compute(perOpTimes, perOpCycles, samples);

        return new BenchmarkResult(
            name: name,
            statistics: stats,
            iterationsPerSample: config.IterationsPerSample,
            sampleCount: config.SampleCount,
            samples: config.RetainSamples ? samples : null
        );
    }

    #endregion
}
