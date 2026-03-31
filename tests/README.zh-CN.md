# 测试

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

**PicoBench** 的单元测试，使用 [TUnit](https://github.com/thomhurst/TUnit) 测试框架。

**总计：313 个测试**

## 运行

```bash
dotnet run --project tests/PicoBench.Tests/PicoBench.Tests.csproj -c Debug
```

## 测试分类

### Formatters/ (224 个测试)

所有五个输出格式化器及其支持基础设施的测试。

| 文件 | 测试数 | 描述 |
|------|-------|-------------|
| `ConsoleFormatterTests.cs` | 40+ | 盒绘制表格生成、对齐、编码 |
| `MarkdownFormatterTests.cs` | 40+ | GitHub Markdown 表格渲染 |
| `HtmlFormatterTests.cs` | 40+ | 带样式的 HTML 报告生成 |
| `CsvFormatterTests.cs` | 40+ | 带正确转义的 CSV 导出 |
| `SummaryFormatterTests.cs` | 20+ | 胜/负摘要文本 |
| `FormatterBaseTests.cs` | 15+ | 模板方法基类行为 |
| `FormatterOptionsTests.cs` | 10+ | 选项默认值、预设、路径解析 |
| `CrossPlatformTests.cs` | 10+ | 行尾和编码一致性 |

### Formatters/Integration/ (8 个测试)

| 文件 | 测试数 | 描述 |
|------|-------|-------------|
| `FormatterIntegrationTests.cs` | 8 | 完整 `BenchmarkSuite` 对象的端到端格式化 |

### Attributes/ (18 个测试)

| 文件 | 测试数 | 描述 |
|------|-------|-------------|
| `AttributeTests.cs` | 18 | 所有七个属性：默认值、属性设置、`AttributeUsage` 目标、`[Params]` 值存储 |

### BenchmarkRunnerTests.cs (8 个测试)

| 文件 | 测试数 | 描述 |
|------|-------|-------------|
| `BenchmarkRunnerTests.cs` | 8 | `BenchmarkRunner.Run<T>()` 带无参数/预配置实例、空值检查、配置传播 |

### Generators/ (47 个测试)

| 文件 | 测试数 | 描述 |
|------|-------|-------------|
| `EmitterTests.cs` | 25 | 源生成器代码发射：类结构、参数迭代、设置/清理钩子、基准比较、`global::` 限定 |
| `ModelsTests.cs` | 22 | `BenchmarkClassModel`、`BenchmarkMethodModel`、`ParamsPropertyModel` 相等性、哈希码、边界情况 |

### TestData/

用于构建一致测试夹具的工厂类：

| 文件 | 用途 |
|------|---------|
| `BenchmarkResultFactory.cs` | 创建具有合理默认值的 `BenchmarkResult` 实例 |
| `BenchmarkSuiteFactory.cs` | 创建包含结果和比较的 `BenchmarkSuite` |
| `ComparisonResultFactory.cs` | 创建 `ComparisonResult` 对 |
| `GcInfoFactory.cs` | 创建 `GcInfo` 记录 |
| `StatisticsFactory.cs` | 创建具有真实分布的 `Statistics` |

### Utilities/

| 文件 | 用途 |
|------|---------|
| `FileSystemHelper.cs` | 文件输出测试的临时目录管理 |
| `TestContextLogger.cs` | TUnit 测试上下文的日志记录助手 |
