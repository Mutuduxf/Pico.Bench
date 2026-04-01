# 測試

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

**PicoBench** 的單元測試，使用 [TUnit](https://github.com/thomhurst/TUnit) 測試框架。

**總計：454 個測試**

## 運行

```bash
dotnet run --project tests/PicoBench.Tests/PicoBench.Tests.csproj -c Debug
```

## 測試分類

### Formatters/ (224 個測試)

所有五個輸出格式化器及其支持基礎設施的測試。

| 文件 | 測試數 | 描述 |
|------|-------|-------------|
| `ConsoleFormatterTests.cs` | 40+ | 盒繪製表格生成、對齊、編碼 |
| `MarkdownFormatterTests.cs` | 40+ | GitHub Markdown 表格渲染 |
| `HtmlFormatterTests.cs` | 40+ | 帶樣式的 HTML 報告生成 |
| `CsvFormatterTests.cs` | 40+ | 帶正確轉義的 CSV 導出 |
| `SummaryFormatterTests.cs` | 20+ | 勝/負摘要文本 |
| `FormatterBaseTests.cs` | 15+ | 模板方法基類行為 |
| `FormatterOptionsTests.cs` | 10+ | 選項默認值、預設、路徑解析 |
| `CrossPlatformTests.cs` | 10+ | 行尾和編碼一致性 |

### Formatters/Integration/ (8 個測試)

| 文件 | 測試數 | 描述 |
|------|-------|-------------|
| `FormatterIntegrationTests.cs` | 8 | 完整 `BenchmarkSuite` 對象的端到端格式化 |

### Attributes/ (18 個測試)

| 文件 | 測試數 | 描述 |
|------|-------|-------------|
| `AttributeTests.cs` | 18 | 所有七個屬性：默認值、屬性設置、`AttributeUsage` 目標、`[Params]` 值存儲 |

### BenchmarkRunnerTests.cs (8 個測試)

| 文件 | 測試數 | 描述 |
|------|-------|-------------|
| `BenchmarkRunnerTests.cs` | 8 | `BenchmarkRunner.Run<T>()` 帶無參數/預配置實例、空值檢查、配置傳播 |

### Generators/ (47 個測試)

| 文件 | 測試數 | 描述 |
|------|-------|-------------|
| `EmitterTests.cs` | 25 | 源生成器代碼發射：類結構、參數迭代、設置/清理鉤子、基準比較、`global::` 限定 |
| `ModelsTests.cs` | 22 | `BenchmarkClassModel`、`BenchmarkMethodModel`、`ParamsPropertyModel` 相等性、哈希碼、邊界情況 |
| `BenchmarkGeneratorDiagnosticsTests.cs` | 10+ | 無效簽名、重複 baseline、非法 `[Params]` 與列舉參數發射的端到端生成器診斷測試 |

### 核心執行時覆蓋

| 文件 | 測試數 | 描述 |
|------|-------|-------------|
| `BenchmarkTests.cs` | 40+ | 命令式 API、作用域執行、保留樣本、比較以及自動校準行為 |
| `StatisticsCalculatorTests.cs` | 10+ | 包含標準誤、CPU 週期和邊界情況的統計計算 |
| `ModelsTests.cs` | 40+ | 結果模型校驗、CPU 計數器元資料和波動輔助屬性 |

格式化器測試現在也覆蓋更偏精度分析的輸出，例如標準誤、相對標準差，以及 Console、Markdown、HTML、CSV 中的 CPU 計數器說明。

### TestData/

用於構建一致測試夾具的工廠類：

| 文件 | 用途 |
|------|---------|
| `BenchmarkResultFactory.cs` | 創建具有合理默認值的 `BenchmarkResult` 實例 |
| `BenchmarkSuiteFactory.cs` | 創建包含結果和比較的 `BenchmarkSuite` |
| `ComparisonResultFactory.cs` | 創建 `ComparisonResult` 對 |
| `GcInfoFactory.cs` | 創建 `GcInfo` 記錄 |
| `StatisticsFactory.cs` | 創建具有真實分佈的 `Statistics` |

### Utilities/

| 文件 | 用途 |
|------|---------|
| `FileSystemHelper.cs` | 文件輸出測試的臨時目錄管理 |
| `TestContextLogger.cs` | TUnit 測試上下文的日誌記錄助手 |
