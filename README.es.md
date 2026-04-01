# PicoBench

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

Una biblioteca de benchmarking ligera y sin dependencias para .NET con **dos API complementarias**: una API imperativa fluida y una API basada en atributos, generada por código fuente, completamente **compatible con AOT**.

## Características

- **Cero Dependencias** - Implementación pura de .NET, no se requieren paquetes externos
- **Dos APIs** - Imperativa (`Benchmark.Run`) para pruebas ad hoc; basada en atributos (`[Benchmark]` + generador de código fuente) para suites estructuradas
- **Generador de Código Fuente Compatible con AOT** - El generador incremental emite llamadas directas a métodos sin reflexión en tiempo de ejecución
- **Multiplataforma** - Soporte completo para Windows, Linux y macOS
- **Cronometría de Alta Precisión** - Utiliza `Stopwatch` con granularidad a nivel de nanosegundos
- **Seguimiento de GC** - Monitorea los recuentos de recolección Gen0/Gen1/Gen2 durante los benchmarks
- **Conteo de Ciclos de CPU** - Conteo de ciclos a nivel de hardware (Windows vía `QueryThreadCycleTime`, Linux vía `perf_event`, macOS vía `mach_absolute_time`)
- **Análisis Estadístico** - Media, Mediana, P90, P95, P99, Mínimo, Máximo, Desviación Estándar, error estándar y varianza relativa
- **Múltiples Formatos de Salida** - Consola, Markdown, HTML, CSV y resumen programático
- **Benchmarks Parametrizados** - Atributo `[Params]` con iteración automática de producto cartesiano
- **Soporte de Comparación** - Línea base vs candidato con cálculos de aceleración
- **Configurable** - Preajustes Quick, Default y Precise, auto-calibración o configuración completamente personalizada
- **netstandard2.0** - Compatible con .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+

## Instalación

Referencia el paquete NuGet **PicoBench**. El generador de código fuente (`PicoBench.Generators`) se incluye automáticamente como un analizador - no se necesita referencia adicional.

```bash
dotnet add package PicoBench
```

## Inicio Rápido

### API Imperativa

```csharp
using PicoBench;

var result = Benchmark.Run("My Benchmark", () =>
{
    Thread.SpinWait(100);
});

Console.WriteLine($"Average: {result.Statistics.Avg:F1} ns/op");
```

### API Basada en Atributos (Generada por Código Fuente)

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

> La clase **debe** ser `partial`. El generador de código fuente emite una implementación `IBenchmarkClass` en tiempo de compilación - sin reflexión, completamente segura para AOT.

> El uso inválido de atributos ahora produce diagnósticos del generador para errores comunes como clases no `partial`, baselines duplicadas, firmas de ciclo de vida no válidas y valores `[Params]` incompatibles.

---

## Referencia de API Imperativa

### Benchmark Básico

```csharp
using PicoBench;
using PicoBench.Formatters;

var result = Benchmark.Run("SpinWait", () => Thread.SpinWait(100));
Console.WriteLine(new ConsoleFormatter().Format(result));
```

### Benchmark con Estado (Evitar Closures)

```csharp
var data = new byte[1024];
var result = Benchmark.Run("ArrayCopy", data, static d =>
{
    var copy = new byte[d.Length];
    Buffer.BlockCopy(d, 0, copy, 0, d.Length);
});
```

### Benchmarks con Alcance (Amigable con DI)

```csharp
var result = Benchmark.RunScoped("DbQuery",
    () => new MyDbContext(),
    static ctx => ctx.Users.FirstOrDefault()
);
// Se crea un nuevo alcance por muestra; el alcance se elimina después de cada muestra.
```

### Comparar Dos Implementaciones

```csharp
var comparison = Benchmark.Compare(
    "String vs StringBuilder",
    "String Concat",  () => { var s = ""; for (int i = 0; i < 100; i++) s += "a"; },
    "StringBuilder",  () => { var sb = new StringBuilder(); for (int i = 0; i < 100; i++) sb.Append('a'); _ = sb.ToString(); }
);

Console.WriteLine($"Speedup: {comparison.Speedup:F2}x ({comparison.ImprovementPercent:F1}%)");
```

### Avanzado: Calentamiento, Configuración y Limpieza Separados

```csharp
var result = Benchmark.Run(
    name:     "Custom",
    action:   () => DoWork(),
    warmup:   () => DoWork(),      // null para omitir calentamiento
    config:   BenchmarkConfig.Precise,
    setup:    () => PrepareState(), // llamado antes de cada muestra (no cronometrado)
    teardown: () => CleanUp()       // llamado después de cada muestra (no cronometrado)
);
```

---

## Referencia de API Basada en Atributos

Decora una clase **partial** con `[BenchmarkClass]` y sus métodos/propiedades con los atributos siguientes. El generador de código fuente emite todo el código de conexión en tiempo de compilación.

### Atributos

