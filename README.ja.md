# PicoBench

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

軽量で依存関係ゼロの.NETベンチマークライブラリで、**2つの補完的API**を提供します：命令型のfluent APIと、属性ベースでソース生成される、完全に**AOT互換**のAPIです。

## 特徴

- **依存関係ゼロ** - 純粋な.NET実装、外部パッケージ不要
- **2つのAPI** - アドホックテスト用の命令型API (`Benchmark.Run`)；構造化されたスイート用の属性ベースAPI (`[Benchmark]` + ソースジェネレーター)
- **AOT互換ソースジェネレーター** - インクリメンタルジェネレーターが実行時に直接メソッド呼び出しを生成、リフレクションゼロ
- **クロスプラットフォーム** - Windows、Linux、macOSを完全サポート
- **高精度タイミング** - ナノ秒レベルの精度で`Stopwatch`を使用
- **GC追跡** - ベンチマーク中のGen0/Gen1/Gen2コレクション回数を監視
- **CPUサイクルカウント** - ハードウェアレベルのサイクルカウント（Windowsは`QueryThreadCycleTime`、Linuxは`perf_event`、macOSは`mach_absolute_time`）
- **統計分析** - 平均、中央値、P90、P95、P99、最小値、最大値、標準偏差、標準誤差、相対分散
- **複数出力形式** - コンソール、Markdown、HTML、CSV、プログラム要約
- **パラメータ化ベンチマーク** - `[Params]`属性で自動デカルト積反復
- **比較サポート** - ベースライン vs 候補、高速化計算付き
- **設定可能** - Quick、Default、Preciseプリセット、自動キャリブレーション、または完全カスタム設定
- **netstandard2.0** - .NET Framework 4.6.1+、.NET Core 2.0+、.NET 5+と互換性

## インストール

**PicoBench** NuGetパッケージを参照してください。ソースジェネレーター (`PicoBench.Generators`) はアナライザーとして自動的にバンドルされます - 追加の参照は不要です。

```bash
dotnet add package PicoBench
```

## クイックスタート

### 命令型API

```csharp
using PicoBench;

var result = Benchmark.Run("My Benchmark", () =>
{
    Thread.SpinWait(100);
});

Console.WriteLine($"Average: {result.Statistics.Avg:F1} ns/op");
```

### 属性ベースAPI（ソース生成）

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

> クラスは`partial`で**なければなりません**。ソースジェネレーターはコンパイル時に`IBenchmarkClass`実装を生成します - リフレクションなし、完全AOT安全。

> 不正な属性の使い方に対しては、非`partial`クラス、重複した baseline、無効なライフサイクルシグネチャ、互換性のない`[Params]`値などの一般的なミスをジェネレーター診断として報告します。

---

## 命令型APIリファレンス

### 基本ベンチマーク

```csharp
using PicoBench;
using PicoBench.Formatters;

var result = Benchmark.Run("SpinWait", () => Thread.SpinWait(100));
Console.WriteLine(new ConsoleFormatter().Format(result));
```

### 状態付きベンチマーク（クロージャ回避）

```csharp
var data = new byte[1024];
var result = Benchmark.Run("ArrayCopy", data, static d =>
{
    var copy = new byte[d.Length];
    Buffer.BlockCopy(d, 0, copy, 0, d.Length);
});
```

### スコープ付きベンチマーク（DIフレンドリー）

```csharp
var result = Benchmark.RunScoped("DbQuery",
    () => new MyDbContext(),
    static ctx => ctx.Users.FirstOrDefault()
);
// 各サンプルで新しいスコープが作成され、各サンプル後にスコープが破棄されます。
```

### 2つの実装を比較

```csharp
var comparison = Benchmark.Compare(
    "String vs StringBuilder",
    "String Concat",  () => { var s = ""; for (int i = 0; i < 100; i++) s += "a"; },
    "StringBuilder",  () => { var sb = new StringBuilder(); for (int i = 0; i < 100; i++) sb.Append('a'); _ = sb.ToString(); }
);

Console.WriteLine($"Speedup: {comparison.Speedup:F2}x ({comparison.ImprovementPercent:F1}%)");
```

### 詳細：独立したウォームアップ、セットアップ、ティアダウン

```csharp
var result = Benchmark.Run(
    name:     "Custom",
    action:   () => DoWork(),
    warmup:   () => DoWork(),      // nullでウォームアップスキップ
    config:   BenchmarkConfig.Precise,
    setup:    () => PrepareState(), // 各サンプル前に呼び出し（計時対象外）
    teardown: () => CleanUp()       // 各サンプル後に呼び出し（計時対象外）
);
```

---

## 属性ベースAPIリファレンス

`[BenchmarkClass]`で**partial**クラスを修飾し、そのメソッド/プロパティを以下の属性で修飾します。ソースジェネレーターがコンパイル時にすべての接続コードを生成します。

### 属性

