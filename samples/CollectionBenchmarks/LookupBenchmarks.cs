using Pico.Bench;

// ═══════════════════════════════════════════════════════════════════════
//  Benchmark class: List vs Dictionary vs HashSet lookup performance
// ═══════════════════════════════════════════════════════════════════════
//
//  The source generator reads these attributes at compile time and emits
//  a RunBenchmarks() method that:
//    1. Iterates [Params] values (N = 100, 1_000, 10_000)
//    2. Calls [GlobalSetup] to populate the collections
//    3. Runs each [Benchmark] method through Benchmark.Run()
//    4. Compares every candidate against the [Benchmark(Baseline = true)]
//    5. Calls [GlobalCleanup] to release resources
//    6. Returns a BenchmarkSuite with all results & comparisons
//
//  No reflection is used — everything is statically dispatched.
// ═══════════════════════════════════════════════════════════════════════

[BenchmarkClass(
    Description = "Lookup performance: List.Contains vs Dictionary.ContainsKey vs HashSet.Contains"
)]
public partial class LookupBenchmarks
{
    // ── Parameterised size ──────────────────────────────────────────
    [Params(100, 1_000, 10_000)]
    public int N { get; set; }

    // ── Data ────────────────────────────────────────────────────────
    private List<int> _list = null!;
    private Dictionary<int, bool> _dictionary = null!;
    private HashSet<int> _hashSet = null!;
    private int _target;

    // ── [GlobalSetup]: called once per N value ──────────────────────
    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42); // fixed seed for reproducibility
        var data = Enumerable.Range(0, N).OrderBy(_ => random.Next()).ToArray();

        _list = new List<int>(data);
        _dictionary = data.ToDictionary(x => x, _ => true);
        _hashSet = new HashSet<int>(data);

        // Always search for the last element (worst case for List).
        _target = data[^1];
    }

    // ── [GlobalCleanup]: release large allocations ──────────────────
    [GlobalCleanup]
    public void Cleanup()
    {
        _list = null!;
        _dictionary = null!;
        _hashSet = null!;
    }

    // ── [IterationSetup]: shuffle target before each sample ─────────
    [IterationSetup]
    public void ShuffleTarget()
    {
        // Vary the lookup target slightly per-sample to reduce branch
        // prediction effects, while keeping it within the collection.
        _target = (_target + 7) % N;
    }

    // ── Benchmarks ──────────────────────────────────────────────────

    /// <summary>
    /// Baseline: linear scan through List&lt;int&gt;.Contains().
    /// O(n) — performance degrades linearly with collection size.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Linear scan O(n)")]
    public void ListContains()
    {
        _ = _list.Contains(_target);
    }

    /// <summary>
    /// Candidate: Dictionary&lt;int, bool&gt;.ContainsKey().
    /// O(1) average — hash-based lookup.
    /// </summary>
    [Benchmark(Description = "Hash lookup O(1)")]
    public void DictionaryContainsKey()
    {
        _ = _dictionary.ContainsKey(_target);
    }

    /// <summary>
    /// Candidate: HashSet&lt;int&gt;.Contains().
    /// O(1) average — optimised for membership tests.
    /// </summary>
    [Benchmark(Description = "Hash lookup O(1), set-optimised")]
    public void HashSetContains()
    {
        _ = _hashSet.Contains(_target);
    }
}
