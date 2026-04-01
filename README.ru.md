# PicoBench

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

Легковесная библиотека для бенчмаркинга в .NET без зависимостей с **двумя взаимодополняющими API**: императивным fluent API и API на основе атрибутов, генерируемым из исходного кода, полностью **совместимым с AOT**.

## Особенности

- **Нет зависимостей** - Чистая реализация .NET, не требует внешних пакетов
- **Два API** - Императивный (`Benchmark.Run`) для ad-hoc тестов; на основе атрибутов (`[Benchmark]` + генератор исходного кода) для структурированных наборов
- **Генератор исходного кода, совместимый с AOT** - Инкрементальный генератор создает прямые вызовы методов без рефлексии во время выполнения
- **Кроссплатформенность** - Полная поддержка Windows, Linux и macOS
- **Высокоточное измерение времени** - Использует `Stopwatch` с наносекундной точностью
- **Отслеживание GC** - Мониторинг количества сборок Gen0/Gen1/Gen2 во время бенчмарков
- **Подсчет циклов CPU** - Аппаратный подсчет циклов (Windows через `QueryThreadCycleTime`, Linux через `perf_event`, macOS через `mach_absolute_time`)
- **Статистический анализ** - Среднее, Медиана, P90, P95, P99, Минимум, Максимум, Стандартное отклонение, стандартная ошибка и относительная вариативность
- **Несколько форматов вывода** - Консоль, Markdown, HTML, CSV и программная сводка
- **Параметризованные бенчмарки** - Атрибут `[Params]` с автоматической итерацией декартова произведения
- **Поддержка сравнений** - Базовый вариант vs кандидат с расчетом ускорения
- **Настраиваемость** - Предустановки Quick, Default и Precise, автокалибровка или полностью пользовательская конфигурация
- **netstandard2.0** - Совместимо с .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+

## Установка

Добавьте ссылку на пакет NuGet **PicoBench**. Генератор исходного кода (`PicoBench.Generators`) автоматически включается как анализатор - дополнительная ссылка не требуется.

```bash
dotnet add package PicoBench
```

## Быстрый старт

### Императивный API

```csharp
using PicoBench;

var result = Benchmark.Run("My Benchmark", () =>
{
    Thread.SpinWait(100);
});

Console.WriteLine($"Average: {result.Statistics.Avg:F1} ns/op");
```

### API на основе атрибутов (генерируемый из исходного кода)

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

> Класс **должен** быть `partial`. Генератор исходного кода создает реализацию `IBenchmarkClass` во время компиляции - без рефлексии, полностью безопасно для AOT.

> Некорректное использование атрибутов теперь дает диагностику генератора для типичных ошибок: класс не `partial`, дублирующиеся baseline, неверные сигнатуры lifecycle-методов и несовместимые значения `[Params]`.

---

## Справочник по императивному API

### Базовый бенчмарк

```csharp
using PicoBench;
using PicoBench.Formatters;

var result = Benchmark.Run("SpinWait", () => Thread.SpinWait(100));
Console.WriteLine(new ConsoleFormatter().Format(result));
```

### Бенчмарк с состоянием (избегание замыканий)

```csharp
var data = new byte[1024];
var result = Benchmark.Run("ArrayCopy", data, static d =>
{
    var copy = new byte[d.Length];
    Buffer.BlockCopy(d, 0, copy, 0, d.Length);
});
```

### Бенчмарки с областью видимости (удобно для DI)

```csharp
var result = Benchmark.RunScoped("DbQuery",
    () => new MyDbContext(),
    static ctx => ctx.Users.FirstOrDefault()
);
// Новая область создается для каждого образца; область удаляется после каждого образца.
```

### Сравнение двух реализаций

```csharp
var comparison = Benchmark.Compare(
    "String vs StringBuilder",
    "String Concat",  () => { var s = ""; for (int i = 0; i < 100; i++) s += "a"; },
    "StringBuilder",  () => { var sb = new StringBuilder(); for (int i = 0; i < 100; i++) sb.Append('a'); _ = sb.ToString(); }
);

Console.WriteLine($"Speedup: {comparison.Speedup:F2}x ({comparison.ImprovementPercent:F1}%)");
```

### Продвинутый: отдельные разогрев, настройка и очистка

```csharp
var result = Benchmark.Run(
    name:     "Custom",
    action:   () => DoWork(),
    warmup:   () => DoWork(),      // null чтобы пропустить разогрев
    config:   BenchmarkConfig.Precise,
    setup:    () => PrepareState(), // вызывается перед каждым образцом (не учитывается во времени)
    teardown: () => CleanUp()       // вызывается после каждого образца (не учитывается во времени)
);
```

---

## Справочник по API на основе атрибутов

Пометьте класс **partial** атрибутом `[BenchmarkClass]` и его методы/свойства атрибутами ниже. Генератор исходного кода создает весь связующий код во время компиляции.

### Атрибуты

