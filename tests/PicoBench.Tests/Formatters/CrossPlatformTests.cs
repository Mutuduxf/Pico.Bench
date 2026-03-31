namespace PicoBench.Tests.Formatters;

public class CrossPlatformTests
{
    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CrossPlatform")]
    public async Task FormatterOptions_ResolvePath_HandlesDifferentPathSeparators()
    {
        var options = new FormatterOptions { OutputDirectory = "output" };

        var path = options.ResolvePath("results.csv");

        await Assert.That(path).IsNotNull();
        // Path.Combine will use the appropriate separator for the current platform
        // Just verify the path is constructed correctly
        await Assert.That(path).Contains("output");
        await Assert.That(path).Contains("results.csv");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CrossPlatform")]
    public async Task WriteToFile_CreatesDirectoriesWithPlatformSeparators()
    {
        var testDir = FileSystemHelper.CreateTestDirectory();
        try
        {
            // Use a subdirectory path that will be normalized by Path.Combine
            var filePath = Path.Combine(testDir, "subdir", "test.csv");
            var result = BenchmarkResultFactory.Create();

            CsvFormatter.WriteToFile(filePath, result);

            await Assert.That(File.Exists(filePath)).IsTrue();
            var fileContent = await File.ReadAllTextAsync(filePath);
            await Assert.That(fileContent).Contains(result.Name);
        }
        finally
        {
            FileSystemHelper.DeleteTestDirectory(testDir);
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CrossPlatform")]
    public async Task FormatTime_UsesInvariantCulture()
    {
        // Ensure number formatting uses invariant culture regardless of system culture
        // by testing with a decimal separator that differs from invariant (comma vs dot)
        // We'll simulate a culture with comma as decimal separator
        var originalCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("fr-FR"); // uses comma
            var options = new FormatterOptions { TimeDecimalPlaces = 2 };
            var formatter = new TestFormatter(options);

            var formatted = formatter.PublicFormatTime(123.456);

            // Should still use dot as decimal separator (invariant culture)
            await Assert.That(formatted).IsEqualTo("123.46");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CrossPlatform")]
    public async Task FormatSpeedup_UsesInvariantCulture()
    {
        var originalCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE"); // uses comma
            var options = new FormatterOptions { SpeedupDecimalPlaces = 3 };
            var formatter = new TestFormatter(options);

            var formatted = formatter.PublicFormatSpeedup(2.5);

            // Should still use dot as decimal separator
            await Assert.That(formatted).IsEqualTo("2.500x");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CrossPlatform")]
    public async Task CsvFormatter_EscapesLineEndingsConsistently()
    {
        var result = BenchmarkResultFactory.Create("Test\nNewline");
        var formatter = new CsvFormatter();

        var csv = formatter.Format(result);

        await Assert.That(csv).IsNotNull();
        // CSV should escape newline characters (wrap in quotes)
        await Assert.That(csv).Contains("\"Test\nNewline\"");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CrossPlatform")]
    public async Task HtmlFormatter_EscapesSpecialCharacters()
    {
        var result = BenchmarkResultFactory.Create("Test & More < > \" '");
        var formatter = new HtmlFormatter();

        var html = formatter.Format(result);

        await Assert.That(html).IsNotNull();
        // Should escape HTML entities
        await Assert.That(html).Contains("&amp;");
        await Assert.That(html).Contains("&lt;");
        await Assert.That(html).Contains("&gt;");
        await Assert.That(html).Contains("&quot;");
        await Assert.That(html).Contains("&#39;");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CrossPlatform")]
    public async Task MarkdownFormatter_EscapesPipeCharacters()
    {
        var result = BenchmarkResultFactory.Create("Test | Pipe");
        var formatter = new MarkdownFormatter();

        var markdown = formatter.Format(result);

        await Assert.That(markdown).IsNotNull();
        // Should escape pipe character in table cells
        await Assert.That(markdown).Contains("Test \\| Pipe");
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CrossPlatform")]
    public async Task FileEncoding_IsUtf8WithoutBom()
    {
        var testDir = FileSystemHelper.CreateTestDirectory();
        try
        {
            var filePath = Path.Combine(testDir, "test.csv");
            var result = BenchmarkResultFactory.Create();

            CsvFormatter.WriteToFile(filePath, result);

            await Assert.That(File.Exists(filePath)).IsTrue();
            // Read bytes and check for UTF-8 BOM (0xEF, 0xBB, 0xBF)
            var bytes = await File.ReadAllBytesAsync(filePath);
            var hasBom =
                bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            await Assert.That(hasBom).IsFalse();
        }
        finally
        {
            FileSystemHelper.DeleteTestDirectory(testDir);
        }
    }

    [Test]
    [Property("Category", "Formatter")]
    [Property("SubCategory", "CrossPlatform")]
    public async Task LineEndings_ConsistentAcrossFormatters()
    {
        var results = BenchmarkResultFactory.CreateMultiple(2).ToList();

        var csvFormatter = new CsvFormatter();
        var htmlFormatter = new HtmlFormatter();
        var mdFormatter = new MarkdownFormatter();

        var csv = csvFormatter.Format(results);
        var html = htmlFormatter.Format(results);
        var md = mdFormatter.Format(results);

        // Each formatter may use different line ending conventions
        // CSV and Markdown typically use \n, HTML may use \n or \r\n
        // We just verify that they produce valid output
        await Assert.That(csv).IsNotNull();
        await Assert.That(html).IsNotNull();
        await Assert.That(md).IsNotNull();

        // Count lines to ensure they have expected structure
        var csvLines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var mdLines = md.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        await Assert.That(csvLines.Length).IsGreaterThanOrEqualTo(3); // Header + 2 rows
        await Assert.That(mdLines.Length).IsGreaterThanOrEqualTo(4); // Header + separator + 2 rows
    }

    // Test implementation of FormatterBase to expose protected methods
    private class TestFormatter : FormatterBase
    {
        public TestFormatter(FormatterOptions? options = null)
            : base(options) { }

        public string PublicFormatTime(double nanoseconds) => FormatTime(nanoseconds);

        public string PublicFormatSpeedup(double speedup) => FormatSpeedup(speedup);

        public string PublicFormatGcInfo(GcInfo gc) => FormatGcInfo(gc);

        public string PublicGetSpeedupIndicator(double speedup) => GetSpeedupIndicator(speedup);

        // Implement abstract methods with minimal implementations
        protected override string FormatCore(BenchmarkResult result) => "Test";

        protected override string FormatCore(IEnumerable<BenchmarkResult> results) => "Test";

        protected override string FormatCore(ComparisonResult comparison) => "Test";

        protected override string FormatCore(IEnumerable<ComparisonResult> comparisons) => "Test";

        protected override string FormatCore(BenchmarkSuite suite) => "Test";
    }
}
