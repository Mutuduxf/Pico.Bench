# PicoBench

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Русский](README.ru.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Deutsch](README.de.md) | [Português (Brasil)](README.pt-BR.md)

Une bibliothèque de benchmarking légère et sans dépendances pour .NET avec **deux API complémentaires** : une API impérative fluide et une API basée sur des attributs, générée à partir du code source, entièrement **compatible AOT**.

## Caractéristiques

- **Zéro Dépendance** - Implémentation pure .NET, aucun package externe requis
- **Deux API** - Impérative (`Benchmark.Run`) pour les tests ad hoc ; basée sur les attributs (`[Benchmark]` + générateur de source) pour les suites structurées
- **Générateur de Source Compatible AOT** - Le générateur incrémental émet des appels de méthode directs sans réflexion à l'exécution
- **Multiplateforme** - Support complet pour Windows, Linux et macOS
- **Chronométrage Haute Précision** - Utilise `Stopwatch` avec une granularité au niveau nanoseconde
- **Suivi GC** - Surveille les comptes de collection Gen0/Gen1/Gen2 pendant les benchmarks
- **Comptage de Cycles CPU** - Comptage de cycles au niveau matériel (Windows via `QueryThreadCycleTime`, Linux via `perf_event`, macOS via `mach_absolute_time`)
- **Analyse Statistique** - Moyenne, Médiane, P90, P95, P99, Minimum, Maximum, Écart Type, erreur standard et variance relative
- **Formats de Sortie Multiples** - Console, Markdown, HTML, CSV et résumé programmatique
- **Benchmarks Paramétrés** - Attribut `[Params]` avec itération automatique du produit cartésien
- **Support de Comparaison** - Base vs candidat avec calculs d'accélération
- **Configurable** - Préconfigurations Quick, Default et Precise, auto-calibrage ou configuration entièrement personnalisée
- **netstandard2.0** - Compatible avec .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+

## Installation

Référencez le package NuGet **PicoBench**. Le générateur de source (`PicoBench.Generators`) est inclus automatiquement comme un analyseur - aucune référence supplémentaire n'est nécessaire.

```bash
dotnet add package PicoBench
```

## Démarrage Rapide

### API Impérative

```csharp
using PicoBench;

var result = Benchmark.Run("My Benchmark", () =>
{
    Thread.SpinWait(100);
});

Console.WriteLine($"Average: {result.Statistics.Avg:F1} ns/op");
```

### API Basée sur les Attributs (Générée par Source)

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

> La classe **doit** être `partial`. Le générateur de source émet une implémentation `IBenchmarkClass` au moment de la compilation - sans réflexion, entièrement sûr pour AOT.

> Une utilisation invalide des attributs produit désormais des diagnostics du générateur pour les erreurs courantes comme les classes non `partial`, les baselines dupliquées, les signatures de cycle de vie invalides et les valeurs `[Params]` incompatibles.

---

## Référence de l'API Impérative

### Benchmark Basique

```csharp
using PicoBench;
using PicoBench.Formatters;

var result = Benchmark.Run("SpinWait", () => Thread.SpinWait(100));
Console.WriteLine(new ConsoleFormatter().Format(result));
```

### Benchmark avec État (Éviter les Clôtures)

```csharp
var data = new byte[1024];
var result = Benchmark.Run("ArrayCopy", data, static d =>
{
    var copy = new byte[d.Length];
    Buffer.BlockCopy(d, 0, copy, 0, d.Length);
});
```

### Benchmarks avec Portée (DI-Friendly)

```csharp
var result = Benchmark.RunScoped("DbQuery",
    () => new MyDbContext(),
    static ctx => ctx.Users.FirstOrDefault()
);
// Une nouvelle portée est créée par échantillon ; la portée est supprimée après chaque échantillon.
```

### Comparer Deux Implémentations

```csharp
var comparison = Benchmark.Compare(
    "String vs StringBuilder",
    "String Concat",  () => { var s = ""; for (int i = 0; i < 100; i++) s += "a"; },
    "StringBuilder",  () => { var sb = new StringBuilder(); for (int i = 0; i < 100; i++) sb.Append('a'); _ = sb.ToString(); }
);

Console.WriteLine($"Speedup: {comparison.Speedup:F2}x ({comparison.ImprovementPercent:F1}%)");
```

### Avancé : Échauffement, Configuration et Nettoyage Séparés

```csharp
var result = Benchmark.Run(
    name:     "Custom",
    action:   () => DoWork(),
    warmup:   () => DoWork(),      // null pour ignorer l'échauffement
    config:   BenchmarkConfig.Precise,
    setup:    () => PrepareState(), // appelé avant chaque échantillon (non chronométré)
    teardown: () => CleanUp()       // appelé après chaque échantillon (non chronométré)
);
```

---

## Référence de l'API Basée sur les Attributs

Décorer une classe **partial** avec `[BenchmarkClass]` et ses méthodes/propriétés avec les attributs ci-dessous. Le générateur de source émet tout le code de connexion au moment de la compilation.

### Attributs

