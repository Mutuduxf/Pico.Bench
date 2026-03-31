namespace PicoBench.Tests;

/// <summary>
/// Tests for model classes: <see cref="BenchmarkResult"/>, <see cref="ComparisonResult"/>,
/// <see cref="BenchmarkSuite"/>, <see cref="EnvironmentInfo"/>, <see cref="GcInfo"/>,
/// <see cref="TimingSample"/>, and <see cref="Statistics"/>.
/// </summary>
public class ModelsTests
{
    // ═══════════════════════════════════════════════════════════════
    //  GcInfo
    // ═══════════════════════════════════════════════════════════════

    [Test]
    [Property("Category", "Models")]
    public async Task GcInfo_Total_SumsAllGenerations()
    {
        var gc = new GcInfo
        {
            Gen0 = 10,
            Gen1 = 5,
            Gen2 = 2
        };
        await Assert.That(gc.Total).IsEqualTo(17);
    }

    [Test]
    [Property("Category", "Models")]
    public async Task GcInfo_IsZero_WhenAllZero()
    {
        var gc = new GcInfo
        {
            Gen0 = 0,
            Gen1 = 0,
            Gen2 = 0
        };
        await Assert.That(gc.IsZero).IsTrue();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task GcInfo_IsZero_FalseWhenAnyGenNonZero()
    {
        await Assert
            .That(
                new GcInfo
                {
                    Gen0 = 1,
                    Gen1 = 0,
                    Gen2 = 0
                }.IsZero
            )
            .IsFalse();
        await Assert
            .That(
                new GcInfo
                {
                    Gen0 = 0,
                    Gen1 = 1,
                    Gen2 = 0
                }.IsZero
            )
            .IsFalse();
        await Assert
            .That(
                new GcInfo
                {
                    Gen0 = 0,
                    Gen1 = 0,
                    Gen2 = 1
                }.IsZero
            )
            .IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task GcInfo_ToString_FormatsCorrectly()
    {
        var gc = new GcInfo
        {
            Gen0 = 3,
            Gen1 = 2,
            Gen2 = 1
        };
        await Assert.That(gc.ToString()).IsEqualTo("3/2/1");
    }

    // ═══════════════════════════════════════════════════════════════
    //  TimingSample
    // ═══════════════════════════════════════════════════════════════

    [Test]
    [Property("Category", "Models")]
    public async Task TimingSample_DefaultGcInfo_IsNotNull()
    {
        var sample = new TimingSample();
        await Assert.That(sample.GcInfo).IsNotNull();
        await Assert.That(sample.GcInfo.IsZero).IsTrue();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task TimingSample_PropertiesAreInitOnly()
    {
        var sample = new TimingSample
        {
            ElapsedNanoseconds = 1000.0,
            ElapsedMilliseconds = 0.001,
            ElapsedTicks = 100,
            CpuCycles = 5000
        };

        await Assert.That(sample.ElapsedNanoseconds).IsEqualTo(1000.0);
        await Assert.That(sample.ElapsedMilliseconds).IsEqualTo(0.001);
        await Assert.That(sample.ElapsedTicks).IsEqualTo(100);
        await Assert.That(sample.CpuCycles).IsEqualTo(5000UL);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Statistics
    // ═══════════════════════════════════════════════════════════════

    [Test]
    [Property("Category", "Models")]
    public async Task Statistics_DefaultGcInfo_IsNotNull()
    {
        var stats = new Statistics();
        await Assert.That(stats.GcInfo).IsNotNull();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task Statistics_AllPropertiesAreSet()
    {
        var stats = StatisticsFactory.Create();

        await Assert.That(stats.Avg).IsEqualTo(150.5);
        await Assert.That(stats.P50).IsEqualTo(145.0);
        await Assert.That(stats.P90).IsEqualTo(160.0);
        await Assert.That(stats.P95).IsEqualTo(165.0);
        await Assert.That(stats.P99).IsEqualTo(180.0);
        await Assert.That(stats.Min).IsEqualTo(120.0);
        await Assert.That(stats.Max).IsEqualTo(200.0);
        await Assert.That(stats.StdDev).IsEqualTo(20.0);
        await Assert.That(stats.CpuCyclesPerOp).IsEqualTo(300.0);
    }

    // ═══════════════════════════════════════════════════════════════
    //  BenchmarkResult — constructor validation
    // ═══════════════════════════════════════════════════════════════

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkResult_NullName_ThrowsArgumentNullException()
    {
        await Assert
            .That(
                () =>
                    new BenchmarkResult(
                        name: null!,
                        statistics: StatisticsFactory.Create(),
                        iterationsPerSample: 1,
                        sampleCount: 1
                    )
            )
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkResult_NullStatistics_ThrowsArgumentNullException()
    {
        await Assert
            .That(
                () =>
                    new BenchmarkResult(
                        name: "Test",
                        statistics: null!,
                        iterationsPerSample: 1,
                        sampleCount: 1
                    )
            )
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkResult_ZeroIterationsPerSample_ThrowsArgumentOutOfRangeException()
    {
        await Assert
            .That(
                () =>
                    new BenchmarkResult(
                        name: "Test",
                        statistics: StatisticsFactory.Create(),
                        iterationsPerSample: 0,
                        sampleCount: 1
                    )
            )
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkResult_NegativeIterationsPerSample_ThrowsArgumentOutOfRangeException()
    {
        await Assert
            .That(
                () =>
                    new BenchmarkResult(
                        name: "Test",
                        statistics: StatisticsFactory.Create(),
                        iterationsPerSample: -1,
                        sampleCount: 1
                    )
            )
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkResult_ZeroSampleCount_ThrowsArgumentOutOfRangeException()
    {
        await Assert
            .That(
                () =>
                    new BenchmarkResult(
                        name: "Test",
                        statistics: StatisticsFactory.Create(),
                        iterationsPerSample: 1,
                        sampleCount: 0
                    )
            )
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkResult_NegativeSampleCount_ThrowsArgumentOutOfRangeException()
    {
        await Assert
            .That(
                () =>
                    new BenchmarkResult(
                        name: "Test",
                        statistics: StatisticsFactory.Create(),
                        iterationsPerSample: 1,
                        sampleCount: -1
                    )
            )
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkResult_OptionalProperties_SetCorrectly()
    {
        var tags = new Dictionary<string, string> { ["Key"] = "Value" };
        var samples = new List<TimingSample> { new() };
        var timestamp = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        var result = new BenchmarkResult(
            name: "Test",
            statistics: StatisticsFactory.Create(),
            iterationsPerSample: 10,
            sampleCount: 5,
            category: "UnitTest",
            tags: tags,
            samples: samples,
            timestamp: timestamp
        );

        await Assert.That(result.Category).IsEqualTo("UnitTest");
        await Assert.That(result.Tags).IsNotNull();
        await Assert.That(result.Tags!["Key"]).IsEqualTo("Value");
        await Assert.That(result.Samples).IsNotNull();
        await Assert.That(result.Samples!.Count).IsEqualTo(1);
        await Assert.That(result.Timestamp).IsEqualTo(timestamp);
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkResult_NoTimestamp_UsesUtcNow()
    {
        var before = DateTime.UtcNow;
        var result = BenchmarkResultFactory.Create();
        var after = DateTime.UtcNow;

        await Assert.That(result.Timestamp).IsGreaterThanOrEqualTo(before);
        await Assert.That(result.Timestamp).IsLessThanOrEqualTo(after);
    }

    // ═══════════════════════════════════════════════════════════════
    //  ComparisonResult — constructor and computed properties
    // ═══════════════════════════════════════════════════════════════

    [Test]
    [Property("Category", "Models")]
    public async Task ComparisonResult_NullName_ThrowsArgumentNullException()
    {
        var baseline = BenchmarkResultFactory.Create("B");
        var candidate = BenchmarkResultFactory.Create("C");

        await Assert
            .That(() => new ComparisonResult(name: null!, baseline: baseline, candidate: candidate))
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task ComparisonResult_NullBaseline_ThrowsArgumentNullException()
    {
        var candidate = BenchmarkResultFactory.Create("C");

        await Assert
            .That(() => new ComparisonResult(name: "Cmp", baseline: null!, candidate: candidate))
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task ComparisonResult_NullCandidate_ThrowsArgumentNullException()
    {
        var baseline = BenchmarkResultFactory.Create("B");

        await Assert
            .That(() => new ComparisonResult(name: "Cmp", baseline: baseline, candidate: null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task ComparisonResult_Speedup_CandidateFaster_GreaterThanOne()
    {
        var comparison = ComparisonResultFactory.WithHighSpeedup();

        // baseline avg=1000, candidate avg=100 → speedup=10
        await Assert.That(comparison.Speedup).IsEqualTo(10.0);
        await Assert.That(comparison.IsFaster).IsTrue();
        await Assert.That(comparison.ImprovementPercent).IsEqualTo(900.0);
    }

    [Test]
    [Property("Category", "Models")]
    public async Task ComparisonResult_Speedup_CandidateSlower_LessThanOne()
    {
        var comparison = ComparisonResultFactory.WithSlowCandidate();

        // baseline avg=100, candidate avg=150 → speedup ≈ 0.667
        await Assert.That(comparison.Speedup).IsLessThan(1.0);
        await Assert.That(comparison.IsFaster).IsFalse();
        await Assert.That(comparison.ImprovementPercent).IsLessThan(0);
    }

    [Test]
    [Property("Category", "Models")]
    public async Task ComparisonResult_Speedup_NearZeroCandidateTime_ReturnsPositiveInfinity()
    {
        // Candidate avg is near-zero (0.001), baseline avg is 100
        var baseline = BenchmarkResultFactory.Create(
            "B",
            statistics: StatisticsFactory.Create(avg: 100.0)
        );
        var candidate = BenchmarkResultFactory.Create(
            "C",
            statistics: StatisticsFactory.Create(avg: 0.0)
        );

        var comparison = new ComparisonResult("Test", baseline, candidate);

        await Assert.That(double.IsPositiveInfinity(comparison.Speedup)).IsTrue();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task ComparisonResult_Speedup_BothNearZero_ReturnsOne()
    {
        var baseline = BenchmarkResultFactory.Create(
            "B",
            statistics: StatisticsFactory.Create(avg: 0.0)
        );
        var candidate = BenchmarkResultFactory.Create(
            "C",
            statistics: StatisticsFactory.Create(avg: 0.0)
        );

        var comparison = new ComparisonResult("Test", baseline, candidate);

        await Assert.That(comparison.Speedup).IsEqualTo(1.0);
    }

    [Test]
    [Property("Category", "Models")]
    public async Task ComparisonResult_OptionalProperties_SetCorrectly()
    {
        var tags = new Dictionary<string, string> { ["Env"] = "CI" };
        var comparison = new ComparisonResult(
            name: "Tagged",
            baseline: BenchmarkResultFactory.Create("B"),
            candidate: BenchmarkResultFactory.Create("C"),
            category: "Performance",
            tags: tags
        );

        await Assert.That(comparison.Category).IsEqualTo("Performance");
        await Assert.That(comparison.Tags).IsNotNull();
        await Assert.That(comparison.Tags!["Env"]).IsEqualTo("CI");
    }

    // ═══════════════════════════════════════════════════════════════
    //  BenchmarkSuite — constructor validation
    // ═══════════════════════════════════════════════════════════════

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkSuite_NullName_ThrowsArgumentNullException()
    {
        await Assert
            .That(
                () =>
                    new BenchmarkSuite(
                        name: null!,
                        environment: new EnvironmentInfo(),
                        results: new List<BenchmarkResult>(),
                        duration: TimeSpan.Zero
                    )
            )
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkSuite_NullEnvironment_ThrowsArgumentNullException()
    {
        await Assert
            .That(
                () =>
                    new BenchmarkSuite(
                        name: "Suite",
                        environment: null!,
                        results: new List<BenchmarkResult>(),
                        duration: TimeSpan.Zero
                    )
            )
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkSuite_NullResults_ThrowsArgumentNullException()
    {
        await Assert
            .That(
                () =>
                    new BenchmarkSuite(
                        name: "Suite",
                        environment: new EnvironmentInfo(),
                        results: null!,
                        duration: TimeSpan.Zero
                    )
            )
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkSuite_AllProperties_SetCorrectly()
    {
        var env = new EnvironmentInfo();
        var results = new List<BenchmarkResult> { BenchmarkResultFactory.Create() };
        var comparisons = new List<ComparisonResult> { ComparisonResultFactory.Create() };
        var timestamp = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromSeconds(10);

        var suite = new BenchmarkSuite(
            name: "Full",
            environment: env,
            results: results,
            duration: duration,
            description: "Desc",
            comparisons: comparisons,
            timestamp: timestamp
        );

        await Assert.That(suite.Name).IsEqualTo("Full");
        await Assert.That(suite.Description).IsEqualTo("Desc");
        await Assert.That(suite.Environment).IsEqualTo(env);
        await Assert.That(suite.Results.Count).IsEqualTo(1);
        await Assert.That(suite.Comparisons).IsNotNull();
        await Assert.That(suite.Comparisons!.Count).IsEqualTo(1);
        await Assert.That(suite.Duration).IsEqualTo(duration);
        await Assert.That(suite.Timestamp).IsEqualTo(timestamp);
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkSuite_NoTimestamp_UsesUtcNow()
    {
        var before = DateTime.UtcNow;
        var suite = BenchmarkSuiteFactory.Create();
        var after = DateTime.UtcNow;

        await Assert.That(suite.Timestamp).IsGreaterThanOrEqualTo(before);
        await Assert.That(suite.Timestamp).IsLessThanOrEqualTo(after);
    }

    // ═══════════════════════════════════════════════════════════════
    //  EnvironmentInfo
    // ═══════════════════════════════════════════════════════════════

    [Test]
    [Property("Category", "Models")]
    public async Task EnvironmentInfo_DefaultValues_ArePopulated()
    {
        var env = new EnvironmentInfo();

        await Assert.That(env.Os).IsNotNull();
        await Assert.That(env.Architecture).IsNotNull();
        await Assert.That(env.RuntimeVersion).IsNotNull();
        await Assert.That(env.ProcessorCount).IsGreaterThan(0);
    }

    [Test]
    [Property("Category", "Models")]
    public async Task EnvironmentInfo_ToString_ContainsRuntimeVersion()
    {
        var env = new EnvironmentInfo();
        var str = env.ToString();

        await Assert.That(str).Contains(env.RuntimeVersion);
        await Assert.That(str).Contains(env.Os);
        await Assert.That(str).Contains(env.Architecture);
    }

    [Test]
    [Property("Category", "Models")]
    public async Task EnvironmentInfo_IsNativeAot_ReflectedInToString()
    {
        var env = new EnvironmentInfo { IsNativeAot = true };
        await Assert.That(env.ToString()).Contains("AOT");

        var envJit = new EnvironmentInfo { IsNativeAot = false };
        await Assert.That(envJit.ToString()).Contains("JIT");
    }

    [Test]
    [Property("Category", "Models")]
    public async Task EnvironmentInfo_CustomTags_CanBeSet()
    {
        var tags = new Dictionary<string, string> { ["CI"] = "true" };
        var env = new EnvironmentInfo { CustomTags = tags };

        await Assert.That(env.CustomTags).IsNotNull();
        await Assert.That(env.CustomTags!["CI"]).IsEqualTo("true");
    }
}
