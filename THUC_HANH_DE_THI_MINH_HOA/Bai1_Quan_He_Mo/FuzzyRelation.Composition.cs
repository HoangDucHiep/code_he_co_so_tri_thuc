namespace THUC_HANH_DE_THI_MINH_HOA.Bai1_Quan_He_Mo;

public partial class FuzzyRelation<T1, T2>
	where T1 : notnull
	where T2 : notnull
{
	public FuzzySet<T2> Compose(
		FuzzySet<T1> fuzzySet,
		IFuzzyCompositionStrategy strategy)
	{
		ArgumentNullException.ThrowIfNull(fuzzySet);
		ArgumentNullException.ThrowIfNull(strategy);

		if (!Universe1.Equals(fuzzySet.Universe))
		{
			throw new InvalidOperationException(
				$"Không thể hợp thành {fuzzySet.Name} và {Name}: vũ trụ của tập mờ và vế đầu của quan hệ không giống nhau.");
		}

		var result = new FuzzySet<T2>(
			Universe2,
			$"({fuzzySet.Name} o {Name})[{strategy.Name}]");

		foreach (var y in Universe2.GetAllElements())
		{
			var combinedValues = Universe1.GetAllElements()
				.Select(x => strategy.Combine(fuzzySet[x], this[x, y]));

			result[y] = strategy.Aggregate(combinedValues);
		}

		return result;
	}

	public static class Compositions
	{
		public static IFuzzyCompositionStrategy MaxMin { get; } = new MaxMinComposition();
		public static IFuzzyCompositionStrategy MaxProduct { get; } = new MaxProductComposition();
		public static IFuzzyCompositionStrategy MaxLukasiewicz { get; } = new MaxLukasiewiczComposition();
	}
}

public interface IFuzzyCompositionStrategy
{
	string Name { get; }

	// bước 1: kết hợp giá trị của quan hệ và giá trị của tập mờ
	double Combine(double relationValue, double otherValue);

	// bước 2: tổng hợp các giá trị kết hợp để tính giá trị thành viên cuối cùng
	double Aggregate(IEnumerable<double> values);
}

public sealed class MaxMinComposition : IFuzzyCompositionStrategy
{
	public string Name => "Max-Min";

	public double Combine(double relationValue, double otherValue)
		=> Math.Min(relationValue, otherValue);

	public double Aggregate(IEnumerable<double> values)
		=> values.DefaultIfEmpty(0.0).Max();
}

public sealed class MaxProductComposition : IFuzzyCompositionStrategy
{
	public string Name => "Max-Product";

	public double Combine(double relationValue, double otherValue)
		=> relationValue * otherValue;

	public double Aggregate(IEnumerable<double> values)
		=> values.DefaultIfEmpty(0.0).Max();
}

public sealed class MaxLukasiewiczComposition : IFuzzyCompositionStrategy
{
	public string Name => "Max-Lukasiewicz";

	public double Combine(double relationValue, double otherValue)
		=> Math.Max(0.0, relationValue + otherValue - 1.0);

	public double Aggregate(IEnumerable<double> values)
		=> values.DefaultIfEmpty(0.0).Max();
}
