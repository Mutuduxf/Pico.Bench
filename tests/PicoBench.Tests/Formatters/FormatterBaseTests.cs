namespace PicoBench.Tests.Formatters;

public class FormatterBaseTests
{
    // Test implementation of FormatterBase to expose protected methods
    private class TestFormatter : FormatterBase
    {
        public TestFormatter(FormatterOptions? options = null)
            : base(options) { }

        // Expose protected methods for testing
        public string PublicFormatTime(double nanoseconds) => FormatTime(nanoseconds);

        public string PublicFormatSpeedup(double speedup) => FormatSpeedup(speedup);

        public string PublicFormatGcInfo(GcInfo gc) => FormatGcInfo(gc);

        public string PublicGetSpeedupIndicator(double speedup) => GetSpeedupIndicator(speedup);

        public static void PublicWriteToFileInternal(string filePath, string content) =>
            WriteToFileInternal(filePath, content);

        // Implement abstract methods with minimal implementations
        protected override string FormatCore(BenchmarkResult result) => "Test";

        protected override string FormatCore(IEnumerable<BenchmarkResult> results) => "Test";

        protected override string FormatCore(ComparisonResult comparison) => "Test";

        protected override string FormatCore(IEnumerable<ComparisonResult> comparisons) => "Test";

        protected override string FormatCore(BenchmarkSuite suite) => "Test";
    }

    [Test]
    [Property("Category", "Formatter")]
    [Arguments(0.0, 1, "0.0")]
    [Arguments(123.456, 1, "123.5")]
    [Arguments(123.456, 0, "123")]
    [Arguments(123.456, 3, "123.456")]
    [Arguments(0.001, 2, "0.00")]
    [Arguments(999999.999, 1, "1000000.0")]
    public async Task FormatTime_WithDifferentDecimalPlaces_ReturnsFormattedString(
        double nanoseconds,
        int decimalPlaces,
        string expected
    )
    {
        var options = new FormatterOptions { TimeDecimalPlaces = decimalPlaces };
        var formatter = new TestFormatter(options);

        var result = formatter.PublicFormatTime(nanoseconds);

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Property("Category", "Formatter")]
    [Arguments(1.0, 2, "1.00x")]
    [Arguments(2.5, 2, "2.50x")]
    [Arguments(2.5, 0, "2x")]
    [Arguments(2.5, 4, "2.5000x")]
    [Arguments(0.5, 2, "0.50x")]
    [Arguments(100.123, 1, "100.1x")]
    public async Task FormatSpeedup_WithDifferentDecimalPlaces_ReturnsFormattedString(
        double speedup,
        int decimalPlaces,
        string expected
    )
    {
        var options = new FormatterOptions { SpeedupDecimalPlaces = decimalPlaces };
        var formatter = new TestFormatter(options);

        var result = formatter.PublicFormatSpeedup(speedup);

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Property("Category", "Formatter")]
    [Arguments(0, 0, 0, "0/0/0")]
    [Arguments(10, 2, 0, "10/2/0")]
    [Arguments(100, 50, 10, "100/50/10")]
    [Arguments(1, 0, 0, "1/0/0")]
    [Arguments(0, 1, 0, "0/1/0")]
    [Arguments(0, 0, 1, "0/0/1")]
    public async Task FormatGcInfo_ReturnsCorrectFormat(
        int gen0,
        int gen1,
        int gen2,
        string expected
    )
    {
        var gcInfo = GcInfoFactory.Create(gen0, gen1, gen2);
        var formatter = new TestFormatter();

        var result = formatter.PublicFormatGcInfo(gcInfo);

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Property("Category", "Formatter")]
    [MethodDataSource(nameof(GetSpeedupIndicatorTestCases))]
    public async Task GetSpeedupIndicator_AllThresholds_ReturnsCorrectIndicator(
        double speedup,
        string expectedIndicator
    )
    {
        var formatter = new TestFormatter();

        var result = formatter.PublicGetSpeedupIndicator(speedup);

        await Assert.That(result).IsEqualTo(expectedIndicator);
    }

