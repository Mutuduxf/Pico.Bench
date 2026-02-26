using Pico.Bench;

namespace Pico.Bench.Tests.TestData;

public static class ComparisonResultFactory
{
    /// <summary>
    /// Creates a ComparisonResult with reasonable default values.
    /// </summary>
    public static ComparisonResult Create(
        string name = "TestComparison",
        string? category = "Test",
        BenchmarkResult? baseline = null,
        BenchmarkResult? candidate = null,
        IReadOnlyDictionary<string, string>? tags = null)
    {
        baseline ??= BenchmarkResultFactory.Create("Baseline");
        candidate ??= BenchmarkResultFactory.Create("Candidate");
        
        return new ComparisonResult(
            name: name,
            baseline: baseline,
            candidate: candidate,
            category: category,
            tags: tags
        );
    }

    /// <summary>
    /// Creates a ComparisonResult where candidate is much faster (high speedup).
    /// </summary>
    public static ComparisonResult WithHighSpeedup()
    {
        var baseline = BenchmarkResultFactory.Create(
            "Slow",
            statistics: StatisticsFactory.Create(avg: 1000.0)
        );
        
        var candidate = BenchmarkResultFactory.Create(
            "Fast",
            statistics: StatisticsFactory.Create(avg: 100.0) // 10x faster
        );
        
        return Create("HighSpeedup", baseline: baseline, candidate: candidate);
    }

    /// <summary>
    /// Creates a ComparisonResult where candidate is slightly slower (speedup < 1).
    /// </summary>
    public static ComparisonResult WithSlowCandidate()
    {
        var baseline = BenchmarkResultFactory.Create(
            "Fast",
            statistics: StatisticsFactory.Create(avg: 100.0)
        );
        
        var candidate = BenchmarkResultFactory.Create(
            "Slow",
            statistics: StatisticsFactory.Create(avg: 150.0) // 0.67x speedup
        );
        
        return Create("SlowCandidate", baseline: baseline, candidate: candidate);
    }

    /// <summary>
    /// Creates a ComparisonResult with near-zero candidate time (infinite speedup).
    /// </summary>
    public static ComparisonResult WithNearZeroCandidateTime()
    {
        var baseline = BenchmarkResultFactory.Create(
            "Baseline",
            statistics: StatisticsFactory.Create(avg: 100.0)
        );
        
        var candidate = BenchmarkResultFactory.Create(
            "NearZero",
            statistics: StatisticsFactory.WithZeroTime()
        );
        
        return Create("NearZeroCandidate", baseline: baseline, candidate: candidate);
    }

    /// <summary>
    /// Creates a ComparisonResult with near-zero baseline and candidate times (speedup = 1).
    /// </summary>
    public static ComparisonResult WithBothNearZeroTimes()
    {
        var baseline = BenchmarkResultFactory.Create(
            "Baseline",
            statistics: StatisticsFactory.WithZeroTime()
        );
        
        var candidate = BenchmarkResultFactory.Create(
            "Candidate",
            statistics: StatisticsFactory.WithZeroTime()
        );
        
        return Create("BothNearZero", baseline: baseline, candidate: candidate);
    }

    /// <summary>
    /// Creates a ComparisonResult with NaN/infinite statistics.
    /// </summary>
    public static ComparisonResult WithSpecialNumericValues()
    {
        var baseline = BenchmarkResultFactory.Create(
            "Baseline",
            statistics: StatisticsFactory.WithSpecialNumericValues()
        );
        
        var candidate = BenchmarkResultFactory.Create(
            "Candidate",
            statistics: StatisticsFactory.WithSpecialNumericValues()
        );
        
        return Create("SpecialNumerics", baseline: baseline, candidate: candidate);
    }

    /// <summary>
    /// Creates a ComparisonResult with Unicode characters in name.
    /// </summary>
    public static ComparisonResult WithUnicodeName()
    {
        return Create(name: "比较测试🎯性能对比");
    }

    /// <summary>
    /// Creates a ComparisonResult with null category.
    /// </summary>
    public static ComparisonResult WithNullCategory()
    {
        return Create(category: null);
    }

    /// <summary>
    /// Creates a ComparisonResult with empty category.
    /// </summary>
    public static ComparisonResult WithEmptyCategory()
    {
        return Create(category: "");
    }

    /// <summary>
    /// Creates a ComparisonResult with tags.
    /// </summary>
    public static ComparisonResult WithTags()
    {
        var tags = new Dictionary<string, string>
        {
            ["TestType"] = "Performance",
            ["Priority"] = "Critical",
            ["Component"] = "Formatter"
        };
        
        return Create(tags: tags);
    }

    /// <summary>
    /// Gets a collection of edge-case ComparisonResult instances for testing.
    /// </summary>
    public static IEnumerable<ComparisonResult> GetEdgeCases()
    {
        yield return WithHighSpeedup();
        yield return WithSlowCandidate();
        yield return WithNearZeroCandidateTime();
        yield return WithBothNearZeroTimes();
        yield return WithSpecialNumericValues();
        yield return WithUnicodeName();
        yield return WithNullCategory();
        yield return WithEmptyCategory();
        yield return WithTags();
    }

    /// <summary>
    /// Creates multiple ComparisonResult instances with varying speedups for table testing.
    /// </summary>
    public static IEnumerable<ComparisonResult> CreateMultiple(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var baseline = BenchmarkResultFactory.Create(
                $"Baseline_{i + 1}",
                statistics: StatisticsFactory.Create(avg: 200.0 + i * 100.0)
            );
            
            var candidate = BenchmarkResultFactory.Create(
                $"Candidate_{i + 1}",
                statistics: StatisticsFactory.Create(avg: 100.0 + i * 50.0)
            );
            
            yield return Create(
                name: $"Comparison_{i + 1}",
                category: i % 2 == 0 ? "CategoryA" : "CategoryB",
                baseline: baseline,
                candidate: candidate
            );
        }
    }
}