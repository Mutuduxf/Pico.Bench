using Pico.Bench;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Pico.Bench.Tests.Attributes;

public class AttributeTests
{
    // ─── BenchmarkClassAttribute ────────────────────────────────────

    [Test]
    [Property("Category", "Attributes")]
    public async Task BenchmarkClassAttribute_DefaultDescription_IsNull()
    {
        var attr = new BenchmarkClassAttribute();

        await Assert.That(attr.Description).IsNull();
    }

    [Test]
    [Property("Category", "Attributes")]
    public async Task BenchmarkClassAttribute_SetDescription_ReturnsValue()
    {
        var attr = new BenchmarkClassAttribute { Description = "Test suite" };

        await Assert.That(attr.Description).IsEqualTo("Test suite");
    }

    [Test]
    [Property("Category", "Attributes")]
    public async Task BenchmarkClassAttribute_TargetsClassOnly()
    {
        var usage = (AttributeUsageAttribute)
            Attribute.GetCustomAttribute(
                typeof(BenchmarkClassAttribute),
                typeof(AttributeUsageAttribute)
            )!;

        await Assert.That(usage.ValidOn).IsEqualTo(AttributeTargets.Class);
        await Assert.That(usage.AllowMultiple).IsFalse();
        await Assert.That(usage.Inherited).IsFalse();
    }

    // ─── BenchmarkAttribute ─────────────────────────────────────────

    [Test]
    [Property("Category", "Attributes")]
    public async Task BenchmarkAttribute_DefaultBaseline_IsFalse()
    {
        var attr = new BenchmarkAttribute();

        await Assert.That(attr.Baseline).IsFalse();
    }

    [Test]
    [Property("Category", "Attributes")]
    public async Task BenchmarkAttribute_SetBaseline_ReturnsTrue()
    {
        var attr = new BenchmarkAttribute { Baseline = true };

        await Assert.That(attr.Baseline).IsTrue();
    }

    [Test]
    [Property("Category", "Attributes")]
    public async Task BenchmarkAttribute_DefaultDescription_IsNull()
    {
        var attr = new BenchmarkAttribute();

        await Assert.That(attr.Description).IsNull();
    }

    [Test]
    [Property("Category", "Attributes")]
    public async Task BenchmarkAttribute_SetDescription_ReturnsValue()
    {
        var attr = new BenchmarkAttribute { Description = "My benchmark" };

        await Assert.That(attr.Description).IsEqualTo("My benchmark");
    }

    [Test]
    [Property("Category", "Attributes")]
    public async Task BenchmarkAttribute_TargetsMethodOnly()
    {
        var usage = (AttributeUsageAttribute)
            Attribute.GetCustomAttribute(
                typeof(BenchmarkAttribute),
                typeof(AttributeUsageAttribute)
            )!;

        await Assert.That(usage.ValidOn).IsEqualTo(AttributeTargets.Method);
        await Assert.That(usage.AllowMultiple).IsFalse();
        await Assert.That(usage.Inherited).IsFalse();
    }

    // ─── GlobalSetupAttribute ───────────────────────────────────────

    [Test]
    [Property("Category", "Attributes")]
    public async Task GlobalSetupAttribute_TargetsMethodOnly()
    {
        var usage = (AttributeUsageAttribute)
            Attribute.GetCustomAttribute(
                typeof(GlobalSetupAttribute),
                typeof(AttributeUsageAttribute)
            )!;

        await Assert.That(usage.ValidOn).IsEqualTo(AttributeTargets.Method);
        await Assert.That(usage.AllowMultiple).IsFalse();
        await Assert.That(usage.Inherited).IsFalse();
    }

    [Test]
    [Property("Category", "Attributes")]
    public async Task GlobalSetupAttribute_CanBeInstantiated()
    {
        var attr = new GlobalSetupAttribute();

        await Assert.That(attr).IsNotNull();
    }

    // ─── GlobalCleanupAttribute ─────────────────────────────────────

