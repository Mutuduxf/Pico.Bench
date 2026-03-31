# Тесты

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

Модульные тесты для **PicoBench** с использованием фреймворка тестирования [TUnit](https://github.com/thomhurst/TUnit).

**Всего: 313 тестов**

## Запуск

```bash
dotnet run --project tests/PicoBench.Tests/PicoBench.Tests.csproj -c Debug
```

## Категории тестов

### Formatters/ (224 теста)

Тесты для всех пяти форматтеров вывода и их поддерживающей инфраструктуры.

| Файл | Тесты | Описание |
|------|-------|-------------|
| `ConsoleFormatterTests.cs` | 40+ | Генерация таблиц с рисованием рамок, выравнивание, кодировка |
| `MarkdownFormatterTests.cs` | 40+ | Рендеринг Markdown таблиц GitHub |
| `HtmlFormatterTests.cs` | 40+ | Генерация HTML отчетов со стилями |
| `CsvFormatterTests.cs` | 40+ | Экспорт CSV с правильным экранированием |
| `SummaryFormatterTests.cs` | 20+ | Текст сводки побед/поражений |
| `FormatterBaseTests.cs` | 15+ | Поведение базового класса Template Method |
| `FormatterOptionsTests.cs` | 10+ | Значения по умолчанию опций, предустановки, разрешение путей |
| `CrossPlatformTests.cs` | 10+ | Согласованность конца строк и кодировки |

### Formatters/Integration/ (8 тестов)

| Файл | Тесты | Описание |
|------|-------|-------------|
| `FormatterIntegrationTests.cs` | 8 | Сквозное форматирование полных объектов `BenchmarkSuite` |

### Attributes/ (18 тестов)

| Файл | Тесты | Описание |
|------|-------|-------------|
| `AttributeTests.cs` | 18 | Все семь атрибутов: значения по умолчанию, установка свойств, цели `AttributeUsage`, хранение значений `[Params]` |

### BenchmarkRunnerTests.cs (8 тестов)

| Файл | Тесты | Описание |
|------|-------|-------------|
| `BenchmarkRunnerTests.cs` | 8 | `BenchmarkRunner.Run<T>()` с экземпляром без параметров / предварительно настроенным, проверки null, распространение конфигурации |

### Generators/ (47 тестов)

| Файл | Тесты | Описание |
|------|-------|-------------|
| `EmitterTests.cs` | 25 | Генерация кода генератором исходного кода: структура класса, итерация параметров, хуки настройки/очистки, сравнения базовых вариантов, квалификация `global::` |
| `ModelsTests.cs` | 22 | Равенство `BenchmarkClassModel`, `BenchmarkMethodModel`, `ParamsPropertyModel`, хэш-коды, граничные случаи |

### TestData/

Фабричные классы для построения согласованных тестовых фикстур:

| Файл | Назначение |
|------|---------|
| `BenchmarkResultFactory.cs` | Создает экземпляры `BenchmarkResult` с разумными значениями по умолчанию |
| `BenchmarkSuiteFactory.cs` | Создает `BenchmarkSuite` с результатами и сравнениями |
| `ComparisonResultFactory.cs` | Создает пары `ComparisonResult` |
| `GcInfoFactory.cs` | Создает записи `GcInfo` |
| `StatisticsFactory.cs` | Создает `Statistics` с реалистичными распределениями |

### Utilities/

| Файл | Назначение |
|------|---------|
| `FileSystemHelper.cs` | Управление временными директориями для тестов с выводом в файлы |
| `TestContextLogger.cs` | Помощник ведения журнала для контекста тестов TUnit |