| Atributo | Destino | Descripción |
|-----------|--------|-------------|
| `[BenchmarkClass]` | Clase | Marca la clase para generación de código. Propiedad opcional `Description`. |
| `[Benchmark]` | Método | Marca un método sin parámetros como benchmark. Establece `Baseline = true` para el método de referencia. Opcional `Description`. |
| `[Params(values)]` | Propiedad / Campo | Itera los valores constantes en tiempo de compilación dados. Múltiples propiedades `[Params]` producen un producto cartesiano. |
| `[GlobalSetup]` | Método | Llamado **una vez** por combinación de parámetros, antes de que se ejecuten los benchmarks. |
| `[GlobalCleanup]` | Método | Llamado **una vez** por combinación de parámetros, después de que se ejecuten los benchmarks. |
| `[IterationSetup]` | Método | Llamado antes de **cada muestra** (no cronometrado). |
| `[IterationCleanup]` | Método | Llamado después de **cada muestra** (no cronometrado). |

Los métodos `[Benchmark]` deben ser de instancia, no genéricos y sin parámetros. Los métodos de ciclo de vida deben ser de instancia, no genéricos, sin parámetros y `void`. Los destinos de `[Params]` deben ser propiedades de instancia editables o campos de instancia no `readonly`.

### Ejemplo Completo

```csharp
using PicoBench;

[BenchmarkClass(Description = "Comparando estrategias de concatenación de cadenas")]
public partial class StringBenchmarks
{
    [Params(10, 100, 1000)]
    public int N { get; set; }

    [GlobalSetup]
    public void Setup() { /* preparar datos para N actual */ }

    [GlobalCleanup]
    public void Cleanup() { /* liberar recursos */ }

    [IterationSetup]
    public void BeforeSample() { /* preparación por muestra */ }

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

### Ejecución

```csharp
// Crear instancia internamente:
var suite = BenchmarkRunner.Run<StringBenchmarks>(BenchmarkConfig.Quick);

// O con una instancia preconfigurada:
var instance = new StringBenchmarks();
var suite2 = BenchmarkRunner.Run(instance, BenchmarkConfig.Quick);
```

---

## Configuración

### Preajustes

| Preajuste | Calentamiento | Muestras | Iteraciones base/Muestra | Auto-calibración | Caso de Uso |
|--------|--------|---------|--------------------------|------------------|----------|
| `Quick` | 100 | 10 | 1,000 | Sí | Iteración rápida / CI |
| `Default` | 1,000 | 100 | 10,000 | No | Benchmarking general |
| `Precise` | 5,000 | 200 | 50,000 | Sí | Mediciones finales |

### Configuración Personalizada

```csharp
var config = new BenchmarkConfig
{
    WarmupIterations    = 500,
    SampleCount         = 50,
    IterationsPerSample = 5000,
    RetainSamples       = true,  // Conservar datos brutos de TimingSample
    AutoCalibrateIterations = true,
    MinSampleTime       = TimeSpan.FromMilliseconds(0.5),
    MaxAutoIterationsPerSample = 1_000_000
};

var result = Benchmark.Run("Test", action, config);
```

Cuando la auto-calibración está habilitada, PicoBench incrementa `IterationsPerSample` hasta alcanzar un presupuesto mínimo de tiempo por muestra o hasta llegar a `MaxAutoIterationsPerSample`. Esto es especialmente útil para operaciones ultrarrápidas dominadas por el ruido del temporizador.

---

## Formateadores de Salida

Cinco formateadores incorporados implementan `IFormatter`:

```csharp
using PicoBench.Formatters;

var console  = new ConsoleFormatter();     // Tablas de consola con dibujo de cajas
var markdown = new MarkdownFormatter();    // Markdown amigable con GitHub
var html     = new HtmlFormatter();        // Informe HTML estilizado
var csv      = new CsvFormatter();         // CSV para análisis de datos

// Ayudante estático para resúmenes de comparación:
Console.WriteLine(SummaryFormatter.Format(suite.Comparisons));
```

Las salidas de Console, Markdown, HTML y CSV ahora incluyen metadatos orientados a la precisión, como error estándar, desviación estándar relativa y notas sobre el contador de CPU cuando están disponibles.

### Objetivos de Formateo

```csharp
formatter.Format(result);               // BenchmarkResult único
formatter.Format(results);              // IEnumerable<BenchmarkResult>
formatter.Format(comparison);           // ComparisonResult único
formatter.Format(comparisons);          // IEnumerable<ComparisonResult>
formatter.Format(suite);                // BenchmarkSuite completo
```

### Opciones del Formateador

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
// También disponibles: FormatterOptions.Default, .Compact, .Minimal
```

### Guardar Resultados

```csharp
var dir = Path.Combine(AppContext.BaseDirectory, "results");
Directory.CreateDirectory(dir);

File.WriteAllText(Path.Combine(dir, "results.md"),   new MarkdownFormatter().Format(suite));
File.WriteAllText(Path.Combine(dir, "results.html"), new HtmlFormatter().Format(suite));
File.WriteAllText(Path.Combine(dir, "results.csv"),  new CsvFormatter().Format(suite));
```

---

## Modelo de Resultados