| 属性 | ターゲット | 説明 |
|-----------|--------|-------------|
| `[BenchmarkClass]` | クラス | コード生成のためクラスをマーク。オプションの`Description`プロパティ。 |
| `[Benchmark]` | メソッド | パラメータなしメソッドをベンチマークとしてマーク。参照メソッドには`Baseline = true`を設定。オプションの`Description`。 |
| `[Params(values)]` | プロパティ / フィールド | 指定されたコンパイル時定数値を反復。複数の`[Params]`プロパティはデカルト積を生成。 |
| `[GlobalSetup]` | メソッド | 各パラメータ組み合わせごとに**1回呼び出し**、ベンチマーク実行前。 |
| `[GlobalCleanup]` | メソッド | 各パラメータ組み合わせごとに**1回呼び出し**、ベンチマーク実行後。 |
| `[IterationSetup]` | メソッド | 各サンプル**前に呼び出し**（計時対象外）。 |
| `[IterationCleanup]` | メソッド | 各サンプル**後に呼び出し**（計時対象外）。 |

`[Benchmark]`メソッドは、インスタンスメソッドであり、ジェネリックではなく、引数なしでなければなりません。ライフサイクルメソッドは、インスタンスメソッドであり、ジェネリックではなく、引数なしで、戻り値が`void`である必要があります。`[Params]`の対象は書き込み可能なインスタンスプロパティ、または`readonly`でないインスタンスフィールドでなければなりません。

### 完全な例

```csharp
using PicoBench;

[BenchmarkClass(Description = "文字列連結戦略の比較")]
public partial class StringBenchmarks
{
    [Params(10, 100, 1000)]
    public int N { get; set; }

    [GlobalSetup]
    public void Setup() { /* 現在のNのデータを準備 */ }

    [GlobalCleanup]
    public void Cleanup() { /* リソースを解放 */ }

    [IterationSetup]
    public void BeforeSample() { /* サンプルごとの準備 */ }

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

### 実行

```csharp
// 内部でインスタンス作成：
var suite = BenchmarkRunner.Run<StringBenchmarks>(BenchmarkConfig.Quick);

// または事前設定済みインスタンスで：
var instance = new StringBenchmarks();
var suite2 = BenchmarkRunner.Run(instance, BenchmarkConfig.Quick);
```

---

## 設定

### プリセット

| プリセット | ウォームアップ | サンプル数 | 基本反復数/サンプル | 自動キャリブレーション | 使用例 |
|--------|--------|---------|----------------------|------------------------|----------|
| `Quick` | 100 | 10 | 1,000 | はい | 高速反復 / CI |
| `Default` | 1,000 | 100 | 10,000 | いいえ | 一般的なベンチマーク |
| `Precise` | 5,000 | 200 | 50,000 | はい | 最終測定 |

### カスタム設定

```csharp
var config = new BenchmarkConfig
{
    WarmupIterations    = 500,
    SampleCount         = 50,
    IterationsPerSample = 5000,
    RetainSamples       = true,  // 生のTimingSampleデータを保持
    AutoCalibrateIterations = true,
    MinSampleTime       = TimeSpan.FromMilliseconds(0.5),
    MaxAutoIterationsPerSample = 1_000_000
};

var result = Benchmark.Run("Test", action, config);
```

自動キャリブレーションを有効にすると、PicoBench は最小サンプル時間の予算に達するか、`MaxAutoIterationsPerSample` に達するまで `IterationsPerSample` を増やします。これは、タイマーノイズの影響を受けやすい極めて高速な処理で特に有効です。

---

## 出力フォーマッター

5つの組み込みフォーマッターが`IFormatter`を実装：

```csharp
using PicoBench.Formatters;

var console  = new ConsoleFormatter();     // ボックス描画コンソールテーブル
var markdown = new MarkdownFormatter();    // GitHub対応Markdown
var html     = new HtmlFormatter();        // スタイル付きHTMLレポート
var csv      = new CsvFormatter();         // データ分析用CSV

// 比較要約の静的ヘルパー：
Console.WriteLine(SummaryFormatter.Format(suite.Comparisons));
```

Console、Markdown、HTML、CSV の出力には、標準誤差、相対標準偏差、利用可能な場合の CPU カウンター注記など、精度指向のメタデータが含まれるようになりました。

### フォーマット対象

```csharp
formatter.Format(result);               // 単一のBenchmarkResult
formatter.Format(results);              // IEnumerable<BenchmarkResult>
formatter.Format(comparison);           // 単一のComparisonResult
formatter.Format(comparisons);          // IEnumerable<ComparisonResult>
formatter.Format(suite);                // 完全なBenchmarkSuite
```

### フォーマッターオプション

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
// 他にも利用可能：FormatterOptions.Default, .Compact, .Minimal
```

### 結果の保存

```csharp
var dir = Path.Combine(AppContext.BaseDirectory, "results");
Directory.CreateDirectory(dir);

File.WriteAllText(Path.Combine(dir, "results.md"),   new MarkdownFormatter().Format(suite));
File.WriteAllText(Path.Combine(dir, "results.html"), new HtmlFormatter().Format(suite));
File.WriteAllText(Path.Combine(dir, "results.csv"),  new CsvFormatter().Format(suite));
```

