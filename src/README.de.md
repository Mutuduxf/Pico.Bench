# Quellprojekte

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

Dieses Verzeichnis enthält die beiden Bibliotheksprojekte, die PicoBench ausmachen.

## PicoBench

Die Haupt-Benchmarking-Bibliothek mit Ziel **netstandard2.0** ohne externe Abhängigkeiten.

### Wichtige Dateien

| Datei | Zweck |
|------|---------|
| `Benchmark.cs` | Imperative API - `Run()`, `Run<TState>()`, `RunScoped<TScope>()`, `Compare()` |
| `BenchmarkRunner.cs` | Attributbasierter Einstiegspunkt - `Run<T>()` |
| `Attributes.cs` | Sieben Attribute: `[BenchmarkClass]`, `[Benchmark]`, `[Params]`, `[GlobalSetup]`, `[GlobalCleanup]`, `[IterationSetup]`, `[IterationCleanup]` |
| `IBenchmarkClass.cs` | Vom Quellgenerator auf dekorierte Klassen implementierte Schnittstelle |
| `BenchmarkConfig.cs` | Konfiguration mit Quick / Default / Precise Voreinstellungen plus optionaler Auto-Kalibrierung |
| `Runner.cs` | Low-Level-Timing-Engine mit plattformspezifischer CPU-Zykluszählung |
| `StatisticsCalculator.cs` | Prozentil- und Statistikberechnung |
| `Models.cs` | Ergebnistypen einschließlich Präzisionsfeldern in `Statistics` und CPU-Zähler-Metadaten in `EnvironmentInfo` |
| `Formatters/` | Fünf Formatierer: Console, Markdown, HTML, CSV, Summary |

### Paketierung

Das Projekt bindet `PicoBench.Generators` als Analyzer ein, sodass Verbraucher den Quellgenerator automatisch erhalten:

```bash
# Projektreferenz hinzufügen
dotnet add reference ../PicoBench.Generators/PicoBench.Generators.csproj

# Fügen Sie dann manuell die folgenden Attribute zum <ProjectReference>-Element in Ihrer .csproj-Datei hinzu:
# PrivateAssets="all"
# ReferenceOutputAssembly="false"
# OutputItemType="Analyzer"
```

## PicoBench.Generators

Ein **inkrementeller Quellgenerator** (`IIncrementalGenerator`), der mit `[BenchmarkClass]` dekorierte partielle Klassen zur Kompilierzeit in vollständige `IBenchmarkClass`-Implementierungen umwandelt.

- **Ziel**: netstandard2.0
- **Abhängigkeit**: Microsoft.CodeAnalysis.CSharp 5.0.0
- **Ausgabe**: AOT-kompatibles C# mit `global::`-qualifizierten Aufrufen und ohne Reflection

### Wichtige Dateien

| Datei | Zweck |
|------|---------|
| `BenchmarkGenerator.cs` | Generator-Einstiegspunkt mit `ForAttributeWithMetadataName` |
| `DiagnosticDescriptors.cs` | Zentrale Generator-Diagnosen für ungültige Benchmark-Deklarationen |
| `Emitter.cs` | C#-Code-Emitter – generiert `RunBenchmarks()` mit Parameteriteration, Setup/Teardown-Hooks und Vergleichslogik |
| `Models.cs` | Roslyn-Analysemodelle: `BenchmarkClassModel`, `BenchmarkMethodModel`, `ParamsPropertyModel` (alle `IEquatable<T>` für Caching) |

Der Generator validiert jetzt häufige Fehler vor der Codeerzeugung und meldet Diagnosen für ungültige Benchmark-Methoden, Lifecycle-Methoden, doppelte Baselines, ungültige `[Params]`-Ziele und inkompatible Parameterwerte.

### Generierter Code

Für eine Klasse wie:

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

Der Generator gibt eine `partial class MyBench : IBenchmarkClass` mit einer `RunBenchmarks()`-Methode aus, die:

1. Jeden `[Params]`-Wert iteriert (kartesisches Produkt für mehrere Eigenschaften)
2. Die Eigenschaft setzt, `[GlobalSetup]` aufruft
3. Jede `[Benchmark]`-Methode über `Benchmark.Run()` mit `[IterationSetup]`/`[IterationCleanup]` als Setup/Teardown ausführt
4. Kandidaten gegen den Baseline vergleicht
5. `[GlobalCleanup]` aufruft
6. Ein `BenchmarkSuite` mit allen Ergebnissen und Vergleichen zurückgibt
