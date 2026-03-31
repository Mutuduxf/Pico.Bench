# Tests

[English](README.md) | [中文](README.zh-CN.md) | [中文 (Traditional)](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

Unit tests for **PicoBench** using the [TUnit](https://github.com/thomhurst/TUnit) testing framework.

**Total: 313 tests**

## Running

```bash
dotnet run --project tests/PicoBench.Tests/PicoBench.Tests.csproj -c Debug
```

## Test Categories

### Formatters/ (224 tests)

Tests for all five output formatters and their supporting infrastructure.

| File | Tests | Description |
|------|-------|-------------|
| `ConsoleFormatterTests.cs` | 40+ | Box-drawing table generation, alignment, encoding |
| `MarkdownFormatterTests.cs` | 40+ | GitHub Markdown table rendering |
| `HtmlFormatterTests.cs` | 40+ | HTML report generation with styles |
| `CsvFormatterTests.cs` | 40+ | CSV export with proper escaping |
| `SummaryFormatterTests.cs` | 20+ | Win/loss summary text |
| `FormatterBaseTests.cs` | 15+ | Template Method base class behaviour |
| `FormatterOptionsTests.cs` | 10+ | Options defaults, presets, path resolution |
| `CrossPlatformTests.cs` | 10+ | Line-ending and encoding consistency |

### Formatters/Integration/ (8 tests)

| File | Tests | Description |
|------|-------|-------------|
| `FormatterIntegrationTests.cs` | 8 | End-to-end formatting of full `BenchmarkSuite` objects |

### Attributes/ (18 tests)

| File | Tests | Description |
|------|-------|-------------|
| `AttributeTests.cs` | 18 | All seven attributes: default values, property setting, `AttributeUsage` targets, `[Params]` value storage |

### BenchmarkRunnerTests.cs (8 tests)

| File | Tests | Description |
|------|-------|-------------|
| `BenchmarkRunnerTests.cs` | 8 | `BenchmarkRunner.Run<T>()` with parameterless / pre-configured instance, null checks, config propagation |

### Generators/ (47 tests)

| File | Tests | Description |
|------|-------|-------------|
| `EmitterTests.cs` | 25 | Source generator code emission: class structure, parameter iteration, setup/teardown hooks, baseline comparisons, `global::` qualification |
| `ModelsTests.cs` | 22 | `BenchmarkClassModel`, `BenchmarkMethodModel`, `ParamsPropertyModel` equality, hash codes, edge cases |

### TestData/

Factory classes for building consistent test fixtures:

| File | Purpose |
|------|---------|
| `BenchmarkResultFactory.cs` | Creates `BenchmarkResult` instances with sensible defaults |
| `BenchmarkSuiteFactory.cs` | Creates `BenchmarkSuite` with results and comparisons |
| `ComparisonResultFactory.cs` | Creates `ComparisonResult` pairs |
| `GcInfoFactory.cs` | Creates `GcInfo` records |
| `StatisticsFactory.cs` | Creates `Statistics` with realistic distributions |

### Utilities/

| File | Purpose |
|------|---------|
| `FileSystemHelper.cs` | Temp directory management for file-output tests |
| `TestContextLogger.cs` | Logging helper for TUnit test context |
