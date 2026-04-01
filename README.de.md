# PicoBench

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

Eine leichte, abhängigkeitsfreie Benchmarking-Bibliothek für .NET mit **zwei komplementären APIs**: einer imperativen fluent API und einer attributbasierten, quellgenerierten API, die vollständig **AOT-kompatibel** ist.

## Funktionen

- **Keine Abhängigkeiten** - Reine .NET-Implementierung, keine externen Pakete erforderlich
- **Zwei APIs** - Imperativ (`Benchmark.Run`) für Ad-hoc-Tests; attributbasiert (`[Benchmark]` + Quellgenerator) für strukturierte Suiten
- **AOT-kompatibler Quellgenerator** - Der inkrementelle Generator erzeugt direkte Methodenaufrufe ohne Reflection zur Laufzeit
- **Plattformübergreifend** - Vollständige Unterstützung für Windows, Linux und macOS
- **Hochpräzises Timing** - Verwendet `Stopwatch` mit Nanosekunden-Granularität
- **GC-Tracking** - Überwacht Gen0/Gen1/Gen2-Sammlungszählungen während Benchmarks
- **CPU-Zykluszählung** - Hardware-Level-Zykluszählung (Windows über `QueryThreadCycleTime`, Linux über `perf_event`, macOS über `mach_absolute_time`)
- **Statistische Analyse** - Mittelwert, Median, P90, P95, P99, Minimum, Maximum, Standardabweichung, Standardfehler und relative Varianz
- **Mehrere Ausgabeformate** - Konsole, Markdown, HTML, CSV und programmatische Zusammenfassung
- **Parametrisierte Benchmarks** - `[Params]`-Attribut mit automatischer kartesischer Produktiteration
- **Vergleichsunterstützung** - Baseline vs Kandidat mit Beschleunigungsberechnungen
- **Konfigurierbar** - Quick-, Default- und Precise-Voreinstellungen, Auto-Kalibrierung oder vollständig benutzerdefinierte Konfiguration
- **netstandard2.0** - Kompatibel mit .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+

## Installation

Verweisen Sie auf das **PicoBench** NuGet-Paket. Der Quellgenerator (`PicoBench.Generators`) wird automatisch als Analyzer gebündelt - kein zusätzlicher Verweis erforderlich.

```bash
dotnet add package PicoBench
```

## Schnellstart

### Imperative API

```csharp
using PicoBench;

var result = Benchmark.Run("My Benchmark", () =>
{
    Thread.SpinWait(100);
});

Console.WriteLine($"Average: {result.Statistics.Avg:F1} ns/op");
```

### Attributbasierte API (Quellgeneriert)

```csharp
using PicoBench;

var suite = BenchmarkRunner.Run<MyBenchmarks>();
Console.WriteLine(new PicoBench.Formatters.ConsoleFormatter().Format(suite));

[BenchmarkClass]
public partial class MyBenchmarks
{
    [Benchmark(Baseline = true)]
    public void Baseline() { /* ... */ }

    [Benchmark]
    public void Candidate() { /* ... */ }
}
```

> Die Klasse **muss** `partial` sein. Der Quellgenerator erzeugt zur Kompilierzeit eine `IBenchmarkClass`-Implementierung - ohne Reflection, vollständig AOT-sicher.

> Ungültige Attributverwendung erzeugt jetzt Generator-Diagnosen für häufige Fehler wie nicht-`partial` Klassen, doppelte Baselines, ungültige Lifecycle-Signaturen und inkompatible `[Params]`-Werte.

---

## Referenz der imperativen API

### Grundlegender Benchmark

```csharp
using PicoBench;
using PicoBench.Formatters;

var result = Benchmark.Run("SpinWait", () => Thread.SpinWait(100));
Console.WriteLine(new ConsoleFormatter().Format(result));
```

### Benchmark mit Zustand (Vermeidung von Closures)

```csharp
var data = new byte[1024];
var result = Benchmark.Run("ArrayCopy", data, static d =>
{
    var copy = new byte[d.Length];
    Buffer.BlockCopy(d, 0, copy, 0, d.Length);
});
```

### Bereichsbenchmarks (DI-freundlich)

```csharp
var result = Benchmark.RunScoped("DbQuery",
    () => new MyDbContext(),
    static ctx => ctx.Users.FirstOrDefault()
);
// Ein neuer Bereich wird pro Sample erstellt; der Bereich wird nach jedem Sample verworfen.
```