    public static IEnumerable<(
        double speedup,
        string expectedIndicator
    )> GetSpeedupIndicatorTestCases()
    {
        yield return (15.0, "***"); // >=10
        yield return (10.0, "***"); // Boundary
        yield return (9.9, "**"); // <10
        yield return (7.0, "**"); // >=5
        yield return (5.0, "**"); // Boundary
        yield return (4.9, "*"); // <5
        yield return (3.0, "*"); // >=2
        yield return (2.0, "*"); // Boundary
        yield return (1.9, ""); // <2
        yield return (1.0, ""); // >=1 Boundary
        yield return (0.9, "(!)"); // <1
        yield return (0.0, "(!)"); // Minimum
        yield return (0.001, "(!)"); // Very small positive
        yield return (999.9, "***"); // Very large
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("FileSystem", "true")]
    [NotInParallel] // File system tests should run sequentially
    public async Task WriteToFileInternal_CreatesMissingDirectory()
    {
        var testDir = FileSystemHelper.CreateTestDirectory();
        try
        {
            var filePath = Path.Combine(testDir, "subdir", "test.txt");
            var content = "Test content";

            TestFormatter.PublicWriteToFileInternal(filePath, content);

            await Assert.That(File.Exists(filePath)).IsTrue();
            var fileContent = await File.ReadAllTextAsync(filePath);
            await Assert.That(fileContent).IsEqualTo(content);

            // File created successfully
        }
        finally
        {
            FileSystemHelper.DeleteTestDirectory(testDir);
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("FileSystem", "true")]
    [NotInParallel]
    public async Task WriteToFileInternal_OverwritesExistingFile()
    {
        var testDir = FileSystemHelper.CreateTestDirectory();
        try
        {
            var filePath = Path.Combine(testDir, "test.txt");

            // Create initial file
            await File.WriteAllTextAsync(filePath, "Initial content");
            await Assert.That(File.Exists(filePath)).IsTrue();

            // Overwrite with new content
            var newContent = "New content";
            TestFormatter.PublicWriteToFileInternal(filePath, newContent);

            var fileContent = await File.ReadAllTextAsync(filePath);
            await Assert.That(fileContent).IsEqualTo(newContent);

            // File overwritten successfully
        }
        finally
        {
            FileSystemHelper.DeleteTestDirectory(testDir);
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("FileSystem", "true")]
    [NotInParallel]
    public async Task WriteToFileInternal_HandlesLongContent()
    {
        var testDir = FileSystemHelper.CreateTestDirectory();
        try
        {
            var filePath = Path.Combine(testDir, "large.txt");
            var content = new string('X', 10000); // 10KB of content

            TestFormatter.PublicWriteToFileInternal(filePath, content);

            await Assert.That(File.Exists(filePath)).IsTrue();
            var fileContent = await File.ReadAllTextAsync(filePath);
            await Assert.That(fileContent.Length).IsEqualTo(content.Length);
            await Assert.That(fileContent).IsEqualTo(content);

            // Large file created successfully
        }
        finally
        {
            FileSystemHelper.DeleteTestDirectory(testDir);
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    public async Task FormatterBase_WithNullOptions_UsesDefault()
    {
        var formatter = new TestFormatter(null);

        // Verify that default options are used by testing format time with default decimal places
        var result = formatter.PublicFormatTime(123.456);
        await Assert.That(result).IsEqualTo("123.5"); // Default is 1 decimal place
    }

    [Test]
    [Property("Category", "Formatter")]
    public async Task FormatterBase_WithCustomOptions_UsesProvidedOptions()
    {
        var options = new FormatterOptions { TimeDecimalPlaces = 3 };
        var formatter = new TestFormatter(options);

        var result = formatter.PublicFormatTime(123.456);
        await Assert.That(result).IsEqualTo("123.456"); // 3 decimal places
    }
}
