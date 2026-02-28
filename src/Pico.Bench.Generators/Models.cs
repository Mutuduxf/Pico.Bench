using System;
using System.Collections.Immutable;
using System.Linq;

namespace Pico.Bench.Generators;

/// <summary>
/// Describes a benchmark class discovered by the source generator.
/// All members are value-comparable to support incremental generator caching.
/// </summary>
internal sealed class BenchmarkClassModel : IEquatable<BenchmarkClassModel>
{
    public string? Namespace { get; init; }
    public string ClassName { get; init; } = "";
    public string AccessModifier { get; init; } = "public";
    public string? Description { get; init; }
    public string? GlobalSetupMethod { get; init; }
    public string? GlobalCleanupMethod { get; init; }
    public string? IterationSetupMethod { get; init; }
    public string? IterationCleanupMethod { get; init; }
    public ImmutableArray<BenchmarkMethodModel> Methods { get; init; } =
        ImmutableArray<BenchmarkMethodModel>.Empty;
    public ImmutableArray<ParamsPropertyModel> ParamsProperties { get; init; } =
        ImmutableArray<ParamsPropertyModel>.Empty;

    public bool Equals(BenchmarkClassModel? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Namespace == other.Namespace
            && ClassName == other.ClassName
            && AccessModifier == other.AccessModifier
            && Description == other.Description
            && GlobalSetupMethod == other.GlobalSetupMethod
            && GlobalCleanupMethod == other.GlobalCleanupMethod
            && IterationSetupMethod == other.IterationSetupMethod
            && IterationCleanupMethod == other.IterationCleanupMethod
            && Methods.SequenceEqual(other.Methods)
            && ParamsProperties.SequenceEqual(other.ParamsProperties);
    }

    public override bool Equals(object? obj) => Equals(obj as BenchmarkClassModel);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (Namespace?.GetHashCode() ?? 0);
            hash = hash * 31 + ClassName.GetHashCode();
            hash = hash * 31 + AccessModifier.GetHashCode();
            hash = hash * 31 + (Description?.GetHashCode() ?? 0);
            hash = hash * 31 + (GlobalSetupMethod?.GetHashCode() ?? 0);
            hash = hash * 31 + (GlobalCleanupMethod?.GetHashCode() ?? 0);
            hash = hash * 31 + (IterationSetupMethod?.GetHashCode() ?? 0);
            hash = hash * 31 + (IterationCleanupMethod?.GetHashCode() ?? 0);
            hash = hash * 31 + Methods.Length;
            hash = hash * 31 + ParamsProperties.Length;
            return hash;
        }
    }
}

/// <summary>
/// Describes a single [Benchmark]-attributed method.
/// </summary>
internal sealed class BenchmarkMethodModel : IEquatable<BenchmarkMethodModel>
{
    public string Name { get; init; } = "";
    public bool IsBaseline { get; init; }
    public string? Description { get; init; }

    public bool Equals(BenchmarkMethodModel? other)
    {
        if (other is null)
            return false;
        return Name == other.Name
            && IsBaseline == other.IsBaseline
            && Description == other.Description;
    }

    public override bool Equals(object? obj) => Equals(obj as BenchmarkMethodModel);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Name.GetHashCode();
            hash = hash * 31 + IsBaseline.GetHashCode();
            hash = hash * 31 + (Description?.GetHashCode() ?? 0);
            return hash;
        }
    }
}

/// <summary>
/// Describes a [Params]-attributed property or field.
/// Values are stored as pre-formatted C# literals for direct code emission.
/// </summary>
internal sealed class ParamsPropertyModel : IEquatable<ParamsPropertyModel>
{
    public string Name { get; init; } = "";
    public string TypeFullName { get; init; } = "";
    public ImmutableArray<string> FormattedValues { get; init; } = ImmutableArray<string>.Empty;

    public bool Equals(ParamsPropertyModel? other)
    {
        if (other is null)
            return false;
        return Name == other.Name
            && TypeFullName == other.TypeFullName
            && FormattedValues.SequenceEqual(other.FormattedValues);
    }

    public override bool Equals(object? obj) => Equals(obj as ParamsPropertyModel);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Name.GetHashCode();
            hash = hash * 31 + TypeFullName.GetHashCode();
            hash = hash * 31 + FormattedValues.Length;
            return hash;
        }
    }
}
