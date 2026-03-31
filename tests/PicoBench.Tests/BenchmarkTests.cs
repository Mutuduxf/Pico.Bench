namespace PicoBench.Tests;

/// <summary>
/// Tests for <see cref="Benchmark"/> static orchestrator covering all overloads,
/// validation branches, and configuration combinations.
/// </summary>
public class BenchmarkTests
{
    /// <summary>
    /// Minimal config to keep tests fast.
    /// </summary>
    private static readonly BenchmarkConfig FastConfig =
        new()
        {
            WarmupIterations = 1,
            SampleCount = 2,
            IterationsPerSample = 3
        };

    private static readonly BenchmarkConfig FastConfigNoWarmup =
        new()
        {
            WarmupIterations = 0,
            SampleCount = 2,
            IterationsPerSample = 3
        };

    private static readonly BenchmarkConfig FastConfigRetainSamples =
        new()
        {
            WarmupIterations = 1,
            SampleCount = 2,
            IterationsPerSample = 3,
            RetainSamples = true
        };

    private static readonly BenchmarkConfig AutoCalibratedConfig =
        new()
        {
            WarmupIterations = 0,
            SampleCount = 2,
            IterationsPerSample = 1,
            AutoCalibrateIterations = true,
            MinSampleTime = TimeSpan.FromMilliseconds(1),
            MaxAutoIterationsPerSample = 1_000_000
        };

