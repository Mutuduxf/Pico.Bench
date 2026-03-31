namespace PicoBench.Tests;

/// <summary>
/// Tests that verify statistics computation indirectly through the public
/// <see cref="Benchmark.Run"/> API, without accessing internal types.
/// </summary>
public class StatisticsCalculatorTests
{
    private static readonly BenchmarkConfig FastConfig =
        new()
        {
            WarmupIterations = 1,
            SampleCount = 5,
            IterationsPerSample = 2,
            RetainSamples = true
        };

    // ─── Basic statistics are populated ─────────────────────────────

    [Test]
    [Property("Category", "Statistics")]
    public async Task Run_ProducesStatistics_WithAvgGreaterThanOrEqualToZero()
    {
        var result = Benchmark.Run("AvgTest", () => { }, FastConfig);

        await Assert.That(result.Statistics.Avg).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    [Property("Category", "Statistics")]
    public async Task Run_ProducesStatistics_MinLessThanOrEqualToMax()
    {
        var result = Benchmark.Run("MinMaxTest", () => { }, FastConfig);

        await Assert.That(result.Statistics.Min).IsLessThanOrEqualTo(result.Statistics.Max);
    }

    [Test]
    [Property("Category", "Statistics")]
    public async Task Run_ProducesStatistics_AvgBetweenMinAndMax()
    {
        var result = Benchmark.Run("AvgBoundsTest", () => { }, FastConfig);

        await Assert.That(result.Statistics.Avg).IsGreaterThanOrEqualTo(result.Statistics.Min);
        await Assert.That(result.Statistics.Avg).IsLessThanOrEqualTo(result.Statistics.Max);
    }

    [Test]
    [Property("Category", "Statistics")]
    public async Task Run_ProducesStatistics_StdDevIsNonNegative()
    {
        var result = Benchmark.Run("StdDevTest", () => { }, FastConfig);

        await Assert.That(result.Statistics.StdDev).IsGreaterThanOrEqualTo(0);
    }

    // ─── Percentiles are ordered ────────────────────────────────────

    [Test]
    [Property("Category", "Statistics")]
    public async Task Run_PercentilesAreOrdered()
    {
        var result = Benchmark.Run("PercentileOrder", () => { }, FastConfig);
        var stats = result.Statistics;

        await Assert.That(stats.Min).IsLessThanOrEqualTo(stats.P50);
        await Assert.That(stats.P50).IsLessThanOrEqualTo(stats.P90);
        await Assert.That(stats.P90).IsLessThanOrEqualTo(stats.P95);
        await Assert.That(stats.P95).IsLessThanOrEqualTo(stats.P99);
        await Assert.That(stats.P99).IsLessThanOrEqualTo(stats.Max);
    }

    // ─── GC info is populated ───────────────────────────────────────

    [Test]
    [Property("Category", "Statistics")]
    public async Task Run_GcInfoIsPopulated()
    {
        var result = Benchmark.Run("GcInfoTest", () => { }, FastConfig);

        await Assert.That(result.Statistics.GcInfo).IsNotNull();
        await Assert.That(result.Statistics.GcInfo.Gen0).IsGreaterThanOrEqualTo(0);
        await Assert.That(result.Statistics.GcInfo.Gen1).IsGreaterThanOrEqualTo(0);
        await Assert.That(result.Statistics.GcInfo.Gen2).IsGreaterThanOrEqualTo(0);
    }

    // ─── CPU cycles ─────────────────────────────────────────────────

    [Test]
    [Property("Category", "Statistics")]
    public async Task Run_CpuCyclesPerOp_IsNonNegative()
    {
        var result = Benchmark.Run("CyclesTest", () => { }, FastConfig);

        await Assert.That(result.Statistics.CpuCyclesPerOp).IsGreaterThanOrEqualTo(0);
    }

    // ─── RetainSamples gives correct count ──────────────────────────

    [Test]
    [Property("Category", "Statistics")]
    public async Task Run_RetainSamples_SampleCountMatchesConfig()
    {
        var result = Benchmark.Run("SamplesCount", () => { }, FastConfig);

        await Assert.That(result.Samples).IsNotNull();
        await Assert.That(result.Samples!.Count).IsEqualTo(FastConfig.SampleCount);
    }

    [Test]
    [Property("Category", "Statistics")]
    public async Task Run_RetainSamples_EachSampleHasValidTiming()
    {
        var result = Benchmark.Run("SampleTiming", () => { }, FastConfig);

        foreach (var sample in result.Samples!)
        {
            await Assert.That(sample.ElapsedNanoseconds).IsGreaterThanOrEqualTo(0);
            await Assert.That(sample.ElapsedMilliseconds).IsGreaterThanOrEqualTo(0);
            await Assert.That(sample.ElapsedTicks).IsGreaterThanOrEqualTo(0);
            await Assert.That(sample.GcInfo).IsNotNull();
        }
    }

    // ─── Single sample config ───────────────────────────────────────

    [Test]
    [Property("Category", "Statistics")]
    public async Task Run_SingleSample_StdDevIsZero()
    {
        var singleConfig = new BenchmarkConfig
        {
            WarmupIterations = 0,
            SampleCount = 1,
            IterationsPerSample = 1
        };

        var result = Benchmark.Run("SingleSample", () => { }, singleConfig);

        // With a single sample, standard deviation should be zero
        await Assert.That(result.Statistics.StdDev).IsEqualTo(0.0);
    }

    // ─── Statistics with measurable work ────────────────────────────

    [Test]
    [Property("Category", "Statistics")]
    public async Task Run_WithMeasurableWork_AvgIsPositive()
    {
        var config = new BenchmarkConfig
        {
            WarmupIterations = 1,
            SampleCount = 3,
            IterationsPerSample = 2
        };

        var result = Benchmark.Run("MeasurableWork", () => Thread.SpinWait(100), config);

        await Assert.That(result.Statistics.Avg).IsGreaterThan(0);
    }

    // ─── Compare produces valid speedup ─────────────────────────────

    [Test]
    [Property("Category", "Statistics")]
    public async Task Compare_SpeedupIsPositive()
    {
        var comparison = Benchmark.Compare(
            "SpeedupTest",
            "A",
            () => { },
            "B",
            () => { },
            FastConfig
        );

        await Assert.That(comparison.Speedup).IsGreaterThan(0);
    }
}
