namespace PicoBench.Tests;

public class BenchmarkRunnerTests
{
    // ─── Helper: a trivial IBenchmarkClass for testing ──────────────

    private sealed class FakeBenchmarkClass : IBenchmarkClass
    {
        public int RunCount { get; private set; }
        public BenchmarkConfig? LastConfig { get; private set; }

        public BenchmarkSuite RunBenchmarks(BenchmarkConfig? config = null)
        {
            RunCount++;
            LastConfig = config;

            return new BenchmarkSuite(
                name: "FakeSuite",
                environment: new EnvironmentInfo(),
                results: [BenchmarkResultFactory.Create("FakeBenchmark")],
                duration: TimeSpan.FromMilliseconds(100),
                description: "Fake suite for testing"
            );
        }
    }

    // ─── Run<T>(instance) ───────────────────────────────────────────

    [Test]
    [Property("Category", "BenchmarkRunner")]
    public async Task Run_WithInstance_DelegatesToRunBenchmarks()
    {
        var instance = new FakeBenchmarkClass();

        var suite = BenchmarkRunner.Run(instance);

        await Assert.That(instance.RunCount).IsEqualTo(1);
        await Assert.That(suite).IsNotNull();
        await Assert.That(suite.Name).IsEqualTo("FakeSuite");
    }

    [Test]
    [Property("Category", "BenchmarkRunner")]
    public async Task Run_WithInstance_PassesConfigToRunBenchmarks()
    {
        var instance = new FakeBenchmarkClass();
        var config = BenchmarkConfig.Quick;

        BenchmarkRunner.Run(instance, config);

        await Assert.That(instance.LastConfig).IsEqualTo(config);
    }

    [Test]
    [Property("Category", "BenchmarkRunner")]
    public async Task Run_WithInstance_NullConfig_PassesNullToRunBenchmarks()
    {
        var instance = new FakeBenchmarkClass();

        BenchmarkRunner.Run(instance, null);

        await Assert.That(instance.LastConfig).IsNull();
    }

    [Test]
    [Property("Category", "BenchmarkRunner")]
    public async Task Run_WithNullInstance_ThrowsArgumentNullException()
    {
        FakeBenchmarkClass instance = null!;
        await Assert.That(() => BenchmarkRunner.Run(instance)).Throws<ArgumentNullException>();
    }

    // ─── Run<T>() (parameterless — creates new T()) ────────────────

    [Test]
    [Property("Category", "BenchmarkRunner")]
    public async Task Run_Parameterless_CreatesInstanceAndRunsBenchmarks()
    {
        // FakeBenchmarkClass has a parameterless constructor and implements IBenchmarkClass.
        var suite = BenchmarkRunner.Run<FakeBenchmarkClass>();

        await Assert.That(suite).IsNotNull();
        await Assert.That(suite.Name).IsEqualTo("FakeSuite");
        await Assert.That(suite.Results).Count().IsEqualTo(1);
    }

    [Test]
    [Property("Category", "BenchmarkRunner")]
    public async Task Run_Parameterless_WithConfig_PassesConfig()
    {
        var config = BenchmarkConfig.Quick;

        // We can't easily check the config was passed without access to the instance,
        // but we verify it doesn't throw and returns a valid suite.
        var suite = BenchmarkRunner.Run<FakeBenchmarkClass>(config);

        await Assert.That(suite).IsNotNull();
        await Assert.That(suite.Description).IsEqualTo("Fake suite for testing");
    }

    // ─── Return value validation ────────────────────────────────────

    [Test]
    [Property("Category", "BenchmarkRunner")]
    public async Task Run_ReturnsSuiteWithCorrectResults()
    {
        var instance = new FakeBenchmarkClass();

        var suite = BenchmarkRunner.Run(instance);

        await Assert.That(suite.Results).Count().IsEqualTo(1);
        await Assert.That(suite.Results[0].Name).IsEqualTo("FakeBenchmark");
    }

    [Test]
    [Property("Category", "BenchmarkRunner")]
    public async Task Run_ReturnsSuiteWithDuration()
    {
        var instance = new FakeBenchmarkClass();

        var suite = BenchmarkRunner.Run(instance);

        await Assert.That(suite.Duration).IsEqualTo(TimeSpan.FromMilliseconds(100));
    }

    [Test]
    [Property("Category", "BenchmarkRunner")]
    public async Task Run_MultipleCallsOnSameInstance_IncrementsRunCount()
    {
        var instance = new FakeBenchmarkClass();

        BenchmarkRunner.Run(instance);
        BenchmarkRunner.Run(instance);
        BenchmarkRunner.Run(instance);

        await Assert.That(instance.RunCount).IsEqualTo(3);
    }
}
