# サンプル

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

3つのサンプルプロジェクトがPicoBenchが提供する2つのAPIを実演します。

## StringVsStringBuilder（命令型API）

命令型API `Benchmark.Run()` と `Benchmark.Compare()` を使用して、さまざまなサイズでの文字列連結戦略を測定します。

**ハイライト:**

- クロージャ付き`Benchmark.Run()`と`BenchmarkConfig.Quick`
- 状態付きでクロージャ割り当てを回避する`Benchmark.Run<TState>()`
- カスタムグループ化のための手動`ComparisonResult`作成
- すべての結果と比較を含む`BenchmarkSuite`構築
- `FormatterOptions`経由でコンソール、Markdown、HTML、CSV出力（カスタムラベル）

```bash
dotnet run --project samples/StringVsStringBuilder -c Release
```

## AttributeBased（属性ベースAPI + ソースジェネレーター）

属性ベースAPIを使用して同じ文字列ベンチマークを書き直します。

**ハイライト:**

- `Description`付き`[BenchmarkClass]`
- パラメータ化実行のための`[Params(10, 100, 1000)]`
- 参照メソッドをマークする`[Benchmark(Baseline = true)]`
- パラメータ組み合わせごとの準備のための`[GlobalSetup]`
- `BenchmarkRunner.Run<T>()`によるワンライナー実行
- 勝敗概要の`SummaryFormatter`

```csharp
[BenchmarkClass(Description = "文字列連結戦略の比較")]
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

## CollectionBenchmarks（完全な属性展示）

List、Dictionary、HashSetの検索性能を比較して、**ほとんどの**属性を実演します。

**ハイライト:**

- `Description`付き`[BenchmarkClass]`
- `[Params(100, 1_000, 10_000)]` - 3つのコレクションサイズ
- `[GlobalSetup]` - ランダム化データで3つのコレクションをすべて埋める
- `[GlobalCleanup]` - コレクションを解放
- `[IterationSetup]` - 各サンプル前に検索対象をシャッフル
- `[Benchmark(Baseline = true, Description = "...")]` - `List.Contains()`をベースラインとして
- `[Benchmark(Description = "...")]` - `Dictionary.ContainsKey()`と`HashSet.Contains()`
- マルチフォーマット出力：コンソール、Markdown、HTML、CSV

```csharp
[BenchmarkClass(Description = "検索性能：List vs Dictionary vs HashSet")]
public partial class LookupBenchmarks
{
    [Params(100, 1_000, 10_000)]
    public int N { get; set; }

    [GlobalSetup]   public void Setup()         { /* コレクションを埋める */ }
    [GlobalCleanup] public void Cleanup()       { /* コレクションを解放 */ }
    [IterationSetup] public void ShuffleTarget() { /* 検索対象を変更 */ }

    [Benchmark(Baseline = true, Description = "線形スキャン O(n)")]
    public void ListContains() { _ = _list.Contains(_target); }

    [Benchmark(Description = "ハッシュ検索 O(1)")]
    public void DictionaryContainsKey() { _ = _dictionary.ContainsKey(_target); }

    [Benchmark(Description = "ハッシュ検索 O(1)、セット最適化")]
    public void HashSetContains() { _ = _hashSet.Contains(_target); }
}
```

```bash
dotnet run --project samples/CollectionBenchmarks -c Release
```

## 出力

すべてのサンプルは、Markdown、HTML、CSV形式で出力フォルダーの下の`results/`サブディレクトリに結果を保存します。

これらのレポートには、標準誤差、相対標準偏差、利用可能な場合の CPU カウンター注記など、精度重視のメタデータも含まれるようになりました。
