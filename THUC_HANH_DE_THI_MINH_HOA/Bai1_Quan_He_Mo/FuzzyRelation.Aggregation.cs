namespace THUC_HANH_DE_THI_MINH_HOA.Bai1_Quan_He_Mo;

public partial class FuzzyRelation<T1, T2>
	where T1 : notnull
	where T2 : notnull
{
	public static FuzzyRelation<T1, T2> Aggregate(
			FuzzyRelation<T1, T2> relation1,
			FuzzyRelation<T1, T2> relation2,
			IFuzzyAggregationStrategy strategy,
			string name = "R")
	{
		if (!relation1.Universe1.Equals(relation2.Universe1) ||
			!relation1.Universe2.Equals(relation2.Universe2))
			throw new ArgumentException("Hai quan hệ mờ phải có cùng tập vũ trụ.");

		var result = new FuzzyRelation<T1, T2>(
			relation1.Universe1,
			relation1.Universe2,
			name);

		foreach (T1 x in relation1.Universe1.GetAllElements())
			foreach (T2 y in relation1.Universe2.GetAllElements())
			{
				result[x, y] = strategy.Aggregate(relation1[x, y], relation2[x, y]);
			}

		return result;
	}

	public static class Aggregations
	{
		public static IFuzzyAggregationStrategy Max { get; } = new MaxAggregation();
		public static IFuzzyAggregationStrategy Min { get; } = new MinAggregation();
	}


}


public interface IFuzzyAggregationStrategy
{
	string Name { get; }

	double Aggregate(double muA, double muB);
}

public class MaxAggregation : IFuzzyAggregationStrategy
{
	public string Name => "Max Aggregation";

	public double Aggregate(double muA, double muB)
	{
		return Math.Max(muA, muB);
	}
}

public class MinAggregation : IFuzzyAggregationStrategy
{
	public string Name => "Min Aggregation";

	public double Aggregate(double muA, double muB)
	{
		return Math.Min(muA, muB);
	}
}