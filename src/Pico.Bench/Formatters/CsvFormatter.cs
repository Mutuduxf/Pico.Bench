using System.Globalization;
using System.Text;

namespace Pico.Bench.Formatters;

/// <summary>
/// Formats benchmark results as CSV for data analysis.
/// </summary>
public sealed class CsvFormatter : FormatterBase
{
    private const string Separator = ",";

    public CsvFormatter(FormatterOptions? options = null)
        : base(options) { }

    public override string Format(BenchmarkResult result)
    {
        return Format([result]);
    }

    public override string Format(IEnumerable<BenchmarkResult> results)
    {
        var sb = new StringBuilder();
        var list = results.ToList();

        if (list.Count == 0)
            return string.Empty;

        // Header row
        AppendResultsHeader(sb);

        // Data rows
        foreach (var result in list)
        {
            AppendResultRow(sb, result);
        }

        return sb.ToString();
    }

    public override string Format(ComparisonResult comparison)
    {
        return Format([comparison]);
    }

    public override string Format(IEnumerable<ComparisonResult> comparisons)
    {
        var sb = new StringBuilder();
        var list = comparisons.ToList();

        if (list.Count == 0)
            return string.Empty;

        // Header row
        AppendComparisonsHeader(sb);

        // Data rows
        foreach (var comparison in list)
        {
            AppendComparisonRow(sb, comparison);
        }

        return sb.ToString();
    }

    public override string Format(BenchmarkSuite suite)
    {
        var sb = new StringBuilder();

        // Suite metadata as comment header
        if (Options.IncludeEnvironment)
        {
            sb.AppendLine($"# Suite: {Escape(suite.Name)}");
            if (!string.IsNullOrEmpty(suite.Description))
                sb.AppendLine($"# Description: {Escape(suite.Description)}");
            sb.AppendLine($"# Environment: {Escape(suite.Environment.ToString())}");
        }

        if (Options.IncludeTimestamp)
        {
            sb.AppendLine($"# Timestamp: {suite.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"# Duration: {suite.Duration.TotalSeconds:F2}s");
        }

        sb.AppendLine();

        // Results section
        if (suite.Results.Count > 0)
        {
            sb.AppendLine("# Results");
            AppendResultsHeader(sb);
            foreach (var result in suite.Results)
            {
                AppendResultRow(sb, result);
            }
        }

        // Comparisons section
        if (suite.Comparisons?.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("# Comparisons");
            AppendComparisonsHeader(sb);
            foreach (var comparison in suite.Comparisons)
            {
                AppendComparisonRow(sb, comparison);
            }
        }

        return sb.ToString();
    }

    #region Results

    private void AppendResultsHeader(StringBuilder sb)
    {
        var columns = new List<string> { "Name", "Category", "Avg_ns", "P50_ns" };

        if (Options.IncludePercentiles)
        {
            columns.AddRange(["P90_ns", "P95_ns", "P99_ns", "Min_ns", "Max_ns", "StdDev_ns"]);
        }

        if (Options.IncludeCpuCycles)
        {
            columns.Add("CpuCycles");
        }

        if (Options.IncludeGcInfo)
        {
            columns.AddRange(["GC0", "GC1", "GC2"]);
        }

        columns.AddRange(["SampleCount", "IterationsPerSample"]);

        if (Options.IncludeTimestamp)
        {
            columns.Add("Timestamp");
        }

        sb.AppendLine(string.Join(Separator, columns));
    }

    private void AppendResultRow(StringBuilder sb, BenchmarkResult result)
    {
        var s = result.Statistics;
        var values = new List<string>
        {
            Escape(result.Name),
            Escape(result.Category ?? ""),
            FormatNumber(s.Avg),
            FormatNumber(s.P50)
        };

        if (Options.IncludePercentiles)
        {
            values.AddRange(
                [
                    FormatNumber(s.P90),
                    FormatNumber(s.P95),
                    FormatNumber(s.P99),
                    FormatNumber(s.Min),
                    FormatNumber(s.Max),
                    FormatNumber(s.StdDev)
                ]
            );
        }

        if (Options.IncludeCpuCycles)
        {
            values.Add(FormatNumber(s.CpuCyclesPerOp));
        }

        if (Options.IncludeGcInfo)
        {
            values.AddRange(
                [
                    s.GcInfo.Gen0.ToString(CultureInfo.InvariantCulture),
                    s.GcInfo.Gen1.ToString(CultureInfo.InvariantCulture),
                    s.GcInfo.Gen2.ToString(CultureInfo.InvariantCulture)
                ]
            );
        }

        values.Add(result.SampleCount.ToString(CultureInfo.InvariantCulture));
        values.Add(result.IterationsPerSample.ToString(CultureInfo.InvariantCulture));

        if (Options.IncludeTimestamp)
        {
            values.Add(result.Timestamp.ToString("o", CultureInfo.InvariantCulture));
        }

        sb.AppendLine(string.Join(Separator, values));
    }

    #endregion

    #region Comparisons

    private void AppendComparisonsHeader(StringBuilder sb)
    {
        var columns = new List<string> { "TestCase", "Provider", "Avg_ns", "Speedup" };

        if (Options.IncludePercentiles)
        {
            columns.AddRange(["P50_ns", "P90_ns", "P99_ns"]);
        }

        if (Options.IncludeCpuCycles)
        {
            columns.Add("CpuCycles");
        }

        if (Options.IncludeGcInfo)
        {
            columns.AddRange(["GC0", "GC1", "GC2"]);
        }

        sb.AppendLine(string.Join(Separator, columns));
    }

    private void AppendComparisonRow(StringBuilder sb, ComparisonResult c)
    {
        // Output two rows: one for Pico.DI (candidate) and one for MS.DI (baseline)
        AppendComparisonSingleRow(sb, c.Name, "Pico.DI", c.Candidate.Statistics, c.Speedup);
        AppendComparisonSingleRow(sb, c.Name, "MS.DI", c.Baseline.Statistics, null);
    }

    private void AppendComparisonSingleRow(
        StringBuilder sb,
        string testCase,
        string provider,
        Statistics stats,
        double? speedup
    )
    {
        var values = new List<string>
        {
            Escape($"{provider} * {testCase}"),
            Escape(provider),
            FormatNumber(stats.Avg),
            speedup.HasValue ? FormatNumber(speedup.Value) : ""
        };

        if (Options.IncludePercentiles)
        {
            values.AddRange(
                [FormatNumber(stats.P50), FormatNumber(stats.P90), FormatNumber(stats.P99)]
            );
        }

        if (Options.IncludeCpuCycles)
        {
            values.Add(FormatNumber(stats.CpuCyclesPerOp));
        }

        if (Options.IncludeGcInfo)
        {
            values.AddRange(
                [
                    stats.GcInfo.Gen0.ToString(CultureInfo.InvariantCulture),
                    stats.GcInfo.Gen1.ToString(CultureInfo.InvariantCulture),
                    stats.GcInfo.Gen2.ToString(CultureInfo.InvariantCulture)
                ]
            );
        }

        sb.AppendLine(string.Join(Separator, values));
    }

    #endregion

    #region Helpers

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        // If contains comma, quote, or newline, wrap in quotes and escape internal quotes
        if (
            value.Contains(',')
            || value.Contains('"')
            || value.Contains('\n')
            || value.Contains('\r')
        )
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("G", CultureInfo.InvariantCulture);
    }