    // ─── Run(string, Action, BenchmarkConfig?) ──────────────────────

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Run_SimpleOverload_ReturnsValidResult()
    {
        var result = Benchmark.Run("Simple", () => { }, FastConfig);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Name).IsEqualTo("Simple");
        await Assert.That(result.SampleCount).IsEqualTo(2);
        await Assert.That(result.IterationsPerSample).IsEqualTo(3);
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Run_SimpleOverload_NullName_ThrowsArgumentException()
    {
        await Assert
            .That(() => Benchmark.Run(null!, () => { }, FastConfig))
            .Throws<ArgumentException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Run_SimpleOverload_WhitespaceName_ThrowsArgumentException()
    {
        await Assert
            .That(() => Benchmark.Run("   ", () => { }, FastConfig))
            .Throws<ArgumentException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Run_SimpleOverload_EmptyName_ThrowsArgumentException()
    {
        await Assert
            .That(() => Benchmark.Run("", () => { }, FastConfig))
            .Throws<ArgumentException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Run_SimpleOverload_NullAction_ThrowsArgumentNullException()
    {
        await Assert
            .That(() => Benchmark.Run("Test", (Action)null!, FastConfig))
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Run_SimpleOverload_DefaultConfig_DelegatesToFullOverload()
    {
        // Verify that the simple overload delegates correctly.
        // The full overload is tested separately; here we just ensure
        // the simple overload returns a valid result with explicit config.
        var result = Benchmark.Run("DefaultConfigTest", () => { }, FastConfig);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Name).IsEqualTo("DefaultConfigTest");
    }

    // ─── Run(string, Action, Action?, BenchmarkConfig?, Action?, Action?) ───

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Run_FullOverload_NullName_ThrowsArgumentException()
    {
        await Assert
            .That(() => Benchmark.Run(null!, () => { }, warmup: null, FastConfig))
            .Throws<ArgumentException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Run_FullOverload_WhitespaceName_ThrowsArgumentException()
    {
        await Assert
            .That(() => Benchmark.Run("  \t", () => { }, warmup: null, FastConfig))
            .Throws<ArgumentException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Run_FullOverload_NullAction_ThrowsArgumentNullException()
    {
        await Assert
            .That(() => Benchmark.Run("Test", (Action)null!, warmup: null, FastConfig))
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Run_FullOverload_NullWarmup_SkipsWarmupPhase()
    {
        var result = Benchmark.Run("NoWarmup", () => { }, warmup: null, FastConfig);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Name).IsEqualTo("NoWarmup");
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Run_FullOverload_ZeroWarmupIterations_SkipsWarmupPhase()
    {
        var result = Benchmark.Run("ZeroWarmup", () => { }, warmup: () => { }, FastConfigNoWarmup);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Name).IsEqualTo("ZeroWarmup");
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Run_FullOverload_WithSetupAndTeardown_ExecutesThem()
    {
        int setupCount = 0;
        int teardownCount = 0;

        var result = Benchmark.Run(
            "SetupTeardown",
            () => { },
            warmup: () => { },
            FastConfig,
            setup: () => setupCount++,
            teardown: () => teardownCount++
        );

        await Assert.That(result).IsNotNull();
        // setup and teardown run once per sample
        await Assert.That(setupCount).IsEqualTo(FastConfig.SampleCount);
        await Assert.That(teardownCount).IsEqualTo(FastConfig.SampleCount);
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Run_FullOverload_RetainSamples_IncludesSamplesInResult()
    {
        var result = Benchmark.Run(
            "Retained",
            () => { },
            warmup: () => { },
            FastConfigRetainSamples
        );

        await Assert.That(result.Samples).IsNotNull();
        await Assert.That(result.Samples!.Count).IsEqualTo(FastConfigRetainSamples.SampleCount);
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Run_FullOverload_NoRetainSamples_SamplesAreNull()
    {
        var result = Benchmark.Run("NotRetained", () => { }, warmup: () => { }, FastConfig);

        await Assert.That(result.Samples).IsNull();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Run_FullOverload_StatisticsAreComputed()
    {
        var result = Benchmark.Run("Stats", () => { }, FastConfig);

        await Assert.That(result.Statistics).IsNotNull();
        await Assert.That(result.Statistics.Avg).IsGreaterThanOrEqualTo(0);
        await Assert.That(result.Statistics.Min).IsGreaterThanOrEqualTo(0);
        await Assert.That(result.Statistics.Max).IsGreaterThanOrEqualTo(result.Statistics.Min);
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Run_AutoCalibrateIterations_IncreasesIterationsForFastWork()
    {
        var result = Benchmark.Run("AutoCalibrated", () => { }, AutoCalibratedConfig);

        await Assert.That(result.IterationsPerSample).IsGreaterThan(1);
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Run_AutoCalibrateIterations_RespectsMaxIterations()
    {
        var config = new BenchmarkConfig
        {
            WarmupIterations = 0,
            SampleCount = 1,
            IterationsPerSample = 1,
            AutoCalibrateIterations = true,
            MinSampleTime = TimeSpan.FromSeconds(1),
            MaxAutoIterationsPerSample = 128
        };

        var result = Benchmark.Run("AutoCalibratedMax", () => { }, config);

        await Assert.That(result.IterationsPerSample).IsLessThanOrEqualTo(128);
    }

    // ─── Run<TState>(...) — generic state overload ──────────────────

    [Test]
    [Property("Category", "Benchmark")]
    public async Task RunWithState_ReturnsValidResult()
    {
        int state = 42;
        var result = Benchmark.Run(
            "Stateful",
            state,
            s =>
            {
                var _ = s + 1;
            },
            config: FastConfig
        );

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Name).IsEqualTo("Stateful");
        await Assert.That(result.SampleCount).IsEqualTo(2);
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task RunWithState_NullName_ThrowsArgumentException()
    {
        await Assert
            .That(() => Benchmark.Run<int>(null!, 0, s => { }, config: FastConfig))
            .Throws<ArgumentException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task RunWithState_WhitespaceName_ThrowsArgumentException()
    {
        await Assert
            .That(() => Benchmark.Run<int>("  ", 0, s => { }, config: FastConfig))
            .Throws<ArgumentException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task RunWithState_NullAction_ThrowsArgumentNullException()
    {
        await Assert
            .That(() => Benchmark.Run<int>("Test", 0, null!, config: FastConfig))
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task RunWithState_WithWarmup_ExecutesWarmup()
    {
        int warmupCount = 0;
        var result = Benchmark.Run(
            "StateWarmup",
            0,
            s => { },
            warmup: s => warmupCount++,
            config: FastConfig
        );

        await Assert.That(result).IsNotNull();
        await Assert.That(warmupCount).IsEqualTo(FastConfig.WarmupIterations);
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task RunWithState_NullWarmup_UsesActionAsWarmup()
    {
        int actionCount = 0;
        var result = Benchmark.Run(
            "NullWarmup",
            0,
            s => actionCount++,
            warmup: null,
            config: FastConfig
        );

        // Action runs for warmup iterations + sample iterations
        var expectedMin =
            FastConfig.WarmupIterations + FastConfig.SampleCount * FastConfig.IterationsPerSample;
        await Assert.That(actionCount).IsEqualTo(expectedMin);
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task RunWithState_ZeroWarmupIterations_SkipsWarmup()
    {
        int warmupCount = 0;
        var result = Benchmark.Run(
            "NoWarmup",
            0,
            s => { },
            warmup: s => warmupCount++,
            config: FastConfigNoWarmup
        );

        await Assert.That(warmupCount).IsEqualTo(0);
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task RunWithState_RetainSamples_IncludesSamples()
    {
        var config = new BenchmarkConfig
        {
            WarmupIterations = 1,
            SampleCount = 3,
            IterationsPerSample = 2,
            RetainSamples = true
        };

        var result = Benchmark.Run("RetainState", 0, s => { }, config: config);

        await Assert.That(result.Samples).IsNotNull();
        await Assert.That(result.Samples!.Count).IsEqualTo(3);
    }

    // ─── RunScoped<TScope>(...) ─────────────────────────────────────

    private sealed class TestScope : IDisposable
    {
        public bool Disposed { get; private set; }
        public int ActionCount { get; set; }

        public void Dispose() => Disposed = true;
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task RunScoped_ReturnsValidResult()
    {
        var result = Benchmark.RunScoped(
            "Scoped",
            () => new TestScope(),
            scope => scope.ActionCount++,
            FastConfig
        );

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Name).IsEqualTo("Scoped");
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task RunScoped_NullName_ThrowsArgumentException()
    {
        await Assert
            .That(() => Benchmark.RunScoped(null!, () => new TestScope(), scope => { }, FastConfig))
            .Throws<ArgumentException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task RunScoped_WhitespaceName_ThrowsArgumentException()
    {
        await Assert
            .That(() => Benchmark.RunScoped("   ", () => new TestScope(), scope => { }, FastConfig))
            .Throws<ArgumentException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task RunScoped_NullScopeFactory_ThrowsArgumentNullException()
    {
        await Assert
            .That(() => Benchmark.RunScoped<TestScope>("Test", null!, scope => { }, FastConfig))
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task RunScoped_NullAction_ThrowsArgumentNullException()
    {
        await Assert
            .That(
                () =>
                    Benchmark.RunScoped(
                        "Test",
                        () => new TestScope(),
                        (Action<TestScope>)null!,
                        FastConfig
                    )
            )
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task RunScoped_DisposesScopes()
    {
        var scopes = new List<TestScope>();
        var result = Benchmark.RunScoped(
            "DisposedScoped",
            () =>
            {
                var s = new TestScope();
                scopes.Add(s);
                return s;
            },
            scope => scope.ActionCount++,
            FastConfig
        );

        // Each sample creates a scope, plus warmup creates one scope
        // All scopes should be disposed
        foreach (var scope in scopes)
        {
            await Assert.That(scope.Disposed).IsTrue();
        }
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task RunScoped_ZeroWarmupIterations_SkipsWarmupScope()
    {
        int scopeCount = 0;
        var result = Benchmark.RunScoped(
            "NoWarmupScoped",
            () =>
            {
                scopeCount++;
                return new TestScope();
            },
            scope => { },
            FastConfigNoWarmup
        );

        // Only sample scopes, no warmup scope
        await Assert.That(scopeCount).IsEqualTo(FastConfigNoWarmup.SampleCount);
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task RunScoped_RetainSamples_IncludesSamples()
    {
        var result = Benchmark.RunScoped(
            "RetainScoped",
            () => new TestScope(),
            scope => { },
            FastConfigRetainSamples
        );

        await Assert.That(result.Samples).IsNotNull();
        await Assert.That(result.Samples!.Count).IsEqualTo(FastConfigRetainSamples.SampleCount);
    }

    // ─── Compare(string, BenchmarkResult, BenchmarkResult) ──────────

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Compare_WithResults_ReturnsComparisonResult()
    {
        var baseline = Benchmark.Run("Baseline", () => { }, FastConfig);
        var candidate = Benchmark.Run("Candidate", () => { }, FastConfig);

        var comparison = Benchmark.Compare("TestCompare", baseline, candidate);

        await Assert.That(comparison).IsNotNull();
        await Assert.That(comparison.Name).IsEqualTo("TestCompare");
        await Assert.That(comparison.Baseline).IsEqualTo(baseline);
        await Assert.That(comparison.Candidate).IsEqualTo(candidate);
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Compare_WithResults_NullName_ThrowsArgumentException()
    {
        var baseline = Benchmark.Run("B", () => { }, FastConfig);
        var candidate = Benchmark.Run("C", () => { }, FastConfig);

        await Assert
            .That(() => Benchmark.Compare(null!, baseline, candidate))
            .Throws<ArgumentException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Compare_WithResults_WhitespaceName_ThrowsArgumentException()
    {
        var baseline = Benchmark.Run("B", () => { }, FastConfig);
        var candidate = Benchmark.Run("C", () => { }, FastConfig);

        await Assert
            .That(() => Benchmark.Compare("  ", baseline, candidate))
            .Throws<ArgumentException>();
    }

    // ─── Compare(string, string, Action, string, Action, BenchmarkConfig?) ──

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Compare_WithActions_ReturnsComparisonResult()
    {
        var comparison = Benchmark.Compare(
            "ActionCompare",
            "Baseline",
            () => { },
            "Candidate",
            () => { },
            FastConfig
        );

        await Assert.That(comparison).IsNotNull();
        await Assert.That(comparison.Name).IsEqualTo("ActionCompare");
        await Assert.That(comparison.Baseline.Name).IsEqualTo("Baseline");
        await Assert.That(comparison.Candidate.Name).IsEqualTo("Candidate");
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Compare_WithActions_NullName_ThrowsArgumentException()
    {
        await Assert
            .That(() => Benchmark.Compare(null!, "B", () => { }, "C", () => { }, FastConfig))
            .Throws<ArgumentException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Compare_WithActions_WhitespaceName_ThrowsArgumentException()
    {
        await Assert
            .That(() => Benchmark.Compare(" ", "B", () => { }, "C", () => { }, FastConfig))
            .Throws<ArgumentException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Compare_WithActions_NullBaselineName_ThrowsArgumentException()
    {
        await Assert
            .That(() => Benchmark.Compare("Cmp", null!, () => { }, "C", () => { }, FastConfig))
            .Throws<ArgumentException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Compare_WithActions_WhitespaceBaselineName_ThrowsArgumentException()
    {
        await Assert
            .That(() => Benchmark.Compare("Cmp", "  ", () => { }, "C", () => { }, FastConfig))
            .Throws<ArgumentException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Compare_WithActions_NullBaselineAction_ThrowsArgumentNullException()
    {
        await Assert
            .That(() => Benchmark.Compare("Cmp", "B", null!, "C", () => { }, FastConfig))
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Compare_WithActions_NullCandidateName_ThrowsArgumentException()
    {
        await Assert
            .That(() => Benchmark.Compare("Cmp", "B", () => { }, null!, () => { }, FastConfig))
            .Throws<ArgumentException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Compare_WithActions_WhitespaceCandidateName_ThrowsArgumentException()
    {
        await Assert
            .That(() => Benchmark.Compare("Cmp", "B", () => { }, "\t", () => { }, FastConfig))
            .Throws<ArgumentException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Compare_WithActions_NullCandidateAction_ThrowsArgumentNullException()
    {
        await Assert
            .That(() => Benchmark.Compare("Cmp", "B", () => { }, "C", null!, FastConfig))
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Benchmark")]
    public async Task Compare_WithActions_SpeedupIsPositive()
    {
        var comparison = Benchmark.Compare(
            "SpeedupCheck",
            "B",
            () => { },
            "C",
            () => { },
            FastConfig
        );

        await Assert.That(comparison.Speedup).IsGreaterThan(0);
    }
}
