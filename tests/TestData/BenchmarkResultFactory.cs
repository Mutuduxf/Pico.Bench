using Pico.Bench;

namespace Pico.Bench.Tests.TestData;

public static class BenchmarkResultFactory
{
    /// <summary>
    /// Creates a BenchmarkResult with reasonable default values.
    /// </summary>
    public static BenchmarkResult Create(
        string name = "TestBenchmark",
        string? category = "Test",
        Statistics? statistics = null,
        int sampleCount = 100,
        int iterationsPerSample = 1000,
        IReadOnlyDictionary<string, string>? tags = null,
        IReadOnlyList<TimingSample>? samples = null,
        DateTime? timestamp = null)
    {
        return new BenchmarkResult(
            name: name,
            statistics: statistics ?? StatisticsFactory.Create(),
            iterationsPerSample: iterationsPerSample,
            sampleCount: sampleCount,
            category: category,
            tags: tags,
            samples: samples,
            timestamp: timestamp
        );
    }

    /// <summary>
    /// Creates a BenchmarkResult with Unicode characters in name.
    /// </summary>
    public static BenchmarkResult WithUnicodeName()
    {
        return Create(name: "测试基准🎯性能测试");
    }

    /// <summary>
    /// Creates a BenchmarkResult with very long name.
    /// </summary>
    public static BenchmarkResult WithLongName()
    {
        return Create(name: new string('A', 100));
    }

    /// <summary>
    /// Creates a BenchmarkResult with null category.
    /// </summary>
    public static BenchmarkResult WithNullCategory()
    {
        return Create(category: null);
    }

    /// <summary>
    /// Creates a BenchmarkResult with empty category.
    /// </summary>
    public static BenchmarkResult WithEmptyCategory()
    {
        return Create(category: "");
    }

    /// <summary>
    /// Creates a BenchmarkResult with minimal sample count (1).
    /// </summary>
    public static BenchmarkResult WithMinimalSamples()
    {
        return Create(sampleCount: 1, iterationsPerSample: 1);
    }

    /// <summary>
    /// Creates a BenchmarkResult with large sample count.
    /// </summary>
    public static BenchmarkResult WithLargeSamples()
    {
        return Create(sampleCount: 10_000, iterationsPerSample: 100_000);
    }

    /// <summary>
    /// Creates a BenchmarkResult with tags.
    /// </summary>
    public static BenchmarkResult WithTags()
    {
        var tags = new Dictionary<string, string>
        {
            ["Environment"] = "Test",
            ["Priority"] = "High",
            ["Owner"] = "QA"
        };
        return Create(tags: tags);
    }

    /// <summary>
    /// Creates a BenchmarkResult with specific timestamp.
    /// </summary>
    public static BenchmarkResult WithSpecificTimestamp()
    {
        return Create(timestamp: new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc));
    }

    /// <summary>
    /// Gets a collection of edge-case BenchmarkResult instances for testing.
    /// </summary>
    public static IEnumerable<BenchmarkResult> GetEdgeCases()
    {
        yield return WithUnicodeName();
        yield return WithLongName();
        yield return WithNullCategory();
        yield return WithEmptyCategory();
        yield return WithMinimalSamples();
        yield return WithLargeSamples();
        yield return WithTags();
        yield return WithSpecificTimestamp();
        
        // Combine with statistical edge cases
        foreach (var stats in StatisticsFactory.GetEdgeCases())
        {
            yield return Create(
                name: $"EdgeCase_{stats.Avg}",
                statistics: stats
            );
        }
    }

    /// <summary>
    /// Creates multiple BenchmarkResult instances with varying names for table testing.
    /// </summary>
    public static IEnumerable<BenchmarkResult> CreateMultiple(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return Create(
                name: $"Benchmark_{i + 1}",
                category: i % 2 == 0 ? "CategoryA" : "CategoryB",
                statistics: StatisticsFactory.Create(avg: 100.0 + i * 50.0)
            );
        }
    }
}