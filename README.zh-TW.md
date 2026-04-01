# PicoBench

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

一個輕量級、零依賴的 .NET 基準測試庫，提供 **兩種互補的 API**：命令式流式 API 和基於屬性、源生成的 API，完全 **AOT 相容**。

## 特性

- **零依賴** - 純 .NET 實現，無需外部套件
- **兩種 API** - 命令式 (`Benchmark.Run`) 用於臨時測試；基於屬性 (`[Benchmark]` + 源生成器) 用於結構化測試套件
- **AOT 相容的源生成器** - 增量生成器在執行時生成直接方法呼叫，零反射
- **跨平台** - 完全支援 Windows、Linux 和 macOS
- **高精度計時** - 使用 `Stopwatch`，奈秒級精度
- **GC 追蹤** - 監控基準測試期間的 Gen0/Gen1/Gen2 回收計數
- **CPU 週期計數** - 硬體級週期計數（Windows 通過 `QueryThreadCycleTime`，Linux 通過 `perf_event`，macOS 通過 `mach_absolute_time`）
- **統計分析** - 均值、中位數、P90、P95、P99、最小值、最大值、標準差、標準誤和相對波動
- **多種輸出格式** - 控制台、Markdown、HTML、CSV 和程式化摘要
- **參數化基準測試** - `[Params]` 屬性，支援自動笛卡爾積迭代
- **比較支援** - 基準 vs 候選實現，帶加速比計算
- **可配置** - Quick、Default 和 Precise 預設、自動校準或完全自訂配置
- **netstandard2.0** - 相容 .NET Framework 4.6.1+、.NET Core 2.0+、.NET 5+

## 安裝

引用 **PicoBench** NuGet 套件。源生成器 (`PicoBench.Generators`) 作為分析器自動捆綁 - 無需額外引用。

```bash
dotnet add package PicoBench
```

## 快速開始

### 命令式 API

```csharp
using PicoBench;

var result = Benchmark.Run("My Benchmark", () =>
{
    Thread.SpinWait(100);
});

Console.WriteLine($"Average: {result.Statistics.Avg:F1} ns/op");
```

### 基於屬性的 API（源生成）

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

> 類 **必須** 是 `partial`。源生成器在編譯時生成 `IBenchmarkClass` 實現 - 無反射，完全 AOT 安全。

> 常見錯誤用法現在會得到源生成器診斷，例如非 `partial` 類、重複 baseline、非法生命週期方法簽名，以及不相容的 `[Params]` 值。

---

## 命令式 API 參考

### 基本基準測試

```csharp
using PicoBench;
using PicoBench.Formatters;

var result = Benchmark.Run("SpinWait", () => Thread.SpinWait(100));
Console.WriteLine(new ConsoleFormatter().Format(result));
```

### 帶狀態的基準測試（避免閉包）

```csharp
var data = new byte[1024];
var result = Benchmark.Run("ArrayCopy", data, static d =>
{
    var copy = new byte[d.Length];
    Buffer.BlockCopy(d, 0, copy, 0, d.Length);
});
```

### 作用域基準測試（DI 友好）

```csharp
var result = Benchmark.RunScoped("DbQuery",
    () => new MyDbContext(),
    static ctx => ctx.Users.FirstOrDefault()
);
// 每個樣本創建新作用域；每個樣本後銷毀作用域。
```

### 比較兩種實現

```csharp
var comparison = Benchmark.Compare(
    "String vs StringBuilder",
    "String Concat",  () => { var s = ""; for (int i = 0; i < 100; i++) s += "a"; },
    "StringBuilder",  () => { var sb = new StringBuilder(); for (int i = 0; i < 100; i++) sb.Append('a'); _ = sb.ToString(); }
);

Console.WriteLine($"Speedup: {comparison.Speedup:F2}x ({comparison.ImprovementPercent:F1}%)");
```

### 高級：獨立的熱身、設置和清理

```csharp
var result = Benchmark.Run(
    name:     "Custom",
    action:   () => DoWork(),
    warmup:   () => DoWork(),      // null 跳過熱身
    config:   BenchmarkConfig.Precise,
    setup:    () => PrepareState(), // 每個樣本前呼叫（不計時）
    teardown: () => CleanUp()       // 每個樣本後呼叫（不計時）
);
```

---

## 基於屬性的 API 參考

用 `[BenchmarkClass]` 裝飾一個 **partial** 類，並用下面的屬性裝飾其方法/屬性。源生成器在編譯時生成所有連接代碼。

### 屬性

