# Ejemplos

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

Tres proyectos de ejemplo demuestran las dos API proporcionadas por PicoBench.

## StringVsStringBuilder (API Imperativa)

Utiliza la API imperativa `Benchmark.Run()` y `Benchmark.Compare()` para medir estrategias de concatenación de cadenas en varios tamaños.

**Destacados:**

- `Benchmark.Run()` con closures y `BenchmarkConfig.Quick`
- `Benchmark.Run<TState>()` con estado para evitar asignación de closures
- Creación manual de `ComparisonResult` para agrupación personalizada
- Construcción de `BenchmarkSuite` con todos los resultados y comparaciones
- Salidas a Consola, Markdown, HTML y CSV a través de `FormatterOptions` (etiquetas personalizadas)

```bash
dotnet run --project samples/StringVsStringBuilder -c Release
```

## AttributeBased (API Basada en Atributos + Generador de Código Fuente)

Reescribe el mismo benchmark de cadenas usando la API basada en atributos.

**Destacados:**

- `[BenchmarkClass]` con `Description`
- `[Params(10, 100, 1000)]` para ejecuciones parametrizadas
- `[Benchmark(Baseline = true)]` para marcar el método de referencia
- `[GlobalSetup]` para preparación por combinación de parámetros
- Ejecución de una línea a través de `BenchmarkRunner.Run<T>()`
- `SummaryFormatter` para visión rápida de ganancias/pérdidas

```csharp
[BenchmarkClass(Description = "Comparando estrategias de concatenación de cadenas")]
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

## CollectionBenchmarks (Exhibición Completa de Atributos)

Demuestra **la mayoría** de los atributos comparando el rendimiento de búsqueda de List, Dictionary y HashSet.

**Destacados:**

- `[BenchmarkClass]` con `Description`
- `[Params(100, 1_000, 10_000)]` - tres tamaños de colección
- `[GlobalSetup]` - llena las tres colecciones con datos aleatorizados
- `[GlobalCleanup]` - libera las colecciones
- `[IterationSetup]` - baraja el objetivo de búsqueda antes de cada muestra
- `[Benchmark(Baseline = true, Description = "...")]` - `List.Contains()` como línea base
- `[Benchmark(Description = "...")]` - `Dictionary.ContainsKey()` y `HashSet.Contains()`
- Salida multi-formato: Consola, Markdown, HTML, CSV

```csharp
[BenchmarkClass(Description = "Rendimiento de búsqueda: List vs Dictionary vs HashSet")]
public partial class LookupBenchmarks
{
    [Params(100, 1_000, 10_000)]
    public int N { get; set; }

    [GlobalSetup]   public void Setup()         { /* llenar colecciones */ }
    [GlobalCleanup] public void Cleanup()       { /* liberar colecciones */ }
    [IterationSetup] public void ShuffleTarget() { /* variar objetivo de búsqueda */ }

    [Benchmark(Baseline = true, Description = "Escaneo lineal O(n)")]
    public void ListContains() { _ = _list.Contains(_target); }

    [Benchmark(Description = "Búsqueda hash O(1)")]
    public void DictionaryContainsKey() { _ = _dictionary.ContainsKey(_target); }

    [Benchmark(Description = "Búsqueda hash O(1), optimizado para conjuntos")]
    public void HashSetContains() { _ = _hashSet.Contains(_target); }
}
```

```bash
dotnet run --project samples/CollectionBenchmarks -c Release
```

## Salida

Todos los ejemplos guardan los resultados en un subdirectorio `results/` bajo la carpeta de salida en formatos Markdown, HTML y CSV.

Esos informes ahora también incluyen metadatos orientados a la precisión, como error estándar, desviación estándar relativa y notas sobre el contador de CPU cuando están disponibles.
