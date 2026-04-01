# Exemplos

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

Três projetos de exemplo demonstram as duas API fornecidas pelo PicoBench.

## StringVsStringBuilder (API Imperativa)

Usa a API imperativa `Benchmark.Run()` e `Benchmark.Compare()` para medir estratégias de concatenação de strings em vários tamanhos.

**Destaques:**

- `Benchmark.Run()` com closures e `BenchmarkConfig.Quick`
- `Benchmark.Run<TState>()` com estado para evitar alocação de closure
- Criação manual de `ComparisonResult` para agrupamento personalizado
- Construção de `BenchmarkSuite` com todos os resultados e comparações
- Saídas para Console, Markdown, HTML e CSV via `FormatterOptions` (rótulos personalizados)

```bash
dotnet run --project samples/StringVsStringBuilder -c Release
```

## AttributeBased (API de Atributos + Gerador de Código Fonte)

Reescreve o mesmo benchmark de strings usando a API baseada em atributos.

**Destaques:**

- `[BenchmarkClass]` com `Description`
- `[Params(10, 100, 1000)]` para execuções parametrizadas
- `[Benchmark(Baseline = true)]` para marcar o método de referência
- `[GlobalSetup]` para preparação por combinação de parâmetros
- Execução de uma linha via `BenchmarkRunner.Run<T>()`
- `SummaryFormatter` para visão rápida de vitórias/derrotas

```csharp
[BenchmarkClass(Description = "Comparing string concatenation strategies")]
public partial class StringBenchmarks
{
    [Params(10, 100, 1000)]
    public int N { get; set; }

    [GlobalSetup]
    public void Setup() { }

    [Benchmark(Baseline = true)]
    public void StringConcat() { /* ... */ }

    [Benchmark]
    public void StringBuilder() { /* ... */ }

    [Benchmark]
    public void StringBuilderWithCapacity() { /* ... */ }
}
```

```bash
dotnet run --project samples/AttributeBased -c Release
```

## CollectionBenchmarks (Demonstração Completa de Atributos)

Demonstra **a maioria** dos atributos comparando o desempenho de busca em List, Dictionary e HashSet.

**Destaques:**

- `[BenchmarkClass]` com `Description`
- `[Params(100, 1_000, 10_000)]` - três tamanhos de coleção
- `[GlobalSetup]` - preenche todas as três coleções com dados aleatórios
- `[GlobalCleanup]` - libera as coleções
- `[IterationSetup]` - embaralha o alvo de busca antes de cada amostra
- `[Benchmark(Baseline = true, Description = "...")]` - `List.Contains()` como baseline
- `[Benchmark(Description = "...")]` - `Dictionary.ContainsKey()` e `HashSet.Contains()`
- Saída multi-formato: Console, Markdown, HTML, CSV

```csharp
[BenchmarkClass(Description = "Lookup performance: List vs Dictionary vs HashSet")]
public partial class LookupBenchmarks
{
    [Params(100, 1_000, 10_000)]
    public int N { get; set; }

    [GlobalSetup]   public void Setup()         { /* populate collections */ }
    [GlobalCleanup] public void Cleanup()       { /* release collections */ }
    [IterationSetup] public void ShuffleTarget() { /* vary lookup target */ }

    [Benchmark(Baseline = true, Description = "Linear scan O(n)")]
    public void ListContains() { _ = _list.Contains(_target); }

    [Benchmark(Description = "Hash lookup O(1)")]
    public void DictionaryContainsKey() { _ = _dictionary.ContainsKey(_target); }

    [Benchmark(Description = "Hash lookup O(1), set-optimised")]
    public void HashSetContains() { _ = _hashSet.Contains(_target); }
}
```

```bash
dotnet run --project samples/CollectionBenchmarks -c Release
```

## Saída

Todos os exemplos salvam resultados em um subdiretório `results/` sob a pasta de saída nos formatos Markdown, HTML e CSV.

Esses relatórios agora também incluem metadados voltados à precisão, como erro padrão, desvio padrão relativo e observações sobre o contador de CPU quando disponíveis.
