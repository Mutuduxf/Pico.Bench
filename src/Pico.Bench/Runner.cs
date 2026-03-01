namespace Pico.Bench;

/// <summary>
/// Low-level timing utilities for benchmark measurements.
/// Provides high-precision timing with GC and CPU cycle tracking.
/// </summary>
public static partial class Runner
{
    private static readonly Lazy<bool> Initializer =
        new(InitializeCore, LazyThreadSafetyMode.ExecutionAndPublication);
    private static volatile int _linuxPerfFd = -1;

    /// <summary>
    /// Initialize the runner by setting process/thread priority and warming up timing APIs.
    /// Call this once at the start of your benchmark session.
    /// Thread-safe: uses <see cref="Lazy{T}"/> to guarantee single initialization.
    /// </summary>
    public static void Initialize()
    {
        _ = Initializer.Value;
    }

    private static bool InitializeCore()
    {
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

        return true;
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
    public static TimingSample Time(int iterations, Action action, Action? setup, Action? teardown)
    {
        ValidateIterations(iterations);
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var gcBaseline = GetGcBaselineCounts();

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

        return CreateSample(watch, cycleStart, cycleEnd, gcBaseline);
    }

    /// <summary>
    /// Run a timed measurement with state passed to the action (avoids closure allocation).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimingSample Time<TState>(int iterations, TState state, Action<TState> action)
    {
        ValidateIterations(iterations);
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var gcBaseline = GetGcBaselineCounts();

        var cycleStart = GetCpuCycles();
        var watch = Stopwatch.StartNew();

        for (var i = 0; i < iterations; i++)
            action(state);

        watch.Stop();
        var cycleEnd = GetCpuCycles();

        return CreateSample(watch, cycleStart, cycleEnd, gcBaseline);
    }

    /// <summary>
    /// Validate that the iterations parameter is positive.
    /// </summary>
    private static void ValidateIterations(int iterations)
    {
        if (iterations <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(iterations),
                "Iterations must be positive."
            );
    }

    /// <summary>
    /// Create a <see cref="TimingSample"/> from stopwatch and CPU cycle measurements.
    /// </summary>
    private static TimingSample CreateSample(
        Stopwatch watch,
        ulong cycleStart,
        ulong cycleEnd,
        long[] gcBaseline
    )
    {
        var elapsedTicks = watch.ElapsedTicks;
        var elapsedNs = elapsedTicks * (1_000_000_000.0 / Stopwatch.Frequency);

        return new TimingSample
        {
            ElapsedNanoseconds = elapsedNs,
            ElapsedMilliseconds = elapsedNs / 1_000_000.0,
            ElapsedTicks = elapsedTicks,
            CpuCycles = cycleEnd - cycleStart,
            GcInfo = CalculateGcDelta(gcBaseline)
        };
    }

    #region GC Helpers

    private static long[] GetGcBaselineCounts()
    {
        // Record current GC counts without forcing a collection.
        // Forced GC.Collect per sample introduces significant overhead and distorts
        // the timing results. The caller (Benchmark) should perform a single forced
        // GC before the entire collection loop if a clean baseline is desired.
        var gcCounts = new long[GC.MaxGeneration + 1];
        for (int i = 0; i <= GC.MaxGeneration; i++)
            gcCounts[i] = GC.CollectionCount(i);

        return gcCounts;
    }

    private static GcInfo CalculateGcDelta(long[] baselineCounts)
    {
        // Helper to compute GC count delta with handling of 32-bit wraparound
        static int ComputeDelta(int current, long baseline)
        {
            // Treat both as unsigned 32-bit integers to handle wraparound
            var currentU = unchecked((uint)current);
            var baselineU = unchecked((uint)baseline);
            var deltaU = currentU - baselineU; // Underflow is fine, gives correct difference modulo 2^32
            return unchecked((int)deltaU); // Convert back to signed int (negative values possible but unlikely for typical benchmarks)
        }

        var gen0 = ComputeDelta(GC.CollectionCount(0), baselineCounts[0]);
        var gen1 =
            GC.MaxGeneration >= 1 ? ComputeDelta(GC.CollectionCount(1), baselineCounts[1]) : 0;
        var gen2 =
            GC.MaxGeneration >= 2 ? ComputeDelta(GC.CollectionCount(2), baselineCounts[2]) : 0;

        // Ensure non-negative (should be, but guard against extreme cases)
        if (gen0 < 0)
            gen0 = 0;
        if (gen1 < 0)
            gen1 = 0;
        if (gen2 < 0)
            gen2 = 0;

        return new GcInfo
        {
            Gen0 = gen0,
            Gen1 = gen1,
            Gen2 = gen2
        };
    }

    #endregion

    #region CPU Cycle Counter (Cross-platform)

    private static ulong GetCpuCycles()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ulong cycleCount = 0;
            if (QueryThreadCycleTime(GetCurrentThread(), ref cycleCount))
                return cycleCount;
            return 0;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && _linuxPerfFd >= 0)
        {
            return ReadLinuxPerfCounter();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS: mach_absolute_time is a monotonic clock, not real CPU cycles.
            // CpuCyclesPerOp will not be meaningful on this platform.
            return GetMacOsMonotonicTime();
        }

