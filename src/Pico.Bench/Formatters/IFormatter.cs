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

    /// <summary>Label for the baseline result in comparisons.</summary>
    public string BaselineLabel { get; init; } = "Baseline";

    /// <summary>Label for the candidate result in comparisons.</summary>
    public string CandidateLabel { get; init; } = "Candidate";

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
        if (fileName == null)
            throw new ArgumentNullException(nameof(fileName));
        return string.IsNullOrEmpty(OutputDirectory)
            ? fileName
            : Path.Combine(OutputDirectory!, fileName);
    }
}

/// <summary>
/// Base class for formatters with common helper methods.
/// Uses the Template Method pattern: public <see cref="Format"/> methods validate
/// inputs and delegate to <c>FormatCore</c> overrides in derived classes.
/// </summary>
public abstract class FormatterBase(FormatterOptions? options = null) : IFormatter
{
    protected FormatterOptions Options { get; } = options ?? FormatterOptions.Default;

    #region IFormatter — public entry points with null validation

    public string Format(BenchmarkResult result)
    {
        return result == null
            ? throw new ArgumentNullException(nameof(result))
            : FormatCore(result);
    }

    public string Format(IEnumerable<BenchmarkResult> results)
    {
        return results == null
            ? throw new ArgumentNullException(nameof(results))
            : FormatCore(results);
    }

    public string Format(ComparisonResult comparison)
    {
        return comparison == null
            ? throw new ArgumentNullException(nameof(comparison))
            : FormatCore(comparison);
    }

    public string Format(IEnumerable<ComparisonResult> comparisons)
    {
        return comparisons == null
            ? throw new ArgumentNullException(nameof(comparisons))
            : FormatCore(comparisons);
    }

    public string Format(BenchmarkSuite suite)
    {
        return suite == null ? throw new ArgumentNullException(nameof(suite)) : FormatCore(suite);
    }

    #endregion

    #region Protected abstract/virtual — override in derived classes

    /// <summary>
    /// Format a single result. Default delegates to the collection overload.
    /// Override for custom single-item formatting (e.g., <see cref="ConsoleFormatter"/>).
    /// </summary>
    protected virtual string FormatCore(BenchmarkResult result) => FormatCore(new[] { result });

    protected abstract string FormatCore(IEnumerable<BenchmarkResult> results);

    /// <summary>
    /// Format a single comparison. Default delegates to the collection overload.
    /// Override for custom single-item formatting.
    /// </summary>
    protected virtual string FormatCore(ComparisonResult comparison) =>
        FormatCore(new[] { comparison });

    protected abstract string FormatCore(IEnumerable<ComparisonResult> comparisons);
    protected abstract string FormatCore(BenchmarkSuite suite);

    #endregion

    protected string FormatTime(double nanoseconds)
    {
        return nanoseconds.ToString($"F{Options.TimeDecimalPlaces}", CultureInfo.InvariantCulture);
    }

    protected string FormatSpeedup(double speedup)
    {
        return speedup.ToString($"F{Options.SpeedupDecimalPlaces}", CultureInfo.InvariantCulture)
            + "x";
    }

    protected static string FormatGcInfo(GcInfo gc)
    {
        return $"{gc.Gen0}/{gc.Gen1}/{gc.Gen2}";
    }

    /// <summary>
    /// Get a visual indicator for the speedup magnitude.
    /// </summary>
    public static string GetSpeedupIndicator(double speedup)
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
