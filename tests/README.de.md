# Tests

[English](README.md) | [中文](README.zh-CN.md) | [中文 (Traditional)](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

Unit-Tests für **PicoBench** mit dem [TUnit](https://github.com/thomhurst/TUnit)-Testframework.

**Gesamt: 313 Tests**

## Ausführen

```bash
dotnet run --project tests/PicoBench.Tests/PicoBench.Tests.csproj -c Debug
```

## Testkategorien

### Formatters/ (224 Tests)

Tests für alle fünf Ausgabeformatierer und ihre unterstützende Infrastruktur.

| Datei | Tests | Beschreibung |
|------|-------|-------------|
| `ConsoleFormatterTests.cs` | 40+ | Box-drawing-Tabellengenerierung, Ausrichtung, Kodierung |
| `MarkdownFormatterTests.cs` | 40+ | GitHub-Markdown-Tabellenrendering |
| `HtmlFormatterTests.cs` | 40+ | HTML-Berichtsgenerierung mit Stilen |
| `CsvFormatterTests.cs` | 40+ | CSV-Export mit korrektem Escaping |
| `SummaryFormatterTests.cs` | 20+ | Gewinn/Verlust-Zusammenfassungstext |
| `FormatterBaseTests.cs` | 15+ | Template-Method-Basisklassenverhalten |
| `FormatterOptionsTests.cs` | 10+ | Optionsstandards, Voreinstellungen, Pfadauflösung |
| `CrossPlatformTests.cs` | 10+ | Zeilenende- und Kodierungskonsistenz |

### Formatters/Integration/ (8 Tests)

| Datei | Tests | Beschreibung |
|------|-------|-------------|
| `FormatterIntegrationTests.cs` | 8 | End-to-End-Formatierung vollständiger `BenchmarkSuite`-Objekte |

### Attributes/ (18 Tests)

| Datei | Tests | Beschreibung |
|------|-------|-------------|
| `AttributeTests.cs` | 18 | Alle sieben Attribute: Standardwerte, Eigenschafteneinstellung, `AttributeUsage`-Ziele, `[Params]`-Wertspeicherung |

### BenchmarkRunnerTests.cs (8 Tests)

| Datei | Tests | Beschreibung |
|------|-------|-------------|
| `BenchmarkRunnerTests.cs` | 8 | `BenchmarkRunner.Run<T>()` mit parameterloser / vorkonfigurierter Instanz, Nullprüfungen, Konfigurationspropagation |

### Generators/ (47 Tests)

| Datei | Tests | Beschreibung |
|------|-------|-------------|
| `EmitterTests.cs` | 25 | Quellgenerator-Codeemission: Klassenstruktur, Parameteriteration, Setup/Teardown-Hooks, Baseline-Vergleiche, `global::`-Qualifizierung |
| `ModelsTests.cs` | 22 | `BenchmarkClassModel`, `BenchmarkMethodModel`, `ParamsPropertyModel` Gleichheit, Hashcodes, Randfälle |

### TestData/

Factory-Klassen zum Erstellen konsistenter Test-Fixtures:

| Datei | Zweck |
|------|---------|
| `BenchmarkResultFactory.cs` | Erstellt `BenchmarkResult`-Instanzen mit sinnvollen Standardwerten |
| `BenchmarkSuiteFactory.cs` | Erstellt `BenchmarkSuite` mit Ergebnissen und Vergleichen |
| `ComparisonResultFactory.cs` | Erstellt `ComparisonResult`-Paare |
| `GcInfoFactory.cs` | Erstellt `GcInfo`-Records |
| `StatisticsFactory.cs` | Erstellt `Statistics` mit realistischen Verteilungen |

### Utilities/

| Datei | Zweck |
|------|---------|
| `FileSystemHelper.cs` | Temporäres Verzeichnismanagement für Dateiausgabe-Tests |
| `TestContextLogger.cs` | Logging-Helfer für TUnit-Testkontext |
