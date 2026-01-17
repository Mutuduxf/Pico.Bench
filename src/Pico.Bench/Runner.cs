namespace Pico.Bench;

/// <summary>
/// Low-level timing utilities for benchmark measurements.
/// Provides high-precision timing with GC and CPU cycle tracking.
/// </summary>
public static partial class Runner
{
    private static bool _initialized;
    private static int _linuxPerfFd = -1;

    static Runner()
    {
        AppDomain.CurrentDomain.ProcessExit += (sender, args) => CleanupLinuxPerf();
        AppDomain.CurrentDomain.DomainUnload += (sender, args) => CleanupLinuxPerf();
    }

    /// <summary>
    /// Initialize the runner by setting process/thread priority and warming up timing APIs.
    /// Call this once at the start of your benchmark session.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
            return;

        try
        {
            // Set high priority on all platforms (may require elevated permissions)
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
        }
        catch
        {
            // Ignore if we can't set priority (e.g., insufficient permissions)
        }

        // Initialize Linux perf event for CPU cycle counting
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            InitializeLinuxPerf();
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
        if (iterations <= 0)
            throw new ArgumentOutOfRangeException(nameof(iterations), "Iterations must be positive.");
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        // Force GC and record baseline counts
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

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
    {
        if (iterations <= 0)
            throw new ArgumentOutOfRangeException(nameof(iterations), "Iterations must be positive.");
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

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

    #region CPU Cycle Counter (Cross-platform)

    private static ulong GetCpuCycles()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ulong cycleCount = 0;
            QueryThreadCycleTime(GetCurrentThread(), ref cycleCount);
            return cycleCount;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && _linuxPerfFd >= 0)
        {
            return ReadLinuxPerfCounter();
        }

        // macOS and other platforms: not supported yet
        return 0;
    }

    #region Windows

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryThreadCycleTime(IntPtr threadHandle, ref ulong cycleTime);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentThread();

    #endregion

    #region Linux

    // perf_event_open constants
    private const int PERF_TYPE_HARDWARE = 0;
    private const int PERF_COUNT_HW_CPU_CYCLES = 0;
    private const int SYSCALL_PERF_EVENT_OPEN = 298; // x86_64

    [StructLayout(LayoutKind.Sequential)]
    private struct PerfEventAttr
    {
        public uint Type;
        public uint Size;
        public ulong Config;
        public ulong SamplePeriod;
        public ulong SampleType;
        public ulong ReadFormat;
        public ulong Flags;
        public uint WakeupEvents;
        public uint BpType;
        public ulong BpAddr;
        public ulong BpLen;
        public ulong BranchSampleType;
        public ulong SampleRegsUser;
        public ulong SampleStackUser;
        public int ClockId;
        public ulong SampleRegsIntr;
        public uint AuxWatermark;
        public ushort SampleMaxStack;
        public ushort Reserved2;
        public uint AuxSampleSize;
        public uint Reserved3;
    }

    [DllImport("libc", EntryPoint = "syscall", SetLastError = true)]
    private static extern int SyscallPerfEventOpen(
        int syscallNumber,
        ref PerfEventAttr attr,
        int pid,
        int cpu,
        int groupFd,
        ulong flags
    );

    [DllImport("libc", EntryPoint = "read", SetLastError = true)]
    private static extern IntPtr LinuxRead(int fd, out ulong buf, IntPtr count);

    [DllImport("libc", EntryPoint = "close", SetLastError = true)]
    private static extern int LinuxClose(int fd);

    private static void CleanupLinuxPerf()
    {
        if (_linuxPerfFd >= 0)
        {
            try
            {
                LinuxClose(_linuxPerfFd);
                _linuxPerfFd = -1;
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }
    }

    private static void InitializeLinuxPerf()
    {
        try
        {
            var attr = new PerfEventAttr
            {
                Type = PERF_TYPE_HARDWARE,
                Size = (uint)Marshal.SizeOf<PerfEventAttr>(),
                Config = PERF_COUNT_HW_CPU_CYCLES,
                Flags = 0 // disabled=0, inherit=0, exclude_kernel=0, exclude_hv=0
            };

            // pid=0 means current process, cpu=-1 means any CPU
            _linuxPerfFd = SyscallPerfEventOpen(SYSCALL_PERF_EVENT_OPEN, ref attr, 0, -1, -1, 0);

            if (_linuxPerfFd < 0)
            {
                // perf_event_open failed, likely due to permissions
                // Try reading /proc/sys/kernel/perf_event_paranoid
                // Value > 1 requires CAP_SYS_ADMIN or root
                _linuxPerfFd = -1;
            }
        }
        catch
        {
            _linuxPerfFd = -1;
        }
    }

    private static ulong ReadLinuxPerfCounter()
    {
        if (_linuxPerfFd < 0)
            return 0;

        try
        {
            var bytesRead = LinuxRead(_linuxPerfFd, out var value, (IntPtr)sizeof(ulong));
            return bytesRead == (IntPtr)sizeof(ulong) ? value : 0;
        }
        catch
        {
            return 0;
        }
    }

    #endregion

    #endregion
}
