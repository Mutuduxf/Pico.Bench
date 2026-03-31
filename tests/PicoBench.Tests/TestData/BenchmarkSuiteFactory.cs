namespace PicoBench.Tests.TestData;

public static class BenchmarkSuiteFactory
{
    /// <summary>
    /// Creates a BenchmarkSuite with reasonable default values.
    /// </summary>
    public static BenchmarkSuite Create(
        string name = "TestSuite",
        string? description = "Test benchmark suite",
        EnvironmentInfo? environment = null,
        IReadOnlyList<BenchmarkResult>? results = null,
        IReadOnlyList<ComparisonResult>? comparisons = null,
        DateTime? timestamp = null,
        TimeSpan? duration = null
    )
    {
        return new BenchmarkSuite(
            name: name,
            environment: environment ?? new EnvironmentInfo(),
            results: results ?? [.. BenchmarkResultFactory.CreateMultiple(3)],
            duration: duration ?? TimeSpan.FromSeconds(5.5),
            description: description,
            comparisons: comparisons ?? [.. ComparisonResultFactory.CreateMultiple(2)],
            timestamp: timestamp
        );
    }

    /// <summary>
    /// Creates a BenchmarkSuite with no results.
    /// </summary>
    public static BenchmarkSuite WithNoResults()
    {
        return Create(
            name: "EmptySuite",
            description: "Suite with no benchmark results",
            results: []
        );
    }

    /// <summary>
    /// Creates a BenchmarkSuite with no comparisons.
    /// </summary>
    public static BenchmarkSuite WithNoComparisons()
    {
        return Create(
            name: "NoComparisonsSuite",
            description: "Suite with results but no comparisons",
            comparisons: Array.Empty<ComparisonResult>()
        );
    }

    /// <summary>
    /// Creates a BenchmarkSuite with many results and comparisons.
    /// </summary>
    public static BenchmarkSuite WithManyItems()
    {
        return Create(
            name: "LargeSuite",
            description: "Suite with many items for testing",
            results: [.. BenchmarkResultFactory.CreateMultiple(10)],
            comparisons: [.. ComparisonResultFactory.CreateMultiple(8)]
        );
    }

    /// <summary>
    /// Creates a BenchmarkSuite with Unicode characters.
    /// </summary>
    public static BenchmarkSuite WithUnicodeContent()
    {
        return Create(
            name: "测试套件🎯",
            description: "性能测试套件包含多种场景",
            results: [BenchmarkResultFactory.WithUnicodeName()],
            comparisons: [ComparisonResultFactory.WithUnicodeName()]
        );
    }

    /// <summary>
    /// Creates a BenchmarkSuite with null description.
    /// </summary>
    public static BenchmarkSuite WithNullDescription()
    {
        return Create(name: "NoDescriptionSuite", description: null);
    }

    /// <summary>
    /// Creates a BenchmarkSuite with empty description.
    /// </summary>
    public static BenchmarkSuite WithEmptyDescription()
    {
        return Create(name: "EmptyDescriptionSuite", description: "");
    }

    /// <summary>
    /// Creates a BenchmarkSuite with custom environment info.
    /// </summary>
    public static BenchmarkSuite WithCustomEnvironment()
    {
        var customTags = new Dictionary<string, string>
        {
            ["CI"] = "GitHubActions",
            ["Runner"] = "ubuntu-latest",
            ["NETVersion"] = "10.0"
        };

        var environment = new EnvironmentInfo { CustomTags = customTags };

        return Create(
            name: "CustomEnvSuite",
            description: "Suite with custom environment tags",
            environment: environment
        );
    }

    /// <summary>
    /// Creates a BenchmarkSuite with specific timestamp and duration.
    /// </summary>
    public static BenchmarkSuite WithSpecificTiming()
    {
        return Create(
            name: "TimedSuite",
            description: "Suite with specific timing information",
            timestamp: new DateTime(2024, 12, 25, 10, 30, 0, DateTimeKind.Utc),
            duration: TimeSpan.FromMinutes(2.5)
        );
    }

    /// <summary>
    /// Gets a collection of edge-case BenchmarkSuite instances for testing.
    /// </summary>
    public static IEnumerable<BenchmarkSuite> GetEdgeCases()
    {
        yield return WithNoResults();
        yield return WithNoComparisons();
        yield return WithManyItems();
        yield return WithUnicodeContent();
        yield return WithNullDescription();
        yield return WithEmptyDescription();
        yield return WithCustomEnvironment();
        yield return WithSpecificTiming();
    }

    /// <summary>
    /// Creates a BenchmarkSuite with all formatter options variations for comprehensive testing.
    /// </summary>
    public static BenchmarkSuite ForFormatterOptionsTesting()
    {
        // Create results with varying statistics to test all columns
        var results = new List<BenchmarkResult>();

        // Fast result
        results.Add(
            BenchmarkResultFactory.Create(
                "FastOperation",
                statistics: StatisticsFactory.WithZeroTime()
            )
        );

        // Slow result
        results.Add(
            BenchmarkResultFactory.Create(
                "SlowOperation",
                statistics: StatisticsFactory.WithExtremeTime()
            )
        );

        // Result with GC activity
        results.Add(
            BenchmarkResultFactory.Create(
                "GcHeavyOperation",
                statistics: StatisticsFactory.Create(gcInfo: GcInfoFactory.Many())
            )
        );

        // Result with CPU cycles
        results.Add(
            BenchmarkResultFactory.Create(
                "CpuIntensiveOperation",
                statistics: StatisticsFactory.Create(cpuCyclesPerOp: 5000.0)
            )
        );

        // Create comparisons with varying speedups
        var comparisons = new List<ComparisonResult>();

        // High speedup comparison
        comparisons.Add(ComparisonResultFactory.WithHighSpeedup());

        // Slow candidate comparison
        comparisons.Add(ComparisonResultFactory.WithSlowCandidate());

        // Edge case comparison
        comparisons.Add(ComparisonResultFactory.WithNearZeroCandidateTime());

        return Create(
            name: "FormatterTestSuite",
            description: "Suite specifically designed for formatter testing",
            results: results,
            comparisons: comparisons
        );
    }
}
