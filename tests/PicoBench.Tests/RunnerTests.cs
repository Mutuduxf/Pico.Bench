namespace PicoBench.Tests;

/// <summary>
/// Tests for <see cref="Runner"/> low-level timing utilities.
/// Covers validation, timing accuracy, setup/teardown, and generic state overload.
/// </summary>
public class RunnerTests
{
    // ─── Time(int, Action) — basic overload ─────────────────────────

    [Test]
    [Property("Category", "Runner")]
    public async Task Time_BasicOverload_ReturnsTimingSample()
    {
        var sample = Runner.Time(10, () => { });

        await Assert.That(sample).IsNotNull();
        await Assert.That(sample.ElapsedNanoseconds).IsGreaterThanOrEqualTo(0);
        await Assert.That(sample.ElapsedMilliseconds).IsGreaterThanOrEqualTo(0);
        await Assert.That(sample.ElapsedTicks).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    [Property("Category", "Runner")]
    public async Task Time_BasicOverload_GcInfoIsPopulated()
    {
        var sample = Runner.Time(10, () => { });

        await Assert.That(sample.GcInfo).IsNotNull();
        await Assert.That(sample.GcInfo.Gen0).IsGreaterThanOrEqualTo(0);
    }

    // ─── Time(int, Action, Action?, Action?) — with setup/teardown ──

    [Test]
    [Property("Category", "Runner")]
    public async Task Time_WithSetupAndTeardown_ExecutesThem()
    {
        bool setupCalled = false;
        bool teardownCalled = false;

        Runner.Time(
            1,
            () => { },
            setup: () => setupCalled = true,
            teardown: () => teardownCalled = true
        );

        await Assert.That(setupCalled).IsTrue();
        await Assert.That(teardownCalled).IsTrue();
    }

    [Test]
    [Property("Category", "Runner")]
    public async Task Time_NullSetupAndTeardown_DoesNotThrow()
    {
        var sample = Runner.Time(1, () => { }, setup: null, teardown: null);

        await Assert.That(sample).IsNotNull();
    }

    [Test]
    [Property("Category", "Runner")]
    public async Task Time_ZeroIterations_ThrowsArgumentOutOfRangeException()
    {
        await Assert.That(() => Runner.Time(0, () => { })).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Property("Category", "Runner")]
    public async Task Time_NegativeIterations_ThrowsArgumentOutOfRangeException()
    {
        await Assert.That(() => Runner.Time(-1, () => { })).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Property("Category", "Runner")]
    public async Task Time_NullAction_ThrowsArgumentNullException()
    {
        await Assert.That(() => Runner.Time(1, (Action)null!)).Throws<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Runner")]
    public async Task Time_FullOverload_NullAction_ThrowsArgumentNullException()
    {
        await Assert
            .That(() => Runner.Time(1, (Action)null!, null, null))
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Runner")]
    public async Task Time_FullOverload_ZeroIterations_ThrowsArgumentOutOfRangeException()
    {
        await Assert
            .That(() => Runner.Time(0, () => { }, null, null))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Property("Category", "Runner")]
    public async Task Time_ExecutesCorrectNumberOfIterations()
    {
        int count = 0;
        Runner.Time(5, () => count++);

        await Assert.That(count).IsEqualTo(5);
    }

    [Test]
    [Property("Category", "Runner")]
    public async Task Time_ElapsedNanoseconds_MatchesMilliseconds()
    {
        var sample = Runner.Time(1, () => Thread.Sleep(5));

        // ElapsedMilliseconds should be approximately ElapsedNanoseconds / 1_000_000
        var expectedMs = sample.ElapsedNanoseconds / 1_000_000.0;
        var diff = Math.Abs(sample.ElapsedMilliseconds - expectedMs);

        await Assert.That(diff).IsLessThan(0.01); // allow small floating-point error
    }

    // ─── Time<TState>(int, TState, Action<TState>) — generic overload

    [Test]
    [Property("Category", "Runner")]
    public async Task TimeWithState_ReturnsTimingSample()
    {
        var sample = Runner.Time(
            10,
            42,
            s =>
            {
                var _ = s + 1;
            }
        );

        await Assert.That(sample).IsNotNull();
        await Assert.That(sample.ElapsedNanoseconds).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    [Property("Category", "Runner")]
    public async Task TimeWithState_ZeroIterations_ThrowsArgumentOutOfRangeException()
    {
        await Assert
            .That(() => Runner.Time(0, 42, (Action<int>)(s => { })))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Property("Category", "Runner")]
    public async Task TimeWithState_NegativeIterations_ThrowsArgumentOutOfRangeException()
    {
        await Assert
            .That(() => Runner.Time(-1, 42, (Action<int>)(s => { })))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Property("Category", "Runner")]
    public async Task TimeWithState_NullAction_ThrowsArgumentNullException()
    {
        await Assert.That(() => Runner.Time<int>(1, 42, null!)).Throws<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Runner")]
    public async Task TimeWithState_ExecutesCorrectNumberOfIterations()
    {
        int count = 0;
        Runner.Time(7, 0, s => count++);

        await Assert.That(count).IsEqualTo(7);
    }

    [Test]
    [Property("Category", "Runner")]
    public async Task TimeWithState_PassesStateCorrectly()
    {
        int sum = 0;
        Runner.Time(3, 10, s => sum += s);

        await Assert.That(sum).IsEqualTo(30);
    }

    // ─── Initialize ─────────────────────────────────────────────────

    [Test]
    [Property("Category", "Runner")]
    public async Task Initialize_CanBeCalledMultipleTimes_WithoutError()
    {
        // Initialize is idempotent via Lazy<T>
        Runner.Initialize();
        Runner.Initialize();

        // Verify the runner still works after multiple Initialize calls
        var sample = Runner.Time(1, () => { });
        await Assert.That(sample).IsNotNull();
    }
}
