using System.Text;

namespace HCSTT.Fuzzy;

public class FuzzyRelation<T1, T2> where T1 : notnull where T2 : notnull
{
    public Universe<T1> Universe1 { get; }
    public Universe<T2> Universe2 { get; }
    public string Name { get; set; }

    private readonly Dictionary<(T1, T2), double> _mu = new();

    public FuzzyRelation(Universe<T1> u1, Universe<T2> u2, string name = "R")
    {
        Universe1 = u1;
        Universe2 = u2;
        Name = name;
    }

    public FuzzyRelation(FuzzySet<(T1, T2)> fuzzySet, string name = "R")
    {
        Universe1 = new Universe<T1>(fuzzySet.Universe.GetAllElements().Select(pair => pair.Item1));
        Universe2 = new Universe<T2>(fuzzySet.Universe.GetAllElements().Select(pair => pair.Item2));
        Name = name;

        foreach (var (x, y) in fuzzySet.Universe.GetAllElements())
        {
            _mu[(x, y)] = fuzzySet[(x, y)];
        }
    }

    // Indexer
    public double this[T1 x, T2 y]
    {
        get => _mu.GetValueOrDefault((x, y), 0.0);
        set
        {
            if (!Universe1.Contains(x) || !Universe2.Contains(y))
                throw new ArgumentException($"Pair ({x}, {y}) is outside the product universe.");
            if (value is < 0.0 or > 1.0)
                throw new ArgumentOutOfRangeException(nameof(value), "Membership degree must be in [0, 1].");

            _mu[(x, y)] = value;
        }
    }

    /// <summary>
    /// Performs composition with another relation S using a specified strategy.
    /// Solves Question 2d & 2e.
    /// </summary>
    public FuzzyRelation<T1, T3> ComposeWith<T3>(
        FuzzyRelation<T2, T3> s,
        IFuzzyCompositionStrategy strategy)
        where T3 : notnull
    {
        if (!Universe2.Equals(s.Universe1))
        {
            throw new InvalidOperationException(
                $"Cannot compose relation {Name} and {s.Name}. Intermediate universes do not match.");
        }

        var resultRelation = new FuzzyRelation<T1, T3>(
            Universe1,
            s.Universe2,
            $"({Name} ∘ {s.Name})[{strategy.Name}]"
        );

        foreach (var x in Universe1.GetAllElements())
        {
            foreach (var z in s.Universe2.GetAllElements())
            {
                var combinedValues = Universe2.GetAllElements()
                    .Select(y => strategy.Combine(this[x, y], s[y, z]));

                resultRelation[x, z] = strategy.Aggregate(combinedValues);
            }
        }

        return resultRelation;
    }
    public override string ToString()
    {
        var sb = new StringBuilder();
        var rows = Universe1.GetAllElements().ToList();
        var cols = Universe2.GetAllElements().ToList();

        sb.AppendLine($"--- Relation Matrix: {Name} ---");

        sb.Append($"{"",-10}");
        foreach (var col in cols)
        {
            sb.Append($"{col,-10}");
        }
        sb.AppendLine();

        foreach (var row in rows)
        {
            sb.Append($"{row,-10}");
            foreach (var col in cols)
            {
                sb.Append($"{this[row, col],-10:0.###}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}

/// <summary>
/// Strategy interface for fuzzy relation composition (Sup-T composition).
/// </summary>
public interface IFuzzyCompositionStrategy
{
    string Name { get; }

    /// <summary>
    /// Combines two membership values (T-norm step).
    /// </summary>
    double Combine(double rVal, double sVal);

    /// <summary>
    /// Aggregates multiple combined values over the intermediate universe (S-norm step).
    /// </summary>
    double Aggregate(IEnumerable<double> values);
}

/// <summary>
/// Max-Min Composition Strategy.
/// </summary>
public class MaxMinComposition : IFuzzyCompositionStrategy
{
    public string Name => "Max-Min";
    public double Combine(double rVal, double sVal) => Math.Min(rVal, sVal);
    public double Aggregate(IEnumerable<double> values) => values.DefaultIfEmpty(0.0).Max();
}

/// <summary>
/// Max-Product Composition Strategy.
/// </summary>
public class MaxProductComposition : IFuzzyCompositionStrategy
{
    public string Name => "Max-Product";
    public double Combine(double rVal, double sVal) => rVal * sVal;
    public double Aggregate(IEnumerable<double> values) => values.DefaultIfEmpty(0.0).Max();
}

/// <summary>
/// Max-Lukasiewicz Composition Strategy (Bounded Product).
/// </summary>
public class MaxLukasiewiczComposition : IFuzzyCompositionStrategy
{
    public string Name => "Max-Lukasiewicz";
    public double Combine(double rVal, double sVal) => Math.Max(0.0, rVal + sVal - 1.0);
    public double Aggregate(IEnumerable<double> values) => values.DefaultIfEmpty(0.0).Max();
}