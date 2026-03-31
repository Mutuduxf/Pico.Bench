# Tests

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

Tests unitaires pour **PicoBench** utilisant le framework de test [TUnit](https://github.com/thomhurst/TUnit).

**Total : 313 tests**

## Exécution

```bash
dotnet run --project tests/PicoBench.Tests/PicoBench.Tests.csproj -c Debug
```

## Catégories de Tests

### Formatters/ (224 tests)

Tests pour les cinq formateurs de sortie et leur infrastructure de support.

| Fichier | Tests | Description |
|------|-------|-------------|
| `ConsoleFormatterTests.cs` | 40+ | Génération de tableaux avec dessin de boîtes, alignement, encodage |
| `MarkdownFormatterTests.cs` | 40+ | Rendu de tableaux Markdown GitHub |
| `HtmlFormatterTests.cs` | 40+ | Génération de rapports HTML avec styles |
| `CsvFormatterTests.cs` | 40+ | Export CSV avec échappement approprié |
| `SummaryFormatterTests.cs` | 20+ | Texte de résumé victoires/défaites |
| `FormatterBaseTests.cs` | 15+ | Comportement de la classe de base Template Method |
| `FormatterOptionsTests.cs` | 10+ | Valeurs par défaut des options, préconfigurations, résolution de chemins |
| `CrossPlatformTests.cs` | 10+ | Consistance des fins de ligne et de l'encodage |

### Formatters/Integration/ (8 tests)

| Fichier | Tests | Description |
|------|-------|-------------|
| `FormatterIntegrationTests.cs` | 8 | Formatage de bout en bout d'objets `BenchmarkSuite` complets |

### Attributes/ (18 tests)

| Fichier | Tests | Description |
|------|-------|-------------|
| `AttributeTests.cs` | 18 | Les sept attributs : valeurs par défaut, définition des propriétés, cibles `AttributeUsage`, stockage des valeurs `[Params]` |

### BenchmarkRunnerTests.cs (8 tests)

| Fichier | Tests | Description |
|------|-------|-------------|
| `BenchmarkRunnerTests.cs` | 8 | `BenchmarkRunner.Run<T>()` avec instance sans paramètres / préconfigurée, vérifications de null, propagation de configuration |

### Generators/ (47 tests)

| Fichier | Tests | Description |
|------|-------|-------------|
| `EmitterTests.cs` | 25 | Émission de code du générateur de source : structure de classe, itération des paramètres, crochets de configuration/nettoyage, comparaisons de base, qualification `global::` |
| `ModelsTests.cs` | 22 | Égalité de `BenchmarkClassModel`, `BenchmarkMethodModel`, `ParamsPropertyModel`, codes de hachage, cas limites |

### TestData/

Classes d'usine pour construire des fixtures de test cohérentes :

| Fichier | Objectif |
|------|---------|
| `BenchmarkResultFactory.cs` | Crée des instances de `BenchmarkResult` avec des valeurs par défaut sensées |
| `BenchmarkSuiteFactory.cs` | Crée `BenchmarkSuite` avec résultats et comparaisons |
| `ComparisonResultFactory.cs` | Crée des paires de `ComparisonResult` |
| `GcInfoFactory.cs` | Crée des enregistrements `GcInfo` |
| `StatisticsFactory.cs` | Crée `Statistics` avec des distributions réalistes |

### Utilities/

| Fichier | Objectif |
|------|---------|
| `FileSystemHelper.cs` | Gestion de répertoires temporaires pour les tests de sortie de fichiers |
| `TestContextLogger.cs` | Assistant de journalisation pour le contexte de test TUnit |
