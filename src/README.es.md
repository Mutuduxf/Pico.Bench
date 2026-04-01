# Proyectos Fuente

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

Este directorio contiene los dos proyectos de biblioteca que componen PicoBench.

## PicoBench

La principal biblioteca de benchmarking con objetivo **netstandard2.0** y cero dependencias externas.

### Archivos Clave

| Archivo | Propósito |
|------|---------|
| `Benchmark.cs` | API imperativa - `Run()`, `Run<TState>()`, `RunScoped<TScope>()`, `Compare()` |
| `BenchmarkRunner.cs` | Punto de entrada basado en atributos - `Run<T>()` |
| `Attributes.cs` | Siete atributos: `[BenchmarkClass]`, `[Benchmark]`, `[Params]`, `[GlobalSetup]`, `[GlobalCleanup]`, `[IterationSetup]`, `[IterationCleanup]` |
| `IBenchmarkClass.cs` | Interfaz implementada por el generador de código fuente en clases decoradas |
| `BenchmarkConfig.cs` | Configuración con preajustes Quick / Default / Precise más auto-calibración opcional |
| `Runner.cs` | Motor de cronometría de bajo nivel con conteo de ciclos de CPU específico de plataforma |
| `StatisticsCalculator.cs` | Cálculo de percentiles y estadísticas |
| `Models.cs` | Tipos de resultados incluyendo campos de precisión en `Statistics` y metadatos del contador de CPU en `EnvironmentInfo` |
| `Formatters/` | Cinco formateadores: Console, Markdown, HTML, CSV, Summary |

### Empaquetado

El proyecto incluye `PicoBench.Generators` como un analizador para que los consumidores obtengan el generador de código fuente automáticamente:

```bash
# Agregar la referencia del proyecto
dotnet add reference ../PicoBench.Generators/PicoBench.Generators.csproj

# Luego agregue manualmente los siguientes atributos al elemento <ProjectReference> en su archivo .csproj:
# PrivateAssets="all"
# ReferenceOutputAssembly="false"
# OutputItemType="Analyzer"
```

## PicoBench.Generators

Un **generador de código fuente incremental** (`IIncrementalGenerator`) que convierte clases partial decoradas con `[BenchmarkClass]` en implementaciones completas de `IBenchmarkClass` en tiempo de compilación.

- **Objetivo**: netstandard2.0
- **Dependencia**: Microsoft.CodeAnalysis.CSharp 5.0.0
- **Salida**: C# compatible con AOT con llamadas calificadas `global::` y sin reflexión

### Archivos Clave

| Archivo | Propósito |
|------|---------|
| `BenchmarkGenerator.cs` | Punto de entrada del generador usando `ForAttributeWithMetadataName` |
| `DiagnosticDescriptors.cs` | Diagnósticos centralizados del generador para declaraciones de benchmark inválidas |
| `Emitter.cs` | Emisor de código C# - genera `RunBenchmarks()` con iteración de parámetros, ganchos de configuración/limpieza y lógica de comparación |
| `Models.cs` | Modelos de análisis Roslyn: `BenchmarkClassModel`, `BenchmarkMethodModel`, `ParamsPropertyModel` (todos `IEquatable<T>` para caché) |

El generador ahora valida errores comunes antes de emitir código e informa diagnósticos para métodos benchmark inválidos, métodos de ciclo de vida inválidos, baselines duplicadas, destinos `[Params]` no válidos y valores de parámetros incompatibles.

### Código Generado

Para una clase como:

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

El generador emite una `partial class MyBench : IBenchmarkClass` con un método `RunBenchmarks()` que:

1. Itera cada valor `[Params]` (producto cartesiano para múltiples propiedades)
2. Establece la propiedad, llama a `[GlobalSetup]`
3. Ejecuta cada método `[Benchmark]` a través de `Benchmark.Run()` con `[IterationSetup]`/`[IterationCleanup]` como configuración/limpieza
4. Compara candidatos contra la línea base
5. Llama a `[GlobalCleanup]`
6. Devuelve un `BenchmarkSuite` con todos los resultados y comparaciones
