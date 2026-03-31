namespace PicoBench.Tests.TestData;

public static class GcInfoFactory
{
    /// <summary>
    /// Creates a GcInfo with specified generation counts.
    /// </summary>
    public static GcInfo Create(int gen0 = 10, int gen1 = 2, int gen2 = 0)
    {
        return new GcInfo
        {
            Gen0 = gen0,
            Gen1 = gen1,
            Gen2 = gen2
        };
    }

    /// <summary>
    /// Creates GcInfo with zero collections (no GC occurred).
    /// </summary>
    public static GcInfo Zero() => Create(0, 0, 0);

    /// <summary>
    /// Creates GcInfo with many collections (GC-heavy scenario).
    /// </summary>
    public static GcInfo Many() => Create(100, 50, 10);

    /// <summary>
    /// Gets a collection of edge-case GcInfo instances for testing.
    /// </summary>
    public static IEnumerable<GcInfo> GetEdgeCases()
    {
        yield return Zero();
        yield return Create(1, 0, 0);
        yield return Create(0, 1, 0);
        yield return Create(0, 0, 1);
        yield return Create(int.MaxValue, 0, 0);
        yield return Create(0, int.MaxValue, 0);
        yield return Create(0, 0, int.MaxValue);
    }
}
