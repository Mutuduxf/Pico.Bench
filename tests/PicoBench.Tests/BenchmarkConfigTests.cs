namespace PicoBench.Tests;

/// <summary>
/// Tests for <see cref="BenchmarkConfig"/> covering validation, defaults, and preset configs.
/// </summary>
public class BenchmarkConfigTests
{
    // ─── Default values ─────────────────────────────────────────────

    [Test]
    [Property("Category", "BenchmarkConfig")]
    public async Task Default_HasExpectedValues()
    {
        var config = BenchmarkConfig.Default;

        await Assert.That(config.WarmupIterations).IsEqualTo(1000);
        await Assert.That(config.SampleCount).IsEqualTo(100);
        await Assert.That(config.IterationsPerSample).IsEqualTo(10000);
        await Assert.That(config.RetainSamples).IsFalse();
    }

    [Test]
    [Property("Category", "BenchmarkConfig")]
    public async Task Quick_HasExpectedValues()
    {
        var config = BenchmarkConfig.Quick;

        await Assert.That(config.WarmupIterations).IsEqualTo(100);
        await Assert.That(config.SampleCount).IsEqualTo(10);
        await Assert.That(config.IterationsPerSample).IsEqualTo(1000);
    }

    [Test]
    [Property("Category", "BenchmarkConfig")]
    public async Task Precise_HasExpectedValues()
    {
        var config = BenchmarkConfig.Precise;

        await Assert.That(config.WarmupIterations).IsEqualTo(5000);
        await Assert.That(config.SampleCount).IsEqualTo(200);
        await Assert.That(config.IterationsPerSample).IsEqualTo(50000);
    }

    // ─── Singleton behavior ─────────────────────────────────────────

    [Test]
    [Property("Category", "BenchmarkConfig")]
    public async Task Default_ReturnsSameInstance()
    {
        var a = BenchmarkConfig.Default;
        var b = BenchmarkConfig.Default;

        await Assert.That(ReferenceEquals(a, b)).IsTrue();
    }

    [Test]
    [Property("Category", "BenchmarkConfig")]
    public async Task Quick_ReturnsSameInstance()
    {
        var a = BenchmarkConfig.Quick;
        var b = BenchmarkConfig.Quick;

        await Assert.That(ReferenceEquals(a, b)).IsTrue();
    }

    [Test]
    [Property("Category", "BenchmarkConfig")]
    public async Task Precise_ReturnsSameInstance()
    {
        var a = BenchmarkConfig.Precise;
        var b = BenchmarkConfig.Precise;

        await Assert.That(ReferenceEquals(a, b)).IsTrue();
    }

    // ─── Custom valid values ────────────────────────────────────────

    [Test]
    [Property("Category", "BenchmarkConfig")]
    public async Task Custom_ZeroWarmupIterations_IsAllowed()
    {
        var config = new BenchmarkConfig { WarmupIterations = 0 };

        await Assert.That(config.WarmupIterations).IsEqualTo(0);
    }

    [Test]
    [Property("Category", "BenchmarkConfig")]
    public async Task Custom_RetainSamplesTrue_IsAllowed()
    {
        var config = new BenchmarkConfig { RetainSamples = true };

        await Assert.That(config.RetainSamples).IsTrue();
    }

    // ─── Validation: WarmupIterations ───────────────────────────────

    [Test]
    [Property("Category", "BenchmarkConfig")]
    public async Task WarmupIterations_Negative_ThrowsArgumentOutOfRangeException()
    {
        await Assert
            .That(() => new BenchmarkConfig { WarmupIterations = -1 })
            .Throws<ArgumentOutOfRangeException>();
    }

    // ─── Validation: SampleCount ────────────────────────────────────

    [Test]
    [Property("Category", "BenchmarkConfig")]
    public async Task SampleCount_Zero_ThrowsArgumentOutOfRangeException()
    {
        await Assert
            .That(() => new BenchmarkConfig { SampleCount = 0 })
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Property("Category", "BenchmarkConfig")]
    public async Task SampleCount_Negative_ThrowsArgumentOutOfRangeException()
    {
        await Assert
            .That(() => new BenchmarkConfig { SampleCount = -5 })
            .Throws<ArgumentOutOfRangeException>();
    }

    // ─── Validation: IterationsPerSample ────────────────────────────

    [Test]
    [Property("Category", "BenchmarkConfig")]
    public async Task IterationsPerSample_Zero_ThrowsArgumentOutOfRangeException()
    {
        await Assert
            .That(() => new BenchmarkConfig { IterationsPerSample = 0 })
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Property("Category", "BenchmarkConfig")]
    public async Task IterationsPerSample_Negative_ThrowsArgumentOutOfRangeException()
    {
        await Assert
            .That(() => new BenchmarkConfig { IterationsPerSample = -10 })
            .Throws<ArgumentOutOfRangeException>();
    }
}