| 屬性 | 目標 | 描述 |
|-----------|--------|-------------|
| `[BenchmarkClass]` | 類 | 標記類進行代碼生成。可選的 `Description` 屬性。 |
| `[Benchmark]` | 方法 | 將無參數方法標記為基準測試。設置 `Baseline = true` 作為參考方法。可選的 `Description`。 |
| `[Params(values)]` | 屬性 / 欄位 | 迭代給定的編譯時常數值。多個 `[Params]` 屬性產生笛卡爾積。 |
| `[GlobalSetup]` | 方法 | 每個參數組合**呼叫一次**，在基準測試運行之前。 |
| `[GlobalCleanup]` | 方法 | 每個參數組合**呼叫一次**，在基準測試運行之後。 |
| `[IterationSetup]` | 方法 | 每個樣本**前呼叫**（不計時）。 |
| `[IterationCleanup]` | 方法 | 每個樣本**後呼叫**（不計時）。 |

`[Benchmark]` 方法必須是實例、非泛型、無參數方法。生命週期方法必須是實例、非泛型、無參數且返回 `void`。`[Params]` 目標必須是可寫實例屬性或非唯讀實例欄位。

### 完整示例

```csharp
using PicoBench;

[BenchmarkClass(Description = "比較字符串連接策略")]
public partial class StringBenchmarks
{
    [Params(10, 100, 1000)]
    public int N { get; set; }

    [GlobalSetup]
    public void Setup() { /* 為當前 N 準備數據 */ }

    [GlobalCleanup]
    public void Cleanup() { /* 釋放資源 */ }

    [IterationSetup]
    public void BeforeSample() { /* 每個樣本準備 */ }

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

### 運行

```csharp
// 內部創建實例：
var suite = BenchmarkRunner.Run<StringBenchmarks>(BenchmarkConfig.Quick);

// 或使用預配置的實例：
var instance = new StringBenchmarks();
var suite2 = BenchmarkRunner.Run(instance, BenchmarkConfig.Quick);
```

---

## 配置

### 預設

| 預設 | 熱身迭代 | 樣本數 | 基礎每樣本迭代數 | 自動校準 | 使用場景 |
|--------|--------|---------|------------------|----------|----------|
| `Quick` | 100 | 10 | 1,000 | 是 | 快速迭代 / CI |
| `Default` | 1,000 | 100 | 10,000 | 否 | 通用基準測試 |
| `Precise` | 5,000 | 200 | 50,000 | 是 | 最終測量 |

### 自訂配置

```csharp
var config = new BenchmarkConfig
{
    WarmupIterations    = 500,
    SampleCount         = 50,
    IterationsPerSample = 5000,
    RetainSamples       = true,  // 保留原始 TimingSample 數據
    AutoCalibrateIterations = true,
    MinSampleTime       = TimeSpan.FromMilliseconds(0.5),
    MaxAutoIterationsPerSample = 1_000_000
};

var result = Benchmark.Run("Test", action, config);
```

啟用自動校準後，PicoBench 會自動增加 `IterationsPerSample`，直到達到最小樣本耗時預算，或命中 `MaxAutoIterationsPerSample` 上限。這對極快操作特別有用，可降低計時噪聲影響。

---

## 輸出格式化器

五個內置格式化器實現 `IFormatter`：

```csharp
using PicoBench.Formatters;

var console  = new ConsoleFormatter();     // 盒繪製控制台表格
var markdown = new MarkdownFormatter();    // GitHub 友好的 Markdown
var html     = new HtmlFormatter();        // 樣式化的 HTML 報告
var csv      = new CsvFormatter();         // CSV 用於數據分析

// 比較摘要的靜態輔助方法：
Console.WriteLine(SummaryFormatter.Format(suite.Comparisons));
```

控制台、Markdown、HTML 和 CSV 輸出現在會包含更偏精度分析的元資料，例如標準誤、相對標準差，以及 CPU 計數器語義說明。

### 格式化目標

```csharp
formatter.Format(result);               // 單個 BenchmarkResult
formatter.Format(results);              // IEnumerable<BenchmarkResult>
formatter.Format(comparison);           // 單個 ComparisonResult
formatter.Format(comparisons);          // IEnumerable<ComparisonResult>
formatter.Format(suite);                // 完整的 BenchmarkSuite
```

### 格式化器選項

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
// 也可用：FormatterOptions.Default, .Compact, .Minimal
```

### 保存結果

```csharp
var dir = Path.Combine(AppContext.BaseDirectory, "results");
Directory.CreateDirectory(dir);

File.WriteAllText(Path.Combine(dir, "results.md"),   new MarkdownFormatter().Format(suite));
File.WriteAllText(Path.Combine(dir, "results.html"), new HtmlFormatter().Format(suite));
File.WriteAllText(Path.Combine(dir, "results.csv"),  new CsvFormatter().Format(suite));
```