### Zwei Implementierungen vergleichen

```csharp
var comparison = Benchmark.Compare(
    "String vs StringBuilder",
    "String Concat",  () => { var s = ""; for (int i = 0; i < 100; i++) s += "a"; },
    "StringBuilder",  () => { var sb = new StringBuilder(); for (int i = 0; i < 100; i++) sb.Append('a'); _ = sb.ToString(); }
);

Console.WriteLine($"Speedup: {comparison.Speedup:F2}x ({comparison.ImprovementPercent:F1}%)");
```

### Fortgeschritten: Separate Aufwärm-, Setup- & Teardown-Phasen

```csharp
var result = Benchmark.Run(
    name:     "Custom",
    action:   () => DoWork(),
    warmup:   () => DoWork(),      // null zum Überspringen des Aufwärmens
    config:   BenchmarkConfig.Precise,
    setup:    () => PrepareState(), // vor jedem Sample aufgerufen (nicht gemessen)
    teardown: () => CleanUp()       // nach jedem Sample aufgerufen (nicht gemessen)
);
```

---

## Referenz der attributbasierten API

Dekorieren Sie eine **partial**-Klasse mit `[BenchmarkClass]` und ihre Methoden/Eigenschaften mit den unten stehenden Attributen. Der Quellgenerator erzeugt zur Kompilierzeit den gesamten Verbindungscode.

### Attribute

| Attribut | Ziel | Beschreibung |
|-----------|--------|-------------|
| `[BenchmarkClass]` | Klasse | Markiert die Klasse zur Codegenerierung. Optionale `Description`-Eigenschaft. |
| `[Benchmark]` | Methode | Markiert eine parameterlose Methode als Benchmark. Setzen Sie `Baseline = true` für die Referenzmethode. Optionale `Description`. |
| `[Params(values)]` | Eigenschaft / Feld | Iteriert die gegebenen Kompilierzeit-Konstantenwerte. Mehrere `[Params]`-Eigenschaften erzeugen ein kartesisches Produkt. |
| `[GlobalSetup]` | Methode | Wird **einmal** pro Parameterkombination aufgerufen, bevor Benchmarks laufen. |
| `[GlobalCleanup]` | Methode | Wird **einmal** pro Parameterkombination aufgerufen, nachdem Benchmarks laufen. |
| `[IterationSetup]` | Methode | Wird vor **jedem Sample** aufgerufen (nicht gemessen). |
| `[IterationCleanup]` | Methode | Wird nach **jedem Sample** aufgerufen (nicht gemessen). |

`[Benchmark]`-Methoden müssen Instanzmethoden, nicht generisch und parameterlos sein. Lifecycle-Methoden müssen Instanzmethoden, nicht generisch, parameterlos und `void` sein. `[Params]`-Ziele müssen schreibbare Instanz-Eigenschaften oder nicht-readonly Instanz-Felder sein.

### Vollständiges Beispiel

```csharp
using PicoBench;

[BenchmarkClass(Description = "Vergleich von String-Verkettungsstrategien")]
public partial class StringBenchmarks
{
    [Params(10, 100, 1000)]
    public int N { get; set; }

    [GlobalSetup]
    public void Setup() { /* Daten für aktuelles N vorbereiten */ }

    [GlobalCleanup]
    public void Cleanup() { /* Ressourcen freigeben */ }

    [IterationSetup]
    public void BeforeSample() { /* Vorbereitung pro Sample */ }

    [Benchmark(Baseline = true)]
    public void StringConcat()
    {
        var s = string.Empty;
        for (var i = 0; i < N; i++) s += "a";
    }

    [Benchmark]
    public void StringBuilder()
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < N; i++) sb.Append('a');
        _ = sb.ToString();
    }
}
```

### Ausführung

```csharp
// Instanz intern erstellen:
var suite = BenchmarkRunner.Run<StringBenchmarks>(BenchmarkConfig.Quick);

// Oder mit einer vorkonfigurierten Instanz:
var instance = new StringBenchmarks();
var suite2 = BenchmarkRunner.Run(instance, BenchmarkConfig.Quick);
```

---

## Konfiguration

### Voreinstellungen

