namespace PicoBench.Tests.TestData;

public static class StatisticsFactory
{
    /// <summary>
    /// Creates Statistics with reasonable default values.
    /// </summary>
    public static Statistics Create(
        double avg = 150.5,
        double p50 = 145.0,
        double p90 = 160.0,
        double p95 = 165.0,
        double p99 = 180.0,
        double min = 120.0,
        double max = 200.0,
        double stdDev = 20.0,
        double cpuCyclesPerOp = 300.0,
        GcInfo? gcInfo = null
    )
    {
        return new Statistics
        {
            Avg = avg,
            P50 = p50,
            P90 = p90,
            P95 = p95,
            P99 = p99,
            Min = min,
            Max = max,
            StdDev = stdDev,
            CpuCyclesPerOp = cpuCyclesPerOp,
            GcInfo = gcInfo ?? GcInfoFactory.Create()
        };
    }

    /// <summary>
    /// Creates Statistics with very small time values (near zero).
    /// </summary>
    public static Statistics WithZeroTime()
    {
        return Create(
            avg: 0.001,
            p50: 0.001,
            p90: 0.001,
            p95: 0.001,
            p99: 0.001,
            min: 0.0001,
            max: 0.002,
            stdDev: 0.0001,
            cpuCyclesPerOp: 1.0
        );
    }

    /// <summary>
    /// Creates Statistics with extremely large time values.
    /// </summary>
    public static Statistics WithExtremeTime()
    {
        return Create(
            avg: 1_000_000_000.0, // 1 second in nanoseconds
            p50: 950_000_000.0,
            p90: 1_100_000_000.0,
            p95: 1_200_000_000.0,
            p99: 1_500_000_000.0,
            min: 800_000_000.0,
            max: 2_000_000_000.0,
            stdDev: 200_000_000.0,
            cpuCyclesPerOp: 5_000_000_000.0
        );
    }

    /// <summary>
    /// Creates Statistics with zero CPU cycles.
    /// </summary>
    public static Statistics WithZeroCpuCycles()
    {
        var stats = Create();
        return new Statistics
        {
            Avg = stats.Avg,
            P50 = stats.P50,
            P90 = stats.P90,
            P95 = stats.P95,
            P99 = stats.P99,
            Min = stats.Min,
            Max = stats.Max,
            StdDev = stats.StdDev,
            CpuCyclesPerOp = 0.0,
            GcInfo = stats.GcInfo
        };
    }

    /// <summary>
    /// Creates Statistics with NaN or Infinite values for edge-case testing.
    /// </summary>
    public static Statistics WithSpecialNumericValues()
    {
        return Create(
            avg: double.NaN,
            p50: double.PositiveInfinity,
            p90: double.NegativeInfinity,
            p95: double.NaN,
            p99: double.PositiveInfinity,
            min: double.NegativeInfinity,
            max: double.PositiveInfinity,
            stdDev: double.NaN,
            cpuCyclesPerOp: double.NaN
        );
    }

    /// <summary>
    /// Gets a collection of edge-case Statistics instances for testing.
    /// </summary>
    public static IEnumerable<Statistics> GetEdgeCases()
    {
        yield return WithZeroTime();
        yield return WithExtremeTime();
        yield return WithZeroCpuCycles();
        yield return WithSpecialNumericValues();

        // Variations with different GC patterns
        yield return Create(gcInfo: GcInfoFactory.Zero());
        yield return Create(gcInfo: GcInfoFactory.Many());

        // Minimal standard deviation
        yield return Create(stdDev: 0.0);

        // Equal percentiles (perfectly consistent)
        yield return Create(
            avg: 100.0,
            p50: 100.0,
            p90: 100.0,
            p95: 100.0,
            p99: 100.0,
            min: 100.0,
            max: 100.0,
            stdDev: 0.0
        );
    }
}