    [Test]
    [Property("Category", "Attributes")]
    public async Task GlobalCleanupAttribute_TargetsMethodOnly()
    {
        var usage = (AttributeUsageAttribute)
            Attribute.GetCustomAttribute(
                typeof(GlobalCleanupAttribute),
                typeof(AttributeUsageAttribute)
            )!;

        await Assert.That(usage.ValidOn).IsEqualTo(AttributeTargets.Method);
        await Assert.That(usage.AllowMultiple).IsFalse();
        await Assert.That(usage.Inherited).IsFalse();
    }

    // ─── IterationSetupAttribute ────────────────────────────────────

    [Test]
    [Property("Category", "Attributes")]
    public async Task IterationSetupAttribute_TargetsMethodOnly()
    {
        var usage = (AttributeUsageAttribute)
            Attribute.GetCustomAttribute(
                typeof(IterationSetupAttribute),
                typeof(AttributeUsageAttribute)
            )!;

        await Assert.That(usage.ValidOn).IsEqualTo(AttributeTargets.Method);
        await Assert.That(usage.AllowMultiple).IsFalse();
        await Assert.That(usage.Inherited).IsFalse();
    }

    // ─── IterationCleanupAttribute ──────────────────────────────────

    [Test]
    [Property("Category", "Attributes")]
    public async Task IterationCleanupAttribute_TargetsMethodOnly()
    {
        var usage = (AttributeUsageAttribute)
            Attribute.GetCustomAttribute(
                typeof(IterationCleanupAttribute),
                typeof(AttributeUsageAttribute)
            )!;

        await Assert.That(usage.ValidOn).IsEqualTo(AttributeTargets.Method);
        await Assert.That(usage.AllowMultiple).IsFalse();
        await Assert.That(usage.Inherited).IsFalse();
    }

    // ─── ParamsAttribute ────────────────────────────────────────────

    [Test]
    [Property("Category", "Attributes")]
    public async Task ParamsAttribute_StoresValues()
    {
        var attr = new ParamsAttribute(10, 100, 1000);

        await Assert.That(attr.Values).Count().IsEqualTo(3);
        await Assert.That(attr.Values[0]).IsEqualTo(10);
        await Assert.That(attr.Values[1]).IsEqualTo(100);
        await Assert.That(attr.Values[2]).IsEqualTo(1000);
    }

    [Test]
    [Property("Category", "Attributes")]
    public async Task ParamsAttribute_EmptyValues_IsAllowed()
    {
        var attr = new ParamsAttribute();

        await Assert.That(attr.Values).Count().IsEqualTo(0);
    }

    [Test]
    [Property("Category", "Attributes")]
    public async Task ParamsAttribute_NullValues_ThrowsArgumentNullException()
    {
        await Assert.That(() => new ParamsAttribute(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    [Property("Category", "Attributes")]
    public async Task ParamsAttribute_TargetsPropertyAndField()
    {
        var usage = (AttributeUsageAttribute)
            Attribute.GetCustomAttribute(typeof(ParamsAttribute), typeof(AttributeUsageAttribute))!;

        await Assert
            .That(usage.ValidOn)
            .IsEqualTo(AttributeTargets.Property | AttributeTargets.Field);
        await Assert.That(usage.AllowMultiple).IsFalse();
        await Assert.That(usage.Inherited).IsFalse();
    }

    [Test]
    [Property("Category", "Attributes")]
    public async Task ParamsAttribute_MixedTypes_StoresCorrectly()
    {
        var attr = new ParamsAttribute("hello", 42, 3.14, true);

        await Assert.That(attr.Values).Count().IsEqualTo(4);
        await Assert.That(attr.Values[0]).IsEqualTo("hello");
        await Assert.That(attr.Values[1]).IsEqualTo(42);
        await Assert.That(attr.Values[2]).IsEqualTo(3.14);
        await Assert.That(attr.Values[3]).IsEqualTo(true);
    }
}
