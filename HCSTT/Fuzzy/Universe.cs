namespace HCSTT.Fuzzy;

/// <summary>
/// Represents a universe of distinct elements for fuzzy operations.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public class Universe<T> where T : notnull
{
    private HashSet<T> Elements { get; }

    public Universe(IEnumerable<T> elements)
    {
        Elements = [.. elements.Distinct()];
    }

    public int Count => Elements.Count;
    public bool Contains(T x) => Elements.Contains(x);

    public IEnumerable<T> GetAllElements() => Elements;

    /// <summary>
    /// Returns the Cartesian product of two universes.
    /// </summary>
    /// <typeparam name="T1">The element type of the first universe.</typeparam>
    /// <typeparam name="T2">The element type of the second universe.</typeparam>
    /// <param name="u1">The first universe.</param>
    /// <param name="u2">The second universe.</param>
    /// <returns>A universe of ordered pairs from <paramref name="u1"/> and <paramref name="u2"/>.</returns>
    public static Universe<(T1, T2)> CartesianProduct<T1, T2>(Universe<T1> u1, Universe<T2> u2)
        where T1 : notnull where T2 : notnull
    {
        var pairs = u1.GetAllElements().SelectMany(x => u2.GetAllElements().Select(y => (x, y)));
        return new Universe<(T1, T2)>(pairs);
    }

    public override string ToString() => "{" + string.Join(", ", Elements) + "}";

    public override bool Equals(object? obj)
    {
        if (obj is not Universe<T> other) return false;
        return Elements.Count == other.Elements.Count &&
                !Elements.Except(other.Elements).Any();
    }

    public override int GetHashCode() => Elements.Aggregate(0, (h, e) => h ^ e.GetHashCode());
}