---

## 結果モデル

| タイプ | 説明 |
|------|-------------|
| `BenchmarkResult` | 名前、統計、サンプル、サンプルあたり反復数、サンプル数、タグ、カテゴリ |
| `ComparisonResult` | ベースライン、候補、高速化、高速かどうか、改善率 |
| `BenchmarkSuite` | 名前、説明、結果、比較、環境、期間 |
| `Statistics` | 平均、P50、P90、P95、P99、最小、最大、標準偏差、StandardError、RelativeStdDevPercent、操作あたりCPUサイクル、GcInfo |
| `TimingSample` | 経過ナノ秒、経過ミリ秒、経過ティック、CPUサイクル、GcInfo |
| `GcInfo` | Gen0、Gen1、Gen2、合計、ゼロか |
| `EnvironmentInfo` | OS、アーキテクチャ、ランタイムバージョン、プロセッサ数、設定、CPUカウンター種別/可用性 |

---

## アーキテクチャ

```
src/
+-- PicoBench/                        # メインライブラリ (netstandard2.0)
|   +-- Benchmark.cs                   # 命令型API (Run, Compare, RunScoped)
|   +-- BenchmarkRunner.cs             # 属性ベースエントリーポイント (Run<T>)
|   +-- BenchmarkConfig.cs             # プリセット付き設定
|   +-- Attributes.cs                  # 7つのベンチマーク属性
|   +-- IBenchmarkClass.cs             # ジェネレーターが生成するインターフェース
|   +-- Runner.cs                      # 低レベルタイミングエンジン
|   +-- StatisticsCalculator.cs        # パーセンタイル / 統計計算
|   +-- Models.cs                      # 結果タイプ
|   +-- Formatters/
|       +-- IFormatter.cs              # IFormatter, FormatterOptions & FormatterBase
|       +-- ConsoleFormatter.cs        # ボックス描画コンソールテーブル
|       +-- MarkdownFormatter.cs       # GitHub Markdownテーブル
|       +-- HtmlFormatter.cs           # スタイル付きHTMLレポート
|       +-- CsvFormatter.cs            # CSVエクスポート
|       +-- SummaryFormatter.cs        # 勝敗要約
|
+-- PicoBench.Generators/            # ソースジェネレーター (netstandard2.0)
    +-- BenchmarkGenerator.cs          # IIncrementalGeneratorエントリーポイント
    +-- Emitter.cs                     # C#コードエミッター (AOT安全)
    +-- Models.cs                      # Roslyn分析モデル
```

---

## プラットフォーム固有機能

| 機能 | Windows | Linux | macOS |
|---------|---------|-------|-------|
| 高精度タイミング | Stopwatch | Stopwatch | Stopwatch |
| GC追跡 (Gen0/1/2) | はい | はい | はい |
| CPUサイクルカウント | `QueryThreadCycleTime` | `perf_event_open` | `mach_absolute_time` |
| プロセス優先度ブースト | はい | はい | はい |

macOS では、出力される CPU カウンターはアーキテクチャ上の実サイクルではなく、高分解能の単調クロックプロキシです。`EnvironmentInfo` と各フォーマッター出力はこの違いを明示します。

---

## サンプル

| サンプル | APIスタイル | 説明 |
|--------|-----------|-------------|
| `StringVsStringBuilder` | 命令型 | `string +=`、`StringBuilder`、容量指定`StringBuilder`の比較 |
| `AttributeBased` | 属性ベース | `[Benchmark]`、`[Params]`、ソースジェネレーターを使用した同じ比較 |
| `CollectionBenchmarks` | 属性ベース | List vs Dictionary vs HashSet検索 - すべての属性を展示 |

```bash
dotnet run --project samples/StringVsStringBuilder -c Release
dotnet run --project samples/AttributeBased -c Release
dotnet run --project samples/CollectionBenchmarks -c Release
```

---

## BenchmarkDotNetとの比較

| 機能 | PicoBench | BenchmarkDotNet |
|---------|-----------|----------------|
| 依存関係 | 0 | 多数 |
| パッケージサイズ | 小 | 大 |
| ターゲットフレームワーク | netstandard2.0 | net6.0+ |
| AOTサポート | ソースジェネレーター | リフレクションベース |
| 属性API | `[Benchmark]`、`[Params]` | `[Benchmark]`、`[Params]` |
| セットアップ時間 | 即時 | 秒単位 |
| 出力形式 | 5 | 10+ |
| 統計的深さ | 良好 | 広範 |
| 使用例 | 高速A/Bテスト、CI、AOTアプリ | 詳細分析、出版物 |

---

## ライセンス

MITライセンス - 詳細は[LICENSE](LICENSE)ファイルを参照してください。

## 貢献

1. リポジトリをフォーク
2. 機能ブランチを作成
3. テスト付きで変更を実施
4. プルリクエストを送信