| Voreinstellung | Aufwärmiterationen | Samples | Basis Iterationen/Sample | Auto-Kalibrierung | Anwendungsfall |
|--------|--------|---------|--------------------------|-------------------|----------|
| `Quick` | 100 | 10 | 1,000 | Ja | Schnelle Iteration / CI |
| `Default` | 1,000 | 100 | 10,000 | Nein | Allgemeines Benchmarking |
| `Precise` | 5,000 | 200 | 50,000 | Ja | Finale Messungen |

### Benutzerdefinierte Konfiguration

```csharp
var config = new BenchmarkConfig
{
    WarmupIterations    = 500,
    SampleCount         = 50,
    IterationsPerSample = 5000,
    RetainSamples       = true,  // Rohdaten von TimingSample behalten
    AutoCalibrateIterations = true,
    MinSampleTime       = TimeSpan.FromMilliseconds(0.5),
    MaxAutoIterationsPerSample = 1_000_000
};

var result = Benchmark.Run("Test", action, config);
```

Wenn die Auto-Kalibrierung aktiviert ist, erhöht PicoBench `IterationsPerSample`, bis ein minimales Zeitbudget pro Sample erreicht ist oder `MaxAutoIterationsPerSample` erreicht wird. Das ist besonders hilfreich bei extrem schnellen Operationen, die sonst vom Timerrauschen dominiert werden.

---

## Ausgabeformatierer

Fünf eingebaute Formatierer implementieren `IFormatter`:

```csharp
using PicoBench.Formatters;

var console  = new ConsoleFormatter();     // Kastenzeichnungskonsolentabellen
var markdown = new MarkdownFormatter();    // GitHub-freundliches Markdown
var html     = new HtmlFormatter();        // Gestylte HTML-Berichte
var csv      = new CsvFormatter();         // CSV für Datenanalyse

// Statische Hilfsfunktion für Vergleichszusammenfassungen:
Console.WriteLine(SummaryFormatter.Format(suite.Comparisons));
```

Console-, Markdown-, HTML- und CSV-Ausgaben enthalten jetzt präzisionsorientierte Metadaten wie Standardfehler, relative Standardabweichung und Hinweise zum CPU-Zähler, sofern verfügbar.

### Formatierungsziele

```csharp
formatter.Format(result);               // Einzelnes BenchmarkResult
formatter.Format(results);              // IEnumerable<BenchmarkResult>
formatter.Format(comparison);           // Einzelnes ComparisonResult
formatter.Format(comparisons);          // IEnumerable<ComparisonResult>
formatter.Format(suite);                // Vollständiges BenchmarkSuite
```

### Formatiereroptionen

```csharp
var options = new FormatterOptions
{
    IncludeEnvironment   = true,
    IncludeTimestamp      = true,
    IncludeGcInfo         = true,
    IncludeCpuCycles      = true,
    IncludePercentiles    = true,
    TimeDecimalPlaces     = 1,
    SpeedupDecimalPlaces  = 2,
    BaselineLabel         = "Old",
    CandidateLabel        = "New"
};

var formatter = new ConsoleFormatter(options);
// Auch verfügbar: FormatterOptions.Default, .Compact, .Minimal
```

### Ergebnisse speichern

```csharp
var dir = Path.Combine(AppContext.BaseDirectory, "results");
Directory.CreateDirectory(dir);

File.WriteAllText(Path.Combine(dir, "results.md"),   new MarkdownFormatter().Format(suite));
File.WriteAllText(Path.Combine(dir, "results.html"), new HtmlFormatter().Format(suite));
File.WriteAllText(Path.Combine(dir, "results.csv"),  new CsvFormatter().Format(suite));
```

---

## Ergebnis-Modell

| Typ | Beschreibung |
|------|-------------|
| `BenchmarkResult` | Name, Statistik, Samples, IterationenProSample, SampleAnzahl, Tags, Kategorie |
| `ComparisonResult` | Baseline, Kandidat, Beschleunigung, IstSchneller, VerbesserungsProzent |
| `BenchmarkSuite` | Name, Beschreibung, Ergebnisse, Vergleiche, Umgebung, Dauer |
| `Statistics` | Durchschnitt, P50, P90, P95, P99, Minimum, Maximum, Standardabweichung, StandardError, RelativeStdDevPercent, CpuZyklenProOp, GcInfo |
| `TimingSample` | VerstricheneNanosekunden, VerstricheneMillisekunden, VerstricheneTicks, CpuZyklen, GcInfo |
| `GcInfo` | Gen0, Gen1, Gen2, Gesamt, IstNull |
| `EnvironmentInfo` | Betriebssystem, Architektur, Laufzeitversion, Prozessoranzahl, Konfiguration, CPU-Zählerart/-verfügbarkeit |