| Tipo | Descripción |
|------|-------------|
| `BenchmarkResult` | Nombre, Estadísticas, Muestras, IteracionesPorMuestra, RecuentoDeMuestras, Etiquetas, Categoría |
| `ComparisonResult` | Línea base, Candidato, Aceleración, EsMásRápido, PorcentajeDeMejora |
| `BenchmarkSuite` | Nombre, Descripción, Resultados, Comparaciones, Entorno, Duración |
| `Statistics` | Promedio, P50, P90, P95, P99, Mínimo, Máximo, DesviaciónEstándar, StandardError, RelativeStdDevPercent, CiclosDeCpuPorOp, GcInfo |
| `TimingSample` | NanosegundosTranscurridos, MilisegundosTranscurridos, TicksTranscurridos, CiclosDeCpu, GcInfo |
| `GcInfo` | Gen0, Gen1, Gen2, Total, EsCero |
| `EnvironmentInfo` | SO, Arquitectura, VersiónDelRuntime, RecuentoDeProcesadores, Configuración, tipo/disponibilidad del contador de CPU |

---

## Arquitectura

```
src/
+-- PicoBench/                        # Biblioteca principal (netstandard2.0)
|   +-- Benchmark.cs                   # API imperativa (Run, Compare, RunScoped)
|   +-- BenchmarkRunner.cs             # Punto de entrada basado en atributos (Run<T>)
|   +-- BenchmarkConfig.cs             # Configuración con preajustes
|   +-- Attributes.cs                  # 7 atributos de benchmark
|   +-- IBenchmarkClass.cs             # Interfaz emitida por el generador
|   +-- Runner.cs                      # Motor de cronometría de bajo nivel
|   +-- StatisticsCalculator.cs        # Cálculo de percentiles / estadísticas
|   +-- Models.cs                      # Tipos de resultados
|   +-- Formatters/
|       +-- IFormatter.cs              # IFormatter, FormatterOptions & FormatterBase
|       +-- ConsoleFormatter.cs        # Tablas de consola con dibujo de cajas
|       +-- MarkdownFormatter.cs       # Tablas Markdown de GitHub
|       +-- HtmlFormatter.cs           # Informes HTML estilizados
|       +-- CsvFormatter.cs            # Exportación CSV
|       +-- SummaryFormatter.cs        # Resumen de ganancias/pérdidas
|
+-- PicoBench.Generators/            # Generador de código fuente (netstandard2.0)
    +-- BenchmarkGenerator.cs          # Punto de entrada IIncrementalGenerator
    +-- Emitter.cs                     # Emisor de código C# (seguro para AOT)
    +-- Models.cs                      # Modelos de análisis Roslyn
```

---

## Características Específicas de Plataforma

| Característica | Windows | Linux | macOS |
|---------|---------|-------|-------|
| Cronometría de alta precisión | Stopwatch | Stopwatch | Stopwatch |
| Seguimiento de GC (Gen0/1/2) | Sí | Sí | Sí |
| Conteo de ciclos de CPU | `QueryThreadCycleTime` | `perf_event_open` | `mach_absolute_time` |
| Aumento de prioridad de proceso | Sí | Sí | Sí |

En macOS, el contador de CPU exportado es un proxy monotónico de alta resolución y no ciclos arquitectónicos reales. `EnvironmentInfo` y la salida de los formateadores muestran esta diferencia explícitamente.

---

## Ejemplos

| Ejemplo | Estilo de API | Descripción |
|--------|-----------|-------------|
| `StringVsStringBuilder` | Imperativa | Compara `string +=`, `StringBuilder` y `StringBuilder` con capacidad |
| `AttributeBased` | Basada en atributos | Misma comparación usando `[Benchmark]`, `[Params]` y el generador de código fuente |
| `CollectionBenchmarks` | Basada en atributos | Búsqueda List vs Dictionary vs HashSet - muestra cada atributo |

```bash
dotnet run --project samples/StringVsStringBuilder -c Release
dotnet run --project samples/AttributeBased -c Release
dotnet run --project samples/CollectionBenchmarks -c Release
```

---

## Comparación con BenchmarkDotNet

| Característica | PicoBench | BenchmarkDotNet |
|---------|-----------|----------------|
| Dependencias | 0 | Muchas |
| Tamaño del paquete | Pequeño | Grande |
| Framework objetivo | netstandard2.0 | net6.0+ |
| Soporte AOT | Generador de código fuente | Basado en reflexión |
| API de atributos | `[Benchmark]`, `[Params]` | `[Benchmark]`, `[Params]` |
| Tiempo de configuración | Instantáneo | Segundos |
| Formatos de salida | 5 | 10+ |
| Profundidad estadística | Buena | Extensa |
| Caso de uso | Pruebas A/B rápidas, CI, aplicaciones AOT | Análisis detallado, publicaciones |

---

## Licencia

Licencia MIT - consulte el archivo [LICENSE](LICENSE) para más detalles.

## Contribuir

1. Bifurca el repositorio
2. Crea una rama de características
3. Realiza cambios con pruebas
4. Envía una solicitud de extracción
