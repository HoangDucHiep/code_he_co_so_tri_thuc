namespace THUC_HANH_DE_THI_MINH_HOA.Bai1_Quan_He_Mo;

public partial class FuzzyRelation<T1, T2>
	where T1 : notnull
	where T2 : notnull
{
	public static FuzzyRelation<T1, T2> Implication(
		FuzzySet<T1> set1,
		FuzzySet<T2> set2,
		IFuzzyImplicationStrategy strategy,
		string name = "R")
	{
		ArgumentNullException.ThrowIfNull(set1);
		ArgumentNullException.ThrowIfNull(set2);
		ArgumentNullException.ThrowIfNull(strategy);

		var relation = new FuzzyRelation<T1, T2>(
			set1.Universe,
			set2.Universe,
			name);

		foreach (var x in set1.Universe.GetAllElements())
			foreach (var y in set2.Universe.GetAllElements())
			{
				relation[x, y] = strategy.Evaluate(set1[x], set2[y]);
			}

		return relation;
	}

	public static FuzzyRelation<T1, T2> Intersect(
		FuzzyRelation<T1, T2> first,
		FuzzyRelation<T1, T2> second,
		string? name = null)
	{
		ArgumentNullException.ThrowIfNull(first);
		ArgumentNullException.ThrowIfNull(second);

		if (!first.Universe1.Equals(second.Universe1) ||
			!first.Universe2.Equals(second.Universe2))
		{
			throw new InvalidOperationException(
				"Chỉ có thể giao hai quan hệ trên cùng một tích vũ trụ.");
		}

		var result = new FuzzyRelation<T1, T2>(
			first.Universe1,
			first.Universe2,
			name ?? $"({first.Name} giao {second.Name})");

		foreach (var x in first.Universe1.GetAllElements())
			foreach (var y in first.Universe2.GetAllElements())
			{
				result[x, y] = Math.Min(first[x, y], second[x, y]);
			}

		return result;
	}

	public static class Implications
	{
		public static IFuzzyImplicationStrategy Lukasiewicz { get; } =
			new LukasiewiczImplication();

		public static IFuzzyImplicationStrategy DienesRescher { get; } =
			new DienesRescherImplication();

		public static IFuzzyImplicationStrategy Mamdani { get; } =
			new MamdaniImplication();

		public static IFuzzyImplicationStrategy Zadeh { get; } =
			new ZadehImplication();
	}
}

public interface IFuzzyImplicationStrategy
{
	string Name { get; }

	double Evaluate(double muA, double muB);
}


public sealed class LukasiewiczImplication : IFuzzyImplicationStrategy
{
	public string Name => "Lukasiewicz Implication";

	public double Evaluate(double muA, double muB)
	{
		return Math.Min(1.0, 1.0 - muA + muB);
	}
}

public sealed class DienesRescherImplication : IFuzzyImplicationStrategy
{
	public string Name => "Dienes-Rescher Implication";

	public double Evaluate(double muA, double muB)
	{
		return Math.Max(1.0 - muA, muB);
	}
}

public sealed class MamdaniImplication : IFuzzyImplicationStrategy
{
	public string Name => "Mamdani Implication";

	// chuan nen la T_norm, o day chi dung min cho nhanh
	public double Evaluate(double muA, double muB)
	{
		return Math.Min(muA, muB);
	}
}

public sealed class ZadehImplication : IFuzzyImplicationStrategy
{
	public string Name => "Zadeh Implication";

	public double Evaluate(double muA, double muB)
	{
		return Math.Max(1.0 - muA, Math.Min(muA, muB));
	}
}