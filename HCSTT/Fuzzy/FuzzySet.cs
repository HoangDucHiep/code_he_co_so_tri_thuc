using System.Text;

namespace HCSTT.Fuzzy;


/// <summary>
/// Represents a fuzzy set over a universe of discourse.
/// </summary>
/// <typeparam name="T">The type of elements in the universe.</typeparam>
public class FuzzySet<T> where T : notnull
{
    public Universe<T> Universe { get; }
    private readonly Dictionary<T, double> _mu;
    public string Name { get; }

    public FuzzySet(Universe<T> universe, string name)
    {
        Universe = universe;
        Name = name;
        _mu = new Dictionary<T, double>();
    }

    public FuzzySet(Universe<T> universe, IDictionary<T, double> memberships, string name = "A")
            : this(universe, name)
    {
        foreach (var kv in memberships)
            this[kv.Key] = kv.Value;
    }

    public double this[T x]
    {
        get => _mu.GetValueOrDefault(x, 0.0);
        set
        {
            if (!Universe.Contains(x))
                throw new ArgumentException($"Element {x} is not in the universe.");
            if (value < 0.0 || value > 1.0)
                throw new ArgumentOutOfRangeException(nameof(value), "Membership degree must be in [0, 1].");
            _mu[x] = value;
        }
    }

    /// <summary>
    /// Checks if two fuzzy sets have the same universe. Throws an exception if they do not.
    /// </summary>
    /// <param name="a">The first fuzzy set.</param>
    /// <param name="b">The second fuzzy set.</param>
    /// <exception cref="ArgumentException">Thrown when the fuzzy sets have different universes.</exception>
    private static void CheckSameUniverse(FuzzySet<T> a, FuzzySet<T> b)
    {
        if (!a.Universe.Equals(b.Universe))
            throw new ArgumentException("Fuzzy sets must have the same universe.");
    }

    /// <summary>
    /// Returns the union of two fuzzy sets.
    /// </summary>
    /// <param name="a">The first fuzzy set.</param>
    /// <param name="b">The second fuzzy set.</param>
    /// <returns>The union of the two fuzzy sets.</returns>
    /// <exception cref="ArgumentException">Thrown when the fuzzy sets have different universes.</exception>
    public static FuzzySet<T> Union(FuzzySet<T> a, FuzzySet<T> b)
    {
        CheckSameUniverse(a, b);
        var result = new FuzzySet<T>(a.Universe, $"({a.Name} ∪ {b.Name})");
        foreach (var x in a.Universe.GetAllElements())
            result[x] = Math.Max(a[x], b[x]);

        return result;
    }

    /// <summary>
    /// Returns the intersection of two fuzzy sets.
    /// </summary>
    /// <param name="a">The first fuzzy set.</param>
    /// <param name="b">The second fuzzy set.</param>
    /// <returns>The intersection of the two fuzzy sets.</returns>
    /// <exception cref="ArgumentException">Thrown when the fuzzy sets have different universes.</exception>
    public static FuzzySet<T> Intersection(FuzzySet<T> a, FuzzySet<T> b)
    {
        CheckSameUniverse(a, b);
        var result = new FuzzySet<T>(a.Universe, $"({a.Name} ∩ {b.Name})");
        foreach (var x in a.Universe.GetAllElements())
            result[x] = Math.Min(a[x], b[x]);
        return result;
    }

    /// <summary>
    /// Returns the complement of a fuzzy set.
    /// </summary>
    /// <param name="a">The fuzzy set to complement.</param>
    /// <returns>The complement of the fuzzy set.</returns>
    public static FuzzySet<T> Complement(FuzzySet<T> a)
    {
        var result = new FuzzySet<T>(a.Universe, $"¬{a.Name}");
        foreach (var x in a.Universe.GetAllElements())
            result[x] = 1.0 - a[x];
        return result;
    }

    /// <summary>
    /// Checks whether two fuzzy sets are equal within a tolerance.
    /// </summary>
    /// <param name="a">The first fuzzy set.</param>
    /// <param name="b">The second fuzzy set.</param>
    /// <param name="epsilon">The tolerance for equality comparison.</param>
    /// <returns>True if the fuzzy sets are equal within the specified tolerance; otherwise, false.</returns>
    public static bool AreEqual(FuzzySet<T> a, FuzzySet<T> b, double epsilon = 1e-6)
    {
        CheckSameUniverse(a, b);
        return a.Universe.GetAllElements().All(x => Math.Abs(a[x] - b[x]) <= epsilon);
    }

    /// <summary>
    /// Checks whether one fuzzy set is a subset of another within a tolerance.
    /// </summary>
    /// <param name="a">The first fuzzy set.</param>
    /// <param name="b">The second fuzzy set.</param>
    /// <param name="epsilon">The tolerance for subset comparison.</param>
    /// <returns>True if <paramref name="a"/> is a subset of <paramref name="b"/>; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when the fuzzy sets have different universes.</exception>
    public static bool IsSubset(FuzzySet<T> a, FuzzySet<T> b, double epsilon = 1e-6)
    {
        CheckSameUniverse(a, b);
        return a.Universe.GetAllElements().All(x => a[x] <= b[x] + epsilon);
    }


    // Relation-style operations over product universes.



