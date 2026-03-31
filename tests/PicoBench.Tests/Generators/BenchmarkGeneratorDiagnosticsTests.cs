namespace PicoBench.Tests.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public class BenchmarkGeneratorDiagnosticsTests
{
    [Test]
    [Property("Category", "Generators")]
    public async Task NonPartialBenchmarkClass_ReportsDiagnostic()
    {
        var result = RunGenerator(
            """
            using PicoBench;

            [BenchmarkClass]
            public class BadBench
            {
                [Benchmark]
                public void Work() { }
            }
            """
        );

        await Assert.That(result.Diagnostics.Any(d => d.Id == "PBGEN001")).IsTrue();
        await Assert.That(result.GeneratedSources.Length).IsEqualTo(0);
    }

    [Test]
    [Property("Category", "Generators")]
    public async Task BenchmarkClassWithoutBenchmarks_ReportsDiagnostic()
    {
        var result = RunGenerator(
            """
            using PicoBench;

            [BenchmarkClass]
            public partial class EmptyBench
            {
                public void Helper() { }
            }
            """
        );

        await Assert.That(result.Diagnostics.Any(d => d.Id == "PBGEN002")).IsTrue();
    }

    [Test]
    [Property("Category", "Generators")]
    public async Task BenchmarkMethodWithParameters_ReportsDiagnostic()
    {
        var result = RunGenerator(
            """
            using PicoBench;

            [BenchmarkClass]
            public partial class BadBench
            {
                [Benchmark]
                public void Work(int value) { }
            }
            """
        );

        await Assert.That(result.Diagnostics.Any(d => d.Id == "PBGEN003")).IsTrue();
        await Assert.That(result.GeneratedSources.Length).IsEqualTo(0);
    }

    [Test]
    [Property("Category", "Generators")]
    public async Task DuplicateBaseline_ReportsDiagnostic()
    {
        var result = RunGenerator(
            """
            using PicoBench;

            [BenchmarkClass]
            public partial class BadBench
            {
                [Benchmark(Baseline = true)]
                public void A() { }

                [Benchmark(Baseline = true)]
                public void B() { }
            }
            """
        );

        await Assert.That(result.Diagnostics.Any(d => d.Id == "PBGEN005")).IsTrue();
    }

    [Test]
    [Property("Category", "Generators")]
    public async Task InvalidGlobalSetupSignature_ReportsDiagnostic()
    {
        var result = RunGenerator(
            """
            using PicoBench;

            [BenchmarkClass]
            public partial class BadBench
            {
                [GlobalSetup]
                public int Setup() => 42;

                [Benchmark]
                public void Work() { }
            }
            """
        );

        await Assert.That(result.Diagnostics.Any(d => d.Id == "PBGEN004")).IsTrue();
    }

    [Test]
    [Property("Category", "Generators")]
    public async Task DuplicateIterationSetup_ReportsDiagnostic()
    {
        var result = RunGenerator(
            """
            using PicoBench;

            [BenchmarkClass]
            public partial class BadBench
            {
                [IterationSetup]
                public void SetupA() { }

                [IterationSetup]
                public void SetupB() { }

                [Benchmark]
                public void Work() { }
            }
            """
        );

        await Assert.That(result.Diagnostics.Any(d => d.Id == "PBGEN006")).IsTrue();
    }

    [Test]
    [Property("Category", "Generators")]
    public async Task StaticBenchmarkMethod_ReportsDiagnostic()
    {
        var result = RunGenerator(
            """
            using PicoBench;

            [BenchmarkClass]
            public partial class BadBench
            {
                [Benchmark]
                public static void Work() { }
            }
            """
        );

        await Assert.That(result.Diagnostics.Any(d => d.Id == "PBGEN003")).IsTrue();
    }

    [Test]
    [Property("Category", "Generators")]
    public async Task GenericBenchmarkMethod_ReportsDiagnostic()
    {
        var result = RunGenerator(
            """
            using PicoBench;

            [BenchmarkClass]
            public partial class BadBench
            {
                [Benchmark]
                public void Work<T>() { }
            }
            """
        );

        await Assert.That(result.Diagnostics.Any(d => d.Id == "PBGEN003")).IsTrue();
    }

    [Test]
    [Property("Category", "Generators")]
    public async Task StaticParamsField_ReportsDiagnostic()
    {
        var result = RunGenerator(
            """
            using PicoBench;

            [BenchmarkClass]
            public partial class BadBench
            {
                [Params(1, 2)]
                public static int N;

                [Benchmark]
                public void Work() { }
            }
            """
        );

        await Assert.That(result.Diagnostics.Any(d => d.Id == "PBGEN007")).IsTrue();
    }

    [Test]
    [Property("Category", "Generators")]
    public async Task EnumParamsValue_GeneratesEnumCastLiteral()
    {
        var result = RunGenerator(
            """
            using PicoBench;

            public enum TestMode
            {
                Fast = 1,
                Slow = 2
            }

            [BenchmarkClass]
            public partial class GoodBench
            {
                [Params(TestMode.Fast, TestMode.Slow)]
                public TestMode Mode { get; set; }

                [Benchmark]
                public void Work() { }
            }
            """
        );

        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(result.GeneratedSources[0]).Contains("(global::TestMode)1");
        await Assert.That(result.GeneratedSources[0]).Contains("(global::TestMode)2");
    }

    [Test]
    [Property("Category", "Generators")]
    public async Task ReadOnlyParamsProperty_ReportsDiagnostic()
    {
        var result = RunGenerator(
            """
            using PicoBench;

            [BenchmarkClass]
            public partial class BadBench
            {
                [Params(1, 2)]
                public int N { get; }

                [Benchmark]
                public void Work() { }
            }
            """
        );

        await Assert.That(result.Diagnostics.Any(d => d.Id == "PBGEN007")).IsTrue();
    }

    [Test]
    [Property("Category", "Generators")]
    public async Task IncompatibleParamsValue_ReportsDiagnostic()
    {
        var result = RunGenerator(
            """
            using PicoBench;

            [BenchmarkClass]
            public partial class BadBench
            {
                [Params("oops")]
                public int N { get; set; }

                [Benchmark]
                public void Work() { }
            }
            """
        );

        await Assert.That(result.Diagnostics.Any(d => d.Id == "PBGEN008")).IsTrue();
    }

    [Test]
    [Property("Category", "Generators")]
    public async Task ValidBenchmarkClass_GeneratesSourceWithoutErrors()
    {
        var result = RunGenerator(
            """
            using PicoBench;

            [BenchmarkClass(Description = "Suite")]
            public partial class GoodBench
            {
                [Params(1, 2)]
                public int N { get; set; }

                [GlobalSetup]
                public void Setup() { }

                [Benchmark(Baseline = true)]
                public void Baseline() { }

                [Benchmark]
                public void Candidate() { }
            }
            """
        );

        await Assert.That(result.Diagnostics).IsEmpty();
        await Assert.That(result.GeneratedSources.Length).IsEqualTo(1);
        await Assert.That(result.GeneratedSources[0]).Contains("partial class GoodBench");
    }

    private static GeneratorRunResultData RunGenerator(string source)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTests",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new BenchmarkGenerator());
        driver = driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatorResult = runResult.Results.Single();

        return new GeneratorRunResultData(
            generatorResult.Diagnostics,
            generatorResult.GeneratedSources.Select(static s => s.SourceText.ToString()).ToArray()
        );
    }

    private static MetadataReference[] GetMetadataReferences()
    {
        var paths = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            !.Split(Path.PathSeparator)
            .Concat([typeof(BenchmarkClassAttribute).Assembly.Location])
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return paths.Select(static path => MetadataReference.CreateFromFile(path)).ToArray();
    }

    private sealed class GeneratorRunResultData(
        ImmutableArray<Diagnostic> diagnostics,
        string[] generatedSources
    )
    {
        public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
        public string[] GeneratedSources { get; } = generatedSources;
    }
}
