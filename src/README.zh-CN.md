# 源项目

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

此目录包含构成 PicoBench 的两个库项目。

## PicoBench

主要的基准测试库，目标为 **netstandard2.0**，零外部依赖。

### 关键文件

| 文件 | 用途 |
|------|---------|
| `Benchmark.cs` | 命令式 API - `Run()`、`Run<TState>()`、`RunScoped<TScope>()`、`Compare()` |
| `BenchmarkRunner.cs` | 基于属性的入口点 - `Run<T>()` |
| `Attributes.cs` | 七个属性：`[BenchmarkClass]`、`[Benchmark]`、`[Params]`、`[GlobalSetup]`、`[GlobalCleanup]`、`[IterationSetup]`、`[IterationCleanup]` |
| `IBenchmarkClass.cs` | 由源生成器在装饰类上实现的接口 |
| `BenchmarkConfig.cs` | 包含 Quick / Default / Precise 预设以及可选自动校准的配置 |
| `Runner.cs` | 底层计时流程与样本构造 |
| `Runner.Gc.cs` | GC 基线与增量跟踪 |
| `Runner.Cpu.cs` | 平台相关 CPU 计数器实现 |
| `StatisticsCalculator.cs` | 百分位数和统计计算 |
| `Models.cs` | 结果类型，包括 `Statistics` 的精度字段以及 `EnvironmentInfo` 中的 CPU 计数器元数据 |
| `Formatters/` | 四个 `IFormatter` 实现（Console、Markdown、HTML、CSV）以及 `SummaryFormatter` |

### 打包

项目将 `PicoBench.Generators` 捆绑为分析器，因此使用者自动获取源生成器：

```bash
# 添加项目引用
dotnet add reference ../PicoBench.Generators/PicoBench.Generators.csproj

# 然后在你的 .csproj 文件中的 <ProjectReference> 元素内手动添加以下属性：
# PrivateAssets="all"
# ReferenceOutputAssembly="false"
# OutputItemType="Analyzer"
```

## PicoBench.Generators

一个**增量源生成器** (`IIncrementalGenerator`)，将用 `[BenchmarkClass]` 装饰的 partial 类在编译时转换为完整的 `IBenchmarkClass` 实现。

- **目标**：netstandard2.0
- **依赖**：Microsoft.CodeAnalysis.CSharp 5.0.0
- **输出**：AOT 兼容的 C#，使用 `global::` 限定调用，无反射

### 关键文件

| 文件 | 用途 |
|------|---------|
| `BenchmarkGenerator.cs` | 生成器入口点，使用 `ForAttributeWithMetadataName` |
| `BenchmarkClassAnalyzer.cs` | 代码发射前的 Roslyn 分析与诊断 |
| `CSharpLiteralFormatter.cs` | 为生成的 `[Params]` 值格式化 C# 字面量 |
| `DiagnosticDescriptors.cs` | 集中的生成器诊断定义，用于无效基准声明 |
| `Emitter.cs` | C# 代码发射器 - 生成 `RunBenchmarks()`，包含参数迭代、设置/清理钩子和比较逻辑 |
| `Models.cs` | Roslyn 分析模型：`BenchmarkClassModel`、`BenchmarkMethodModel`、`ParamsPropertyModel`（全部实现 `IEquatable<T>` 以支持缓存） |

生成器现在会在发射代码之前验证常见错误，并对非法 benchmark 方法、生命周期方法、重复 baseline、非法 `[Params]` 目标以及不兼容参数值给出诊断。

### 生成的代码

对于如下类：

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

生成器发射一个 `partial class MyBench : IBenchmarkClass`，其中包含 `RunBenchmarks()` 方法，该方法：

1. 迭代每个 `[Params]` 值（多个属性产生笛卡尔积）
2. 设置属性，调用 `[GlobalSetup]`
3. 通过 `Benchmark.Run()` 运行每个 `[Benchmark]` 方法，使用 `[IterationSetup]`/`[IterationCleanup]` 作为设置/清理
4. 将候选方法与基准进行比较
5. 调用 `[GlobalCleanup]`
6. 返回包含所有结果和比较的 `BenchmarkSuite`
