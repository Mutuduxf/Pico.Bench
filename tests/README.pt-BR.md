# Testes

[English](README.md) | [中文](README.zh-CN.md) | [中文 (Traditional)](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

Testes unitários para **PicoBench** usando o framework de testes [TUnit](https://github.com/thomhurst/TUnit).

**Total: 454 testes**

## Executando

```bash
dotnet run --project tests/PicoBench.Tests/PicoBench.Tests.csproj -c Debug
```

## Categorias de Testes

### Formatters/ (224 testes)

Testes para todos os cinco formatadores de saída e sua infraestrutura de suporte.

| Arquivo | Testes | Descrição |
|------|-------|-------------|
| `ConsoleFormatterTests.cs` | 40+ | Geração de tabelas com desenho de caixas, alinhamento, codificação |
| `MarkdownFormatterTests.cs` | 40+ | Renderização de tabelas em GitHub Markdown |
| `HtmlFormatterTests.cs` | 40+ | Geração de relatórios HTML com estilos |
| `CsvFormatterTests.cs` | 40+ | Exportação CSV com escape adequado |
| `SummaryFormatterTests.cs` | 20+ | Texto de resumo de vitórias/derrotas |
| `FormatterBaseTests.cs` | 15+ | Comportamento da classe base Template Method |
| `FormatterOptionsTests.cs` | 10+ | Padrões de opções, predefinições, resolução de caminhos |
| `CrossPlatformTests.cs` | 10+ | Consistência de terminações de linha e codificação |

### Formatters/Integration/ (8 testes)

| Arquivo | Testes | Descrição |
|------|-------|-------------|
| `FormatterIntegrationTests.cs` | 8 | Formatação de ponta a ponta de objetos completos `BenchmarkSuite` |

### Attributes/ (18 testes)

| Arquivo | Testes | Descrição |
|------|-------|-------------|
| `AttributeTests.cs` | 18 | Todos os sete atributos: valores padrão, configuração de propriedades, destinos `AttributeUsage`, armazenamento de valores `[Params]` |

### BenchmarkRunnerTests.cs (8 testes)

| Arquivo | Testes | Descrição |
|------|-------|-------------|
| `BenchmarkRunnerTests.cs` | 8 | `BenchmarkRunner.Run<T>()` com instância sem parâmetros / pré-configurada, verificações de nulidade, propagação de configuração |

### Generators/ (47 testes)

| Arquivo | Testes | Descrição |
|------|-------|-------------|
| `EmitterTests.cs` | 25 | Emissão de código do gerador de fonte: estrutura de classe, iteração de parâmetros, hooks de setup/teardown, comparações de baseline, qualificação `global::` |
| `ModelsTests.cs` | 22 | `BenchmarkClassModel`, `BenchmarkMethodModel`, `ParamsPropertyModel` igualdade, códigos hash, casos extremos |
| `BenchmarkGeneratorDiagnosticsTests.cs` | 10+ | Diagnósticos end-to-end do gerador para assinaturas inválidas, baselines duplicados, `[Params]` inválidos e emissão de parâmetros enum |

### Cobertura do runtime principal

| Arquivo | Testes | Descrição |
|------|-------|-------------|
| `BenchmarkTests.cs` | 40+ | API imperativa, execução scoped, amostras retidas, comparações e comportamento de auto-calibração |
| `StatisticsCalculatorTests.cs` | 10+ | Cálculo estatístico incluindo erro padrão, ciclos de CPU e casos extremos |
| `ModelsTests.cs` | 40+ | Validação do modelo de resultados, metadados do contador de CPU e auxiliares de variância |

Os testes de formatadores agora também cobrem saídas orientadas à precisão, como erro padrão, desvio padrão relativo e observações do contador de CPU em Console, Markdown, HTML e CSV.

### TestData/

Classes de fábrica para construir fixtures de teste consistentes:

| Arquivo | Propósito |
|------|---------|
| `BenchmarkResultFactory.cs` | Cria instâncias `BenchmarkResult` com padrões sensíveis |
| `BenchmarkSuiteFactory.cs` | Cria `BenchmarkSuite` com resultados e comparações |
| `ComparisonResultFactory.cs` | Cria pares `ComparisonResult` |
| `GcInfoFactory.cs` | Cria registros `GcInfo` |
| `StatisticsFactory.cs` | Cria `Statistics` com distribuições realistas |

### Utilities/

| Arquivo | Propósito |
|------|---------|
| `FileSystemHelper.cs` | Gerenciamento de diretórios temporários para testes de saída de arquivo |
| `TestContextLogger.cs` | Auxiliar de logging para contexto de teste TUnit |
