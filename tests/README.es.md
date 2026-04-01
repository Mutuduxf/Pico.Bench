# Pruebas

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

Pruebas unitarias para **PicoBench** usando el framework de pruebas [TUnit](https://github.com/thomhurst/TUnit).

**Total: 454 pruebas**

## Ejecución

```bash
dotnet run --project tests/PicoBench.Tests/PicoBench.Tests.csproj -c Debug
```

## Categorías de Pruebas

### Formatters/ (224 pruebas)

Pruebas para los cinco formateadores de salida y su infraestructura de soporte.

| Archivo | Pruebas | Descripción |
|------|-------|-------------|
| `ConsoleFormatterTests.cs` | 40+ | Generación de tablas con dibujo de cajas, alineación, codificación |
| `MarkdownFormatterTests.cs` | 40+ | Renderizado de tablas Markdown de GitHub |
| `HtmlFormatterTests.cs` | 40+ | Generación de informes HTML con estilos |
| `CsvFormatterTests.cs` | 40+ | Exportación CSV con escape adecuado |
| `SummaryFormatterTests.cs` | 20+ | Texto de resumen de ganancias/pérdidas |
| `FormatterBaseTests.cs` | 15+ | Comportamiento de clase base Template Method |
| `FormatterOptionsTests.cs` | 10+ | Valores predeterminados de opciones, preajustes, resolución de rutas |
| `CrossPlatformTests.cs` | 10+ | Consistencia de fin de línea y codificación |

### Formatters/Integration/ (8 pruebas)

| Archivo | Pruebas | Descripción |
|------|-------|-------------|
| `FormatterIntegrationTests.cs` | 8 | Formateo de extremo a extremo de objetos `BenchmarkSuite` completos |

### Attributes/ (18 pruebas)

| Archivo | Pruebas | Descripción |
|------|-------|-------------|
| `AttributeTests.cs` | 18 | Los siete atributos: valores predeterminados, configuración de propiedades, objetivos `AttributeUsage`, almacenamiento de valores `[Params]` |

### BenchmarkRunnerTests.cs (8 pruebas)

| Archivo | Pruebas | Descripción |
|------|-------|-------------|
| `BenchmarkRunnerTests.cs` | 8 | `BenchmarkRunner.Run<T>()` con instancia sin parámetros / preconfigurada, comprobaciones de nulos, propagación de configuración |

### Generators/ (47 pruebas)

| Archivo | Pruebas | Descripción |
|------|-------|-------------|
| `EmitterTests.cs` | 25 | Emisión de código del generador de código fuente: estructura de clase, iteración de parámetros, ganchos de configuración/limpieza, comparaciones de línea base, calificación `global::` |
| `ModelsTests.cs` | 22 | Igualdad de `BenchmarkClassModel`, `BenchmarkMethodModel`, `ParamsPropertyModel`, códigos hash, casos límite |
| `BenchmarkGeneratorDiagnosticsTests.cs` | 10+ | Diagnósticos end-to-end del generador para firmas inválidas, baselines duplicadas, `[Params]` inválidos y emisión de parámetros enum |

### Cobertura del runtime principal

| Archivo | Pruebas | Descripción |
|------|-------|-------------|
| `BenchmarkTests.cs` | 40+ | API imperativa, ejecución scoped, muestras retenidas, comparaciones y comportamiento de auto-calibración |
| `StatisticsCalculatorTests.cs` | 10+ | Cálculo estadístico incluyendo error estándar, ciclos de CPU y casos límite |
| `ModelsTests.cs` | 40+ | Validación del modelo de resultados, metadatos del contador de CPU y ayudas de varianza |

Las pruebas de formateadores ahora también cubren salidas orientadas a la precisión como error estándar, desviación estándar relativa y notas del contador de CPU en Console, Markdown, HTML y CSV.

### TestData/

Clases de fábrica para construir fixtures de prueba consistentes:

| Archivo | Propósito |
|------|---------|
| `BenchmarkResultFactory.cs` | Crea instancias de `BenchmarkResult` con valores predeterminados sensatos |
| `BenchmarkSuiteFactory.cs` | Crea `BenchmarkSuite` con resultados y comparaciones |
| `ComparisonResultFactory.cs` | Crea pares de `ComparisonResult` |
| `GcInfoFactory.cs` | Crea registros de `GcInfo` |
| `StatisticsFactory.cs` | Crea `Statistics` con distribuciones realistas |

### Utilities/

| Archivo | Propósito |
|------|---------|
| `FileSystemHelper.cs` | Gestión de directorios temporales para pruebas de salida de archivos |
| `TestContextLogger.cs` | Ayudante de registro para el contexto de prueba TUnit |
