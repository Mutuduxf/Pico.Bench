using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using Pico.Bench.Formatters;

namespace Pico.Bench.Tests.Formatters;

public class FormatterOptionsTests
{
    [Test]
    [Property("Category", "Formatter")]
    public async Task DefaultOptions_ShouldHaveCorrectValues()
    {
        var options = FormatterOptions.Default;
        
        await Assert.That(options.IncludeEnvironment).IsTrue();
        await Assert.That(options.IncludeTimestamp).IsTrue();
        await Assert.That(options.IncludeGcInfo).IsTrue();
        await Assert.That(options.IncludeCpuCycles).IsTrue();
        await Assert.That(options.IncludePercentiles).IsTrue();
        await Assert.That(options.TimeDecimalPlaces).IsEqualTo(1);
        await Assert.That(options.SpeedupDecimalPlaces).IsEqualTo(2);
        await Assert.That(options.BaselineLabel).IsEqualTo("Baseline");
        await Assert.That(options.CandidateLabel).IsEqualTo("Candidate");
        await Assert.That(options.OutputDirectory).IsNull();
    }
    
    [Test]
    [Property("Category", "Formatter")]
    public async Task CompactOptions_ShouldExcludeSomeColumns()
    {
        var options = FormatterOptions.Compact;
        
        await Assert.That(options.IncludePercentiles).IsFalse();
        await Assert.That(options.IncludeCpuCycles).IsFalse();
        // Other options should remain at defaults
        await Assert.That(options.IncludeEnvironment).IsTrue();
        await Assert.That(options.IncludeTimestamp).IsTrue();
        await Assert.That(options.IncludeGcInfo).IsTrue();
    }
    
    [Test]
    [Property("Category", "Formatter")]
    public async Task MinimalOptions_ShouldExcludeMostColumns()
    {
        var options = FormatterOptions.Minimal;
        
        await Assert.That(options.IncludeEnvironment).IsFalse();
        await Assert.That(options.IncludeTimestamp).IsFalse();
        await Assert.That(options.IncludeGcInfo).IsFalse();
        await Assert.That(options.IncludeCpuCycles).IsFalse();
        await Assert.That(options.IncludePercentiles).IsFalse();
        // Labels and decimal places should remain at defaults
        await Assert.That(options.BaselineLabel).IsEqualTo("Baseline");
        await Assert.That(options.CandidateLabel).IsEqualTo("Candidate");
    }
    
    [Test]
    [Property("Category", "Formatter")]
    [Arguments(null, "file.csv", "file.csv")]
    [Arguments("", "file.csv", "file.csv")]
    [Arguments("output", "file.csv", "output/file.csv")]
    [Arguments("output/subdir", "results.csv", "output/subdir/results.csv")]
    [Arguments("/absolute/path", "file.csv", "/absolute/path/file.csv")]
    public async Task ResolvePath_WithDifferentDirectories_ReturnsCorrectPath(
        string? outputDirectory, 
        string fileName, 
        string expectedPath)
    {
        var options = new FormatterOptions { OutputDirectory = outputDirectory };
        var resolvedPath = options.ResolvePath(fileName);
        
        await Assert.That(resolvedPath).IsEqualTo(expectedPath);
    }
    
    [Test]
    [Property("Category", "Formatter")]
    public async Task CustomOptions_ShouldOverrideDefaults()
    {
        var options = new FormatterOptions
        {
            OutputDirectory = "custom/output",
            IncludeEnvironment = false,
            IncludeTimestamp = false,
            IncludeGcInfo = false,
            IncludeCpuCycles = false,
            IncludePercentiles = false,
            TimeDecimalPlaces = 3,
            SpeedupDecimalPlaces = 4,
            BaselineLabel = "Old",
            CandidateLabel = "New"
        };
        
        await Assert.That(options.OutputDirectory).IsEqualTo("custom/output");
        await Assert.That(options.IncludeEnvironment).IsFalse();
        await Assert.That(options.IncludeTimestamp).IsFalse();
        await Assert.That(options.IncludeGcInfo).IsFalse();
        await Assert.That(options.IncludeCpuCycles).IsFalse();
        await Assert.That(options.IncludePercentiles).IsFalse();
        await Assert.That(options.TimeDecimalPlaces).IsEqualTo(3);
        await Assert.That(options.SpeedupDecimalPlaces).IsEqualTo(4);
        await Assert.That(options.BaselineLabel).IsEqualTo("Old");
        await Assert.That(options.CandidateLabel).IsEqualTo("New");
    }
    
    [Test]
    [Property("Category", "Formatter")]
    public async Task ResolvePath_WithNullFileName_ThrowsArgumentNullException()
    {
        var options = FormatterOptions.Default;
        
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            Task.Run(() => options.ResolvePath(null!)));
    }
    
    [Test]
    [Property("Category", "Formatter")]
    public async Task ResolvePath_WithEmptyFileName_ReturnsOutputDirectoryOnly()
    {
        var options = new FormatterOptions { OutputDirectory = "output" };
        var resolvedPath = options.ResolvePath("");
        
        await Assert.That(resolvedPath).IsEqualTo("output");
    }
    
    [Test]
    [Property("Category", "Formatter")]
    [MethodDataSource(nameof(GetAllOptionCombinations))]
    public async Task Options_AllCombinations_AreConsistent(FormatterOptions options)
    {
        // Test that options don't interfere with each other
        await Assert.That(options).IsNotNull();
        
        // Verify that labels are never null
        await Assert.That(options.BaselineLabel).IsNotNull();
        await Assert.That(options.CandidateLabel).IsNotNull();
        
        // Verify decimal places are non-negative
        await Assert.That(options.TimeDecimalPlaces).IsGreaterThanOrEqualTo(0);
        await Assert.That(options.SpeedupDecimalPlaces).IsGreaterThanOrEqualTo(0);
    }
    
    public static IEnumerable<FormatterOptions> GetAllOptionCombinations()
    {
        var boolOptions = new[] { true, false };
        
        foreach (var env in boolOptions)
        foreach (var ts in boolOptions)
        foreach (var gc in boolOptions)
        foreach (var cpu in boolOptions)
        foreach (var perc in boolOptions)
        {
            yield return new FormatterOptions
            {
                IncludeEnvironment = env,
                IncludeTimestamp = ts,
                IncludeGcInfo = gc,
                IncludeCpuCycles = cpu,
                IncludePercentiles = perc,
                TimeDecimalPlaces = env ? 1 : 2,  // Vary decimal places
                SpeedupDecimalPlaces = ts ? 2 : 3,
                BaselineLabel = gc ? "Baseline" : "Control",
                CandidateLabel = cpu ? "Candidate" : "Test"
            };
        }
    }
}