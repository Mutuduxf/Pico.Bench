namespace Pico.Bench;

/// <summary>
/// Low-level timing utilities for benchmark measurements.
/// Provides high-precision timing with GC and CPU cycle tracking.
/// </summary>
public static class Runner
{
    private static bool _initialized;

    /// <summary>
    /// Initialize the runner by setting process/thread priority and warming up timing APIs.
    /// Call this once at the start of your benchmark session.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
            return;

        if (OperatingSystem.IsWindows())
        {
            try
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
            }
            catch
            {
                // Ignore if we can't set priority (e.g., insufficient permissions)
            }
        }

        // Warm-up: touch Stopwatch/GC/cycle APIs once.
        Time(1, static () => { });
        _initialized = true;
    }

    /// <summary>
    /// Run a timed measurement of the given action.
    /// </summary>
    /// <param name="iterations">Number of times to invoke the action.</param>
    /// <param name="action">The action to measure.</param>
    /// <returns>A <see cref="TimingSample"/> containing timing and GC data.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimingSample Time(int iterations, Action action) =>
        Time(iterations, action, setup: null, teardown: null);

    /// <summary>
    /// Run a timed measurement with optional setup and teardown.
    /// </summary>
    /// <param name="iterations">Number of times to invoke the action.</param>
    /// <param name="action">The action to measure.</param>
    /// <param name="setup">Optional setup action (not timed).</param>
    /// <param name="teardown">Optional teardown action (not timed).</param>
    /// <returns>A <see cref="TimingSample"/> containing timing and GC data.</returns>
    public static TimingSample Time(
        int iterations,
        Action action,
        Action? setup,
        Action? teardown
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);
        ArgumentNullException.ThrowIfNull(action);

        // Force GC and record baseline counts
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);

        var gcCounts = new int[GC.MaxGeneration + 1];
        for (int i = 0; i <= GC.MaxGeneration; i++)
            gcCounts[i] = GC.CollectionCount(i);

        // Run setup (not timed)
        setup?.Invoke();

        // Start timing
        var cycleStart = GetCpuCycles();
        var watch = Stopwatch.StartNew();

        // Run the measured work
        for (var i = 0; i < iterations; i++)
            action();

        // Stop timing
        watch.Stop();
        var cycleEnd = GetCpuCycles();

        // Run teardown (not timed)
        teardown?.Invoke();

        // Compute deltas
        var elapsedTicks = watch.ElapsedTicks;
        var elapsedNs = elapsedTicks * (1_000_000_000.0 / Stopwatch.Frequency);

        var gen0 = GC.CollectionCount(0) - gcCounts[0];
        var gen1 = GC.MaxGeneration >= 1 ? GC.CollectionCount(1) - gcCounts[1] : 0;
        var gen2 = GC.MaxGeneration >= 2 ? GC.CollectionCount(2) - gcCounts[2] : 0;

        return new TimingSample
        {
            ElapsedNanoseconds = elapsedNs,
            ElapsedMilliseconds = watch.Elapsed.TotalMilliseconds,
            ElapsedTicks = elapsedTicks,
            CpuCycles = cycleEnd - cycleStart,
            GcInfo = new GcInfo { Gen0 = gen0, Gen1 = gen1, Gen2 = gen2 }
        };
    }

    /// <summary>
    /// Run a timed measurement with state passed to the action (avoids closure allocation).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimingSample Time<TState>(int iterations, TState state, Action<TState> action)
        where TState : allows ref struct
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);
        ArgumentNullException.ThrowIfNull(action);

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);

        var gcCounts = new int[GC.MaxGeneration + 1];
        for (int i = 0; i <= GC.MaxGeneration; i++)
            gcCounts[i] = GC.CollectionCount(i);

        var cycleStart = GetCpuCycles();
        var watch = Stopwatch.StartNew();

        for (var i = 0; i < iterations; i++)
            action(state);

        watch.Stop();
        var cycleEnd = GetCpuCycles();

        var elapsedTicks = watch.ElapsedTicks;
        var elapsedNs = elapsedTicks * (1_000_000_000.0 / Stopwatch.Frequency);

        var gen0 = GC.CollectionCount(0) - gcCounts[0];
        var gen1 = GC.MaxGeneration >= 1 ? GC.CollectionCount(1) - gcCounts[1] : 0;
        var gen2 = GC.MaxGeneration >= 2 ? GC.CollectionCount(2) - gcCounts[2] : 0;

        return new TimingSample
        {
            ElapsedNanoseconds = elapsedNs,
            ElapsedMilliseconds = watch.Elapsed.TotalMilliseconds,
            ElapsedTicks = elapsedTicks,
            CpuCycles = cycleEnd - cycleStart,
            GcInfo = new GcInfo { Gen0 = gen0, Gen1 = gen1, Gen2 = gen2 }
        };
    }

    #region CPU Cycle Counter (Windows only)

    private static ulong GetCpuCycles()
    {
        if (!OperatingSystem.IsWindows())
            return 0;

        ulong cycleCount = 0;
        QueryThreadCycleTime(GetCurrentThread(), ref cycleCount);
        return cycleCount;
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryThreadCycleTime(IntPtr threadHandle, ref ulong cycleTime);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentThread();

    #endregion
}
