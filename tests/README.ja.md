# テスト

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

[TUnit](https://github.com/thomhurst/TUnit) テストフレームワークを使用した **PicoBench** の単体テストです。

**合計：313テスト**

## 実行

```bash
dotnet run --project tests/PicoBench.Tests/PicoBench.Tests.csproj -c Debug
```

## テストカテゴリー

### Formatters/ (224テスト)

5つの出力フォーマッターすべてとそのサポートインフラのテスト。

| ファイル | テスト数 | 説明 |
|------|-------|-------------|
| `ConsoleFormatterTests.cs` | 40+ | ボックス描画テーブル生成、整列、エンコーディング |
| `MarkdownFormatterTests.cs` | 40+ | GitHub Markdownテーブルレンダリング |
| `HtmlFormatterTests.cs` | 40+ | スタイル付きHTMLレポート生成 |
| `CsvFormatterTests.cs` | 40+ | 適切なエスケープ付きCSVエクスポート |
| `SummaryFormatterTests.cs` | 20+ | 勝敗概要テキスト |
| `FormatterBaseTests.cs` | 15+ | Template Method基本クラスの動作 |
| `FormatterOptionsTests.cs` | 10+ | オプションのデフォルト値、プリセット、パス解決 |
| `CrossPlatformTests.cs` | 10+ | 行末とエンコーディングの一貫性 |

### Formatters/Integration/ (8テスト)

| ファイル | テスト数 | 説明 |
|------|-------|-------------|
| `FormatterIntegrationTests.cs` | 8 | 完全な`BenchmarkSuite`オブジェクトのエンドツーエンドフォーマット |

### Attributes/ (18テスト)

| ファイル | テスト数 | 説明 |
|------|-------|-------------|
| `AttributeTests.cs` | 18 | 7つの属性すべて：デフォルト値、プロパティ設定、`AttributeUsage`ターゲット、`[Params]`値ストレージ |

### BenchmarkRunnerTests.cs (8テスト)

| ファイル | テスト数 | 説明 |
|------|-------|-------------|
| `BenchmarkRunnerTests.cs` | 8 | パラメータなし/事前設定済みインスタンスでの`BenchmarkRunner.Run<T>()`、nullチェック、設定伝播 |

### Generators/ (47テスト)

| ファイル | テスト数 | 説明 |
|------|-------|-------------|
| `EmitterTests.cs` | 25 | ソースジェネレーターコード生成：クラス構造、パラメータ反復、セットアップ/ティアダウンフック、ベースライン比較、`global::`修飾 |
| `ModelsTests.cs` | 22 | `BenchmarkClassModel`、`BenchmarkMethodModel`、`ParamsPropertyModel`の等価性、ハッシュコード、エッジケース |

### TestData/

一貫したテストフィクスチャを構築するためのファクトリークラス：

| ファイル | 目的 |
|------|---------|
| `BenchmarkResultFactory.cs` | 適切なデフォルト値を持つ`BenchmarkResult`インスタンスを作成 |
| `BenchmarkSuiteFactory.cs` | 結果と比較を含む`BenchmarkSuite`を作成 |
| `ComparisonResultFactory.cs` | `ComparisonResult`ペアを作成 |
| `GcInfoFactory.cs` | `GcInfo`レコードを作成 |
| `StatisticsFactory.cs` | 現実的な分布を持つ`Statistics`を作成 |

### Utilities/

| ファイル | 目的 |
|------|---------|
| `FileSystemHelper.cs` | ファイル出力テスト用の一時ディレクトリ管理 |
| `TestContextLogger.cs` | TUnitテストコンテキストのログヘルパー |
