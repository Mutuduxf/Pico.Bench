namespace Pico.Bench;

/// <summary>
/// Static helper for running attribute-based benchmarks.
/// Provides a generic <c>Run&lt;T&gt;</c> entry point that is fully AOT-compatible
/// because the source generator implements <see cref="IBenchmarkClass"/> on <typeparamref name="T"/>.
/// </summary>
public static class BenchmarkRunner
{
    /// <summary>
    /// Creates a new instance of <typeparamref name="T"/> and runs all benchmarks
    /// declared with <see cref="BenchmarkAttribute"/>.
    /// </summary>
    /// <typeparam name="T">
    /// A <see cref="BenchmarkClassAttribute"/>-decorated partial class.
    /// The source generator implements <see cref="IBenchmarkClass"/> automatically.
    /// </typeparam>
    /// <param name="config">
    /// Optional configuration. Defaults to <see cref="BenchmarkConfig.Default"/> when <c>null</c>.
    /// </param>
    /// <returns>A <see cref="BenchmarkSuite"/> containing all results and comparisons.</returns>
    public static BenchmarkSuite Run<T>(BenchmarkConfig? config = null)
        where T : IBenchmarkClass, new()
    {
        return new T().RunBenchmarks(config);
    }

    /// <summary>
    /// Runs all benchmarks on an existing instance.
    /// Useful when the benchmark class requires constructor arguments or pre-configured state.
    /// </summary>
    public static BenchmarkSuite Run<T>(T instance, BenchmarkConfig? config = null)
        where T : IBenchmarkClass
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));
        return instance.RunBenchmarks(config);
    }
}
