using System.Collections.Immutable;
using Pico.Bench.Generators;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Pico.Bench.Tests.Generators;

public class ModelsTests
{
    // ═══════════════════════════════════════════════════════════════════
    //  BenchmarkMethodModel
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkMethodModel_EqualInstances_AreEqual()
    {
        var a = new BenchmarkMethodModel
        {
            Name = "Run",
            IsBaseline = true,
            Description = "desc"
        };
        var b = new BenchmarkMethodModel
        {
            Name = "Run",
            IsBaseline = true,
            Description = "desc"
        };

        await Assert.That(a.Equals(b)).IsTrue();
        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkMethodModel_DifferentName_AreNotEqual()
    {
        var a = new BenchmarkMethodModel { Name = "RunA" };
        var b = new BenchmarkMethodModel { Name = "RunB" };

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkMethodModel_DifferentBaseline_AreNotEqual()
    {
        var a = new BenchmarkMethodModel { Name = "Run", IsBaseline = true };
        var b = new BenchmarkMethodModel { Name = "Run", IsBaseline = false };

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkMethodModel_DifferentDescription_AreNotEqual()
    {
        var a = new BenchmarkMethodModel { Name = "Run", Description = "A" };
        var b = new BenchmarkMethodModel { Name = "Run", Description = "B" };

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkMethodModel_NullDescription_VsNonNull_AreNotEqual()
    {
        var a = new BenchmarkMethodModel { Name = "Run", Description = null };
        var b = new BenchmarkMethodModel { Name = "Run", Description = "X" };

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkMethodModel_ComparedToNull_ReturnsFalse()
    {
        var a = new BenchmarkMethodModel { Name = "Run" };

        await Assert.That(a.Equals((BenchmarkMethodModel?)null)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkMethodModel_EqualsObject_WorksCorrectly()
    {
        var a = new BenchmarkMethodModel { Name = "Run", IsBaseline = true };
        object b = new BenchmarkMethodModel { Name = "Run", IsBaseline = true };

        await Assert.That(a.Equals(b)).IsTrue();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkMethodModel_EqualsObject_WrongType_ReturnsFalse()
    {
        var a = new BenchmarkMethodModel { Name = "Run" };

        await Assert.That(a.Equals("not a model")).IsFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ParamsPropertyModel
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    [Property("Category", "Models")]
    public async Task ParamsPropertyModel_EqualInstances_AreEqual()
    {
        var a = new ParamsPropertyModel
        {
            Name = "N",
            TypeFullName = "int",
            FormattedValues = ImmutableArray.Create("10", "100"),
        };
        var b = new ParamsPropertyModel
        {
            Name = "N",
            TypeFullName = "int",
            FormattedValues = ImmutableArray.Create("10", "100"),
        };

        await Assert.That(a.Equals(b)).IsTrue();
        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    [Test]
    [Property("Category", "Models")]
    public async Task ParamsPropertyModel_DifferentName_AreNotEqual()
    {
        var a = new ParamsPropertyModel
        {
            Name = "N",
            TypeFullName = "int",
            FormattedValues = ImmutableArray.Create("10")
        };
        var b = new ParamsPropertyModel
        {
            Name = "M",
            TypeFullName = "int",
            FormattedValues = ImmutableArray.Create("10")
        };

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task ParamsPropertyModel_DifferentType_AreNotEqual()
    {
        var a = new ParamsPropertyModel
        {
            Name = "N",
            TypeFullName = "int",
            FormattedValues = ImmutableArray.Create("10")
        };
        var b = new ParamsPropertyModel
        {
            Name = "N",
            TypeFullName = "long",
            FormattedValues = ImmutableArray.Create("10")
        };

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task ParamsPropertyModel_DifferentValues_AreNotEqual()
    {
        var a = new ParamsPropertyModel
        {
            Name = "N",
            TypeFullName = "int",
            FormattedValues = ImmutableArray.Create("10")
        };
        var b = new ParamsPropertyModel
        {
            Name = "N",
            TypeFullName = "int",
            FormattedValues = ImmutableArray.Create("20")
        };

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task ParamsPropertyModel_DifferentValueCount_AreNotEqual()
    {
        var a = new ParamsPropertyModel
        {
            Name = "N",
            TypeFullName = "int",
            FormattedValues = ImmutableArray.Create("10")
        };
        var b = new ParamsPropertyModel
        {
            Name = "N",
            TypeFullName = "int",
            FormattedValues = ImmutableArray.Create("10", "20")
        };

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task ParamsPropertyModel_ComparedToNull_ReturnsFalse()
    {
        var a = new ParamsPropertyModel { Name = "N", TypeFullName = "int" };

        await Assert.That(a.Equals((ParamsPropertyModel?)null)).IsFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  BenchmarkClassModel
    // ═══════════════════════════════════════════════════════════════════

    private static BenchmarkClassModel CreateModel(
        string className = "Test",
        string? ns = "NS",
        string accessModifier = "public",
        string? description = null,
        string? globalSetup = null,
        string? globalCleanup = null,
        string? iterSetup = null,
        string? iterCleanup = null,
        ImmutableArray<BenchmarkMethodModel>? methods = null,
        ImmutableArray<ParamsPropertyModel>? paramsProps = null
    )
    {
        return new BenchmarkClassModel
        {
            Namespace = ns,
            ClassName = className,
            AccessModifier = accessModifier,
            Description = description,
            GlobalSetupMethod = globalSetup,
            GlobalCleanupMethod = globalCleanup,
            IterationSetupMethod = iterSetup,
            IterationCleanupMethod = iterCleanup,
            Methods = methods ?? ImmutableArray.Create(new BenchmarkMethodModel { Name = "Run" }),
            ParamsProperties = paramsProps ?? ImmutableArray<ParamsPropertyModel>.Empty,
        };
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkClassModel_EqualInstances_AreEqual()
    {
        var a = CreateModel();
        var b = CreateModel();

        await Assert.That(a.Equals(b)).IsTrue();
        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkClassModel_DifferentClassName_AreNotEqual()
    {
        var a = CreateModel(className: "A");
        var b = CreateModel(className: "B");

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkClassModel_DifferentNamespace_AreNotEqual()
    {
        var a = CreateModel(ns: "NS1");
        var b = CreateModel(ns: "NS2");

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkClassModel_NullVsNonNullNamespace_AreNotEqual()
    {
        var a = CreateModel(ns: null);
        var b = CreateModel(ns: "NS");

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkClassModel_DifferentAccessModifier_AreNotEqual()
    {
        var a = CreateModel(accessModifier: "public");
        var b = CreateModel(accessModifier: "internal");

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkClassModel_DifferentDescription_AreNotEqual()
    {
        var a = CreateModel(description: "A");
        var b = CreateModel(description: "B");

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkClassModel_DifferentGlobalSetup_AreNotEqual()
    {
        var a = CreateModel(globalSetup: "SetupA");
        var b = CreateModel(globalSetup: "SetupB");

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkClassModel_DifferentGlobalCleanup_AreNotEqual()
    {
        var a = CreateModel(globalCleanup: "CleanA");
        var b = CreateModel(globalCleanup: "CleanB");

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkClassModel_DifferentIterSetup_AreNotEqual()
    {
        var a = CreateModel(iterSetup: "IA");
        var b = CreateModel(iterSetup: "IB");

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkClassModel_DifferentIterCleanup_AreNotEqual()
    {
        var a = CreateModel(iterCleanup: "CA");
        var b = CreateModel(iterCleanup: "CB");

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkClassModel_DifferentMethods_AreNotEqual()
    {
        var a = CreateModel(
            methods: ImmutableArray.Create(new BenchmarkMethodModel { Name = "A" })
        );
        var b = CreateModel(
            methods: ImmutableArray.Create(new BenchmarkMethodModel { Name = "B" })
        );

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkClassModel_DifferentParams_AreNotEqual()
    {
        var p1 = ImmutableArray.Create(
            new ParamsPropertyModel
            {
                Name = "N",
                TypeFullName = "int",
                FormattedValues = ImmutableArray.Create("10")
            }
        );
        var p2 = ImmutableArray.Create(
            new ParamsPropertyModel
            {
                Name = "M",
                TypeFullName = "int",
                FormattedValues = ImmutableArray.Create("10")
            }
        );

        var a = CreateModel(paramsProps: p1);
        var b = CreateModel(paramsProps: p2);

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkClassModel_ComparedToNull_ReturnsFalse()
    {
        var a = CreateModel();

        await Assert.That(a.Equals((BenchmarkClassModel?)null)).IsFalse();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkClassModel_SameReference_ReturnsTrue()
    {
        var a = CreateModel();

        await Assert.That(a.Equals(a)).IsTrue();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkClassModel_EqualsObject_WorksCorrectly()
    {
        var a = CreateModel();
        object b = CreateModel();

        await Assert.That(a.Equals(b)).IsTrue();
    }

    [Test]
    [Property("Category", "Models")]
    public async Task BenchmarkClassModel_EqualsObject_WrongType_ReturnsFalse()
    {
        var a = CreateModel();

        await Assert.That(a.Equals("not a model")).IsFalse();
    }
}
