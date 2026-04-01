namespace PicoBench.Tests.Formatters;

public class SummaryFormatterTests
{
    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Summary")]
    public async Task Format_WithComparisons_ReturnsSummary()
    {
        var comparisons = ComparisonResultFactory.CreateMultiple(3).ToList();

        var summary = SummaryFormatter.Format(comparisons);

        await Assert.That(summary).IsNotNull();
        await Assert.That(summary).Contains("SUMMARY");
        await Assert.That(summary).Contains("╔");
        await Assert.That(summary).Contains("╗");
        await Assert.That(summary).Contains("╚");
        await Assert.That(summary).Contains("╝");
        await Assert.That(summary).Contains("wins");
        await Assert.That(summary).Contains("scenarios");
        await Assert.That(summary).Contains("faster");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Summary")]
    public async Task Format_EmptyComparisons_ReturnsNoResultsMessage()
    {
        var summary = SummaryFormatter.Format(Enumerable.Empty<ComparisonResult>());

        await Assert.That(summary).IsNotNull();
        await Assert.That(summary).Contains("No comparison results.");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Summary")]
    public async Task Format_WithDuration_IncludesDuration()
    {
        var comparisons = ComparisonResultFactory.CreateMultiple(2).ToList();
        var duration = TimeSpan.FromSeconds(10.5);

        var summary = SummaryFormatter.Format(comparisons, duration);

        await Assert.That(summary).IsNotNull();
        await Assert.That(summary).Contains("Duration:");
        await Assert.That(summary).Contains("10.50s");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Summary")]
    public async Task Format_WithoutDuration_ExcludesDuration()
    {
        var comparisons = ComparisonResultFactory.CreateMultiple(2).ToList();
        var options = new SummaryOptions { ShowDuration = false };

        var summary = SummaryFormatter.Format(comparisons, null, options);

        await Assert.That(summary).IsNotNull();
        await Assert.That(summary).DoesNotContain("Duration:");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Summary")]
    public async Task GroupByCategory_WhenEnabled_GroupsResults()
    {
        var comparisons = ComparisonResultFactory.CreateMultiple(4).ToList();
        // Set different categories
        var updatedComparisons = comparisons
            .Select(
                (c, i) =>
                    ComparisonResultFactory.Create(
                        name: c.Name,
                        category: i % 2 == 0 ? "CategoryA" : "CategoryB",
                        baseline: c.Baseline,
                        candidate: c.Candidate
                    )
            )
            .ToList();

        var options = new SummaryOptions { GroupByCategory = true };
        var summary = SummaryFormatter.Format(updatedComparisons, null, options);

        await Assert.That(summary).IsNotNull();
        await Assert.That(summary).Contains("▶ CategoryA:");
        await Assert.That(summary).Contains("▶ CategoryB:");
        await Assert.That(summary).Contains("average speedup");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Summary")]
    public async Task GroupByCategory_WhenDisabled_DoesNotGroup()
    {
        var comparisons = ComparisonResultFactory.CreateMultiple(4).ToList();
        var options = new SummaryOptions { GroupByCategory = false };

        var summary = SummaryFormatter.Format(comparisons, null, options);

        await Assert.That(summary).IsNotNull();
        // Category headings should not appear, but "▶ Detailed Results:" is OK
        await Assert.That(summary).DoesNotContain("average speedup");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Summary")]
    public async Task ShowDetailedTable_WhenEnabled_IncludesTable()
    {
        var comparisons = ComparisonResultFactory.CreateMultiple(2).ToList();
        var options = new SummaryOptions { ShowDetailedTable = true };

        var summary = SummaryFormatter.Format(comparisons, null, options);

        await Assert.That(summary).IsNotNull();
        await Assert.That(summary).Contains("▶ Detailed Results:");
        await Assert.That(summary).Contains("│"); // Table borders
        await Assert.That(summary).Contains("Test Case");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Summary")]
    public async Task ShowDetailedTable_WhenDisabled_ExcludesTable()
    {
        var comparisons = ComparisonResultFactory.CreateMultiple(2).ToList();
        var options = new SummaryOptions { ShowDetailedTable = false };

        var summary = SummaryFormatter.Format(comparisons, null, options);

        await Assert.That(summary).IsNotNull();
        await Assert.That(summary).DoesNotContain("▶ Detailed Results:");
        await Assert.That(summary).DoesNotContain("│"); // Table borders
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Summary")]
    public async Task CustomOptions_OverrideDefaults()
    {
        var comparisons = ComparisonResultFactory.CreateMultiple(2).ToList();
        var options = new SummaryOptions
        {
            Title = "CUSTOM SUMMARY",
            BoxWidth = 60,
            CandidateLabel = "Test",
            WinsLabel = "victories",
            GroupByCategory = false,
            ShowDetailedTable = false,
            ShowDuration = false
        };

        var summary = SummaryFormatter.Format(comparisons, null, options);

        await Assert.That(summary).IsNotNull();
        await Assert.That(summary).Contains("CUSTOM SUMMARY");
        await Assert.That(summary).Contains("Test victories");
        await Assert.That(summary).DoesNotContain("▶ "); // No grouping
        await Assert.That(summary).DoesNotContain("▶ Detailed Results:"); // No table
        await Assert.That(summary).DoesNotContain("Duration:"); // No duration
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Summary")]
    public async Task Write_Method_OutputsToConsole()
    {
        var comparisons = ComparisonResultFactory.CreateMultiple(2).ToList();

        // This should not throw
        SummaryFormatter.Write(comparisons);

        var completed = true;
        await Assert.That(completed).IsTrue();
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Summary")]
    public async Task Write_SuiteMethod_OutputsToConsole()
    {
        var suite = BenchmarkSuiteFactory.Create();

        // This should not throw
        SummaryFormatter.Write(suite);

        var completed = true;
        await Assert.That(completed).IsTrue();
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Summary")]
    public async Task Write_SuiteWithNoComparisons_OutputsMessage()
    {
        var suite = BenchmarkSuiteFactory.WithNoComparisons();

        // This should not throw
        SummaryFormatter.Write(suite);

        var completed = true;
        await Assert.That(completed).IsTrue();
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Summary")]
    public async Task TableOptions_UsesProvidedFormatterOptions()
    {
        var comparisons = ComparisonResultFactory.CreateMultiple(2).ToList();
        var tableOptions = new FormatterOptions
        {
            IncludePercentiles = false,
            BaselineLabel = "Control",
            CandidateLabel = "Test"
        };
        var summaryOptions = new SummaryOptions
        {
            ShowDetailedTable = true,
            TableOptions = tableOptions
        };

        var summary = SummaryFormatter.Format(comparisons, null, summaryOptions);

        await Assert.That(summary).IsNotNull();
        await Assert.That(summary).Contains("Control");
        await Assert.That(summary).Contains("Test");
        // With IncludePercentiles = false, detailed table should not have percentile columns
        // (though this is indirectly tested through ConsoleFormatter)
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Summary")]
    public async Task Format_WithUnicodeCharacters_HandlesCorrectly()
    {
        var comparisons = new List<ComparisonResult> { ComparisonResultFactory.WithUnicodeName() };

        var summary = SummaryFormatter.Format(comparisons);

        await Assert.That(summary).IsNotNull();
        await Assert.That(summary).Contains(comparisons[0].Name);
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Summary")]
    [MethodDataSource(nameof(GetEdgeCaseComparisons))]
    public async Task Format_WithEdgeCaseComparisons_DoesNotThrow(
        IEnumerable<ComparisonResult> comparisons
    )
    {
        var summary = SummaryFormatter.Format(comparisons);

        await Assert.That(summary).IsNotNull();
        await Assert.That(summary).Contains("SUMMARY");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Summary")]
    public async Task Format_WithNullComparisons_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.Run(() => SummaryFormatter.Format(null!))
        );
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Summary")]
    public async Task Write_WithNullComparisons_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.Run(() => SummaryFormatter.Write((IEnumerable<ComparisonResult>)null!))
        );
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Summary")]
    public async Task Write_WithNullSuite_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.Run(() => SummaryFormatter.Write((BenchmarkSuite)null!))
        );
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Summary")]
    public async Task SummaryStatistics_CalculatedCorrectly()
    {
        // Create comparisons with known speedups
        var baseline = BenchmarkResultFactory.Create(
            "Baseline",
            statistics: StatisticsFactory.Create(avg: 100.0)
        );
        var candidate1 = BenchmarkResultFactory.Create(
            "Candidate1",
            statistics: StatisticsFactory.Create(avg: 50.0)
        ); // 2x faster
        var candidate2 = BenchmarkResultFactory.Create(
            "Candidate2",
            statistics: StatisticsFactory.Create(avg: 200.0)
        ); // 0.5x faster (slower)

        var comparisons = new List<ComparisonResult>
        {
            new("Comp1", baseline, candidate1),
            new("Comp2", baseline, candidate2)
        };

        var summary = SummaryFormatter.Format(comparisons);

        await Assert.That(summary).IsNotNull();
        // Should show 1 win out of 2 scenarios
        await Assert.That(summary).Contains("1 / 2");
        // Average speedup = (2.0 + 0.5) / 2 = 1.25
        await Assert.That(summary).Contains("1.25");
        // Maximum speedup = 2.0
        await Assert.That(summary).Contains("2.00");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Summary")]
    public async Task BoxWidth_RespectedInOutput()
    {
        var comparisons = ComparisonResultFactory.CreateMultiple(2).ToList();
        var options = new SummaryOptions { BoxWidth = 50 };

        var summary = SummaryFormatter.Format(comparisons, null, options);

        await Assert.That(summary).IsNotNull();
        // Count characters in box border line
        var lines = summary.Split('\n');
        var borderLine = lines.FirstOrDefault(l => l.StartsWith('╔'));
        if (borderLine != null)
        {
            // Trim trailing \r on Windows where AppendLine produces \r\n
            await Assert.That(borderLine.TrimEnd('\r').Length).IsEqualTo(50);
        }
    }

    public static IEnumerable<IEnumerable<ComparisonResult>> GetEdgeCaseComparisons()
    {
        yield return ComparisonResultFactory.GetEdgeCases().ToList();
        yield return new List<ComparisonResult> { ComparisonResultFactory.WithHighSpeedup() };
        yield return new List<ComparisonResult> { ComparisonResultFactory.WithSlowCandidate() };
        yield return new List<ComparisonResult>
        {
            ComparisonResultFactory.WithNearZeroCandidateTime()
        };
        yield return new List<ComparisonResult> { ComparisonResultFactory.WithBothNearZeroTimes() };
    }
}