---

## Architektur

```
src/
+-- PicoBench/                        # Hauptbibliothek (netstandard2.0)
|   +-- Benchmark.cs                   # Imperative API (Run, Compare, RunScoped)
|   +-- BenchmarkRunner.cs             # Attributbasierter Einstiegspunkt (Run<T>)
|   +-- BenchmarkConfig.cs             # Konfiguration mit Voreinstellungen
|   +-- Attributes.cs                  # 7 Benchmark-Attribute
|   +-- IBenchmarkClass.cs             # Vom Generator erzeugtes Interface
|   +-- Runner.cs                      # Low-Level-Timing-Engine
|   +-- StatisticsCalculator.cs        # Perzentil- / Statistikberechnung
|   +-- Models.cs                      # Ergebnis-Typen
|   +-- Formatters/
|       +-- IFormatter.cs              # IFormatter, FormatterOptions & FormatterBase
|       +-- ConsoleFormatter.cs        # Kastenzeichnungskonsolentabellen
|       +-- MarkdownFormatter.cs       # GitHub-Markdown-Tabellen
|       +-- HtmlFormatter.cs           # Gestylte HTML-Berichte
|       +-- CsvFormatter.cs            # CSV-Export
|       +-- SummaryFormatter.cs        # Gewinn/Verlust-Zusammenfassung
|
+-- PicoBench.Generators/            # Quellgenerator (netstandard2.0)
    +-- BenchmarkGenerator.cs          # IIncrementalGenerator-Einstiegspunkt
    +-- Emitter.cs                     # C#-Code-Emitter (AOT-sicher)
    +-- Models.cs                      # Roslyn-Analysemodelle
```

---

## Plattformspezifische Funktionen

| Funktion | Windows | Linux | macOS |
|---------|---------|-------|-------|
| Hochpräzises Timing | Stopwatch | Stopwatch | Stopwatch |
| GC-Tracking (Gen0/1/2) | Ja | Ja | Ja |
| CPU-Zykluszählung | `QueryThreadCycleTime` | `perf_event_open` | `mach_absolute_time` |
| Prozessprioritätserhöhung | Ja | Ja | Ja |

Unter macOS ist der exportierte CPU-Zähler ein hochauflösender monotonic Proxy und keine architektonische Zykluszahl. `EnvironmentInfo` und die Formatter-Ausgabe machen diesen Unterschied explizit sichtbar.

---

## Beispiele

| Beispiel | API-Stil | Beschreibung |
|--------|-----------|-------------|
| `StringVsStringBuilder` | Imperativ | Vergleicht `string +=`, `StringBuilder` und `StringBuilder` mit Kapazität |
| `AttributeBased` | Attributbasiert | Gleicher Vergleich mit `[Benchmark]`, `[Params]` und dem Quellgenerator |
| `CollectionBenchmarks` | Attributbasiert | List vs Dictionary vs HashSet-Suche - zeigt jedes Attribut |

```bash
dotnet run --project samples/StringVsStringBuilder -c Release
dotnet run --project samples/AttributeBased -c Release
dotnet run --project samples/CollectionBenchmarks -c Release
```

---

## Vergleich mit BenchmarkDotNet

| Funktion | PicoBench | BenchmarkDotNet |
|---------|-----------|----------------|
| Abhängigkeiten | 0 | Viele |
| Paketgröße | Klein | Groß |
| Zielframework | netstandard2.0 | net6.0+ |
| AOT-Unterstützung | Quellgenerator | Reflection-basiert |
| Attribut-API | `[Benchmark]`, `[Params]` | `[Benchmark]`, `[Params]` |
| Einrichtungszeit | Sofort | Sekunden |
| Ausgabeformate | 5 | 10+ |
| Statistische Tiefe | Gut | Umfangreich |
| Anwendungsfall | Schnelle A/B-Tests, CI, AOT-Apps | Detaillierte Analysen, Publikationen |

---

## Lizenz

MIT-Lizenz - siehe [LICENSE](LICENSE)-Datei für Details.

## Beitragen

1. Repository forken
2. Feature-Branch erstellen
3. Änderungen mit Tests durchführen
4. Pull-Request einreichen
