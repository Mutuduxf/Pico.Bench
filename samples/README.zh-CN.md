# 示例

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

三个示例项目展示了 PicoBench 提供的两种 API。

## StringVsStringBuilder（命令式 API）

使用命令式 `Benchmark.Run()` 和 `Benchmark.Compare()` API 测量不同大小的字符串连接策略。

**亮点：**

- 使用闭包的 `Benchmark.Run()` 和 `BenchmarkConfig.Quick`
- 使用状态避免闭包分配的 `Benchmark.Run<TState>()`
- 手动创建 `ComparisonResult` 用于自定义分组
- 构建包含所有结果和比较的 `BenchmarkSuite`
- 通过 `FormatterOptions` 输出到控制台、Markdown、HTML 和 CSV（自定义标签）

```bash
dotnet run --project samples/StringVsStringBuilder -c Release
```

## AttributeBased（基于属性的 API + 源生成器）

使用基于属性的 API 重写相同的字符串基准测试。

**亮点：**

- 带 `Description` 的 `[BenchmarkClass]`
- 用于参数化运行的 `[Params(10, 100, 1000)]`
- 标记参考方法的 `[Benchmark(Baseline = true)]`
- 用于每个参数组合准备的 `[GlobalSetup]`
- 通过 `BenchmarkRunner.Run<T>()` 一行执行
- 快速胜/负概览的 `SummaryFormatter`

```csharp
[BenchmarkClass(Description = "比较字符串连接策略")]
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

## CollectionBenchmarks（完整属性展示）

通过比较 List、Dictionary 和 HashSet 的查找性能，展示**大多数**属性。

**亮点：**

- 带 `Description` 的 `[BenchmarkClass]`
- `[Params(100, 1_000, 10_000)]` - 三种集合大小
- `[GlobalSetup]` - 用随机数据填充所有三个集合
- `[GlobalCleanup]` - 释放集合
- `[IterationSetup]` - 在每个样本前随机化查找目标
- `[Benchmark(Baseline = true, Description = "...")]` - 以 `List.Contains()` 作为基准
- `[Benchmark(Description = "...")]` - `Dictionary.ContainsKey()` 和 `HashSet.Contains()`
- 多格式输出：控制台、Markdown、HTML、CSV

```csharp
[BenchmarkClass(Description = "查找性能：List vs Dictionary vs HashSet")]
public partial class LookupBenchmarks
{
    [Params(100, 1_000, 10_000)]
    public int N { get; set; }

    [GlobalSetup]   public void Setup()         { /* 填充集合 */ }
    [GlobalCleanup] public void Cleanup()       { /* 释放集合 */ }
    [IterationSetup] public void ShuffleTarget() { /* 改变查找目标 */ }

    [Benchmark(Baseline = true, Description = "线性扫描 O(n)")]
    public void ListContains() { _ = _list.Contains(_target); }

    [Benchmark(Description = "哈希查找 O(1)")]
    public void DictionaryContainsKey() { _ = _dictionary.ContainsKey(_target); }

    [Benchmark(Description = "哈希查找 O(1)，集合优化")]
    public void HashSetContains() { _ = _hashSet.Contains(_target); }
}
```

```bash
dotnet run --project samples/CollectionBenchmarks -c Release
```

## 输出

所有示例都将结果保存到输出文件夹下的 `results/` 子目录中，格式为 Markdown、HTML 和 CSV。

这些报告现在还会包含更偏精度分析的元数据，例如标准误、相对标准差，以及在可用时显示 CPU 计数器说明。
