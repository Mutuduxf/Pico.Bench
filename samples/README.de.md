# Beispiele

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

Drei Beispielprojekte demonstrieren die beiden von PicoBench bereitgestellten APIs.

## StringVsStringBuilder (Imperative API)

Verwendet die imperative `Benchmark.Run()`- und `Benchmark.Compare()`-API, um String-Konkatenationsstrategien bei verschiedenen Größen zu messen.

**Highlights:**

- `Benchmark.Run()` mit Closures und `BenchmarkConfig.Quick`
- `Benchmark.Run<TState>()` mit State, um Closure-Allokation zu vermeiden
- Manuelle `ComparisonResult`-Erstellung für benutzerdefiniertes Gruppieren
- `BenchmarkSuite`-Konstruktion mit allen Ergebnissen und Vergleichen
- Ausgaben an Konsole, Markdown, HTML und CSV über `FormatterOptions` (benutzerdefinierte Beschriftungen)

```bash
dotnet run --project samples/StringVsStringBuilder -c Release
```

## AttributeBased (Attribute API + Source Generator)

Schreibt denselben String-Benchmark mit der attributbasierten API neu.

**Highlights:**

- `[BenchmarkClass]` mit `Description`
- `[Params(10, 100, 1000)]` für parametrisierte Läufe
- `[Benchmark(Baseline = true)]` zum Markieren der Referenzmethode
- `[GlobalSetup]` für die Vorbereitung pro Parameterkombination
- Einzeiler-Ausführung über `BenchmarkRunner.Run<T>()`
- `SummaryFormatter` für schnelle Übersicht über Gewinne/Verluste

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

## CollectionBenchmarks (Vollständige Attribut-Demonstration)

Demonstriert **die meisten** Attribute durch Vergleich der Lookup-Leistung von List, Dictionary und HashSet.

**Highlights:**

- `[BenchmarkClass]` mit `Description`
- `[Params(100, 1_000, 10_000)]` – drei Collection-Größen
- `[GlobalSetup]` – füllt alle drei Collections mit randomisierten Daten
- `[GlobalCleanup]` – gibt die Collections frei
- `[IterationSetup]` – mischt das Lookup-Ziel vor jeder Probe
- `[Benchmark(Baseline = true, Description = "...")]` – `List.Contains()` als Baseline
- `[Benchmark(Description = "...")]` – `Dictionary.ContainsKey()` und `HashSet.Contains()`
- Multi-Format-Ausgabe: Konsole, Markdown, HTML, CSV

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

## Ausgabe

Alle Beispiele speichern Ergebnisse in einem `results/`-Unterverzeichnis unter dem Ausgabeverzeichnis in den Formaten Markdown, HTML und CSV.

Diese Berichte enthalten jetzt auch präzisionsorientierte Metadaten wie Standardfehler, relative Standardabweichung und CPU-Zähler-Hinweise, sofern verfügbar.
