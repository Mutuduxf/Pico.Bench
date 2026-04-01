# Примеры

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

Три примера проектов демонстрируют два API, предоставляемые PicoBench.

## StringVsStringBuilder (Императивный API)

Использует императивный API `Benchmark.Run()` и `Benchmark.Compare()` для измерения стратегий конкатенации строк различных размеров.

**Особенности:**

- `Benchmark.Run()` с замыканиями и `BenchmarkConfig.Quick`
- `Benchmark.Run<TState>()` с состоянием для избежания выделения замыканий
- Ручное создание `ComparisonResult` для пользовательской группировки
- Построение `BenchmarkSuite` со всеми результатами и сравнениями
- Вывод в Консоль, Markdown, HTML и CSV через `FormatterOptions` (пользовательские метки)

```bash
dotnet run --project samples/StringVsStringBuilder -c Release
```

## AttributeBased (API на основе атрибутов + генератор исходного кода)

Переписывает тот же строковый бенчмарк, используя API на основе атрибутов.

**Особенности:**

- `[BenchmarkClass]` с `Description`
- `[Params(10, 100, 1000)]` для параметризованных запусков
- `[Benchmark(Baseline = true)]` для пометки эталонного метода
- `[GlobalSetup]` для подготовки каждой комбинации параметров
- Однострочное выполнение через `BenchmarkRunner.Run<T>()`
- `SummaryFormatter` для быстрого обзора побед/поражений

```csharp
[BenchmarkClass(Description = "Сравнение стратегий конкатенации строк")]
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

## CollectionBenchmarks (Полная демонстрация атрибутов)

Демонстрирует **большинство** атрибутов, сравнивая производительность поиска в List, Dictionary и HashSet.

**Особенности:**

- `[BenchmarkClass]` с `Description`
- `[Params(100, 1_000, 10_000)]` - три размера коллекции
- `[GlobalSetup]` - заполняет все три коллекции рандомизированными данными
- `[GlobalCleanup]` - освобождает коллекции
- `[IterationSetup]` - перемешивает цель поиска перед каждым образцом
- `[Benchmark(Baseline = true, Description = "...")]` - `List.Contains()` как базовый
- `[Benchmark(Description = "...")]` - `Dictionary.ContainsKey()` и `HashSet.Contains()`
- Многоформатный вывод: Консоль, Markdown, HTML, CSV

```csharp
[BenchmarkClass(Description = "Производительность поиска: List vs Dictionary vs HashSet")]
public partial class LookupBenchmarks
{
    [Params(100, 1_000, 10_000)]
    public int N { get; set; }

    [GlobalSetup]   public void Setup()         { /* заполнить коллекции */ }
    [GlobalCleanup] public void Cleanup()       { /* освободить коллекции */ }
    [IterationSetup] public void ShuffleTarget() { /* изменить цель поиска */ }

    [Benchmark(Baseline = true, Description = "Линейный поиск O(n)")]
    public void ListContains() { _ = _list.Contains(_target); }

    [Benchmark(Description = "Хеш-поиск O(1)")]
    public void DictionaryContainsKey() { _ = _dictionary.ContainsKey(_target); }

    [Benchmark(Description = "Хеш-поиск O(1), оптимизированный для множеств")]
    public void HashSetContains() { _ = _hashSet.Contains(_target); }
}
```

```bash
dotnet run --project samples/CollectionBenchmarks -c Release
```

## Вывод

Все примеры сохраняют результаты в поддиректорию `results/` внутри выходной папки в форматах Markdown, HTML и CSV.

Эти отчеты теперь также включают метаданные, ориентированные на точность: стандартную ошибку, относительное стандартное отклонение и пояснения по счетчику CPU, если они доступны.
