# Исходные проекты

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

Эта директория содержит два библиотечных проекта, составляющих PicoBench.

## PicoBench

Основная библиотека бенчмаркинга, нацеленная на **netstandard2.0** без внешних зависимостей.

### Ключевые файлы

| Файл | Назначение |
|------|---------|
| `Benchmark.cs` | Императивный API - `Run()`, `Run<TState>()`, `RunScoped<TScope>()`, `Compare()` |
| `BenchmarkRunner.cs` | Точка входа на основе атрибутов - `Run<T>()` |
| `Attributes.cs` | Семь атрибутов: `[BenchmarkClass]`, `[Benchmark]`, `[Params]`, `[GlobalSetup]`, `[GlobalCleanup]`, `[IterationSetup]`, `[IterationCleanup]` |
| `IBenchmarkClass.cs` | Интерфейс, реализуемый генератором исходного кода в декорированных классах |
| `BenchmarkConfig.cs` | Конфигурация с предустановками Quick / Default / Precise и опциональной автокалибровкой |
| `Runner.cs` | Низкоуровневый поток измерения времени и создание образцов |
| `Runner.Gc.cs` | Базовая линия GC и расчёт дельты |
| `Runner.Cpu.cs` | Платформенно-специфичная реализация счётчика CPU |
| `StatisticsCalculator.cs` | Вычисление процентилей и статистики |
| `Models.cs` | Типы результатов, включая поля точности в `Statistics` и метаданные счетчика CPU в `EnvironmentInfo` |
| `Formatters/` | Четыре реализации `IFormatter` (Console, Markdown, HTML, CSV) и `SummaryFormatter` |

### Упаковка

Проект включает `PicoBench.Generators` как анализатор, поэтому потребители автоматически получают генератор исходного кода:

```bash
# Добавить ссылку на проект
dotnet add reference ../PicoBench.Generators/PicoBench.Generators.csproj

# Затем вручную добавьте следующие атрибуты к элементу <ProjectReference> в вашем файле .csproj:
# PrivateAssets="all"
# ReferenceOutputAssembly="false"
# OutputItemType="Analyzer"
```

## PicoBench.Generators

**Инкрементальный генератор исходного кода** (`IIncrementalGenerator`), который превращает декорированные `[BenchmarkClass]` partial-классы в полные реализации `IBenchmarkClass` во время компиляции.

- **Цель**: netstandard2.0
- **Зависимость**: Microsoft.CodeAnalysis.CSharp 5.0.0
- **Вывод**: Совместимый с AOT C# с вызовами, квалифицированными `global::`, без рефлексии

### Ключевые файлы

| Файл | Назначение |
|------|---------|
| `BenchmarkGenerator.cs` | Точка входа генератора, использующая `ForAttributeWithMetadataName` |
| `BenchmarkClassAnalyzer.cs` | Анализ и диагностика Roslyn перед генерацией кода |
| `CSharpLiteralFormatter.cs` | Форматирование литералов C# для генерируемых значений `[Params]` |
| `DiagnosticDescriptors.cs` | Централизованные диагностики генератора для некорректных объявлений benchmark |
| `Emitter.cs` | Генератор кода C# - создает `RunBenchmarks()` с итерацией параметров, хуками настройки/очистки и логикой сравнения |
| `Models.cs` | Модели анализа Roslyn: `BenchmarkClassModel`, `BenchmarkMethodModel`, `ParamsPropertyModel` (все `IEquatable<T>` для кэширования) |

Теперь генератор проверяет типичные ошибки до генерации кода и сообщает диагностику для некорректных benchmark-методов, lifecycle-методов, дублирующихся baseline, некорректных целей `[Params]` и несовместимых значений параметров.

### Сгенерированный код

Для класса, такого как:

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

Генератор создает `partial class MyBench : IBenchmarkClass` с методом `RunBenchmarks()`, который:

1. Итерирует каждое значение `[Params]` (декартово произведение для нескольких свойств)
2. Устанавливает свойство, вызывает `[GlobalSetup]`
3. Запускает каждый метод `[Benchmark]` через `Benchmark.Run()` с `[IterationSetup]`/`[IterationCleanup]` как настройка/очистка
4. Сравнивает кандидатов с базовым вариантом
5. Вызывает `[GlobalCleanup]`
6. Возвращает `BenchmarkSuite` со всеми результатами и сравнениями