    #endregion

    #region Static Helpers

    /// <summary>
    /// Write CSV to a file, creating directory if needed.
    /// </summary>
    public static void WriteToFile(
        string filePath,
        BenchmarkResult result,
        FormatterOptions? options = null
    )
    {
        var formatter = new CsvFormatter(options);
        WriteToFileInternal(filePath, formatter.Format(result));
    }

    /// <summary>
    /// Write CSV to a file, creating directory if needed.
    /// </summary>
    public static void WriteToFile(
        string filePath,
        IEnumerable<BenchmarkResult> results,
        FormatterOptions? options = null
    )
    {
        var formatter = new CsvFormatter(options);
        WriteToFileInternal(filePath, formatter.Format(results));
    }

    /// <summary>
    /// Write CSV to a file, creating directory if needed.
    /// </summary>
    public static void WriteToFile(
        string filePath,
        IEnumerable<ComparisonResult> comparisons,
        FormatterOptions? options = null
    )
    {
        var formatter = new CsvFormatter(options);
        WriteToFileInternal(filePath, formatter.Format(comparisons));
    }

    /// <summary>
    /// Write CSV to a file, creating directory if needed.
    /// </summary>
    public static void WriteToFile(
        string filePath,
        BenchmarkSuite suite,
        FormatterOptions? options = null
    )
    {
        var formatter = new CsvFormatter(options);
        WriteToFileInternal(filePath, formatter.Format(suite));
    }

    /// <summary>
    /// Append CSV to a file (without header if file exists), creating directory if needed.
    /// </summary>
    public static void AppendToFile(
        string filePath,
        BenchmarkResult result,
        FormatterOptions? options = null
    )
    {
        var formatter = new CsvFormatter(options);
        var content = formatter.Format(result);

        if (File.Exists(filePath))
        {
            // Skip header line if file already exists
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 1)
            {
                File.AppendAllLines(filePath, lines.Skip(1));
            }
        }
        else
        {
            WriteToFileInternal(filePath, content);
        }
    }

    #endregion
}
