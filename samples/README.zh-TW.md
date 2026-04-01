# 示例

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

三個示例項目展示了 PicoBench 提供的兩種 API。

## StringVsStringBuilder（命令式 API）

使用命令式 `Benchmark.Run()` 和 `Benchmark.Compare()` API 測量不同大小的字符串連接策略。

**亮點：**

- 使用閉包的 `Benchmark.Run()` 和 `BenchmarkConfig.Quick`
- 使用狀態避免閉包分配的 `Benchmark.Run<TState>()`
- 手動創建 `ComparisonResult` 用於自訂分組
- 構建包含所有結果和比較的 `BenchmarkSuite`
- 通過 `FormatterOptions` 輸出到控制台、Markdown、HTML 和 CSV（自訂標籤）

```bash
dotnet run --project samples/StringVsStringBuilder -c Release
```

## AttributeBased（基於屬性的 API + 源生成器）

使用基於屬性的 API 重寫相同的字符串基準測試。

**亮點：**

- 帶 `Description` 的 `[BenchmarkClass]`
- 用於參數化運行的 `[Params(10, 100, 1000)]`
- 標記參考方法的 `[Benchmark(Baseline = true)]`
- 用於每個參數組合準備的 `[GlobalSetup]`
- 通過 `BenchmarkRunner.Run<T>()` 一行執行
- 快速勝/負概覽的 `SummaryFormatter`

```csharp
[BenchmarkClass(Description = "比較字符串連接策略")]
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

## CollectionBenchmarks（完整屬性展示）

通過比較 List、Dictionary 和 HashSet 的查找性能，展示**大多數**屬性。

**亮點：**

- 帶 `Description` 的 `[BenchmarkClass]`
- `[Params(100, 1_000, 10_000)]` - 三種集合大小
- `[GlobalSetup]` - 用隨機數據填充所有三個集合
- `[GlobalCleanup]` - 釋放集合
- `[IterationSetup]` - 在每個樣本前隨機化查找目標
- `[Benchmark(Baseline = true, Description = "...")]` - 以 `List.Contains()` 作為基準
- `[Benchmark(Description = "...")]` - `Dictionary.ContainsKey()` 和 `HashSet.Contains()`
- 多格式輸出：控制台、Markdown、HTML、CSV

```csharp
[BenchmarkClass(Description = "查找性能：List vs Dictionary vs HashSet")]
public partial class LookupBenchmarks
{
    [Params(100, 1_000, 10_000)]
    public int N { get; set; }

    [GlobalSetup]   public void Setup()         { /* 填充集合 */ }
    [GlobalCleanup] public void Cleanup()       { /* 釋放集合 */ }
    [IterationSetup] public void ShuffleTarget() { /* 改變查找目標 */ }

    [Benchmark(Baseline = true, Description = "線性掃描 O(n)")]
    public void ListContains() { _ = _list.Contains(_target); }

    [Benchmark(Description = "哈希查找 O(1)")]
    public void DictionaryContainsKey() { _ = _dictionary.ContainsKey(_target); }

    [Benchmark(Description = "哈希查找 O(1)，集合優化")]
    public void HashSetContains() { _ = _hashSet.Contains(_target); }
}
```

```bash
dotnet run --project samples/CollectionBenchmarks -c Release
```

## 輸出

所有示例都將結果保存到輸出文件夾下的 `results/` 子目錄中，格式為 Markdown、HTML 和 CSV。

這些報告現在還會包含更偏精度分析的元資料，例如標準誤、相對標準差，以及在可用時顯示 CPU 計數器說明。
