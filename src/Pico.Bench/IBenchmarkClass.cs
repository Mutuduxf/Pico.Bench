namespace Pico.Bench;

/// <summary>
/// Interface implemented by source-generated benchmark classes.
/// The source generator adds this interface to any class decorated with
/// <see cref="BenchmarkClassAttribute"/>, emitting a full <see cref="RunBenchmarks"/>
/// implementation that is AOT-compatible (no reflection).
/// </summary>
public interface IBenchmarkClass
{
    /// <summary>
    /// Runs all <see cref="BenchmarkAttribute"/>-marked methods in this class
    /// and returns a <see cref="BenchmarkSuite"/> containing the results.
    /// </summary>
    /// <param name="config">
    /// Optional configuration. Defaults to <see cref="BenchmarkConfig.Default"/> when <c>null</c>.
    /// </param>
    /// <returns>A suite containing individual results and any comparisons.</returns>
    BenchmarkSuite RunBenchmarks(BenchmarkConfig? config = null);
}
