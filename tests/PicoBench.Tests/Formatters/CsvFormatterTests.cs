namespace PicoBench.Tests.Formatters;

public class CsvFormatterTests
{
    private static readonly string TestOutputDir = Path.Combine(
        Path.GetTempPath(),
        $"PicoBenchCsvTests_{Guid.NewGuid():N}"
    );

    [Before(Assembly)]
    public static async Task AssemblySetup()
    {
        Directory.CreateDirectory(TestOutputDir);
    }

    [After(Assembly)]
    public static async Task AssemblyCleanup()
    {
        if (Directory.Exists(TestOutputDir))
        {
            Directory.Delete(TestOutputDir, recursive: true);
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CSV")]
    public async Task Format_Results_ReturnsValidCsv()
    {
        var results = BenchmarkResultFactory.CreateMultiple(2).ToList();
        var formatter = new CsvFormatter();

        var csv = formatter.Format(results);

        await Assert.That(csv).IsNotNull();
        await Assert.That(csv).Contains("Name,Category,Avg_ns,P50_ns");
        await Assert.That(csv).Contains(results[0].Name);
        await Assert.That(csv).Contains(results[1].Name);

        // Count lines (header + 2 data rows)
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        await Assert.That(lines.Length).IsEqualTo(3);
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CSV")]
    public async Task Format_EmptyResults_ReturnsEmptyString()
    {
        var formatter = new CsvFormatter();

        var csv = formatter.Format(Enumerable.Empty<BenchmarkResult>());

        await Assert.That(csv).IsEqualTo(string.Empty);
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CSV")]
    [MethodDataSource(nameof(GetEscapeTestCases))]
    public async Task Escape_HandlesSpecialCharacters(string input, string expectedPattern)
    {
        // Note: Escape is a private method, we test it through public Format methods
        var result = BenchmarkResultFactory.Create(input);
        var formatter = new CsvFormatter();

        var csv = formatter.Format(result);

        await Assert.That(csv).IsNotNull();

        // The escaped value should appear in the CSV
        if (expectedPattern == "quoted")
        {
            // Should be wrapped in quotes; internal quotes are doubled per CSV standard
            var escaped = input.Replace("\"", "\"\"");
            await Assert.That(csv).Contains($"\"{escaped}\"");
        }
        else
        {
            // Should appear as-is
            await Assert.That(csv).Contains(input);
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CSV")]
    [Property("FileSystem", "true")]
    [NotInParallel]
    [MethodDataSource(nameof(GetDummyTestContext))]
    public async Task WriteToFile_CreatesNewFile(TestContext context)
    {
        var testDir = FileSystemHelper.CreateTestDirectory();
        try
        {
            var filePath = Path.Combine(testDir, "test.csv");
            var result = BenchmarkResultFactory.Create();

            CsvFormatter.WriteToFile(filePath, result);

            await Assert.That(File.Exists(filePath)).IsTrue();
            var content = await File.ReadAllTextAsync(filePath);
            await Assert.That(content).Contains("Name,Category,Avg_ns,P50_ns");
            await Assert.That(content).Contains(result.Name);

            context?.LogFileOperation("WriteToFile", filePath);
        }
        finally
        {
            FileSystemHelper.DeleteTestDirectory(testDir);
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CSV")]
    [Property("FileSystem", "true")]
    [NotInParallel]
    [MethodDataSource(nameof(GetDummyTestContext))]
    public async Task AppendToFile_SkipsHeaderWhenFileExists(TestContext context)
    {
        var testDir = FileSystemHelper.CreateTestDirectory();
        try
        {
            var filePath = Path.Combine(testDir, "test.csv");
            var result1 = BenchmarkResultFactory.Create("Test1");
            var result2 = BenchmarkResultFactory.Create("Test2");

            // First write creates file with header
            CsvFormatter.WriteToFile(filePath, result1);

            // Append should skip header
            CsvFormatter.AppendToFile(filePath, result2);

            await Assert.That(File.Exists(filePath)).IsTrue();
            var content = await File.ReadAllTextAsync(filePath);
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // Should have header + 2 data rows (not header + data + header + data)
            await Assert.That(lines.Length).IsEqualTo(3);
            await Assert.That(content).Contains("Test1");
            await Assert.That(content).Contains("Test2");
            // Should only have one header line
            await Assert
                .That(content.IndexOf("Name,Category,Avg_ns,P50_ns"))
                .IsEqualTo(content.LastIndexOf("Name,Category,Avg_ns,P50_ns"));

            context?.LogFileOperation("AppendToFile", filePath);
        }
        finally
        {
            FileSystemHelper.DeleteTestDirectory(testDir);
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CSV")]
    [Property("FileSystem", "true")]
    [NotInParallel]
    [MethodDataSource(nameof(GetDummyTestContext))]
    public async Task AppendToFile_CreatesNewFileWhenNotExists(TestContext context)
    {
        var testDir = FileSystemHelper.CreateTestDirectory();
        try
        {
            var filePath = Path.Combine(testDir, "test.csv");
            var result = BenchmarkResultFactory.Create();

            CsvFormatter.AppendToFile(filePath, result);

            await Assert.That(File.Exists(filePath)).IsTrue();
            var content = await File.ReadAllTextAsync(filePath);
            // Should include header since file didn't exist
            await Assert.That(content).Contains("Name,Category,Avg_ns,P50_ns");

            context?.LogFileOperation("AppendToFile (new)", filePath);
        }
        finally
        {
            FileSystemHelper.DeleteTestDirectory(testDir);
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CSV")]
    public async Task Format_Comparisons_ReturnsTwoRowsPerComparison()
    {
        var comparisons = ComparisonResultFactory.CreateMultiple(2).ToList();
        var formatter = new CsvFormatter();

        var csv = formatter.Format(comparisons);

        await Assert.That(csv).IsNotNull();
        await Assert.That(csv).Contains("TestCase,Provider,Avg_ns,Speedup");

        // Each comparison generates 2 rows (candidate + baseline)
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // Header + (2 comparisons * 2 rows) = 5 lines
        await Assert.That(lines.Length).IsEqualTo(5);
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CSV")]
    public async Task Format_Suite_IncludesCommentsAndSections()
    {
        var suite = BenchmarkSuiteFactory.Create();
        var formatter = new CsvFormatter();

        var csv = formatter.Format(suite);

        await Assert.That(csv).IsNotNull();
        await Assert.That(csv).Contains("# Suite:");
        await Assert.That(csv).Contains("# Results");
        await Assert.That(csv).Contains("# Comparisons");
        await Assert.That(csv).Contains(suite.Name);
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CSV")]
    [MethodDataSource(nameof(GetOptionCombinations))]
    public async Task OptionalColumns_RespectOptions(FormatterOptions options)
    {
        var results = BenchmarkResultFactory.CreateMultiple(1).ToList();
        var formatter = new CsvFormatter(options);

        var csv = formatter.Format(results);

        if (options.IncludePercentiles)
        {
            await Assert.That(csv).Contains("P90_ns");
            await Assert.That(csv).Contains("P95_ns");
            await Assert.That(csv).Contains("P99_ns");
            await Assert.That(csv).Contains("Min_ns");
            await Assert.That(csv).Contains("Max_ns");
            await Assert.That(csv).Contains("StdDev_ns");
        }
        else
        {
            await Assert.That(csv).DoesNotContain("P90_ns");
            await Assert.That(csv).DoesNotContain("P95_ns");
            await Assert.That(csv).DoesNotContain("P99_ns");
        }

        if (options.IncludeCpuCycles)
        {
            await Assert.That(csv).Contains("CpuCycles");
        }
        else
        {
            await Assert.That(csv).DoesNotContain("CpuCycles");
        }

        if (options.IncludeGcInfo)
        {
            await Assert.That(csv).Contains("GC0");
            await Assert.That(csv).Contains("GC1");
            await Assert.That(csv).Contains("GC2");
        }
        else
        {
            await Assert.That(csv).DoesNotContain("GC0");
            await Assert.That(csv).DoesNotContain("GC1");
            await Assert.That(csv).DoesNotContain("GC2");
        }

        if (options.IncludeTimestamp)
        {
            await Assert.That(csv).Contains("Timestamp");
        }
        else
        {
            await Assert.That(csv).DoesNotContain("Timestamp");
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CSV")]
    [Property("FileSystem", "true")]
    [NotInParallel]
    [MethodDataSource(nameof(GetDummyTestContext))]
    public async Task WriteToFile_MultipleResults_CreatesValidCsv(TestContext context)
    {
        var testDir = FileSystemHelper.CreateTestDirectory();
        try
        {
            var filePath = Path.Combine(testDir, "results.csv");
            var results = BenchmarkResultFactory.CreateMultiple(3).ToList();

            CsvFormatter.WriteToFile(filePath, results);

            await Assert.That(File.Exists(filePath)).IsTrue();
            var content = await File.ReadAllTextAsync(filePath);
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            await Assert.That(lines.Length).IsEqualTo(4); // Header + 3 rows

            context?.LogFileOperation("WriteToFile multiple", filePath);
        }
        finally
        {
            FileSystemHelper.DeleteTestDirectory(testDir);
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CSV")]
    [Property("FileSystem", "true")]
    [NotInParallel]
    [MethodDataSource(nameof(GetDummyTestContext))]
    public async Task WriteToFile_Comparisons_CreatesValidCsv(TestContext context)
    {
        var testDir = FileSystemHelper.CreateTestDirectory();
        try
        {
            var filePath = Path.Combine(testDir, "comparisons.csv");
            var comparisons = ComparisonResultFactory.CreateMultiple(2).ToList();

            CsvFormatter.WriteToFile(filePath, comparisons);

            await Assert.That(File.Exists(filePath)).IsTrue();
            var content = await File.ReadAllTextAsync(filePath);
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            await Assert.That(lines.Length).IsEqualTo(5); // Header + (2 * 2) rows

            context?.LogFileOperation("WriteToFile comparisons", filePath);
        }
        finally
        {
            FileSystemHelper.DeleteTestDirectory(testDir);
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CSV")]
    [Property("FileSystem", "true")]
    [NotInParallel]
    [MethodDataSource(nameof(GetDummyTestContext))]
    public async Task WriteToFile_Suite_CreatesValidCsv(TestContext context)
    {
        var testDir = FileSystemHelper.CreateTestDirectory();
        try
        {
            var filePath = Path.Combine(testDir, "suite.csv");
            var suite = BenchmarkSuiteFactory.Create();

            CsvFormatter.WriteToFile(filePath, suite);

            await Assert.That(File.Exists(filePath)).IsTrue();
            var content = await File.ReadAllTextAsync(filePath);

            await Assert.That(content).Contains("# Suite:");
            await Assert.That(content).Contains("# Results");
            await Assert.That(content).Contains("# Comparisons");

            context?.LogFileOperation("WriteToFile suite", filePath);
        }
        finally
        {
            FileSystemHelper.DeleteTestDirectory(testDir);
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CSV")]
    public async Task Format_WithUnicodeCharacters_HandlesCorrectly()
    {
        var result = BenchmarkResultFactory.WithUnicodeName();
        var formatter = new CsvFormatter();

        var csv = formatter.Format(result);

        await Assert.That(csv).IsNotNull();
        await Assert.That(csv).Contains(result.Name);
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CSV")]
    public async Task Format_WithNullInput_ThrowsArgumentNullException()
    {
        var formatter = new CsvFormatter();

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

    public static IEnumerable<(string input, string expectedPattern)> GetEscapeTestCases()
    {
        yield return ("NormalText", "normal");
        yield return ("Text,WithComma", "quoted");
        yield return ("Text\"WithQuote", "quoted");
        yield return ("Text\nWithNewline", "quoted");
        yield return ("Text\rWithReturn", "quoted");
        yield return ("Text,With\"CommaAndQuote", "quoted");
        yield return ("", "normal");
        yield return ("   ", "normal");
    }

    public static IEnumerable<FormatterOptions> GetOptionCombinations()
    {
        yield return FormatterOptions.Default;
        yield return FormatterOptions.Compact;
        yield return FormatterOptions.Minimal;

        // Custom combinations
        yield return new FormatterOptions { IncludePercentiles = false };
        yield return new FormatterOptions { IncludeCpuCycles = false };
        yield return new FormatterOptions { IncludeGcInfo = false };
        yield return new FormatterOptions { IncludeTimestamp = false };
        yield return new FormatterOptions { IncludeEnvironment = false };
    }

    public static IEnumerable<TestContext> GetDummyTestContext()
    {
        yield return null!;
    }
}
