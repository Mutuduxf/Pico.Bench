namespace PicoBench.Tests.Formatters;

public class MarkdownFormatterTests
{
    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Markdown")]
    public async Task Format_Results_ReturnsValidMarkdownTable()
    {
        var results = BenchmarkResultFactory.CreateMultiple(2).ToList();
        var formatter = new MarkdownFormatter();

        var markdown = formatter.Format(results);

        await Assert.That(markdown).IsNotNull();
        await Assert.That(markdown).Contains("| Name | Avg (ns) | P50 (ns)");
        await Assert.That(markdown).Contains("|------|----------|----------");
        await Assert.That(markdown).Contains(results[0].Name);
        await Assert.That(markdown).Contains(results[1].Name);

        // Count table rows (header + separator + 2 data rows)
        var lines = markdown.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        await Assert.That(lines.Length).IsGreaterThanOrEqualTo(4);
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Markdown")]
    public async Task Format_EmptyResults_ReturnsNoResultsMessage()
    {
        var formatter = new MarkdownFormatter();

        var markdown = formatter.Format(Enumerable.Empty<BenchmarkResult>());

        await Assert.That(markdown).IsEqualTo("*No results.*");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Markdown")]
    [Arguments("Normal|Text", "Normal\\|Text")]
    [Arguments("Text|With|Multiple|Pipes", "Text\\|With\\|Multiple\\|Pipes")]
    [Arguments("NoPipes", "NoPipes")]
    [Arguments("", "")]
    public async Task Escape_HandlesPipeCharacters(string input, string expected)
    {
        // Test through actual formatting
        var result = BenchmarkResultFactory.Create(input);
        var formatter = new MarkdownFormatter();

        var markdown = formatter.Format(result);

        await Assert.That(markdown).IsNotNull();

        if (input.Contains('|'))
        {
            await Assert.That(markdown).Contains(expected);
            await Assert.That(markdown).DoesNotContain(input);
        }
        else
        {
            await Assert.That(markdown).Contains(input);
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Markdown")]
    public async Task Format_Comparisons_ReturnsComparisonTable()
    {
        var comparisons = ComparisonResultFactory.CreateMultiple(2).ToList();
        var formatter = new MarkdownFormatter();

        var markdown = formatter.Format(comparisons);

        await Assert.That(markdown).IsNotNull();
        await Assert.That(markdown).Contains("| Test Case | Avg (ns) | Speedup |");
        await Assert.That(markdown).Contains("**"); // Speedup should be bold
        // Speedup indicator for 2x is "*"; "***" requires >= 10x
        await Assert.That(markdown).Contains("*");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Markdown")]
    public async Task Format_Suite_ReturnsCompleteMarkdown()
    {
        var suite = BenchmarkSuiteFactory.Create();
        var formatter = new MarkdownFormatter();

        var markdown = formatter.Format(suite);

        await Assert.That(markdown).IsNotNull();
        await Assert.That(markdown).StartsWith($"# {suite.Name}");
        await Assert.That(markdown).Contains("## Results");
        await Assert.That(markdown).Contains("## Comparisons");
        await Assert.That(markdown).Contains("### Summary");
        await Assert.That(markdown).Contains("```");

        if (!string.IsNullOrEmpty(suite.Description))
        {
            await Assert.That(markdown).Contains(suite.Description);
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Markdown")]
    public async Task FormatGroupedComparisons_ByCategory_ReturnsGroupedMarkdown()
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

        var markdown = MarkdownFormatter.FormatGroupedComparisons(
            updatedComparisons,
            c => c.Category ?? "Uncategorized"
        );

        await Assert.That(markdown).IsNotNull();
        await Assert.That(markdown).Contains("### CategoryA");
        await Assert.That(markdown).Contains("### CategoryB");
        await Assert.That(markdown).Contains("| Test Case |");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Markdown")]
    [MethodDataSource(nameof(GetOptionCombinations))]
    public async Task OptionalColumns_RespectOptions(FormatterOptions options)
    {
        var results = BenchmarkResultFactory.CreateMultiple(1).ToList();
        var formatter = new MarkdownFormatter(options);

        var markdown = formatter.Format(results);

        if (options.IncludePercentiles)
        {
            await Assert.That(markdown).Contains("| P90 (ns) | P95 (ns) | P99 (ns) ");
        }
        else
        {
            await Assert.That(markdown).DoesNotContain("| P90 (ns) | P95 (ns) | P99 (ns) ");
        }

        if (options.IncludeCpuCycles)
        {
            await Assert.That(markdown).Contains("| CPU Cycle ");
        }
        else
        {
            await Assert.That(markdown).DoesNotContain("| CPU Cycle ");
        }

        if (options.IncludeGcInfo)
        {
            await Assert.That(markdown).Contains("| GC (0/1/2) ");
        }
        else
        {
            await Assert.That(markdown).DoesNotContain("| GC (0/1/2) ");
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Markdown")]
    [Property("FileSystem", "true")]
    [NotInParallel]
    [MethodDataSource(nameof(GetDummyTestContext))]
    public async Task WriteToFile_CreatesValidMarkdownFile(TestContext context)
    {
        var testDir = FileSystemHelper.CreateTestDirectory();
        try
        {
            var filePath = Path.Combine(testDir, "test.md");
            var result = BenchmarkResultFactory.Create();

            MarkdownFormatter.WriteToFile(filePath, result);

            await Assert.That(File.Exists(filePath)).IsTrue();
            var content = await File.ReadAllTextAsync(filePath);

            await Assert.That(content).Contains("| Name | Avg (ns) | P50 (ns)");
            await Assert.That(content).Contains(result.Name);

            context?.LogFileOperation("WriteToFile Markdown", filePath);
        }
        finally
        {
            FileSystemHelper.DeleteTestDirectory(testDir);
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Markdown")]
    public async Task Format_WithUnicodeCharacters_HandlesCorrectly()
    {
        var result = BenchmarkResultFactory.WithUnicodeName();
        var formatter = new MarkdownFormatter();

        var markdown = formatter.Format(result);

        await Assert.That(markdown).IsNotNull();
        await Assert.That(markdown).Contains(result.Name);
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Markdown")]
    public async Task Format_WithCustomLabels_UsesLabels()
    {
        var options = new FormatterOptions
        {
            BaselineLabel = "Control",
            CandidateLabel = "Treatment"
        };
        var comparisons = ComparisonResultFactory.CreateMultiple(1).ToList();
        var formatter = new MarkdownFormatter(options);

        var markdown = formatter.Format(comparisons);

        await Assert.That(markdown).IsNotNull();
        await Assert.That(markdown).Contains("Control");
        await Assert.That(markdown).Contains("Treatment");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Markdown")]
    public async Task Format_WithNullInput_ThrowsArgumentNullException()
    {
        var formatter = new MarkdownFormatter();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.Run(() => formatter.Format((BenchmarkResult)null!))
        );

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.Run(() => formatter.Format((IEnumerable<BenchmarkResult>)null!))
        );

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.Run(() => formatter.Format((ComparisonResult)null!))
        );

        await Assert
            .That(
                async () =>
                    await Task.Run(() => formatter.Format((IEnumerable<ComparisonResult>)null!))
            )
            .Throws<ArgumentNullException>();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Task.Run(() => formatter.Format((BenchmarkSuite)null!))
        );
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Markdown")]
    public async Task MarkdownTableFormat_ProperAlignment()
    {
        var results = BenchmarkResultFactory.CreateMultiple(3).ToList();
        var formatter = new MarkdownFormatter();

        var markdown = formatter.Format(results);

        await Assert.That(markdown).IsNotNull();

        // Split into lines and check table structure
        var lines = markdown.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        await Assert.That(lines.Length).IsGreaterThanOrEqualTo(5); // Header + separator + 3 rows

        // Check header separator line has correct number of columns
        var headerLine = lines.First(l => l.Contains("|---"));
        var columnCount = headerLine.Count(ch => ch == '|') - 1;
        await Assert.That(columnCount).IsGreaterThanOrEqualTo(3); // At least Name, Avg, P50

        // All data lines should have same number of columns
        var dataLines = lines.Where(l => !l.Contains("|---") && l.StartsWith('|')).ToList();
        foreach (var line in dataLines)
        {
            await Assert.That(line.Count(ch => ch == '|')).IsEqualTo(columnCount + 1);
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Markdown")]
    public async Task Format_Suite_WithNoComparisons_OmitsComparisonsSection()
    {
        var suite = BenchmarkSuiteFactory.WithNoComparisons();
        var formatter = new MarkdownFormatter();

        var markdown = formatter.Format(suite);

        await Assert.That(markdown).IsNotNull();
        await Assert.That(markdown).Contains("## Results");
        await Assert.That(markdown).DoesNotContain("## Comparisons");
        await Assert.That(markdown).DoesNotContain("### Summary");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Markdown")]
    public async Task Format_Suite_WithNoResults_ReturnsOnlyHeader()
    {
        var suite = BenchmarkSuiteFactory.WithNoResults();
        var formatter = new MarkdownFormatter();

        var markdown = formatter.Format(suite);

        await Assert.That(markdown).IsNotNull();
        await Assert.That(markdown).StartsWith($"# {suite.Name}");
        await Assert.That(markdown).DoesNotContain("## Results");
        await Assert.That(markdown).DoesNotContain("*No results.*");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Markdown")]
    [Property("FileSystem", "true")]
    [NotInParallel]
    [MethodDataSource(nameof(GetDummyTestContext))]
    public async Task WriteToFile_Comparisons_CreatesValidMarkdown(TestContext context)
    {
        var testDir = FileSystemHelper.CreateTestDirectory();
        try
        {
            var filePath = Path.Combine(testDir, "comparisons.md");
            var comparisons = ComparisonResultFactory.CreateMultiple(2).ToList();

            MarkdownFormatter.WriteToFile(filePath, comparisons);

            await Assert.That(File.Exists(filePath)).IsTrue();
            var content = await File.ReadAllTextAsync(filePath);

            await Assert.That(content).Contains("| Test Case |");
            await Assert.That(content).Contains("**"); // Bold speedup

            context?.LogFileOperation("WriteToFile comparisons Markdown", filePath);
        }
        finally
        {
            FileSystemHelper.DeleteTestDirectory(testDir);
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Markdown")]
    [Property("FileSystem", "true")]
    [NotInParallel]
    [MethodDataSource(nameof(GetDummyTestContext))]
    public async Task WriteToFile_Suite_CreatesValidMarkdown(TestContext context)
    {
        var testDir = FileSystemHelper.CreateTestDirectory();
        try
        {
            var filePath = Path.Combine(testDir, "suite.md");
            var suite = BenchmarkSuiteFactory.Create();

            MarkdownFormatter.WriteToFile(filePath, suite);

            await Assert.That(File.Exists(filePath)).IsTrue();
            var content = await File.ReadAllTextAsync(filePath);

            await Assert.That(content).StartsWith($"# {suite.Name}");
            await Assert.That(content).Contains("## Results");

            context?.LogFileOperation("WriteToFile suite Markdown", filePath);
        }
        finally
        {
            FileSystemHelper.DeleteTestDirectory(testDir);
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "Markdown")]
    public async Task FormatGroupedComparisons_WithNullGroupBy_ThrowsArgumentNullException()
    {
        var comparisons = ComparisonResultFactory.CreateMultiple(2).ToList();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () =>
                Task.Run(
                    () =>
                        MarkdownFormatter.FormatGroupedComparisons(
                            comparisons,
                            (Func<ComparisonResult, string>)null!
                        )
                )
        );
    }

    public static IEnumerable<FormatterOptions> GetOptionCombinations()
    {
        yield return FormatterOptions.Default;
        yield return FormatterOptions.Compact;
        yield return FormatterOptions.Minimal;
        yield return new FormatterOptions { IncludePercentiles = false };
        yield return new FormatterOptions { IncludeCpuCycles = false };
        yield return new FormatterOptions { IncludeGcInfo = false };
    }

    public static IEnumerable<TestContext> GetDummyTestContext()
    {
        yield return null!;
    }
}