| Атрибут | Цель | Описание |
|-----------|--------|-------------|
| `[BenchmarkClass]` | Класс | Помечает класс для генерации кода. Необязательное свойство `Description`. |
| `[Benchmark]` | Метод | Помечает метод без параметров как бенчмарк. Установите `Baseline = true` для эталонного метода. Необязательное `Description`. |
| `[Params(values)]` | Свойство / Поле | Итерирует заданные значения констант времени компиляции. Несколько свойств `[Params]` создают декартово произведение. |
| `[GlobalSetup]` | Метод | Вызывается **один раз** для каждой комбинации параметров, перед запуском бенчмарков. |
| `[GlobalCleanup]` | Метод | Вызывается **один раз** для каждой комбинации параметров, после запуска бенчмарков. |
| `[IterationSetup]` | Метод | Вызывается перед **каждым образцом** (не учитывается во времени). |
| `[IterationCleanup]` | Метод | Вызывается после **каждого образца** (не учитывается во времени). |

Методы `[Benchmark]` должны быть методами экземпляра, не быть обобщенными и не иметь параметров. Lifecycle-методы должны быть методами экземпляра, не быть обобщенными, не иметь параметров и возвращать `void`. Целями `[Params]` должны быть записываемые свойства экземпляра или поля экземпляра без `readonly`.

### Полный пример

```csharp
using PicoBench;

[BenchmarkClass(Description = "Сравнение стратегий конкатенации строк")]
public partial class StringBenchmarks
{
    [Params(10, 100, 1000)]
    public int N { get; set; }

    [GlobalSetup]
    public void Setup() { /* подготовка данных для текущего N */ }

    [GlobalCleanup]
    public void Cleanup() { /* освобождение ресурсов */ }

    [IterationSetup]
    public void BeforeSample() { /* подготовка для каждого образца */ }

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

### Запуск

```csharp
// Создать экземпляр внутри:
var suite = BenchmarkRunner.Run<StringBenchmarks>(BenchmarkConfig.Quick);

// Или с предварительно настроенным экземпляром:
var instance = new StringBenchmarks();
var suite2 = BenchmarkRunner.Run(instance, BenchmarkConfig.Quick);
```

---

## Конфигурация

### Предустановки

| Предустановка | Разогрев | Образцы | Базовых итераций/образец | Автокалибровка | Сценарий использования |
|--------|--------|---------|--------------------------|----------------|----------|
| `Quick` | 100 | 10 | 1,000 | Да | Быстрая итерация / CI |
| `Default` | 1,000 | 100 | 10,000 | Нет | Общий бенчмаркинг |
| `Precise` | 5,000 | 200 | 50,000 | Да | Финальные измерения |

### Пользовательская конфигурация

```csharp
var config = new BenchmarkConfig
{
    WarmupIterations    = 500,
    SampleCount         = 50,
    IterationsPerSample = 5000,
    RetainSamples       = true,  // Сохранять сырые данные TimingSample
    AutoCalibrateIterations = true,
    MinSampleTime       = TimeSpan.FromMilliseconds(0.5),
    MaxAutoIterationsPerSample = 1_000_000
};

var result = Benchmark.Run("Test", action, config);
```

При включенной автокалибровке PicoBench увеличивает `IterationsPerSample`, пока не будет достигнут минимальный бюджет времени на образец или предел `MaxAutoIterationsPerSample`. Это особенно полезно для очень быстрых операций, которые иначе будут сильно зависеть от шума таймера.

---

## Форматтеры вывода

Пять встроенных форматтеров реализуют `IFormatter`:

```csharp
using PicoBench.Formatters;

var console  = new ConsoleFormatter();     // Таблицы консоли с рисованием рамок
var markdown = new MarkdownFormatter();    // Markdown таблицы, совместимые с GitHub
var html     = new HtmlFormatter();        // Стилизованные HTML отчеты
var csv      = new CsvFormatter();         // CSV для анализа данных

// Статический помощник для сводок сравнений:
Console.WriteLine(SummaryFormatter.Format(suite.Comparisons));
```

Вывод Console, Markdown, HTML и CSV теперь включает метаданные, ориентированные на точность: стандартную ошибку, относительное стандартное отклонение и пояснения по счетчику CPU, если они доступны.

### Цели форматирования

```csharp
formatter.Format(result);               // Одиночный BenchmarkResult
formatter.Format(results);              // IEnumerable<BenchmarkResult>
formatter.Format(comparison);           // Одиночный ComparisonResult
formatter.Format(comparisons);          // IEnumerable<ComparisonResult>
formatter.Format(suite);                // Полный BenchmarkSuite
```

### Опции форматтера

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
// Также доступны: FormatterOptions.Default, .Compact, .Minimal
```

### Сохранение результатов

```csharp
var dir = Path.Combine(AppContext.BaseDirectory, "results");
Directory.CreateDirectory(dir);

File.WriteAllText(Path.Combine(dir, "results.md"),   new MarkdownFormatter().Format(suite));
File.WriteAllText(Path.Combine(dir, "results.html"), new HtmlFormatter().Format(suite));
File.WriteAllText(Path.Combine(dir, "results.csv"),  new CsvFormatter().Format(suite));
```

