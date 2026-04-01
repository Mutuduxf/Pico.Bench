# Exemples

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

Trois exemples de projets démontrent les deux API fournies par PicoBench.

## StringVsStringBuilder (API Impérative)

Utilise l'API impérative `Benchmark.Run()` et `Benchmark.Compare()` pour mesurer les stratégies de concaténation de chaînes à différentes tailles.

**Points Forts :**

- `Benchmark.Run()` avec des clôtures et `BenchmarkConfig.Quick`
- `Benchmark.Run<TState>()` avec état pour éviter l'allocation de clôtures
- Création manuelle de `ComparisonResult` pour un regroupement personnalisé
- Construction de `BenchmarkSuite` avec tous les résultats et comparaisons
- Sorties vers Console, Markdown, HTML et CSV via `FormatterOptions` (étiquettes personnalisées)

```bash
dotnet run --project samples/StringVsStringBuilder -c Release
```

## AttributeBased (API Basée sur les Attributs + Générateur de Source)

Réécrit le même benchmark de chaînes en utilisant l'API basée sur les attributs.

**Points Forts :**

- `[BenchmarkClass]` avec `Description`
- `[Params(10, 100, 1000)]` pour les exécutions paramétrées
- `[Benchmark(Baseline = true)]` pour marquer la méthode de référence
- `[GlobalSetup]` pour la préparation par combinaison de paramètres
- Exécution en une ligne via `BenchmarkRunner.Run<T>()`
- `SummaryFormatter` pour un aperçu rapide des victoires/défaites

```csharp
[BenchmarkClass(Description = "Comparaison des stratégies de concaténation de chaînes")]
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

## CollectionBenchmarks (Démonstration Complète des Attributs)

Démontre **la plupart** des attributs en comparant les performances de recherche de List, Dictionary et HashSet.

**Points Forts :**

- `[BenchmarkClass]` avec `Description`
- `[Params(100, 1_000, 10_000)]` - trois tailles de collection
- `[GlobalSetup]` - remplit les trois collections avec des données randomisées
- `[GlobalCleanup]` - libère les collections
- `[IterationSetup]` - mélange la cible de recherche avant chaque échantillon
- `[Benchmark(Baseline = true, Description = "...")]` - `List.Contains()` comme base
- `[Benchmark(Description = "...")]` - `Dictionary.ContainsKey()` et `HashSet.Contains()`
- Sortie multi-format : Console, Markdown, HTML, CSV

```csharp
[BenchmarkClass(Description = "Performances de recherche : List vs Dictionary vs HashSet")]
public partial class LookupBenchmarks
{
    [Params(100, 1_000, 10_000)]
    public int N { get; set; }

    [GlobalSetup]   public void Setup()         { /* remplir les collections */ }
    [GlobalCleanup] public void Cleanup()       { /* libérer les collections */ }
    [IterationSetup] public void ShuffleTarget() { /* varier la cible de recherche */ }

    [Benchmark(Baseline = true, Description = "Scan linéaire O(n)")]
    public void ListContains() { _ = _list.Contains(_target); }

    [Benchmark(Description = "Recherche par hachage O(1)")]
    public void DictionaryContainsKey() { _ = _dictionary.ContainsKey(_target); }

    [Benchmark(Description = "Recherche par hachage O(1), optimisée pour les ensembles")]
    public void HashSetContains() { _ = _hashSet.Contains(_target); }
}
```

```bash
dotnet run --project samples/CollectionBenchmarks -c Release
```

## Sortie

Tous les exemples enregistrent les résultats dans un sous-répertoire `results/` sous le dossier de sortie aux formats Markdown, HTML et CSV.

Ces rapports incluent désormais aussi des métadonnées orientées précision, comme l'erreur standard, l'écart-type relatif et des notes sur le compteur CPU lorsqu'elles sont disponibles.
