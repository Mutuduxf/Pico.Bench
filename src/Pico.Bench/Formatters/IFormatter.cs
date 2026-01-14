namespace Pico.Bench.Formatters;

/// <summary>
/// Interface for formatting benchmark results to various output formats.
/// </summary>
public interface IFormatter
{
    /// <summary>
    /// Format a single benchmark result.
    /// </summary>
    string Format(BenchmarkResult result);

    /// <summary>
    /// Format multiple benchmark results.
    /// </summary>
    string Format(IEnumerable<BenchmarkResult> results);

    /// <summary>
    /// Format a comparison result.
    /// </summary>
    string Format(ComparisonResult comparison);

    /// <summary>
    /// Format multiple comparison results.
    /// </summary>
    string Format(IEnumerable<ComparisonResult> comparisons);

    /// <summary>
    /// Format a complete benchmark suite.
    /// </summary>
    string Format(BenchmarkSuite suite);
}

/// <summary>
/// Options for customizing formatter output.
/// </summary>
public sealed class FormatterOptions
{
    /// <summary>Output directory for WriteToFile methods. If null, uses the file path as-is.</summary>
    public string? OutputDirectory { get; init; }

    /// <summary>Include environment information in output.</summary>
    public bool IncludeEnvironment { get; init; } = true;

    /// <summary>Include timestamp in output.</summary>
    public bool IncludeTimestamp { get; init; } = true;

    /// <summary>Include GC information in output.</summary>
    public bool IncludeGcInfo { get; init; } = true;

    /// <summary>Include CPU cycle information in output.</summary>
    public bool IncludeCpuCycles { get; init; } = true;

    /// <summary>Include percentile columns (P50, P90, P95, P99).</summary>
    public bool IncludePercentiles { get; init; } = true;

    /// <summary>Number of decimal places for time values.</summary>
    public int TimeDecimalPlaces { get; init; } = 1;

    /// <summary>Number of decimal places for speedup values.</summary>
    public int SpeedupDecimalPlaces { get; init; } = 2;

    /// <summary>Default options.</summary>
    public static FormatterOptions Default { get; } = new();

    /// <summary>Compact options with fewer columns.</summary>
    public static FormatterOptions Compact { get; } =
        new() { IncludePercentiles = false, IncludeCpuCycles = false };

    /// <summary>Minimal options for simple output.</summary>
    public static FormatterOptions Minimal { get; } =
        new()
        {
            IncludeEnvironment = false,
            IncludeTimestamp = false,
            IncludeGcInfo = false,
            IncludeCpuCycles = false,
            IncludePercentiles = false
        };

    /// <summary>
    /// Resolves the full file path, combining OutputDirectory if set.
    /// </summary>
    public string ResolvePath(string fileName)
    {
        if (string.IsNullOrEmpty(OutputDirectory))
            return fileName;
        return Path.Combine(OutputDirectory, fileName);
    }
}

/// <summary>
/// Base class for formatters with common helper methods.
/// </summary>
public abstract class FormatterBase : IFormatter
{
    protected FormatterOptions Options { get; }

    protected FormatterBase(FormatterOptions? options = null)
    {
        Options = options ?? FormatterOptions.Default;
    }

    public abstract string Format(BenchmarkResult result);
    public abstract string Format(IEnumerable<BenchmarkResult> results);
    public abstract string Format(ComparisonResult comparison);
    public abstract string Format(IEnumerable<ComparisonResult> comparisons);
    public abstract string Format(BenchmarkSuite suite);

    protected string FormatTime(double nanoseconds)
    {
        return nanoseconds.ToString($"F{Options.TimeDecimalPlaces}");
    }

    protected string FormatSpeedup(double speedup)
    {
        return speedup.ToString($"F{Options.SpeedupDecimalPlaces}") + "x";
    }

    protected string FormatGcInfo(GcInfo gc)
    {
        return $"{gc.Gen0}/{gc.Gen1}/{gc.Gen2}";
    }

    protected static string GetSpeedupIndicator(double speedup)
    {
        return speedup switch
        {
            >= 10 => "***",
            >= 5 => "**",
            >= 2 => "*",
            >= 1 => "",
            _ => "(!)"
        };
    }

    /// <summary>
    /// Write content to a file, creating the directory if it doesn't exist.
    /// </summary>
    protected static void WriteToFileInternal(string filePath, string content)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(filePath, content);
    }
}