    /// <summary>
    /// Returns the Cartesian product of two fuzzy sets using the min t-norm.
    /// </summary>
    /// <typeparam name="T1">The element type of the first fuzzy set.</typeparam>
    /// <typeparam name="T2">The element type of the second fuzzy set.</typeparam>
    /// <param name="a">The first fuzzy set.</param>
    /// <param name="b">The second fuzzy set.</param>
    /// <returns>A fuzzy set over the product universe.</returns>
    public static FuzzySet<(T1, T2)> CartesianProduct<T1, T2>(FuzzySet<T1> a, FuzzySet<T2> b)
        where T1 : notnull where T2 : notnull
    {
        var u = Universe<(T1, T2)>.CartesianProduct(a.Universe, b.Universe);
        var r = new FuzzySet<(T1, T2)>(u, $"({a.Name} x {b.Name})");
        foreach (var (x, y) in u.GetAllElements())
            r[(x, y)] = Math.Min(a[x], b[y]);
        return r;
    }


    /// <summary>
    /// Projects a fuzzy relation onto its first component using max aggregation.
    /// </summary>
    /// <typeparam name="T1">The element type of the first component.</typeparam>
    /// <typeparam name="T2">The element type of the second component.</typeparam>
    /// <param name="r">The fuzzy relation to project.</param>
    /// <returns>The projection on the first component.</returns>
    public static FuzzySet<T1> ProjectionOnFirst<T1, T2>(FuzzySet<(T1, T2)> r)
        where T1 : notnull where T2 : notnull
    {
        var firstElems = r.Universe.GetAllElements().Select(p => p.Item1).Distinct().ToList();
        var u1 = new Universe<T1>(firstElems);
        var result = new FuzzySet<T1>(u1, $"Proj1({r.Name})");
        foreach (var x in u1.GetAllElements())
        {
            var vals = r.Universe.GetAllElements()
                .Where(p => EqualityComparer<T1>.Default.Equals(p.Item1, x))
                .Select(p => r[p]);
            result[x] = vals.DefaultIfEmpty(0.0).Max();
        }
        return result;
    }

    /// <summary>
    /// Projects a fuzzy relation onto its second component using max aggregation.
    /// </summary>
    /// <typeparam name="T1">The element type of the first component.</typeparam>
    /// <typeparam name="T2">The element type of the second component.</typeparam>
    /// <param name="r">The fuzzy relation to project.</param>
    /// <returns>The projection on the second component.</returns>
    public static FuzzySet<T2> ProjectionOnSecond<T1, T2>(FuzzySet<(T1, T2)> r)
        where T1 : notnull where T2 : notnull
    {
        var secondElems = r.Universe.GetAllElements().Select(p => p.Item2).Distinct().ToList();
        var u2 = new Universe<T2>(secondElems);
        var result = new FuzzySet<T2>(u2, $"Proj2({r.Name})");
        foreach (var y in u2.GetAllElements())
        {
            var vals = r.Universe.GetAllElements()
                .Where(p => EqualityComparer<T2>.Default.Equals(p.Item2, y))
                .Select(p => r[p]);
            result[y] = vals.DefaultIfEmpty(0.0).Max();
        }
        return result;
    }

    /// <summary>
    /// Builds the cylindrical extension of a fuzzy set from the first component.
    /// </summary>
    /// <typeparam name="T1">The element type of the first component.</typeparam>
    /// <typeparam name="T2">The element type of the second component.</typeparam>
    /// <param name="a">The fuzzy set on the first universe.</param>
    /// <param name="u2">The second universe.</param>
    /// <returns>A fuzzy set over the product universe.</returns>
    public static FuzzySet<(T1, T2)> CylindricalExtensionFrom1<T1, T2>(FuzzySet<T1> a, Universe<T2> u2)
        where T1 : notnull where T2 : notnull
    {
        var u = Universe<T1>.CartesianProduct(a.Universe, u2);
        var r = new FuzzySet<(T1, T2)>(u, $"CE({a.Name})");
        foreach (var (x, y) in u.GetAllElements())
            r[(x, y)] = a[x];
        return r;
    }

    /// <summary>
    /// Builds the cylindrical extension of a fuzzy set from the second component.
    /// </summary>
    /// <typeparam name="T1">The element type of the first component.</typeparam>
    /// <typeparam name="T2">The element type of the second component.</typeparam>
    /// <param name="u1">The first universe.</param>
    /// <param name="b">The fuzzy set on the second universe.</param>
    /// <returns>A fuzzy set over the product universe.</returns>
    public static FuzzySet<(T1, T2)> CylindricalExtensionFrom2<T1, T2>(Universe<T1> u1, FuzzySet<T2> b)
        where T1 : notnull where T2 : notnull
    {
        var u = Universe<T1>.CartesianProduct(u1, b.Universe);
        var r = new FuzzySet<(T1, T2)>(u, $"CE({b.Name})");
        foreach (var (x, y) in u.GetAllElements())
            r[(x, y)] = b[y];
        return r;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(Name).Append(" = ");
        sb.Append(string.Join(" + ", Universe.GetAllElements().Select(x => $"{this[x]:0.###}/{x}")));
        return sb.ToString();
    }

    public static FuzzySet<T> operator |(FuzzySet<T> a, FuzzySet<T> b) => Union(a, b);
    public static FuzzySet<T> operator &(FuzzySet<T> a, FuzzySet<T> b) => Intersection(a, b);
    public static FuzzySet<T> operator !(FuzzySet<T> a) => Complement(a);


}