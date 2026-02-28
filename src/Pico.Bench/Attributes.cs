namespace Pico.Bench;

/// <summary>
/// Marks a class as containing benchmark methods. The class must be declared <c>partial</c>
/// so the source generator can emit the <see cref="IBenchmarkClass"/> implementation.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class BenchmarkClassAttribute : Attribute
{
    /// <summary>
    /// Optional description for this benchmark suite.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Marks a method as a benchmark to be measured.
/// The method must be parameterless and may return any type (the return value is discarded).
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class BenchmarkAttribute : Attribute
{
    /// <summary>
    /// When <c>true</c>, this method serves as the baseline for comparisons.
    /// Only one method per class should be marked as the baseline.
    /// </summary>
    public bool Baseline { get; set; }

    /// <summary>
    /// Optional description for this benchmark.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Marks a method to be called once before all benchmarks in the class.
/// This runs once per parameter combination when <see cref="ParamsAttribute"/> is used.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class GlobalSetupAttribute : Attribute { }

/// <summary>
/// Marks a method to be called once after all benchmarks in the class.
/// This runs once per parameter combination when <see cref="ParamsAttribute"/> is used.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class GlobalCleanupAttribute : Attribute { }

/// <summary>
/// Marks a method to be called before each sample iteration (not timed).
/// Corresponds to the <c>setup</c> parameter of <see cref="Benchmark.Run"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class IterationSetupAttribute : Attribute { }

/// <summary>
/// Marks a method to be called after each sample iteration (not timed).
/// Corresponds to the <c>teardown</c> parameter of <see cref="Benchmark.Run"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class IterationCleanupAttribute : Attribute { }

/// <summary>
/// Specifies a set of values for a property or field to be iterated during benchmarking.
/// The benchmark methods will be invoked for each parameter combination (Cartesian product).
/// <para>
/// Values must be compile-time constants compatible with the property/field type.
/// </para>
/// </summary>
/// <example>
/// <code>
/// [Params(10, 100, 1000)]
/// public int N { get; set; }
/// </code>
/// </example>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field,
    Inherited = false,
    AllowMultiple = false
)]
public sealed class ParamsAttribute : Attribute
{
    /// <summary>
    /// The set of values to iterate over.
    /// </summary>
    public object[] Values { get; }

    /// <summary>
    /// Creates a new <see cref="ParamsAttribute"/> with the specified values.
    /// </summary>
    /// <param name="values">Compile-time constant values to iterate over.</param>
    public ParamsAttribute(params object[] values)
    {
        Values = values ?? throw new ArgumentNullException(nameof(values));
    }
}
