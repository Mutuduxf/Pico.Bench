namespace PicoBench.Tests.Formatters;

public class ConsoleFormatterTests
{

    [Before(Test)]
    public async Task TestSetup(TestContext context)
    {
        context.Log("Starting test");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Console")]
    public async Task Format_SingleResult_ReturnsFormattedString()
    {
        var result = BenchmarkResultFactory.Create();
        var formatter = new ConsoleFormatter();

        var formatted = formatter.Format(result);

        await Assert.That(formatted).IsNotNull();
        await Assert.That(formatted).Contains(result.Name);
        await Assert.That(formatted).Contains("Avg:");
        await Assert.That(formatted).Contains("ns");
        await Assert.That(formatted).Contains("StdDev:");
        await Assert.That(formatted).Contains("StdErr:");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Console")]
    public async Task Format_MultipleResults_ReturnsTable()
    {
        var results = BenchmarkResultFactory.CreateMultiple(3).ToList();
        var formatter = new ConsoleFormatter();

        var formatted = formatter.Format(results);

        await Assert.That(formatted).IsNotNull();
        await Assert.That(formatted).Contains("Name");
        await Assert.That(formatted).Contains("Avg (ns)");
        await Assert.That(formatted).Contains("P50 (ns)");
        // Should contain table borders
        await Assert.That(formatted).Contains("┌");
        await Assert.That(formatted).Contains("┐");
        await Assert.That(formatted).Contains("└");
        await Assert.That(formatted).Contains("┘");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Console")]
    public async Task Format_EmptyResults_ReturnsNoResultsMessage()
    {
        var formatter = new ConsoleFormatter();

        var formatted = formatter.Format(Enumerable.Empty<BenchmarkResult>());

        await Assert.That(formatted).IsEqualTo("No results.");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Console")]
    [MethodDataSource(nameof(GetOptionCombinations))]
    public async Task OptionalColumns_RespectOptions(Func<FormatterOptions> optionsFactory)
    {
        var options = optionsFactory();
        var results = BenchmarkResultFactory.CreateMultiple(2).ToList();
        var formatter = new ConsoleFormatter(options);

        var formatted = formatter.Format(results);

        // Check for optional columns based on options
        if (options.IncludePercentiles)
        {
            await Assert.That(formatted).Contains("P90 (ns)");
            await Assert.That(formatted).Contains("P95 (ns)");
            await Assert.That(formatted).Contains("P99 (ns)");
        }
        else
        {
            await Assert.That(formatted).DoesNotContain("P90 (ns)");
            await Assert.That(formatted).DoesNotContain("P95 (ns)");
            await Assert.That(formatted).DoesNotContain("P99 (ns)");
        }

        if (options.IncludeCpuCycles)
        {
            await Assert.That(formatted).Contains("CPU Cycles");
        }
        else
        {
            await Assert.That(formatted).DoesNotContain("CPU Cycles");
        }

        if (options.IncludeGcInfo)
        {
            await Assert.That(formatted).Contains("GC (0/1/2)");
        }
        else
        {
            await Assert.That(formatted).DoesNotContain("GC (0/1/2)");
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Console")]
    [Property("Performance", "true")]
    [NotInParallel] // Performance tests should run sequentially
    public async Task StringBuilderPool_ReusesBuilders()
    {
        var formatter = new ConsoleFormatter();
        var results = BenchmarkResultFactory.CreateMultiple(5).ToList();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Format multiple times to exercise the pool
        var outputs = new List<string>();
        for (int i = 0; i < 100; i++)
        {
            outputs.Add(formatter.Format(results));
        }

        stopwatch.Stop();

        await Assert.That(outputs.All(output => output.Contains("Name"))).IsTrue();
        // Performance test completed

        // Note: We can't directly test the thread-static pool internally,
        // but we can verify the functionality works correctly
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Console")]
    public async Task TableColumns_DynamicWidthCalculation()
    {
        // Create results with varying name lengths
        var shortResult = BenchmarkResultFactory.Create("Short");
        var longResult = BenchmarkResultFactory.Create(new string('A', 50));
        var results = new[] { shortResult, longResult };
        var formatter = new ConsoleFormatter();

        var formatted = formatter.Format(results);

        // The table should accommodate the longest name
        await Assert.That(formatted).Contains(new string('A', 50));
        // Table should still be properly formatted
        await Assert.That(formatted).Contains("┌");
        await Assert.That(formatted).Contains("┐");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Console")]
    [Arguments("Test Benchmark", "Test description")]
    [Arguments("", "")]
    [Arguments(null, null)]
    public async Task Format_Suite_IncludesHeader(string? name, string? description)
    {
        var suite = new BenchmarkSuite(
            name: name ?? string.Empty,
            environment: new EnvironmentInfo(),
            results: BenchmarkResultFactory.CreateMultiple(2).ToList(),
            duration: TimeSpan.FromSeconds(5),
            description: description,
            comparisons: ComparisonResultFactory.CreateMultiple(1).ToList()
        );

        var formatter = new ConsoleFormatter();
        var formatted = formatter.Format(suite);

        await Assert.That(formatted).IsNotNull();
        if (!string.IsNullOrEmpty(name))
        {
            await Assert.That(formatted).Contains(name);
        }
        await Assert.That(formatted).Contains("CPU Counter:");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Console")]
    public async Task Format_SingleResult_WithHighVariance_IncludesNoiseWarning()
    {
        var noisy = BenchmarkResultFactory.Create(
            statistics: StatisticsFactory.Create(avg: 100.0, stdDev: 20.0, relativeStdDevPercent: 20.0)
        );
        var formatter = new ConsoleFormatter();

        var formatted = formatter.Format(noisy);

        await Assert.That(formatted).Contains("Noise: High variance");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Console")]
    public async Task Format_ComparisonResult_ReturnsComparisonDetails()
    {
        var comparison = ComparisonResultFactory.Create();
        var formatter = new ConsoleFormatter();

        var formatted = formatter.Format(comparison);

        await Assert.That(formatted).IsNotNull();
        await Assert.That(formatted).Contains(comparison.Name);
        await Assert.That(formatted).Contains("Baseline");
        await Assert.That(formatted).Contains("Candidate");
        await Assert.That(formatted).Contains("Speedup");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Console")]
    public async Task Format_Comparisons_ReturnsComparisonTable()
    {
        var comparisons = ComparisonResultFactory.CreateMultiple(3).ToList();
        var formatter = new ConsoleFormatter();

        var formatted = formatter.Format(comparisons);

        await Assert.That(formatted).IsNotNull();
        await Assert.That(formatted).Contains("Test Case");
        await Assert.That(formatted).Contains("Avg (ns)");
        await Assert.That(formatted).Contains("Speedup");
        await Assert.That(formatted).Contains("Summary:");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Console")]
    [Arguments("Custom Title", 2)]
    [Arguments("Another Title", 5)]
    public async Task FormatTableWithTitle_IncludesTitle(string title, int comparisonCount)
    {
        var comparisons = ComparisonResultFactory.CreateMultiple(comparisonCount).ToList();
        var formatter = new ConsoleFormatter();

        var formatted = formatter.FormatTableWithTitle(title, comparisons);

        await Assert.That(formatted).IsNotNull();
        await Assert.That(formatted).Contains(title);
        await Assert.That(formatted).Contains("Test Case");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Console")]
    public async Task StaticWriteMethods_DoNotThrow()
    {
        var result = BenchmarkResultFactory.Create();
        var results = BenchmarkResultFactory.CreateMultiple(2).ToList();
        var comparison = ComparisonResultFactory.Create();
        var comparisons = ComparisonResultFactory.CreateMultiple(2).ToList();

        // These should not throw exceptions
        ConsoleFormatter.Write(result);
        ConsoleFormatter.Write(results);
        ConsoleFormatter.Write(comparison);
        ConsoleFormatter.Write(comparisons);

        var suite = BenchmarkSuiteFactory.Create();
        ConsoleFormatter.Write(suite);

        var completed = true;
        await Assert.That(completed).IsTrue();
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Console")]
    public async Task WriteHeader_OutputsFormattedHeader()
    {
        // This test verifies the static WriteHeader method
        // We can't easily capture console output, so we just verify it doesn't throw
        ConsoleFormatter.WriteHeader("Test Header", width: 80);
        ConsoleFormatter.WriteHeader("Another Header");

        var completed = true;
        await Assert.That(completed).IsTrue();
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Console")]
    public async Task WriteEnvironment_OutputsEnvironmentInfo()
    {
        var env = new EnvironmentInfo();
        var config = new BenchmarkConfig { WarmupIterations = 100, IterationsPerSample = 1000 };

        ConsoleFormatter.WriteEnvironment(env, config);

        var completed = true;
        await Assert.That(completed).IsTrue();
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Console")]
    [Property("FileSystem", "true")]
    [NotInParallel]
    public async Task WriteFileSaved_OutputsMessage()
    {
        ConsoleFormatter.WriteFileSaved("CSV", "test.csv");

        var completed = true;
        await Assert.That(completed).IsTrue();
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Console")]
    [MethodDataSource(nameof(GetEdgeCaseResults))]
    public async Task Format_WithEdgeCaseResults_DoesNotThrow(BenchmarkResult result)
    {
        var formatter = new ConsoleFormatter();

        var formatted = formatter.Format(result);

        await Assert.That(formatted).IsNotNull();
        await Assert.That(formatted).Contains(result.Name);
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Console")]
    public async Task Format_WithUnicodeCharacters_HandlesCorrectly()
    {
        var result = BenchmarkResultFactory.WithUnicodeName();
        var formatter = new ConsoleFormatter();

        var formatted = formatter.Format(result);

        await Assert.That(formatted).IsNotNull();
        await Assert.That(formatted).Contains(result.Name);
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Console")]
    public async Task Format_WithNullInput_ThrowsArgumentNullException()
    {
        var formatter = new ConsoleFormatter();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.Run(() => formatter.Format((BenchmarkResult)null!))
        );

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.Run(() => formatter.Format((IEnumerable<BenchmarkResult>)null!))
        );

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.Run(() => formatter.Format((ComparisonResult)null!))
        );

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.Run(() => formatter.Format((IEnumerable<ComparisonResult>)null!))
        );

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.Run(() => formatter.Format((BenchmarkSuite)null!))
        );
    }

    public static IEnumerable<Func<FormatterOptions>> GetOptionCombinations()
    {
        yield return () => FormatterOptions.Default;
        yield return () => FormatterOptions.Compact;
        yield return () => FormatterOptions.Minimal;

        // Custom combinations
        yield return () => new FormatterOptions { IncludePercentiles = false, IncludeCpuCycles = true };
        yield return () => new FormatterOptions { IncludePercentiles = true, IncludeCpuCycles = false };
        yield return () => new FormatterOptions { IncludeGcInfo = false, IncludeEnvironment = false };
    }

    public static IEnumerable<BenchmarkResult> GetEdgeCaseResults()
    {
        return BenchmarkResultFactory.GetEdgeCases();
    }
}