---

## 結果模型

| 類型 | 描述 |
|------|-------------|
| `BenchmarkResult` | 名稱、統計、樣本、每次樣本迭代數、樣本數、標籤、類別 |
| `ComparisonResult` | 基準、候選、加速比、是否更快、改進百分比 |
| `BenchmarkSuite` | 名稱、描述、結果、比較、環境、持續時間 |
| `Statistics` | 平均值、P50、P90、P95、P99、最小值、最大值、標準差、標準誤、相對標準差百分比、每操作 CPU 週期、GC 信息 |
| `TimingSample` | 奈秒耗時、毫秒耗時、計時器滴答數、CPU 週期、GC 信息 |
| `GcInfo` | Gen0、Gen1、Gen2、總計、是否為零 |
| `EnvironmentInfo` | 操作系統、架構、執行時版本、處理器數量、配置、CPU 計數器類型/可用性 |

---

## 架構

```
src/
+-- PicoBench/                        # 主庫 (netstandard2.0)
|   +-- Benchmark.cs                   # 命令式 API (Run, Compare, RunScoped)
|   +-- BenchmarkRunner.cs             # 基於屬性的入口點 (Run<T>)
|   +-- BenchmarkConfig.cs             # 配置和預設
|   +-- Attributes.cs                  # 7 個基準測試屬性
|   +-- IBenchmarkClass.cs             # 生成器實現的接口
|   +-- Runner.cs                      # 底層計時引擎
|   +-- StatisticsCalculator.cs        # 百分位數 / 統計計算
|   +-- Models.cs                      # 結果類型
|   +-- Formatters/
|       +-- IFormatter.cs              # IFormatter, FormatterOptions & FormatterBase
|       +-- ConsoleFormatter.cs        # 盒繪製控制台表格
|       +-- MarkdownFormatter.cs       # GitHub Markdown 表格
|       +-- HtmlFormatter.cs           # 樣式化的 HTML 報告
|       +-- CsvFormatter.cs            # CSV 導出
|       +-- SummaryFormatter.cs        # 勝/負摘要
|
+-- PicoBench.Generators/            # 源生成器 (netstandard2.0)
    +-- BenchmarkGenerator.cs          # IIncrementalGenerator 入口點
    +-- Emitter.cs                     # C# 代碼發射器 (AOT 安全)
    +-- Models.cs                      # Roslyn 分析模型
```

---

## 平台特定功能

| 功能 | Windows | Linux | macOS |
|---------|---------|-------|-------|
| 高精度計時 | Stopwatch | Stopwatch | Stopwatch |
| GC 追蹤 (Gen0/1/2) | 是 | 是 | 是 |
| CPU 週期計數 | `QueryThreadCycleTime` | `perf_event_open` | `mach_absolute_time` |
| 進程優先級提升 | 是 | 是 | 是 |

在 macOS 上，導出的 CPU 計數器是高精度單調時鐘代理值，而不是真正的架構 CPU 週期。`EnvironmentInfo` 和格式化輸出會明確區分這一點。

---

## 示例

| 示例 | API 風格 | 描述 |
|--------|-----------|-------------|
| `StringVsStringBuilder` | 命令式 | 比較 `string +=`、`StringBuilder` 和帶容量的 `StringBuilder` |
| `AttributeBased` | 基於屬性 | 使用 `[Benchmark]`、`[Params]` 和源生成器的相同比較 |
| `CollectionBenchmarks` | 基於屬性 | List vs Dictionary vs HashSet 查找 - 展示所有屬性 |

```bash
dotnet run --project samples/StringVsStringBuilder -c Release
dotnet run --project samples/AttributeBased -c Release
dotnet run --project samples/CollectionBenchmarks -c Release
```

---

## 與 BenchmarkDotNet 比較

| 功能 | PicoBench | BenchmarkDotNet |
|---------|-----------|----------------|
| 依賴 | 0 | 多 |
| 套件大小 | 小 | 大 |
| 目標框架 | netstandard2.0 | net6.0+ |
| AOT 支援 | 源生成器 | 基於反射 |
| 屬性 API | `[Benchmark]`、`[Params]` | `[Benchmark]`、`[Params]` |
| 設置時間 | 即時 | 秒級 |
| 輸出格式 | 5 | 10+ |
| 統計深度 | 良好 | 廣泛 |
| 使用場景 | 快速 A/B 測試、CI、AOT 應用 | 詳細分析、出版物 |

---

## 許可證

MIT 許可證 - 詳情見 [LICENSE](LICENSE) 文件。

## 貢獻

1. Fork 倉庫
2. 創建特性分支
3. 進行更改並附帶測試
4. 提交拉取請求
