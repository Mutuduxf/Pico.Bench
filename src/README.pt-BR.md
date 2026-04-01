# Projetos de Código Fonte

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

Este diretório contém os dois projetos de biblioteca que compõem o PicoBench.

## PicoBench

A biblioteca principal de benchmarking com destino a **netstandard2.0** sem dependências externas.

### Arquivos Principais

| Arquivo | Propósito |
|------|---------|
| `Benchmark.cs` | API imperativa - `Run()`, `Run<TState>()`, `RunScoped<TScope>()`, `Compare()` |
| `BenchmarkRunner.cs` | Ponto de entrada baseado em atributos - `Run<T>()` |
| `Attributes.cs` | Sete atributos: `[BenchmarkClass]`, `[Benchmark]`, `[Params]`, `[GlobalSetup]`, `[GlobalCleanup]`, `[IterationSetup]`, `[IterationCleanup]` |
| `IBenchmarkClass.cs` | Interface implementada pelo gerador de código fonte em classes decoradas |
| `BenchmarkConfig.cs` | Configuração com predefinições Quick / Default / Precise mais auto-calibração opcional |
| `Runner.cs` | Motor de temporização de baixo nível com contagem de ciclos de CPU específica da plataforma |
| `StatisticsCalculator.cs` | Cálculo de percentis e estatísticas |
| `Models.cs` | Tipos de resultado incluindo campos de precisão em `Statistics` e metadados do contador de CPU em `EnvironmentInfo` |
| `Formatters/` | Cinco formatadores: Console, Markdown, HTML, CSV, Summary |

### Empacotamento

O projeto inclui `PicoBench.Generators` como um analisador para que os consumidores obtenham o gerador de código fonte automaticamente:

```bash
# Adicionar referência do projeto
dotnet add reference ../PicoBench.Generators/PicoBench.Generators.csproj

# Em seguida, adicione manualmente os seguintes atributos ao elemento <ProjectReference> no seu arquivo .csproj:
# PrivateAssets="all"
# ReferenceOutputAssembly="false"
# OutputItemType="Analyzer"
```

## PicoBench.Generators

Um **gerador de código fonte incremental** (`IIncrementalGenerator`) que transforma classes parciais decoradas com `[BenchmarkClass]` em implementações completas de `IBenchmarkClass` em tempo de compilação.

- **Destino**: netstandard2.0
- **Dependência**: Microsoft.CodeAnalysis.CSharp 5.0.0
- **Saída**: C# compatível com AOT com chamadas qualificadas `global::` e sem reflexão

### Arquivos Principais

| Arquivo | Propósito |
|------|---------|
| `BenchmarkGenerator.cs` | Ponto de entrada do gerador usando `ForAttributeWithMetadataName` |
| `DiagnosticDescriptors.cs` | Diagnósticos centralizados do gerador para declarações de benchmark inválidas |
| `Emitter.cs` | Emissor de código C# - gera `RunBenchmarks()` com iteração de parâmetros, hooks de setup/teardown e lógica de comparação |
| `Models.cs` | Modelos de análise Roslyn: `BenchmarkClassModel`, `BenchmarkMethodModel`, `ParamsPropertyModel` (todos `IEquatable<T>` para caching) |

O gerador agora valida erros comuns antes da emissão do código e reporta diagnósticos para métodos benchmark inválidos, métodos de ciclo de vida inválidos, baselines duplicados, destinos `[Params]` inválidos e valores de parâmetro incompatíveis.

### Código Gerado

Para uma classe como:

```csharp
[BenchmarkClass]
public partial class MyBench
{
    [Params(10, 100)]
    public int N { get; set; }

    [GlobalSetup]
    public void Setup() { }

    [Benchmark(Baseline = true)]
    public void Baseline() { /* ... */ }

    [Benchmark]
    public void Fast() { /* ... */ }
}
```

O gerador emite uma `partial class MyBench : IBenchmarkClass` com um método `RunBenchmarks()` que:

1. Itera cada valor `[Params]` (produto cartesiano para múltiplas propriedades)
2. Define a propriedade, chama `[GlobalSetup]`
3. Executa cada método `[Benchmark]` via `Benchmark.Run()` com `[IterationSetup]`/`[IterationCleanup]` como setup/teardown
4. Compara candidatos contra o baseline
5. Chama `[GlobalCleanup]`
6. Retorna um `BenchmarkSuite` com todos os resultados e comparações
