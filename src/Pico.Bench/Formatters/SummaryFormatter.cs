namespace Pico.Bench.Formatters;

/// <summary>
/// Configuration for summary output.
/// </summary>
public sealed class SummaryOptions
{
    /// <summary>Title for the summary box.</summary>
    public string Title { get; init; } = "SUMMARY";

    /// <summary>Width of the summary box (in characters).</summary>
    public int BoxWidth { get; init; } = 111;

    /// <summary>Label for the candidate (faster) side in comparisons.</summary>
    public string CandidateLabel { get; init; } = "Candidate";

    /// <summary>Label for wins count.</summary>
    public string WinsLabel { get; init; } = "wins";

    /// <summary>Show grouping by category.</summary>
    public bool GroupByCategory { get; init; } = true;

    /// <summary>Show detailed comparison table.</summary>
    public bool ShowDetailedTable { get; init; } = true;

    /// <summary>Show duration in summary.</summary>
    public bool ShowDuration { get; init; } = true;

    /// <summary>Formatter options for the detailed table.</summary>
    public FormatterOptions? TableOptions { get; init; }

    /// <summary>Default summary options.</summary>
    public static SummaryOptions Default { get; } = new();
}

/// <summary>
/// Formats benchmark comparison summaries for console output.
/// Accepts an optional <see cref="IFormatter"/> to render the detailed table,
/// defaulting to <see cref="ConsoleFormatter"/> if none is provided.
/// </summary>
public static class SummaryFormatter
{
    /// <summary>
    /// Format a summary of comparison results.
    /// </summary>
    /// <param name="comparisons">Comparison results to summarize.</param>
    /// <param name="duration">Optional total benchmark duration.</param>
    /// <param name="options">Optional summary display options.</param>
    /// <param name="detailFormatter">Optional formatter for the detailed table. Defaults to <see cref="ConsoleFormatter"/>.</param>
    public static string Format(
        IEnumerable<ComparisonResult> comparisons,
        TimeSpan? duration = null,
        SummaryOptions? options = null,
        IFormatter? detailFormatter = null
    )
    {
        if (comparisons == null)
            throw new ArgumentNullException(nameof(comparisons));
        options ??= SummaryOptions.Default;
        var list = comparisons.ToList();
        var sb = new StringBuilder();

        if (list.Count == 0)
        {
            sb.AppendLine("No comparison results.");
            return sb.ToString();
        }

        // Header box
        AppendBoxLine(sb, options.BoxWidth, '╔', '═', '╗');
        AppendCenteredLine(sb, options.Title, options.BoxWidth);
        AppendBoxLine(sb, options.BoxWidth, '╚', '═', '╝');
        sb.AppendLine();

        // Group by category
        if (options.GroupByCategory)
        {
            var byCategory = list.GroupBy(c => c.Category ?? "Other").OrderBy(g => g.Key);

            foreach (var group in byCategory)
            {
                var avg = group.Average(c => c.Speedup);
                sb.AppendLine($"▶ {group.Key}: {avg:F2}x average speedup");

                foreach (var c in group)
                {
                    var indicator = FormatterBase.GetSpeedupIndicator(c.Speedup);
                    sb.AppendLine($"   {c.Name, -35}: {c.Speedup:F2}x {indicator}");
                }
                sb.AppendLine();
            }
        }

        // Overall stats
        var wins = list.Count(c => c.IsFaster);
        var total = list.Count;
        var overallAvg = list.Average(c => c.Speedup);
        var max = list.Max(c => c.Speedup);

        AppendBoxLine(sb, options.BoxWidth, '╔', '═', '╗');
        AppendPaddedLine(
            sb,
            $"  {options.CandidateLabel} {options.WinsLabel}: {wins} / {total} scenarios",
            options.BoxWidth
        );
        AppendPaddedLine(sb, $"  Average speedup: {overallAvg:F2}x faster", options.BoxWidth);
        AppendPaddedLine(sb, $"  Maximum speedup: {max:F2}x faster", options.BoxWidth);

        if (options.ShowDuration && duration.HasValue)
        {
            AppendPaddedLine(
                sb,
                $"  Duration: {duration.Value.TotalSeconds:F2}s",
                options.BoxWidth
            );
        }

        AppendBoxLine(sb, options.BoxWidth, '╚', '═', '╝');

        // Detailed table
        if (!options.ShowDetailedTable)
            return sb.ToString();
        sb.AppendLine();
        sb.AppendLine("▶ Detailed Results:");

        var formatter =
            detailFormatter
            ?? new ConsoleFormatter(options.TableOptions ?? FormatterOptions.Default);
        sb.Append(formatter.Format(list));

        return sb.ToString();
    }

    /// <summary>
    /// Write summary directly to console.
    /// </summary>
    public static void Write(
        IEnumerable<ComparisonResult> comparisons,
        TimeSpan? duration = null,
        SummaryOptions? options = null
    )
    {
        if (comparisons == null)
            throw new ArgumentNullException(nameof(comparisons));
        Console.Write(Format(comparisons, duration, options));
    }

    /// <summary>
    /// Write summary for a benchmark suite directly to console.
    /// </summary>
    public static void Write(BenchmarkSuite suite, SummaryOptions? options = null)
    {
        if (suite == null)
            throw new ArgumentNullException(nameof(suite));
        if (suite.Comparisons == null || suite.Comparisons.Count == 0)
        {
            Console.WriteLine("No comparison results in suite.");
            return;
        }

        Console.Write(Format(suite.Comparisons, suite.Duration, options));
    }

    #region Helpers

    private static void AppendBoxLine(StringBuilder sb, int width, char left, char fill, char right)
    {
        sb.Append(left);
        sb.Append(fill, width - 2);
        sb.AppendLine(right.ToString());
    }

    private static void AppendCenteredLine(StringBuilder sb, string text, int width)
    {
        var innerWidth = width - 2;
        var padding = (innerWidth - text.Length) / 2;
        sb.Append('║');
        sb.Append(' ', padding);
        sb.Append(text);
        sb.Append(' ', innerWidth - padding - text.Length);
        sb.AppendLine("║");
    }

    private static void AppendPaddedLine(StringBuilder sb, string text, int width)
    {
        var innerWidth = width - 2;
        sb.Append('║');
        sb.Append(text);
        if (text.Length < innerWidth)
            sb.Append(' ', innerWidth - text.Length);
        sb.AppendLine("║");
    }

    #endregion
}