| Attribut | Cible | Description |
|-----------|--------|-------------|
| `[BenchmarkClass]` | Classe | Marque la classe pour la génération de code. Propriété optionnelle `Description`. |
| `[Benchmark]` | Méthode | Marque une méthode sans paramètres comme benchmark. Définir `Baseline = true` pour la méthode de référence. `Description` optionnelle. |
| `[Params(values)]` | Propriété / Champ | Itère les valeurs constantes à la compilation données. Plusieurs propriétés `[Params]` produisent un produit cartésien. |
| `[GlobalSetup]` | Méthode | Appelé **une fois** par combinaison de paramètres, avant l'exécution des benchmarks. |
| `[GlobalCleanup]` | Méthode | Appelé **une fois** par combinaison de paramètres, après l'exécution des benchmarks. |
| `[IterationSetup]` | Méthode | Appelé avant **chaque échantillon** (non chronométré). |
| `[IterationCleanup]` | Méthode | Appelé après **chaque échantillon** (non chronométré). |

Les méthodes `[Benchmark]` doivent être des méthodes d'instance, non génériques et sans paramètres. Les méthodes de cycle de vie doivent être d'instance, non génériques, sans paramètres et retourner `void`. Les cibles `[Params]` doivent être des propriétés d'instance modifiables ou des champs d'instance non `readonly`.

### Exemple Complet

```csharp
using PicoBench;

[BenchmarkClass(Description = "Comparaison des stratégies de concaténation de chaînes")]
public partial class StringBenchmarks
{
    [Params(10, 100, 1000)]
    public int N { get; set; }

    [GlobalSetup]
    public void Setup() { /* préparer les données pour N courant */ }

    [GlobalCleanup]
    public void Cleanup() { /* libérer les ressources */ }

    [IterationSetup]
    public void BeforeSample() { /* préparation par échantillon */ }

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

### Exécution

```csharp
// Créer l'instance en interne :
var suite = BenchmarkRunner.Run<StringBenchmarks>(BenchmarkConfig.Quick);

// Ou avec une instance préconfigurée :
var instance = new StringBenchmarks();
var suite2 = BenchmarkRunner.Run(instance, BenchmarkConfig.Quick);
```

---

## Configuration

### Préconfigurations

| Préconfiguration | Échauffement | Échantillons | Itérations de base/Échantillon | Auto-calibrage | Cas d'Utilisation |
|--------|--------|---------|--------------------------------|----------------|----------|
| `Quick` | 100 | 10 | 1,000 | Oui | Itération rapide / CI |
| `Default` | 1,000 | 100 | 10,000 | Non | Benchmarking général |
| `Precise` | 5,000 | 200 | 50,000 | Oui | Mesures finales |

### Configuration Personnalisée

```csharp
var config = new BenchmarkConfig
{
    WarmupIterations    = 500,
    SampleCount         = 50,
    IterationsPerSample = 5000,
    RetainSamples       = true,  // Conserver les données brutes TimingSample
    AutoCalibrateIterations = true,
    MinSampleTime       = TimeSpan.FromMilliseconds(0.5),
    MaxAutoIterationsPerSample = 1_000_000
};

var result = Benchmark.Run("Test", action, config);
```

Lorsque l'auto-calibrage est activé, PicoBench augmente `IterationsPerSample` jusqu'à atteindre un budget minimal de temps par échantillon ou `MaxAutoIterationsPerSample`. C'est particulièrement utile pour les opérations très rapides autrement dominées par le bruit du minuteur.

---

## Formateurs de Sortie

Cinq formateurs intégrés implémentent `IFormatter` :

```csharp
using PicoBench.Formatters;

var console  = new ConsoleFormatter();     // Tableaux console avec dessin de boîtes
var markdown = new MarkdownFormatter();    // Markdown compatible GitHub
var html     = new HtmlFormatter();        // Rapports HTML stylisés
var csv      = new CsvFormatter();         // CSV pour l'analyse de données

// Assistant statique pour les résumés de comparaison :
Console.WriteLine(SummaryFormatter.Format(suite.Comparisons));
```

Les sorties Console, Markdown, HTML et CSV incluent désormais des métadonnées orientées précision comme l'erreur standard, l'écart-type relatif et des notes sur le compteur CPU lorsqu'elles sont disponibles.

### Cibles de Formatage

```csharp
formatter.Format(result);               // BenchmarkResult unique
formatter.Format(results);              // IEnumerable<BenchmarkResult>
formatter.Format(comparison);           // ComparisonResult unique
formatter.Format(comparisons);          // IEnumerable<ComparisonResult>
formatter.Format(suite);                // BenchmarkSuite complet
```

### Options du Formateur

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
// Également disponibles : FormatterOptions.Default, .Compact, .Minimal
```

### Sauvegarde des Résultats

```csharp
var dir = Path.Combine(AppContext.BaseDirectory, "results");
Directory.CreateDirectory(dir);

File.WriteAllText(Path.Combine(dir, "results.md"),   new MarkdownFormatter().Format(suite));
File.WriteAllText(Path.Combine(dir, "results.html"), new HtmlFormatter().Format(suite));
File.WriteAllText(Path.Combine(dir, "results.csv"),  new CsvFormatter().Format(suite));
```