---

## Модель результатов

| Тип | Описание |
|------|-------------|
| `BenchmarkResult` | Имя, Статистика, Образцы, ИтерацийНаОбразец, КоличествоОбразцов, Метки, Категория |
| `ComparisonResult` | Базовый, Кандидат, Ускорение, Быстрее, ПроцентУлучшения |
| `BenchmarkSuite` | Имя, Описание, Результаты, Сравнения, Окружение, Длительность |
| `Statistics` | Среднее, P50, P90, P95, P99, Минимум, Максимум, СтандартноеОтклонение, StandardError, RelativeStdDevPercent, ЦикловCpuНаОперацию, GcInfo |
| `TimingSample` | ПрошедшиеНаносекунды, ПрошедшиеМиллисекунды, ПрошедшиеТики, ЦиклыCpu, GcInfo |
| `GcInfo` | Gen0, Gen1, Gen2, Всего, Нулевое |
| `EnvironmentInfo` | ОС, Архитектура, ВерсияСредыВыполнения, КоличествоПроцессоров, Конфигурация, тип/доступность счетчика CPU |

---

## Архитектура

```
src/
+-- PicoBench/                        # Основная библиотека (netstandard2.0)
|   +-- Benchmark.cs                   # Императивный API (Run, Compare, RunScoped)
|   +-- BenchmarkRunner.cs             # Точка входа на основе атрибутов (Run<T>)
|   +-- BenchmarkConfig.cs             # Конфигурация с предустановками
|   +-- Attributes.cs                  # 7 атрибутов бенчмаркинга
|   +-- IBenchmarkClass.cs             # Интерфейс, создаваемый генератором
|   +-- Runner.cs                      # Низкоуровневый движок измерения времени
|   +-- StatisticsCalculator.cs        # Вычисление процентилей / статистики
|   +-- Models.cs                      # Типы результатов
|   +-- Formatters/
|       +-- IFormatter.cs              # IFormatter, FormatterOptions & FormatterBase
|       +-- ConsoleFormatter.cs        # Таблицы консоли с рисованием рамок
|       +-- MarkdownFormatter.cs       # Markdown таблицы GitHub
|       +-- HtmlFormatter.cs           # Стилизованные HTML отчеты
|       +-- CsvFormatter.cs            # Экспорт CSV
|       +-- SummaryFormatter.cs        # Сводка побед/поражений
|
+-- PicoBench.Generators/            # Генератор исходного кода (netstandard2.0)
    +-- BenchmarkGenerator.cs          # Точка входа IIncrementalGenerator
    +-- Emitter.cs                     # Генератор кода C# (безопасно для AOT)
    +-- Models.cs                      # Модели анализа Roslyn
```

---

## Особенности платформы

| Особенность | Windows | Linux | macOS |
|---------|---------|-------|-------|
| Высокоточное измерение времени | Stopwatch | Stopwatch | Stopwatch |
| Отслеживание GC (Gen0/1/2) | Да | Да | Да |
| Подсчет циклов CPU | `QueryThreadCycleTime` | `perf_event_open` | `mach_absolute_time` |
| Повышение приоритета процесса | Да | Да | Да |

На macOS экспортируемый счетчик CPU является высокоточным монотонным прокси, а не архитектурным счетчиком циклов. `EnvironmentInfo` и вывод форматтеров явно показывают это различие.

---

## Примеры

| Пример | Стиль API | Описание |
|--------|-----------|-------------|
| `StringVsStringBuilder` | Императивный | Сравнивает `string +=`, `StringBuilder` и `StringBuilder` с емкостью |
| `AttributeBased` | На основе атрибутов | То же сравнение с использованием `[Benchmark]`, `[Params]` и генератора исходного кода |
| `CollectionBenchmarks` | На основе атрибутов | Поиск в List vs Dictionary vs HashSet - демонстрирует каждый атрибут |

```bash
dotnet run --project samples/StringVsStringBuilder -c Release
dotnet run --project samples/AttributeBased -c Release
dotnet run --project samples/CollectionBenchmarks -c Release
```

---

## Сравнение с BenchmarkDotNet

| Особенность | PicoBench | BenchmarkDotNet |
|---------|-----------|----------------|
| Зависимости | 0 | Много |
| Размер пакета | Маленький | Большой |
| Целевая платформа | netstandard2.0 | net6.0+ |
| Поддержка AOT | Генератор исходного кода | На основе рефлексии |
| API атрибутов | `[Benchmark]`, `[Params]` | `[Benchmark]`, `[Params]` |
| Время настройки | Мгновенно | Секунды |
| Форматы вывода | 5 | 10+ |
| Статистическая глубина | Хорошая | Обширная |
| Сценарий использования | Быстрые A/B тесты, CI, AOT приложения | Детальный анализ, публикации |

---

## Лицензия

Лицензия MIT - подробности см. в файле [LICENSE](LICENSE).

## Вклад

1. Форкните репозиторий
2. Создайте ветку функции
3. Внесите изменения с тестами
4. Отправьте pull request
