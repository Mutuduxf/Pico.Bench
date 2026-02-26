using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using Pico.Bench;
using Pico.Bench.Formatters;
using Pico.Bench.Tests.TestData;
using Pico.Bench.Tests.Utilities;
using System.Linq;

namespace Pico.Bench.Tests.Integration;

public class FormatterIntegrationTests
{
    private static readonly string TestOutputDir = Path.Combine(
        Path.GetTempPath(), 
        $"PicoBenchIntegrationTests_{Guid.NewGuid():N}");
    
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
    
    [Before(Test)]
    public async Task TestSetup(TestContext context)
    {
        context.Log("Starting integration test");
    }
    
    [Test]
    [Property("Category", "Integration")]
    [Property("SubCategory", "Formatter")]
    public async Task BenchmarkRun_ConsoleFormatter_OutputsValidString()
    {
        var result = Benchmark.Run("IntegrationTest", () => Thread.Sleep(1));
        
        var formatter = new ConsoleFormatter();
        var formatted = formatter.Format(result);
        
        await Assert.That(formatted).IsNotNull();
        await Assert.That(formatted).Contains(result.Name);
        await Assert.That(formatted).Contains("Avg:");
    }
    
    [Test]
    [Property("Category", "Integration")]
    [Property("SubCategory", "Formatter")]
    [Property("FileSystem", "true")]
    [NotInParallel]
    public async Task CsvFormatter_WriteToFile_CreatesValidFile()
    {
        var result = BenchmarkResultFactory.Create();
        var filePath = Path.Combine(TestOutputDir, $"test_{Guid.NewGuid():N}.csv");
        
        CsvFormatter.WriteToFile(filePath, result);
        
        await Assert.That(File.Exists(filePath)).IsTrue();
        var content = await File.ReadAllTextAsync(filePath);
        await Assert.That(content).Contains(result.Name);
        await Assert.That(content).Contains("Avg");
    }
    
    [Test]
    [Property("Category", "Integration")]
    [Property("SubCategory", "Formatter")]
    [Property("FileSystem", "true")]
    [NotInParallel]
    public async Task HtmlFormatter_WriteToFile_CreatesValidFile()
    {
        var result = BenchmarkResultFactory.Create();
        var filePath = Path.Combine(TestOutputDir, $"test_{Guid.NewGuid():N}.html");
        
        HtmlFormatter.WriteToFile(filePath, result);
        
        await Assert.That(File.Exists(filePath)).IsTrue();
        var content = await File.ReadAllTextAsync(filePath);
        await Assert.That(content).Contains("<html");
        await Assert.That(content).Contains(result.Name);
    }
    
    [Test]
    [Property("Category", "Integration")]
    [Property("SubCategory", "Formatter")]
    public async Task BenchmarkCompare_MarkdownFormatter_OutputsComparisonTable()
    {
        var baseline = BenchmarkResultFactory.Create("Baseline");
        var candidate = BenchmarkResultFactory.Create("Candidate");
        var comparison = new ComparisonResult(
            name: "Test Comparison",
            baseline: baseline,
            candidate: candidate);
        
        var formatter = new MarkdownFormatter();
        var formatted = formatter.Format(comparison);
        
        await Assert.That(formatted).IsNotNull();
        await Assert.That(formatted).Contains("Comparison");
        await Assert.That(formatted).Contains("Speedup");
    }
    
    [Test]
    [Property("Category", "Integration")]
    [Property("SubCategory", "Formatter")]
    [Property("FileSystem", "true")]
    [NotInParallel]
    public async Task AllFormatters_ProduceConsistentResults()
    {
        var results = BenchmarkResultFactory.CreateMultiple(3).ToList();
        
        var consoleFormatter = new ConsoleFormatter();
        var csvFormatter = new CsvFormatter();
        var htmlFormatter = new HtmlFormatter();
        var markdownFormatter = new MarkdownFormatter();
        
        var consoleOutput = consoleFormatter.Format(results);
        var csvOutput = csvFormatter.Format(results);
        var htmlOutput = htmlFormatter.Format(results);
        var markdownOutput = markdownFormatter.Format(results);
        
        await Assert.That(consoleOutput).IsNotNull();
        await Assert.That(csvOutput).IsNotNull();
        await Assert.That(htmlOutput).IsNotNull();
        await Assert.That(markdownOutput).IsNotNull();
        
        // Ensure each output contains all benchmark names
        foreach (var result in results)
        {
            await Assert.That(consoleOutput).Contains(result.Name);
            await Assert.That(csvOutput).Contains(result.Name);
            await Assert.That(htmlOutput).Contains(result.Name);
            await Assert.That(markdownOutput).Contains(result.Name);
        }
    }
    
    [Test]
    [Property("Category", "Integration")]
    [Property("SubCategory", "Formatter")]
    public async Task SummaryFormatter_WithBenchmarkSuite_ProducesSummary()
    {
        var results = BenchmarkResultFactory.CreateMultiple(2).ToList();
        var comparisons = ComparisonResultFactory.CreateMultiple(2).ToList();
        
        var suite = new BenchmarkSuite
        {
            Name = "Integration Test Suite",
            Description = "Test suite for integration tests",
            Results = results,
            Comparisons = comparisons
        };
        
        var summary = SummaryFormatter.Format(suite.Comparisons!, suite.Duration);
        
        await Assert.That(summary).IsNotNull();
        await Assert.That(summary).Contains(suite.Name);
        await Assert.That(summary).Contains("Summary");
    }
}