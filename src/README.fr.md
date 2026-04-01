# Projets Source

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

Ce répertoire contient les deux projets de bibliothèque qui composent PicoBench.

## PicoBench

La bibliothèque de benchmarking principale ciblant **netstandard2.0** sans dépendances externes.

### Fichiers Clés

| Fichier | Objectif |
|------|---------|
| `Benchmark.cs` | API impérative - `Run()`, `Run<TState>()`, `RunScoped<TScope>()`, `Compare()` |
| `BenchmarkRunner.cs` | Point d'entrée basé sur les attributs - `Run<T>()` |
| `Attributes.cs` | Sept attributs : `[BenchmarkClass]`, `[Benchmark]`, `[Params]`, `[GlobalSetup]`, `[GlobalCleanup]`, `[IterationSetup]`, `[IterationCleanup]` |
| `IBenchmarkClass.cs` | Interface implémentée par le générateur de source sur les classes décorées |
| `BenchmarkConfig.cs` | Configuration avec les préconfigurations Quick / Default / Precise plus auto-calibrage optionnel |
| `Runner.cs` | Moteur de chronométrage bas niveau avec comptage de cycles CPU spécifique à la plateforme |
| `StatisticsCalculator.cs` | Calcul des percentiles et des statistiques |
| `Models.cs` | Types de résultats incluant les champs de précision dans `Statistics` et les métadonnées de compteur CPU dans `EnvironmentInfo` |
| `Formatters/` | Cinq formateurs : Console, Markdown, HTML, CSV, Summary |

### Empaquetage

Le projet inclut `PicoBench.Generators` comme un analyseur afin que les consommateurs obtiennent automatiquement le générateur de source :

```bash
# Ajouter la référence du projet
dotnet add reference ../PicoBench.Generators/PicoBench.Generators.csproj

# Puis ajoutez manuellement les attributs suivants à l'élément <ProjectReference> dans votre fichier .csproj :
# PrivateAssets="all"
# ReferenceOutputAssembly="false"
# OutputItemType="Analyzer"
```

## PicoBench.Generators

Un **générateur de source incrémental** (`IIncrementalGenerator`) qui transforme les classes partial décorées avec `[BenchmarkClass]` en implémentations complètes de `IBenchmarkClass` au moment de la compilation.

- **Cible** : netstandard2.0
- **Dépendance** : Microsoft.CodeAnalysis.CSharp 5.0.0
- **Sortie** : C# compatible AOT avec des appels qualifiés `global::` et sans réflexion

### Fichiers Clés

| Fichier | Objectif |
|------|---------|
| `BenchmarkGenerator.cs` | Point d'entrée du générateur utilisant `ForAttributeWithMetadataName` |
| `DiagnosticDescriptors.cs` | Diagnostics centralisés du générateur pour les déclarations de benchmark invalides |
| `Emitter.cs` | Émetteur de code C# - génère `RunBenchmarks()` avec itération de paramètres, crochets de configuration/nettoyage et logique de comparaison |
| `Models.cs` | Modèles d'analyse Roslyn : `BenchmarkClassModel`, `BenchmarkMethodModel`, `ParamsPropertyModel` (tous `IEquatable<T>` pour le cache) |

Le générateur valide désormais les erreurs courantes avant l'émission du code et signale des diagnostics pour les méthodes benchmark invalides, les méthodes de cycle de vie invalides, les baselines dupliquées, les cibles `[Params]` invalides et les valeurs de paramètres incompatibles.

### Code Généré

Pour une classe comme :

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

Le générateur émet une `partial class MyBench : IBenchmarkClass` avec une méthode `RunBenchmarks()` qui :

1. Itère chaque valeur `[Params]` (produit cartésien pour plusieurs propriétés)
2. Définit la propriété, appelle `[GlobalSetup]`
3. Exécute chaque méthode `[Benchmark]` via `Benchmark.Run()` avec `[IterationSetup]`/`[IterationCleanup]` comme configuration/nettoyage
4. Compare les candidats par rapport à la base
5. Appelle `[GlobalCleanup]`
6. Retourne un `BenchmarkSuite` avec tous les résultats et comparaisons