---

## Modèle de Résultats

| Type | Description |
|------|-------------|
| `BenchmarkResult` | Nom, Statistiques, Échantillons, ItérationsParÉchantillon, NombreDÉchantillons, Étiquettes, Catégorie |
| `ComparisonResult` | Base, Candidat, Accélération, EstPlusRapide, PourcentageAmélioration |
| `BenchmarkSuite` | Nom, Description, Résultats, Comparaisons, Environnement, Durée |
| `Statistics` | Moyenne, P50, P90, P95, P99, Minimum, Maximum, ÉcartType, StandardError, RelativeStdDevPercent, CyclesCpuParOp, GcInfo |
| `TimingSample` | NanosecondesÉcoulées, MillisecondesÉcoulées, TicksÉcoulés, CyclesCpu, GcInfo |
| `GcInfo` | Gen0, Gen1, Gen2, Total, EstZéro |
| `EnvironmentInfo` | OS, Architecture, VersionRuntime, NombreProcesseurs, Configuration, type/disponibilité du compteur CPU |

---

## Architecture

```
src/
+-- PicoBench/                        # Bibliothèque principale (netstandard2.0)
|   +-- Benchmark.cs                   # API impérative (Run, Compare, RunScoped)
|   +-- BenchmarkRunner.cs             # Point d'entrée basé sur les attributs (Run<T>)
|   +-- BenchmarkConfig.cs             # Configuration avec préconfigurations
|   +-- Attributes.cs                  # 7 attributs de benchmark
|   +-- IBenchmarkClass.cs             # Interface émise par le générateur
|   +-- Runner.cs                      # Moteur de chronométrage bas niveau
|   +-- StatisticsCalculator.cs        # Calcul des percentiles / statistiques
|   +-- Models.cs                      # Types de résultats
|   +-- Formatters/
|       +-- IFormatter.cs              # IFormatter, FormatterOptions & FormatterBase
|       +-- ConsoleFormatter.cs        # Tableaux console avec dessin de boîtes
|       +-- MarkdownFormatter.cs       # Tableaux Markdown GitHub
|       +-- HtmlFormatter.cs           # Rapports HTML stylisés
|       +-- CsvFormatter.cs            # Export CSV
|       +-- SummaryFormatter.cs        # Résumé victoires/défaites
|
+-- PicoBench.Generators/            # Générateur de source (netstandard2.0)
    +-- BenchmarkGenerator.cs          # Point d'entrée IIncrementalGenerator
    +-- Emitter.cs                     # Émetteur de code C# (sécurisé AOT)
    +-- Models.cs                      # Modèles d'analyse Roslyn
```

---

## Caractéristiques Spécifiques à la Plateforme

| Caractéristique | Windows | Linux | macOS |
|---------|---------|-------|-------|
| Chronométrage haute précision | Stopwatch | Stopwatch | Stopwatch |
| Suivi GC (Gen0/1/2) | Oui | Oui | Oui |
| Comptage de cycles CPU | `QueryThreadCycleTime` | `perf_event_open` | `mach_absolute_time` |
| Augmentation de priorité de processus | Oui | Oui | Oui |

Sous macOS, le compteur CPU exporté est un proxy monotone haute résolution et non un vrai compteur de cycles architecturaux. `EnvironmentInfo` et la sortie des formatteurs exposent explicitement cette différence.

---

## Exemples

| Exemple | Style d'API | Description |
|--------|-----------|-------------|
| `StringVsStringBuilder` | Impérative | Compare `string +=`, `StringBuilder` et `StringBuilder` avec capacité |
| `AttributeBased` | Basée sur les attributs | Même comparaison en utilisant `[Benchmark]`, `[Params]` et le générateur de source |
| `CollectionBenchmarks` | Basée sur les attributs | Recherche List vs Dictionary vs HashSet - montre chaque attribut |

```bash
dotnet run --project samples/StringVsStringBuilder -c Release
dotnet run --project samples/AttributeBased -c Release
dotnet run --project samples/CollectionBenchmarks -c Release
```

---

## Comparaison avec BenchmarkDotNet

| Caractéristique | PicoBench | BenchmarkDotNet |
|---------|-----------|----------------|
| Dépendances | 0 | Nombreuses |
| Taille du package | Petite | Grande |
| Framework cible | netstandard2.0 | net6.0+ |
| Support AOT | Générateur de source | Basé sur la réflexion |
| API d'attributs | `[Benchmark]`, `[Params]` | `[Benchmark]`, `[Params]` |
| Temps de configuration | Instantané | Secondes |
| Formats de sortie | 5 | 10+ |
| Profondeur statistique | Bonne | Étendue |
| Cas d'utilisation | Tests A/B rapides, CI, applications AOT | Analyse détaillée, publications |

---

## Licence

Licence MIT - voir le fichier [LICENSE](LICENSE) pour plus de détails.

## Contribution

1. Forkez le dépôt
2. Créez une branche de fonctionnalité
3. Effectuez des modifications avec des tests
4. Soumettez une pull request