        // Other platforms: not supported yet
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
    private const int PerfTypeHardware = 0;
    private const int PerfCountHwCpuCycles = 0;

    // System call numbers for perf_event_open across architectures
    private static int GetPerfEventOpenSyscallNumber()
    {
        var architecture = RuntimeInformation.ProcessArchitecture;
        return architecture switch
        {
            Architecture.X64 => 298, // x86_64
            Architecture.X86 => 336, // x86 (32-bit)
            Architecture.Arm64 => 241, // ARM64
            Architecture.Arm => 364, // ARM (32-bit)
            _ => -1 // Unknown architecture
        };
    }

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
    private static extern unsafe IntPtr LinuxRead(int fd, byte* buf, IntPtr count);

    [DllImport("libc", EntryPoint = "close", SetLastError = true)]
    private static extern int LinuxClose(int fd);

    private static void CleanupLinuxPerf()
    {
        if (_linuxPerfFd >= 0)
        {
            try
            {
                LinuxClose(_linuxPerfFd);
            }
            catch
            {
                // Ignore errors during cleanup
            }
            finally
            {
                _linuxPerfFd = -1;
            }
        }
    }

    private static void InitializeLinuxPerf()
    {
        // Register cleanup handlers on demand (only when Linux perf is actually used)
        AppDomain.CurrentDomain.ProcessExit += (_, _) => CleanupLinuxPerf();
        AppDomain.CurrentDomain.DomainUnload += (_, _) => CleanupLinuxPerf();

        try
        {
            // Check perf_event_paranoid value first
            if (File.Exists("/proc/sys/kernel/perf_event_paranoid"))
            {
                var paranoidValue = File.ReadAllText("/proc/sys/kernel/perf_event_paranoid").Trim();
                if (int.TryParse(paranoidValue, out var value) && value > 1)
                {
                    // perf_event_paranoid > 1 requires CAP_SYS_ADMIN or root
                    // CPU cycle counting will not be available
                    _linuxPerfFd = -1;
                    return;
                }
            }

            var attr = new PerfEventAttr
            {
                Type = PerfTypeHardware,
                Size = (uint)Marshal.SizeOf<PerfEventAttr>(),
                Config = PerfCountHwCpuCycles,
                Flags = 0 // disabled=0, inherit=0, exclude_kernel=0, exclude_hv=0
            };

            // pid=0 means current process, cpu=-1 means any CPU
            var syscallNumber = GetPerfEventOpenSyscallNumber();
            if (syscallNumber < 0)
            {
                _linuxPerfFd = -1;
                return;
            }
            _linuxPerfFd = SyscallPerfEventOpen(syscallNumber, ref attr, 0, -1, -1, 0);

            if (_linuxPerfFd < 0)
            {
                // perf_event_open failed due to permissions or other reasons
                _linuxPerfFd = -1;
            }
        }
        catch (Exception ex)
            when (ex is UnauthorizedAccessException or IOException or DllNotFoundException)
        {
            // Expected exceptions: permission denied, file not found, libc not available
            _linuxPerfFd = -1;
        }
        catch
        {
            // Catch any other unexpected exceptions
            _linuxPerfFd = -1;
        }
    }

    private static unsafe ulong ReadLinuxPerfCounter()
    {
        if (_linuxPerfFd < 0)
            return 0;

        try
        {
            byte* buffer = stackalloc byte[sizeof(ulong)];
            var bytesRead = LinuxRead(_linuxPerfFd, buffer, (IntPtr)sizeof(ulong));
            if (bytesRead == (IntPtr)sizeof(ulong))
            {
                // Convert bytes to ulong (assuming little-endian)
                ulong value = 0;
                for (var i = 0; i < sizeof(ulong); i++)
                {
                    value |= ((ulong)buffer[i]) << (i * 8);
                }
                return value;
            }

            // Read failed, disable perf counter for future calls
            CleanupLinuxPerf();
            return 0;
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            // File descriptor closed or IO error, disable perf counter
            CleanupLinuxPerf();
            return 0;
        }
        catch
        {
            // Any other error
            return 0;
        }
    }

    #endregion

    #region macOS

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern ulong mach_absolute_time();

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern int mach_timebase_info(out MachTimebaseInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private struct MachTimebaseInfo
    {
        public uint Numer;
        public uint Denom;
    }

    /// <summary>
    /// Returns mach absolute time on macOS. Note: this is a monotonic wall-clock
    /// timestamp, NOT actual CPU hardware cycle counts. The returned value is used
    /// as a best-effort proxy for relative timing; the <c>CpuCyclesPerOp</c> metric
    /// is therefore not meaningful on macOS and should be ignored.
    /// </summary>
    private static ulong GetMacOsMonotonicTime()
    {
        try
        {
            return mach_absolute_time();
        }
        catch
        {
            return 0;
        }
    }

    #endregion

    #endregion
}
