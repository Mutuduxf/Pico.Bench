# 源項目

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

此目錄包含構成 PicoBench 的兩個庫項目。

## PicoBench

主要的基準測試庫，目標為 **netstandard2.0**，零外部依賴。

### 關鍵文件

| 文件 | 用途 |
|------|---------|
| `Benchmark.cs` | 命令式 API - `Run()`、`Run<TState>()`、`RunScoped<TScope>()`、`Compare()` |
| `BenchmarkRunner.cs` | 基於屬性的入口點 - `Run<T>()` |
| `Attributes.cs` | 七個屬性：`[BenchmarkClass]`、`[Benchmark]`、`[Params]`、`[GlobalSetup]`、`[GlobalCleanup]`、`[IterationSetup]`、`[IterationCleanup]` |
| `IBenchmarkClass.cs` | 由源生成器在裝飾類上實現的接口 |
| `BenchmarkConfig.cs` | 包含 Quick / Default / Precise 預設以及可選自動校準的配置 |
| `Runner.cs` | 底層計時引擎，支援平台特定的 CPU 週期計數 |
| `StatisticsCalculator.cs` | 百分位數和統計計算 |
| `Models.cs` | 結果類型，包括 `Statistics` 的精度欄位以及 `EnvironmentInfo` 中的 CPU 計數器元資料 |
| `Formatters/` | 五個格式化器：Console、Markdown、HTML、CSV、Summary |

### 打包

項目將 `PicoBench.Generators` 捆綁為分析器，因此使用者自動獲取源生成器：

```bash
# 添加項目引用
dotnet add reference ../PicoBench.Generators/PicoBench.Generators.csproj

# 然後在你的 .csproj 文件中的 <ProjectReference> 元素內手動添加以下屬性：
# PrivateAssets="all"
# ReferenceOutputAssembly="false"
# OutputItemType="Analyzer"
```

## PicoBench.Generators

一個**增量源生成器** (`IIncrementalGenerator`)，將用 `[BenchmarkClass]` 裝飾的 partial 類在編譯時轉換為完整的 `IBenchmarkClass` 實現。

- **目標**：netstandard2.0
- **依賴**：Microsoft.CodeAnalysis.CSharp 5.0.0
- **輸出**：AOT 相容的 C#，使用 `global::` 限定呼叫，無反射

### 關鍵文件

| 文件 | 用途 |
|------|---------|
| `BenchmarkGenerator.cs` | 生成器入口點，使用 `ForAttributeWithMetadataName` |
| `DiagnosticDescriptors.cs` | 集中的生成器診斷定義，用於無效基準聲明 |
| `Emitter.cs` | C# 代碼發射器 - 生成 `RunBenchmarks()`，包含參數迭代、設置/清理鉤子和比較邏輯 |
| `Models.cs` | Roslyn 分析模型：`BenchmarkClassModel`、`BenchmarkMethodModel`、`ParamsPropertyModel`（全部實現 `IEquatable<T>` 以支援緩存） |

生成器現在會在發射代碼之前驗證常見錯誤，並對非法 benchmark 方法、生命週期方法、重複 baseline、非法 `[Params]` 目標以及不相容參數值給出診斷。

### 生成的代碼

對於如下類：

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

生成器發射一個 `partial class MyBench : IBenchmarkClass`，其中包含 `RunBenchmarks()` 方法，該方法：

1. 迭代每個 `[Params]` 值（多個屬性產生笛卡爾積）
2. 設置屬性，呼叫 `[GlobalSetup]`
3. 通過 `Benchmark.Run()` 運行每個 `[Benchmark]` 方法，使用 `[IterationSetup]`/`[IterationCleanup]` 作為設置/清理
4. 將候選方法與基準進行比較
5. 呼叫 `[GlobalCleanup]`
6. 返回包含所有結果和比較的 `BenchmarkSuite`
