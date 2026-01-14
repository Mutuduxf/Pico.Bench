using System.Text;

namespace Pico.Bench.Formatters;

/// <summary>
/// Formats benchmark results for console output with ASCII tables.
/// </summary>
public sealed class ConsoleFormatter : FormatterBase
{
    public ConsoleFormatter(FormatterOptions? options = null)
        : base(options) { }

    public override string Format(BenchmarkResult result)
    {
        var sb = new StringBuilder();
        AppendSingleResult(sb, result);
        return sb.ToString();
    }

    public override string Format(IEnumerable<BenchmarkResult> results)
    {
        var sb = new StringBuilder();
        var list = results.ToList();

        if (list.Count == 0)
            return "No results.";

        AppendResultsTable(sb, list);
        return sb.ToString();
    }

    public override string Format(ComparisonResult comparison)
    {
        var sb = new StringBuilder();
        AppendComparisonResult(sb, comparison);
        return sb.ToString();
    }

    public override string Format(IEnumerable<ComparisonResult> comparisons)
    {
        var sb = new StringBuilder();
        var list = comparisons.ToList();

        if (list.Count == 0)
            return "No comparisons.";

        AppendComparisonsTable(sb, list);
        return sb.ToString();
    }

    public override string Format(BenchmarkSuite suite)
    {
        var sb = new StringBuilder();

        // Header
        AppendBoxHeader(sb, suite.Name, suite.Description);

        // Environment info
        if (Options.IncludeEnvironment)
        {
            sb.AppendLine();
            sb.AppendLine($"Environment: {suite.Environment}");
        }

        if (Options.IncludeTimestamp)
        {
            sb.AppendLine($"Timestamp: {suite.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"Duration: {suite.Duration.TotalSeconds:F2}s");
        }

        // Results
        if (suite.Results.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("═══ Results ═══");
            AppendResultsTable(sb, suite.Results.ToList());
        }

        // Comparisons
        if (suite.Comparisons?.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("═══ Comparisons ═══");
            AppendComparisonsTable(sb, suite.Comparisons.ToList());
        }

        return sb.ToString();
    }

    #region Single Result

    private void AppendSingleResult(StringBuilder sb, BenchmarkResult result)
    {
        sb.AppendLine($"Benchmark: {result.Name}");
        sb.AppendLine($"  Avg: {FormatTime(result.Statistics.Avg)} ns");
        sb.AppendLine($"  P50: {FormatTime(result.Statistics.P50)} ns");

        if (Options.IncludePercentiles)
        {
            sb.AppendLine($"  P90: {FormatTime(result.Statistics.P90)} ns");
            sb.AppendLine($"  P95: {FormatTime(result.Statistics.P95)} ns");
            sb.AppendLine($"  P99: {FormatTime(result.Statistics.P99)} ns");
        }

        if (Options.IncludeCpuCycles)
        {
            sb.AppendLine($"  CPU Cycles: {result.Statistics.CpuCyclesPerOp:F0}");
        }

        if (Options.IncludeGcInfo)
        {
            sb.AppendLine($"  GC (0/1/2): {FormatGcInfo(result.Statistics.GcInfo)}");
        }
    }

    #endregion

    #region Results Table

    private void AppendResultsTable(StringBuilder sb, List<BenchmarkResult> results)
    {
        // Calculate column widths based on content
        var nameWidth = Math.Max("Name".Length, results.Max(r => r.Name.Length));
        var avgWidth = Math.Max(
            "Avg (ns)".Length,
            results.Max(r => FormatTime(r.Statistics.Avg).Length)
        );
        var p50Width = Math.Max(
            "P50 (ns)".Length,
            results.Max(r => FormatTime(r.Statistics.P50).Length)
        );
        var p90Width = Options.IncludePercentiles
            ? Math.Max("P90 (ns)".Length, results.Max(r => FormatTime(r.Statistics.P90).Length))
            : 0;
        var p95Width = Options.IncludePercentiles
            ? Math.Max("P95 (ns)".Length, results.Max(r => FormatTime(r.Statistics.P95).Length))
            : 0;
        var p99Width = Options.IncludePercentiles
            ? Math.Max("P99 (ns)".Length, results.Max(r => FormatTime(r.Statistics.P99).Length))
            : 0;
        var cpuWidth = Options.IncludeCpuCycles
            ? Math.Max(
                "CPU Cycles".Length,
                results.Max(r => $"{r.Statistics.CpuCyclesPerOp:F0}".Length)
            )
            : 0;
        var gcWidth = Options.IncludeGcInfo
            ? Math.Max(
                "GC (0/1/2)".Length,
                results.Max(r => FormatGcInfo(r.Statistics.GcInfo).Length)
            )
            : 0;

        // Build column definitions
        var columns = new List<(string Header, int Width, Func<BenchmarkResult, string> GetValue)>
        {
            ("Name", nameWidth, r => r.Name),
            ("Avg (ns)", avgWidth, r => FormatTime(r.Statistics.Avg)),
            ("P50 (ns)", p50Width, r => FormatTime(r.Statistics.P50))
        };

        if (Options.IncludePercentiles)
        {
            columns.Add(("P90 (ns)", p90Width, r => FormatTime(r.Statistics.P90)));
            columns.Add(("P95 (ns)", p95Width, r => FormatTime(r.Statistics.P95)));
            columns.Add(("P99 (ns)", p99Width, r => FormatTime(r.Statistics.P99)));
        }

        if (Options.IncludeCpuCycles)
        {
            columns.Add(("CPU Cycles", cpuWidth, r => $"{r.Statistics.CpuCyclesPerOp:F0}"));
        }

        if (Options.IncludeGcInfo)
        {
            columns.Add(("GC (0/1/2)", gcWidth, r => FormatGcInfo(r.Statistics.GcInfo)));
        }

        AppendTable(sb, columns, results, isFirstColumnLeftAlign: true);
    }

    #endregion

    #region Comparison Result

    private void AppendComparisonResult(StringBuilder sb, ComparisonResult comparison)
    {
        var indicator = GetSpeedupIndicator(comparison.Speedup);

        sb.AppendLine($"Comparison: {comparison.Name}");
        sb.AppendLine(
            $"  Baseline ({comparison.Baseline.Name}): {FormatTime(comparison.Baseline.Statistics.Avg)} ns"
        );
        sb.AppendLine(
            $"  Candidate ({comparison.Candidate.Name}): {FormatTime(comparison.Candidate.Statistics.Avg)} ns"
        );
        sb.AppendLine($"  Speedup: {FormatSpeedup(comparison.Speedup)} {indicator}");
        sb.AppendLine(
            $"  Winner: {(comparison.IsFaster ? comparison.Candidate.Name : comparison.Baseline.Name)}"
        );
    }

    #endregion

    #region Comparisons Table

    private void AppendComparisonsTable(StringBuilder sb, List<ComparisonResult> comparisons)
    {
        // Flatten comparisons into individual results for detailed view
        var rows =
            new List<(string Name, string Provider, BenchmarkResult Result, string Speedup)>();
        foreach (var c in comparisons)
        {
            var indicator = GetSpeedupIndicator(c.Speedup);
            rows.Add((c.Name, "Pico.DI", c.Candidate, $"{FormatSpeedup(c.Speedup)} {indicator}"));
            rows.Add((c.Name, "MS.DI", c.Baseline, ""));
        }

        // Calculate column widths based on content
        var nameWidth = Math.Max(
            "Test Case".Length,
            rows.Max(r => $"{r.Provider} * {r.Name}".Length)
        );
        var avgWidth = Math.Max(
            "Avg (ns)".Length,
            rows.Max(r => FormatTime(r.Result.Statistics.Avg).Length)
        );
        var speedupWidth = Math.Max("Speedup".Length, rows.Max(r => r.Speedup.Length));

        // Build column definitions
        var columns = new List<(
            string Header,
            int Width,
            Func<
                (string Name, string Provider, BenchmarkResult Result, string Speedup),
                string
            > GetValue
        )>
        {
            ("Test Case", nameWidth, r => $"{r.Provider} * {r.Name}"),
            ("Avg (ns)", avgWidth, r => FormatTime(r.Result.Statistics.Avg)),
            ("Speedup", speedupWidth, r => r.Speedup)
        };

        // Add optional columns based on Options
        if (Options.IncludePercentiles)
        {
            var p50Width = Math.Max(
                "P50".Length,
                rows.Max(r => FormatTime(r.Result.Statistics.P50).Length)
            );
            var p90Width = Math.Max(
                "P90".Length,
                rows.Max(r => FormatTime(r.Result.Statistics.P90).Length)
            );
            var p99Width = Math.Max(
                "P99".Length,
                rows.Max(r => FormatTime(r.Result.Statistics.P99).Length)
            );
            columns.Add(("P50", p50Width, r => FormatTime(r.Result.Statistics.P50)));
            columns.Add(("P90", p90Width, r => FormatTime(r.Result.Statistics.P90)));
            columns.Add(("P99", p99Width, r => FormatTime(r.Result.Statistics.P99)));
        }

        if (Options.IncludeCpuCycles)
        {
            var cpuWidth = Math.Max(
                "CPU".Length,
                rows.Max(r => $"{r.Result.Statistics.CpuCyclesPerOp:F0}".Length)
            );
            columns.Add(("CPU", cpuWidth, r => $"{r.Result.Statistics.CpuCyclesPerOp:F0}"));
        }

        if (Options.IncludeGcInfo)
        {
            var gcWidth = Math.Max(
                "GC".Length,
                rows.Max(r => FormatGcInfo(r.Result.Statistics.GcInfo).Length)
            );
            columns.Add(("GC", gcWidth, r => FormatGcInfo(r.Result.Statistics.GcInfo)));
        }

        AppendTable(sb, columns, rows, isFirstColumnLeftAlign: true);

        // Summary
        var wins = comparisons.Count(c => c.IsFaster);
        var avgSpeedup = comparisons.Average(c => c.Speedup);
        var maxSpeedup = comparisons.Max(c => c.Speedup);

        sb.AppendLine();
        sb.AppendLine(
            $"Summary: Candidate wins {wins}/{comparisons.Count} | Avg: {FormatSpeedup(avgSpeedup)} | Max: {FormatSpeedup(maxSpeedup)}"
        );
    }

    #endregion

    #region Box Header

    private static void AppendBoxHeader(StringBuilder sb, string title, string? description)
    {
        const int width = 80;
        var border = new string('═', width - 2);

        sb.AppendLine($"╔{border}╗");
        sb.AppendLine($"║{CenterText(title, width - 2)}║");

        if (!string.IsNullOrEmpty(description))
        {
            sb.AppendLine($"║{CenterText(description, width - 2)}║");
        }

        sb.AppendLine($"╚{border}╝");
    }

    private static string CenterText(string text, int width)
    {
        if (text.Length >= width)
            return text[..width];

        var padding = (width - text.Length) / 2;
        return text.PadLeft(padding + text.Length).PadRight(width);
    }

    #endregion

    #region Generic Table Builder

    private static void AppendTable<T>(
        StringBuilder sb,
        List<(string Header, int Width, Func<T, string> GetValue)> columns,
        List<T> rows,
        bool isFirstColumnLeftAlign = false
    )
    {
        // Add padding to widths
        for (int i = 0; i < columns.Count; i++)
        {
            var (header, width, getValue) = columns[i];
            columns[i] = (header, width + 2, getValue); // +2 for padding
        }

        // Top border
        sb.Append('┌');
        for (int i = 0; i < columns.Count; i++)
        {
            sb.Append(new string('─', columns[i].Width));
            sb.Append(i < columns.Count - 1 ? '┬' : '┐');
        }
        sb.AppendLine();

        // Header row
        sb.Append('│');
        for (int i = 0; i < columns.Count; i++)
        {
            var (header, width, _) = columns[i];
            var text =
                i == 0 && isFirstColumnLeftAlign
                    ? $" {header}".PadRight(width)
                    : header.PadLeft(width - 1) + " ";
            sb.Append(text);
            sb.Append('│');
        }
        sb.AppendLine();

        // Header separator
        sb.Append('├');
        for (int i = 0; i < columns.Count; i++)
        {
            sb.Append(new string('─', columns[i].Width));
            sb.Append(i < columns.Count - 1 ? '┼' : '┤');
        }
        sb.AppendLine();

        // Data rows
        foreach (var row in rows)
        {
            sb.Append('│');
            for (int i = 0; i < columns.Count; i++)
            {
                var (_, width, getValue) = columns[i];
                var value = getValue(row);
                var text =
                    i == 0 && isFirstColumnLeftAlign
                        ? $" {value}".PadRight(width)
                        : value.PadLeft(width - 1) + " ";
                sb.Append(text);
                sb.Append('│');
            }
            sb.AppendLine();
        }

        // Bottom border
        sb.Append('└');
        for (int i = 0; i < columns.Count; i++)
        {
            sb.Append(new string('─', columns[i].Width));
            sb.Append(i < columns.Count - 1 ? '┴' : '┘');
        }
        sb.AppendLine();
    }

    #endregion

    #region Table With Title

    /// <summary>
    /// Formats comparisons as a table with a title header.
    /// </summary>
    public string FormatTableWithTitle(string title, IEnumerable<ComparisonResult> comparisons)
    {
        var sb = new StringBuilder();
        var list = comparisons.ToList();

        if (list.Count == 0)
            return "No comparisons.";

        AppendComparisonsTableWithTitle(sb, title, list);
        return sb.ToString();
    }

    private void AppendComparisonsTableWithTitle(
        StringBuilder sb,
        string title,
        List<ComparisonResult> comparisons
    )
    {
        // Flatten comparisons into individual rows for detailed view
        var rows =
            new List<(string Name, string Provider, BenchmarkResult Result, string Speedup)>();
        foreach (var c in comparisons)
        {
            var indicator = GetSpeedupIndicator(c.Speedup);
            rows.Add((c.Name, "Pico.DI", c.Candidate, $"{FormatSpeedup(c.Speedup)} {indicator}"));
            rows.Add((c.Name, "MS.DI", c.Baseline, ""));
        }

        // Calculate column widths based on content
        var nameWidth = Math.Max(
            "Test Case".Length,
            rows.Max(r => $"{r.Provider} * {r.Name}".Length)
        );
        var avgWidth = Math.Max(
            "Avg (ns)".Length,
            rows.Max(r => FormatTime(r.Result.Statistics.Avg).Length)
        );
        var speedupWidth = Math.Max("Speedup".Length, rows.Max(r => r.Speedup.Length));

        // Build column definitions
        var columns = new List<(string Header, int Width)> { ("Test Case", nameWidth + 2) };
        columns.Add(("Avg (ns)", avgWidth + 2));
        columns.Add(("Speedup", speedupWidth + 2));

        if (Options.IncludePercentiles)
        {
            var p50Width = Math.Max(
                "P50".Length,
                rows.Max(r => FormatTime(r.Result.Statistics.P50).Length)
            );
            var p90Width = Math.Max(
                "P90".Length,
                rows.Max(r => FormatTime(r.Result.Statistics.P90).Length)
            );
            var p99Width = Math.Max(
                "P99".Length,
                rows.Max(r => FormatTime(r.Result.Statistics.P99).Length)
            );
            columns.Add(("P50", p50Width + 2));
            columns.Add(("P90", p90Width + 2));
            columns.Add(("P99", p99Width + 2));
        }

        if (Options.IncludeCpuCycles)
        {
            var cpuWidth = Math.Max(
                "CPU".Length,
                rows.Max(r => $"{r.Result.Statistics.CpuCyclesPerOp:F0}".Length)
            );
            columns.Add(("CPU", cpuWidth + 2));
        }

        if (Options.IncludeGcInfo)
        {
            var gcWidth = Math.Max(
                "GC".Length,
                rows.Max(r => FormatGcInfo(r.Result.Statistics.GcInfo).Length)
            );
            columns.Add(("GC", gcWidth + 2));
        }

        var totalWidth = columns.Sum(c => c.Width) + columns.Count + 1;

        // Title row
        sb.AppendLine();
        sb.Append('┌');
        sb.Append(new string('─', totalWidth - 2));
        sb.AppendLine("┐");
        sb.Append("│ ");
        sb.Append(title.PadRight(totalWidth - 4));
        sb.AppendLine(" │");

        // Header separator with column dividers
        sb.Append('├');
        for (int i = 0; i < columns.Count; i++)
        {
            sb.Append(new string('─', columns[i].Width));
            sb.Append(i < columns.Count - 1 ? '┬' : '┤');
        }
        sb.AppendLine();

        // Header row
        sb.Append('│');
        for (int i = 0; i < columns.Count; i++)
        {
            var (header, width) = columns[i];
            var text = i == 0 ? $" {header}".PadRight(width) : header.PadLeft(width - 1) + " ";
            sb.Append(text);
            sb.Append('│');
        }
        sb.AppendLine();

        // Header-data separator
        sb.Append('├');
        for (int i = 0; i < columns.Count; i++)
        {
            sb.Append(new string('─', columns[i].Width));
            sb.Append(i < columns.Count - 1 ? '┼' : '┤');
        }
        sb.AppendLine();

        // Data rows
        foreach (var row in rows)
        {
            var values = new List<string>
            {
                $"{row.Provider} * {row.Name}",
                FormatTime(row.Result.Statistics.Avg),
                row.Speedup
            };

            if (Options.IncludePercentiles)
            {
                values.Add(FormatTime(row.Result.Statistics.P50));
                values.Add(FormatTime(row.Result.Statistics.P90));
                values.Add(FormatTime(row.Result.Statistics.P99));
            }

            if (Options.IncludeCpuCycles)
            {
                values.Add($"{row.Result.Statistics.CpuCyclesPerOp:F0}");
            }

            if (Options.IncludeGcInfo)
            {
                values.Add(FormatGcInfo(row.Result.Statistics.GcInfo));
            }

            sb.Append('│');
            for (int i = 0; i < columns.Count; i++)
            {
                var width = columns[i].Width;
                var text =
                    i == 0 ? $" {values[i]}".PadRight(width) : values[i].PadLeft(width - 1) + " ";
                sb.Append(text);
                sb.Append('│');
            }
            sb.AppendLine();
        }

        // Bottom border
        sb.Append('└');
        for (int i = 0; i < columns.Count; i++)
        {
            sb.Append(new string('─', columns[i].Width));
            sb.Append(i < columns.Count - 1 ? '┴' : '┘');
        }
        sb.AppendLine();
    }

    #endregion

    #region Static Helpers

    /// <summary>
    /// Write formatted results directly to console.
    /// </summary>
    public static void Write(BenchmarkResult result, FormatterOptions? options = null)
    {
        var formatter = new ConsoleFormatter(options);
        Console.WriteLine(formatter.Format(result));
    }

    /// <summary>
    /// Write formatted results directly to console.
    /// </summary>
    public static void Write(IEnumerable<BenchmarkResult> results, FormatterOptions? options = null)
    {
        var formatter = new ConsoleFormatter(options);
        Console.WriteLine(formatter.Format(results));
    }

    /// <summary>
    /// Write formatted comparison directly to console.
    /// </summary>
    public static void Write(ComparisonResult comparison, FormatterOptions? options = null)
    {
        var formatter = new ConsoleFormatter(options);
        Console.WriteLine(formatter.Format(comparison));
    }

    /// <summary>
    /// Write formatted comparisons directly to console.
    /// </summary>
    public static void Write(
        IEnumerable<ComparisonResult> comparisons,
        FormatterOptions? options = null
    )
    {
        var formatter = new ConsoleFormatter(options);
        Console.WriteLine(formatter.Format(comparisons));
    }

    /// <summary>
    /// Write formatted suite directly to console.
    /// </summary>
    public static void Write(BenchmarkSuite suite, FormatterOptions? options = null)
    {
        var formatter = new ConsoleFormatter(options);
        Console.WriteLine(formatter.Format(suite));
    }

    /// <summary>
    /// Write comparisons table with title directly to console.
    /// </summary>
    public static void WriteTableWithTitle(
        string title,
        IEnumerable<ComparisonResult> comparisons,
        FormatterOptions? options = null
    )
    {
        var formatter = new ConsoleFormatter(options);
        Console.Write(formatter.FormatTableWithTitle(title, comparisons));
    }

    /// <summary>
    /// Write a header box to console.
    /// </summary>
    /// <param name="title">The main title.</param>
    /// <param name="width">The width of the box (default 111).</param>
    public static void WriteHeader(string title, int width = 111)
    {
        var border = new string('═', width - 2);
        var padding = (width - 2 - title.Length) / 2;
        var centeredTitle = title.PadLeft(padding + title.Length).PadRight(width - 2);

        Console.WriteLine();
        Console.WriteLine($"╔{border}╗");
        Console.WriteLine($"║{centeredTitle}║");
        Console.WriteLine($"╚{border}╝");
    }

    /// <summary>
    /// Write environment and config info to console.
    /// </summary>
    public static void WriteEnvironment(EnvironmentInfo env, BenchmarkConfig config)
    {
        Console.WriteLine();
        Console.WriteLine($"Environment: {env}");
        Console.WriteLine(
            $"Config: {config.SampleCount} samples × {config.IterationsPerSample} iterations"
        );
        Console.WriteLine();
    }

    /// <summary>
    /// Write file save message to console.
    /// </summary>
    public static void WriteFileSaved(string format, string path)
    {
        Console.WriteLine($"\n{format} results saved to: {path}");
    }

    #endregion
}
