using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;





// Tập vũ trụ
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

	public override string ToString() => "{" + string.Join(", ", Elements) + "}";

	public override bool Equals(object? obj)
	{
		if (obj is not Universe<T> other) return false;
		return Elements.Count == other.Elements.Count &&
				!Elements.Except(other.Elements).Any();
	}

	public override int GetHashCode() => Elements.Aggregate(0, (h, e) => h ^ e.GetHashCode());
}




// Biểu diễn một tập mờ trên một vũ trụ xác định.
public class FuzzySet<T> where T : notnull
{
	// Vũ trụ chứa các phần tử của tập mờ.
	public Universe<T> Universe { get; }

	// Lưu độ thuộc của từng phần tử trong tập mờ.
	private readonly Dictionary<T, double> _mu;

	// Tên dùng để nhận diện tập mờ khi hiển thị hoặc suy diễn.
	public string Name { get; }

	// Khởi tạo một tập mờ rỗng trên vũ trụ cho trước.
	public FuzzySet(Universe<T> universe, string name)
	{
		Universe = universe;
		Name = name;
		_mu = new Dictionary<T, double>();
	}

	// Khởi tạo tập mờ từ một bảng độ thuộc.
	public FuzzySet(Universe<T> universe, IDictionary<T, double> memberships, string name = "A")
				: this(universe, name)
	{
		foreach (KeyValuePair<T, double> kv in memberships)
			this[kv.Key] = kv.Value;
	}

	// Đọc hoặc cập nhật độ thuộc của một phần tử.
	public double this[T x]
	{
		get => _mu.GetValueOrDefault(x, 0.0);
		set
		{
			if (!Universe.Contains(x))
				throw new ArgumentException($"Phần tử {x} không thuộc tập vũ trụ.", nameof(x));
			if (value < 0.0 || value > 1.0)
				throw new ArgumentOutOfRangeException(nameof(value), "Giá trị thành viên phải nằm trong khoảng [0, 1].");
			_mu[x] = value;
		}
	}

	// Trả về độ thuộc của phần tử, mặc định là 0 nếu chưa được gán.
	public double GetMembership(T x) => this[x];

	// Định dạng tập mờ thành biểu thức tổng các phần tử và độ thuộc.
	public string ToMembershipExpression()
	{
		return string.Join(
			" + ",
			Universe.GetAllElements()
				.Select(value => $"{this[value]:0.###}/{value}"));
	}

	// Nhóm các phép mờ hóa để gọi theo cú pháp FuzzySet<T>.Fuzzify....
	public static class Fuzzify
	{
		// Mờ hóa một giá trị rõ thành tập mờ singleton.
		public static FuzzySet<T> Singleton(
			Universe<T> universe,
			T crispValue,
			string name = "A'")
			=> FuzzySet<T>.Singleton(universe, crispValue, name);

		// Mờ hóa bằng một hàm thành viên trên toàn bộ vũ trụ.
		public static FuzzySet<T> FromMembershipFunction(
			Universe<T> universe,
			Func<T, double> membershipFunction,
			string name = "A")
			=> FuzzySet<T>.FromMembershipFunction(
				universe,
				membershipFunction,
				name);
	}

	// Nhóm các phép khử mờ để gọi theo cú pháp FuzzySet<T>.Defuzzify....
	public static class Defuzzify
	{
		// Khử mờ bằng cực đại và chọn phần tử nhỏ nhất khi đồng cực đại.
		public static T MaxMembership(
			FuzzySet<T> fuzzySet,
			IComparer<T>? comparer = null)
			=> fuzzySet.DefuzzifyMaxMembership(comparer);

		// Khử mờ bằng phương pháp trọng tâm.
		public static double Centroid(
			FuzzySet<T> fuzzySet,
			Func<T, double> valueSelector)
			=> fuzzySet.DefuzzifyCentroid(valueSelector);
	}


	// Tạo tập mờ singleton với độ thuộc 1 tại giá trị rõ đầu vào.
	private static FuzzySet<T> Singleton(
		Universe<T> universe,
		T crispValue,
		string name = "A'")
	{
		ArgumentNullException.ThrowIfNull(universe);

		if (!universe.Contains(crispValue))
		{
			throw new ArgumentException(
				$"Giá trị {crispValue} không thuộc vũ trụ mờ hóa.",
				nameof(crispValue));
		}

		var result = new FuzzySet<T>(universe, name);

		foreach (T value in universe.GetAllElements())
		{
			result[value] = EqualityComparer<T>.Default.Equals(value, crispValue)
				? 1.0
				: 0.0;
		}

		return result;
	}

	// Tạo tập mờ bằng cách áp dụng hàm thành viên lên toàn bộ vũ trụ.
	private static FuzzySet<T> FromMembershipFunction(
		Universe<T> universe,
		Func<T, double> membershipFunction,
		string name = "A")
	{
		ArgumentNullException.ThrowIfNull(universe);
		ArgumentNullException.ThrowIfNull(membershipFunction);

		var result = new FuzzySet<T>(universe, name);

		foreach (T value in universe.GetAllElements())
		{
			result[value] = membershipFunction(value);
		}

		return result;
	}

	// Khử mờ bằng phương pháp cực đại, chọn giá trị nhỏ nhất khi đồng cực đại.
	private T DefuzzifyMaxMembership(IComparer<T>? comparer = null, double tolerance = 1e-6)
	{
		var values = Universe.GetAllElements().ToList();
		if (values.Count == 0)
		{
			throw new InvalidOperationException("Không thể khử mờ một vũ trụ rỗng.");
		}

		var maximum = values.Max(GetMembership);
		var candidates = values
			.Where(value => Math.Abs(this[value] - maximum) < tolerance)
			.ToList();

		comparer ??= Comparer<T>.Default;
		return candidates
			.Skip(1)
			.Aggregate(
				candidates[0],
				(smallest, candidate) =>
					comparer.Compare(candidate, smallest) < 0
						? candidate
						: smallest);
	}

	// Khử mờ bằng phương pháp trọng tâm trên các giá trị rời rạc.
	private double DefuzzifyCentroid(Func<T, double> valueSelector)
	{
		ArgumentNullException.ThrowIfNull(valueSelector);

		var weightedSum = Universe
			.GetAllElements()
			.Sum(value => valueSelector(value) * this[value]);
		var membershipSum = Universe
			.GetAllElements()
			.Sum(GetMembership);

		if (membershipSum == 0.0)
		{
			throw new InvalidOperationException(
				"Không thể khử mờ bằng trọng tâm khi tổng độ thuộc bằng 0.");
		}

		return weightedSum / membershipSum;
	}

	// Định dạng tập mờ thành tên và danh sách độ thuộc từng phần tử.
	public override string ToString()
	{
		var result = new System.Text.StringBuilder();
		result.AppendLine($"   {Name}:");

		foreach (T value in Universe.GetAllElements())
		{
			result.AppendLine(
				$"      μ({value}) = {this[value]:0.###}");
		}

		return result.ToString().TrimEnd();
	}
}



public partial class FuzzyRelation<T1, T2> where T1 : notnull where T2 : notnull
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

	// Indexer
	public double this[T1 x, T2 y]
	{
		get => _mu.GetValueOrDefault((x, y), 0.0);
		set
		{
			if (!Universe1.Contains(x) || !Universe2.Contains(y))
				throw new ArgumentException($"Cặp ({x}, {y}) không thuộc tập vũ trụ của quan hệ mờ.");
			if (value is < 0.0 or > 1.0)
				throw new ArgumentOutOfRangeException(nameof(value),
					"Giá trị độ thành viên phải nằm trong khoảng [0, 1].");

			_mu[(x, y)] = value;
		}
	}

	// Định dạng quan hệ mờ thành bảng dòng-cột dùng để trình bày bài giải.
	public override string ToString()
	{
		var result = new System.Text.StringBuilder();

		result.Append("   ");
		result.Append($"{"u\\v",-8}");
		foreach (T2 column in Universe2.GetAllElements())
		{
			result.Append($"{column,8}");
		}

		result.AppendLine();
		foreach (T1 row in Universe1.GetAllElements())
		{
			result.Append($"{row,8}");
			foreach (T2 column in Universe2.GetAllElements())
			{
				result.Append($"{this[row, column],8:0.###}");
			}

			result.AppendLine();
		}

		return result.ToString().TrimEnd();
	}
}



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

		foreach (T1 x in set1.Universe.GetAllElements())
			foreach (T2 y in set2.Universe.GetAllElements())
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

		foreach (T1 x in first.Universe1.GetAllElements())
			foreach (T2 y in first.Universe2.GetAllElements())
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

		foreach (T2 y in Universe2.GetAllElements())
		{
			IEnumerable<double> combinedValues = Universe1.GetAllElements()
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



public sealed record FuzzyRule<TInput, TOutput>(
	FuzzySet<TInput> Antecedent,
	FuzzySet<TOutput> Consequent,
	string Name)
	where TInput : notnull
	where TOutput : notnull;

public static class FuzzyInference
{
	public static FuzzySet<TOutput> Infer<TInput, TOutput>(
		FuzzySet<TInput> input,
		IEnumerable<FuzzyRule<TInput, TOutput>> rules,
		IFuzzyImplicationStrategy implicationStrategy,
		IFuzzyCompositionStrategy compositionStrategy)
		where TInput : notnull
		where TOutput : notnull
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(rules);
		ArgumentNullException.ThrowIfNull(implicationStrategy);
		ArgumentNullException.ThrowIfNull(compositionStrategy);

		var ruleList = rules.ToList();
		if (ruleList.Count == 0)
		{
			throw new ArgumentException("Phải có ít nhất một luật mờ.", nameof(rules));
		}

		var relations = ruleList
			.Select(rule => FuzzyRelation<TInput, TOutput>.Implication(
				rule.Antecedent,
				rule.Consequent,
				implicationStrategy,
				rule.Name))
			.ToList();

		FuzzyRelation<TInput, TOutput> combinedRelation = relations
			.Skip(1)
			.Aggregate(
				relations[0],
				(first, second) => FuzzyRelation<TInput, TOutput>.Intersect(first, second));

		return combinedRelation.Compose(input, compositionStrategy);
	}
}



public class Rule
{
	public string Name { get; init; }
	public HashSet<string> LHS { get; init; }
	public string RHS { get; init; }  // vế phải chỉ có 1 sự kiện
	public int Weight { get; init; } // Trọng số

	public Rule(string name, IEnumerable<string> lhs, string rhs, int weight)
	{
		Name = name;
		LHS = [.. lhs];
		RHS = rhs;
		Weight = weight;
	}

	// lấy phủ định của sự kiện
	public static string Negate(string fact) => fact.StartsWith("¬") ? fact[1..] : "¬" + fact;

	public override string ToString()
	{
		return $"{Name}: {string.Join(" ^ ", LHS)} -> {RHS} (w={Weight})";
	}

	public string GetNormalizedLhsKey()
	{
		return string.Join("^", LHS.OrderBy(x => x, StringComparer.Ordinal));
	}

	/// <summary>
	/// Kiểm tra xem vế trái của luật này có chứa vế trái của luật khác hay không.
	/// Nghĩa là other có là một tập con của vế trái của luật này hay không.
	/// </summary>
	public bool HasLhsContainingOtherLhs(Rule other)
	{
		return other.LHS.IsSubsetOf(LHS);       // other là subset của this
	}

	public bool IsLhsSubsetOf(Rule other)
	{
		return LHS.IsSubsetOf(other.LHS);
	}

	public bool IsConflictingWith(Rule other)
	{
		return RHS == Negate(other.RHS) && (HasLhsContainingOtherLhs(other) || other.HasLhsContainingOtherLhs(this));
	}
}

public class RuleSet
{
	public List<Rule> Rules { get; init; }

	public RuleSet(IEnumerable<Rule> rules)
	{
		Rules = [.. rules];
	}

	public void RemoveRule(Rule rule)
	{
		Rules.Remove(rule);
	}

	public List<HashSet<string>> GetAllLhsKeys()
	{
		return [.. Rules.Select(rule => rule.LHS)];
	}

	public List<string> GetAllRhs()
	{
		return [.. Rules.Select(rule => rule.RHS)];
	}

	public List<Rule> GetAllRules()
	{
		return [.. Rules];
	}


	// Lấy tất cả các cặp luật mâu thuẫn tường minh
	// Return null nếu không có cặp luật mâu thuẫn tường minh nào
	public (bool hasExplicitConflicts, List<(Rule First, Rule Second)>?) CheckExplicitConflicts()
	{
		var conflicts = new List<(Rule First, Rule Second)>();
		for (var i = 0; i < Rules.Count; i++)
		{
			for (var j = i + 1; j < Rules.Count; j++)
			{
				if (Rules[i].IsConflictingWith(Rules[j]))
				{
					conflicts.Add((Rules[i], Rules[j]));
				}
			}
		}

		if (conflicts.Count == 0)
		{
			return (false, null);
		}

		return (true, conflicts);
	}

	public List<HashSet<string>> GetMaximumLhs()
	{
		List<HashSet<string>> lhsKeys = GetAllLhsKeys();
		var maximumLhs = new List<HashSet<string>>();

		// O(n^2)
		foreach (HashSet<string> lhs in lhsKeys)
		{
			if (!lhsKeys.Any(other => other != lhs && other.IsSupersetOf(lhs)))
			{
				maximumLhs.Add(lhs);
			}
		}

		return maximumLhs;
	}

	public (List<HashSet<string>>, List<RuleSet>) PartitionByMaximumLhs()
	{
		List<HashSet<string>> maximumLhs = GetMaximumLhs();
		var partitionedRuleSets = new List<RuleSet>();

		foreach (HashSet<string> lhs in maximumLhs)
		{
			var rulesInPartition = Rules.Where(rule => rule.LHS.IsSubsetOf(lhs)).ToList();
			partitionedRuleSets.Add(new RuleSet(rulesInPartition));
		}

		return (maximumLhs, partitionedRuleSets);
	}

	// simple_r version, loop through all rules instead of using a queue
	public static ClosureResult GetClosure(HashSet<string> initialFacts, RuleSet ruleSet)
	{
		var closure = new ClosureResult(initialFacts);
		var addedNewFact = true;

		while (addedNewFact)
		{
			addedNewFact = false;
			foreach (Rule rule in ruleSet.Rules)
			{
				if (rule.LHS.IsSubsetOf(closure.Facts) && closure.AddFact(rule.RHS, rule))
				{
					addedNewFact = true;
				}
			}
		}

		return closure;
	}


	public List<RedundantRuleResult> RemoveRedundantRules()
	{
		var redundantRules = new List<RedundantRuleResult>();
		var index = 0;

		while (index < Rules.Count)
		{
			Rule rule = Rules[index];
			var tempRuleSet = new RuleSet(
				Rules.Where(candidate => !ReferenceEquals(candidate, rule)));

			var closureWithoutRule = GetClosure(rule.LHS, tempRuleSet);

			if (closureWithoutRule.ContainsFact(rule.RHS))
			{
				redundantRules.Add(new RedundantRuleResult(rule, closureWithoutRule));
				Rules.RemoveAt(index);
				continue;
			}

			index++;
		}

		return redundantRules;
	}

	public List<RedundantFactResult> RemoveRedundantFacts()
	{
		var redundantFacts = new List<RedundantFactResult>();

		foreach (Rule rule in Rules)
		{
			var factsToCheck = rule.LHS.ToList();
			foreach (string fact in factsToCheck)
			{
				if (rule.LHS.Count <= 1)
					break;

				var reducedLhs = new HashSet<string>(rule.LHS);
				reducedLhs.Remove(fact);

				var tempRuleSet = new RuleSet(
					Rules.Where(candidate => !ReferenceEquals(candidate, rule)));
				var closureWithoutFact = GetClosure(reducedLhs, tempRuleSet);

				if (!closureWithoutFact.ContainsFact(fact))
					continue;

				rule.LHS.Remove(fact);
				redundantFacts.Add(
					new RedundantFactResult(rule, fact, closureWithoutFact));
			}
		}

		return redundantFacts;
	}

	public override string ToString()
	{
		return string.Join(Environment.NewLine, Rules.Select(r => r.ToString()));
	}

	public RuleSet Clone()
	{
		var clonedRules = Rules.Select(r => new Rule(r.Name, r.LHS, r.RHS, r.Weight)).ToList();
		return new RuleSet(clonedRules);
	}
}

public sealed class RedundantRuleResult
{
	public Rule Rule { get; }
	public ClosureResult Closure { get; }

	public RedundantRuleResult(Rule rule, ClosureResult closure)
	{
		Rule = rule;
		Closure = closure;
	}
}

public sealed class RedundantFactResult
{
	public Rule Rule { get; }
	public string Fact { get; }
	public ClosureResult Closure { get; }

	public RedundantFactResult(Rule rule, string fact, ClosureResult closure)
	{
		Rule = rule;
		Fact = fact;
		Closure = closure;
	}
}

public class ClosureResult
{
	public HashSet<string> Facts { get; }
	public Dictionary<string, Rule> Provenance { get; }

	public ClosureResult(IEnumerable<string> initialFacts)
	{
		Facts = [.. initialFacts];
		Provenance = new Dictionary<string, Rule>();
	}

	public bool AddFact(string fact, Rule rule)
	{
		if (!Facts.Add(fact))
			return false;

		Provenance[fact] = rule;
		return true;
	}

	public bool ContainsFact(string fact)
	{
		return Facts.Contains(fact);
	}

	public bool TryGetProvenance(string fact, out Rule? rule)
	{
		return Provenance.TryGetValue(fact, out rule);
	}

	public IReadOnlyList<Rule> GetDerivationRuleChain(string fact)
	{
		var chain = new List<Rule>();
		var visitedFacts = new HashSet<string>();

		CollectDerivationRules(fact, chain, visitedFacts);
		return chain;
	}

	private void CollectDerivationRules(
		string fact,
		List<Rule> chain,
		HashSet<string> visitedFacts)
	{
		if (!visitedFacts.Add(fact) ||
			!Provenance.TryGetValue(fact, out Rule? rule))
		{
			return;
		}

		foreach (string prerequisite in rule.LHS)
		{
			CollectDerivationRules(prerequisite, chain, visitedFacts);
		}

		if (!chain.Contains(rule))
		{
			chain.Add(rule);
		}
	}

	public (bool hasConflicts, HashSet<(string f1, string f2)>?) CheckConflicts()
	{
		var conflicts = new HashSet<(string f1, string f2)>();
		foreach (var fact in Facts)
		{
			var negatedFact = Rule.Negate(fact);

			if (!Facts.Contains(negatedFact)) continue;

			(string, string) pair = string.CompareOrdinal(fact, negatedFact) < 0 ? (fact, negatedFact) : (negatedFact, fact);
			conflicts.Add(pair);
		}

		if (conflicts.Count == 0)
		{
			return (false, null);
		}

		return (true, conflicts);
	}

	public HashSet<Rule> GetRulesToRemoveForConflicts()
	{
		var rulesToRemove = new HashSet<Rule>();
		(bool _, HashSet<(string f1, string f2)>? conflicts) = CheckConflicts();

		if (conflicts is null)
			return rulesToRemove;

		foreach ((string? fact, string? negatedFact) in conflicts)
		{
			if (!TryGetProvenance(fact, out Rule? factRule) ||
				!TryGetProvenance(negatedFact, out Rule? negatedFactRule) ||
				factRule is null ||
				negatedFactRule is null)
			{
				continue;
			}

			rulesToRemove.Add(
				factRule.Weight <= negatedFactRule.Weight
					? factRule
					: negatedFactRule);
		}

		return rulesToRemove;
	}
}



public class InferenceEngine
{

	public static void ForwardChaining(RuleSet ruleSet, HashSet<string> initialFacts, string goal, IThoaStategy thoa)
	{
		var tg = new HashSet<string>(initialFacts);
		var R = new RuleSet(ruleSet.Rules);
		var Vet = new List<Rule>();
		var states = new List<ForwardChainingState>();

		thoa.UpdateThoa(tg, R);
		states.Add(new ForwardChainingState(
			null,
			thoa.GetPendingRules(),
			[.. tg],
			[.. R.Rules],
			[.. Vet]));

		while (!tg.Contains(goal) && thoa.HasNextRule())
		{
			Rule? r = thoa.NextRule();

			if (r == null)
				break;

			tg.Add(r.RHS);
			Vet.Add(r);
			R.RemoveRule(r);
			thoa.UpdateThoa(tg, R);
			states.Add(new ForwardChainingState(
				r,
				thoa.GetPendingRules(),
				[.. tg],
				[.. R.Rules],
				[.. Vet]));
		}

		PrintTable(states);

		Console.WriteLine(
			tg.Contains(goal)
				? $"Kết luận: đạt mục tiêu {goal}.\n VET là {FormatRuleNames(Vet)}"
				: $"Không đạt được mục tiêu {goal}.");
	}



	public static (RuleSet rGt, RuleSet rKl, Dictionary<Rule, int> hDict) Rpg(RuleSet ruleSet, HashSet<string> gt, string k)
	{
		var predecessors = ruleSet.Rules.ToDictionary(
		rule => rule,
		_ => new List<Rule>());

		foreach (var rule in ruleSet.Rules)
		{
			foreach (var otherRule in ruleSet.Rules)
			{
				if (rule != otherRule && otherRule.LHS.Contains(rule.RHS))
				{
					predecessors[otherRule].Add(rule);
				}
			}
		}

		RuleSet rGt = new RuleSet(ruleSet.Rules.Where(rule => rule.LHS.IsSubsetOf(gt)).ToList());
		RuleSet rKl = new RuleSet(ruleSet.Rules.Where(rule => rule.RHS == k).ToList());

		var hDict = ruleSet.Rules.ToDictionary(rule => rule, _ => int.MaxValue);

		var queue = new Queue<Rule>();

		foreach (var rule in rKl.Rules)
		{
			hDict[rule] = 0;
			queue.Enqueue(rule);
		}

		while (queue.Count > 0)
		{
			var currentRule = queue.Dequeue();
			foreach (var pred in predecessors[currentRule])
			{
				if (hDict[pred] != int.MaxValue)
				{
					continue;
				}

				hDict[pred] = hDict[currentRule] + 1;
				queue.Enqueue(pred);
			}
		}


		// print predecessors
		foreach (var kvp in predecessors)
		{
			var rule = kvp.Key;
			var preds = kvp.Value;
			if (preds.Count > 0)
			{
				foreach (var pred in preds)
				{
					Console.WriteLine($"{pred.Name} -> {rule.Name}");
				}
			}
		}


		return (rGt, rKl, hDict);
	}

	public interface IThoaStategy
	{
		Rule? NextRule();
		bool HasNextRule();
		void UpdateThoa(HashSet<string> tg, RuleSet R);
		IReadOnlyList<Rule> GetPendingRules();
	}

	public class QueueThoaStrategy : IThoaStategy
	{
		private readonly Queue<Rule> _rules = new();

		public bool HasNextRule()
		{
			return _rules.Count > 0;
		}

		public Rule? NextRule()
		{
			return _rules.Count > 0 ? _rules.Dequeue() : null;
		}

		public void UpdateThoa(HashSet<string> tg, RuleSet R)
		{
			var newRules = new List<Rule>();

			foreach (var rule in R.Rules)
			{
				if (rule.LHS.IsSubsetOf(tg) && !_rules.Contains(rule))
				{
					newRules.Add(rule);
				}
			}
			foreach (var rule in newRules)
			{
				_rules.Enqueue(rule);
			}
		}

		public IReadOnlyList<Rule> GetPendingRules()
		{
			return [.. _rules];
		}
	}

	public class RpgThoaStrategy : IThoaStategy
	{
		private readonly Dictionary<Rule, int> _h;
		private readonly PriorityQueue<Rule, int> _rules = new();

		public RpgThoaStrategy(Dictionary<Rule, int> h)
		{
			_h = h;
		}

		public Rule? NextRule()
		{
			return _rules.Count > 0 ? _rules.Dequeue() : null;
		}

		public bool HasNextRule()
		{
			return _rules.Count > 0;
		}

		public void UpdateThoa(HashSet<string> tg, RuleSet R)
		{
			foreach (var rule in R.Rules)
			{
				if (rule.LHS.IsSubsetOf(tg) && !GetPendingRules().Contains(rule))
				{
					_rules.Enqueue(rule, GetPriority(rule));
				}
			}
		}

		public IReadOnlyList<Rule> GetPendingRules()
		{
			return [.. _rules.UnorderedItems.Select(r => r.Element)];
		}

		private int GetPriority(Rule rule)
		{
			return _h.TryGetValue(rule, out int h)
				? h
				: int.MaxValue;
		}
	}



	// ========================== HELPERS =========================
	private static void PrintTable(List<ForwardChainingState> states)
	{
		int selectedWidth = Math.Max(1, states.Max(state => (state.SelectedRule?.Name ?? "-").Length));
		int thoaWidth = GetSetColumnWidth(
			states.Select(state => state.Thoa.Select(rule => rule.Name)),
			"THOA");
		int tgWidth = GetSetColumnWidth(
			states.Select(state => state.Tg.Select(fact => fact)),
			"TG");
		int ruleWidth = GetSetColumnWidth(
			states.Select(state => state.RemainingRules.Select(rule => rule.Name)),
			"R");
		int vetWidth = GetSetColumnWidth(
			states.Select(state => state.Vet.Select(rule => rule.Name)),
			"VET");

		Console.WriteLine(
			$"| {"r".PadRight(selectedWidth)} " +
			$"| {"THOA".PadRight(thoaWidth)} " +
			$"| {"TG".PadRight(tgWidth)} " +
			$"| {"R".PadRight(ruleWidth)} " +
			$"| {"VET".PadRight(vetWidth)} |");

		Console.WriteLine(
			$"|-{new string('-', selectedWidth)}-" +
			$"|-{new string('-', thoaWidth)}-" +
			$"|-{new string('-', tgWidth)}-" +
			$"|-{new string('-', ruleWidth)}-" +
			$"|-{new string('-', vetWidth)}-|");

		foreach (ForwardChainingState state in states)
		{
			Console.WriteLine(
				$"| {(state.SelectedRule?.Name ?? "-").PadRight(selectedWidth)} " +
				$"| {FormatRuleNames(state.Thoa).PadRight(thoaWidth)} " +
				$"| {FormatFacts(state.Tg).PadRight(tgWidth)} " +
				$"| {FormatRuleNames(state.RemainingRules).PadRight(ruleWidth)} " +
				$"| {FormatRuleNames(state.Vet).PadRight(vetWidth)} |");
		}
	}

	private static int GetSetColumnWidth(
		IEnumerable<IEnumerable<string>> rows,
		string header)
	{
		var itemGroups = rows.Select(row => row.ToList()).ToList();
		int maxFactCount = itemGroups.Max(group => group.Count);
		int maxItemLength = itemGroups
			.SelectMany(group => group)
			.DefaultIfEmpty("")
			.Max(item => item.Length);

		int widthByFactCount =
			maxFactCount == 0
				? 2
				: 2 + (maxFactCount * maxItemLength) + (Math.Max(0, maxFactCount - 1) * 2);

		return Math.Max(header.Length, widthByFactCount);
	}

	private static string FormatFacts(IEnumerable<string> facts)
	{
		return $"{{{string.Join(", ", facts)}}}";
	}

	private static string FormatRuleNames(IEnumerable<Rule> rules)
	{
		return $"{{{string.Join(", ", rules.Select(rule => rule.Name))}}}";
	}

	private sealed class ForwardChainingState
	{
		public Rule? SelectedRule { get; }
		public IReadOnlyList<Rule> Thoa { get; }
		public IReadOnlyList<string> Tg { get; }
		public IReadOnlyList<Rule> RemainingRules { get; }
		public IReadOnlyList<Rule> Vet { get; }

		public ForwardChainingState(
			Rule? selectedRule,
			IReadOnlyList<Rule> thoa,
			IReadOnlyList<string> tg,
			IReadOnlyList<Rule> remainingRules,
			IReadOnlyList<Rule> vet)
		{
			SelectedRule = selectedRule;
			Thoa = thoa;
			Tg = tg;
			RemainingRules = remainingRules;
			Vet = vet;
		}
	}


}

public static class EntryPoint
{
	public static void Main()
	{
		/* Bài 1  ============================================================================*/
		/* a)     ============================================================================*/

		var temperatureUniverse = new Universe<int>([20, 25, 30, 35]);
		var fanSpeedUniverse = new Universe<int>([400, 800, 1200]);

		Console.WriteLine("============================================================");
		Console.WriteLine("BÀI 1 - LẬP LUẬN MỜ ĐƠN ĐIỀU KIỆN");
		Console.WriteLine("============================================================");
		Console.WriteLine("a)");
		Console.WriteLine("Luật R1: Nếu X là A1 (nhiệt độ thấp) thì Y là B1 (quay chậm).");
		Console.WriteLine("Luật R2: Nếu X là A2 (nhiệt độ cao) thì Y là B2 (quay nhanh).");
		Console.WriteLine("Phép kéo theo: Lukasiewicz.");
		Console.WriteLine("Phép tổng hợp quan hệ: min.");
		Console.WriteLine("Phép hợp thành: max-min.");
		Console.WriteLine("Giá trị đầu vào rõ: X = 25°C.");
		Console.WriteLine();

		var a1 = new FuzzySet<int>(
			temperatureUniverse,
			new Dictionary<int, double>
			{
				[20] = 1.0,
				[25] = 0.7,
				[30] = 0.3,
				[35] = 0.0
			},
			"A1 - Nhiệt độ thấp");

		var a2 = new FuzzySet<int>(
			temperatureUniverse,
			new Dictionary<int, double>
			{
				[20] = 0.0,
				[25] = 0.3,
				[30] = 0.6,
				[35] = 1.0
			},
			"A2 - Nhiệt độ cao");

		var b1 = new FuzzySet<int>(
			fanSpeedUniverse,
			new Dictionary<int, double>
			{
				[400] = 1.0,
				[800] = 0.5,
				[1200] = 0.1
			},
			"B1 - Quay chậm");

		var b2 = new FuzzySet<int>(
			fanSpeedUniverse,
			new Dictionary<int, double>
			{
				[400] = 0.2,
				[800] = 0.6,
				[1200] = 1.0
			},
			"B2 - Quay nhanh");

		var implication = new LukasiewiczImplication();
		var composition = new MaxMinComposition();

		var r1 = FuzzyRelation<int, int>.Implication(a1, b1, implication, "R1");
		var r2 = FuzzyRelation<int, int>.Implication(a2, b2, implication, "R2");
		var r = FuzzyRelation<int, int>.Intersect(r1, r2, "R");

		Console.WriteLine("1. Lập ma trận quan hệ mờ R1:");
		Console.WriteLine(r1);
		Console.WriteLine("2. Lập ma trận quan hệ mờ R2:");
		Console.WriteLine(r2);
		Console.WriteLine("3. Tổng hợp hai quan hệ bằng phép giao min:");
		Console.WriteLine("   R(u,v) = min(R1(u,v), R2(u,v))");
		Console.WriteLine(r);

		FuzzySet<int> input = FuzzySet<int>.Fuzzify.Singleton(temperatureUniverse, 25, "X = 25");
		Console.WriteLine("4. Mờ hóa đơn trị X = 25:");
		Console.WriteLine($"   A'25 = {input.ToMembershipExpression()}");
		Console.WriteLine(input);

		Console.WriteLine("5. Hợp thành max-min:");
		Console.WriteLine("   B' = A'25 o R");
		Console.WriteLine("   B'(v) = max_u min(A'25(u), R(u,v))");
		FuzzySet<int> output = r.Compose(input, composition);
		Console.WriteLine(output);

		var crispOutput = FuzzySet<int>.Defuzzify.MaxMembership(output);
		var maximum = output.Universe.GetAllElements().Max(output.GetMembership);
		Console.WriteLine($"6. Độ thuộc cực đại:");
		Console.WriteLine(
			$"   max B'(v) = max{{{string.Join(", ", output.Universe.GetAllElements().Select(v => output[v].ToString("0.###")))}}} = {maximum:0.###}");
		Console.WriteLine($"   Giá trị cực đại đạt tại y = {crispOutput} vòng/phút.");
		Console.WriteLine();
		Console.WriteLine("KẾT LUẬN:");
		Console.WriteLine($"   B' = {output.ToMembershipExpression()}");
		Console.WriteLine($"   Giá trị điều khiển đầu ra sau khi khử mờ: y* = {crispOutput} vòng/phút.");
		Console.WriteLine();

		Console.WriteLine("============================================================");
		Console.WriteLine("b)");
		Console.WriteLine("Phép kéo theo: Dienes-Rescher.");
		Console.WriteLine("Phép tổng hợp quan hệ: min.");
		Console.WriteLine("Phép hợp thành: max-product.");
		Console.WriteLine("Phương pháp khử mờ: trọng tâm.");
		Console.WriteLine();

		var inputB = new FuzzySet<int>(
			temperatureUniverse,
			new Dictionary<int, double>
			{
				[20] = 0.1,
				[25] = 0.4,
				[30] = 0.8,
				[35] = 0.6
			},
			"A' - Câu b");

		Console.WriteLine($"Dữ kiện: A' = {inputB.ToMembershipExpression()}.");
		var implicationB = new DienesRescherImplication();
		var compositionB = new MaxProductComposition();
		var r1B = FuzzyRelation<int, int>.Implication(a1, b1, implicationB, "R1");
		var r2B = FuzzyRelation<int, int>.Implication(a2, b2, implicationB, "R2");
		var rB = FuzzyRelation<int, int>.Intersect(r1B, r2B, "R");

		Console.WriteLine("1. Mờ hóa đầu vào:");
		Console.WriteLine($"   A' = {inputB.ToMembershipExpression()}");
		Console.WriteLine(inputB);

		Console.WriteLine("2. Ma trận quan hệ R1 theo Dienes-Rescher:");
		Console.WriteLine(r1B);
		Console.WriteLine("3. Ma trận quan hệ R2 theo Dienes-Rescher:");
		Console.WriteLine(r2B);
		Console.WriteLine("4. Tổng hợp hai quan hệ bằng phép giao min:");
		Console.WriteLine("   R(u,v) = min(R1(u,v), R2(u,v))");
		Console.WriteLine(rB);

		Console.WriteLine("5. Hợp thành max-product:");
		Console.WriteLine("   B'(v) = max_u [A'(u) * R(u,v)]");
		FuzzySet<int> outputB = rB.Compose(inputB, compositionB);
		Console.WriteLine(outputB);

		var crispOutputB = FuzzySet<int>.Defuzzify.Centroid(outputB, y => y);
		var weightedSumB = fanSpeedUniverse
			.GetAllElements()
			.Sum(y => y * outputB[y]);
		var membershipSumB = fanSpeedUniverse
			.GetAllElements()
			.Sum(outputB.GetMembership);

		Console.WriteLine("6. Khử mờ bằng phương pháp trọng tâm:");
		Console.WriteLine($"   y* = Σ(y * B'(y)) / ΣB'(y)");
		Console.WriteLine($"      = {weightedSumB:0.###} / {membershipSumB:0.###}");
		Console.WriteLine($"      = {crispOutputB:0.###} vòng/phút.");
		Console.WriteLine($"   B' = {outputB.ToMembershipExpression()}");
		Console.WriteLine();

		/* Bài 2  ============================================================================*/
		/* a) Kiểm tra mâu thuẫn tường minh =================================================*/

		var originalRuleSet = new RuleSet(
		[
			new Rule("r1", ["a"], "b", 5),
			new Rule("r2", ["b"], "d", 4),
			new Rule("r3", ["d"], "h", 7),
			new Rule("r4", ["c"], "e", 6),
			new Rule("r5", ["b", "e"], Rule.Negate("h"), 2),
			new Rule("r6", ["a", "c"], "k", 8),
			new Rule("r7", ["a"], Rule.Negate("k"), 3),
			new Rule("r8", ["a"], "d", 1),
			new Rule("r9", ["b", "d"], "m", 4)
		]);

		Console.WriteLine("============================================================");
		Console.WriteLine("BÀI 2 - KIỂM TRA VÀ XỬ LÝ MÂU THUẪN");
		Console.WriteLine("============================================================");
		Console.WriteLine("a) Tìm các mâu thuẫn tường minh trong R");
		Console.WriteLine();

		Console.WriteLine("Tập luật R:");
		Console.WriteLine(originalRuleSet);
		Console.WriteLine();

		(bool hasExplicitConflicts, List<(Rule First, Rule Second)>? explicitConflicts) =
			originalRuleSet.CheckExplicitConflicts();

		if (!hasExplicitConflicts)
		{
			Console.WriteLine("Không tìm thấy cặp luật mâu thuẫn tường minh nào trong R.");
		}
		else
		{

			Console.WriteLine("Danh sách các cặp luật mâu thuẫn tường minh:");
			if (explicitConflicts!.Count == 0)
			{
				Console.WriteLine("   Không tìm thấy cặp luật mâu thuẫn tường minh.");
			}
			else
			{
				foreach ((Rule? first, Rule? second) in explicitConflicts)
				{
					Console.WriteLine(
						$"   ({first.Name}, {second.Name})");
					Console.WriteLine(
						$"      {first}");
					Console.WriteLine(
						$"      {second}");
					Console.WriteLine(
						$"      Điều kiện: vế phải {first.RHS} và {second.RHS} là hai sự kiện phủ định nhau;");
					Console.WriteLine(
						"      điều kiện vế trái của một luật nằm trong hoặc bằng điều kiện vế trái của luật còn lại.");
					Console.WriteLine();
				}
			}
		}


		// R'
		var remainingRuleSet = new RuleSet(originalRuleSet.Rules.Select(rule => rule));

		if (hasExplicitConflicts)
		{
			foreach ((Rule? first, Rule? second) in explicitConflicts!)
			{
				Rule ruleWithLowerWeight = first.Weight <= second.Weight ? first : second;

				Console.WriteLine(
					$"Xử lý cặp ({first.Name}, {second.Name}):");
				Console.WriteLine(
					$"   Trọng số {first.Name} = {first.Weight}; trọng số {second.Name} = {second.Weight}.");
				Console.WriteLine(
					$"   Loại {ruleWithLowerWeight.Name} vì có trọng số nhỏ hơn:");

				Console.WriteLine();

				remainingRuleSet.RemoveRule(ruleWithLowerWeight);
			}
		}

		Console.WriteLine();
		Console.WriteLine("Tập luật còn lại R':");
		Console.WriteLine(remainingRuleSet);
		Console.WriteLine();

		// b) Tập vế trái tối đại.
		Console.WriteLine("============================================================");
		Console.WriteLine("b) ");
		(List<HashSet<string>>? maximumLhs, List<RuleSet>? partitionedRuleSets) =
			remainingRuleSet.PartitionByMaximumLhs();

		Console.WriteLine("Các tập luật con được phân hoạch theo vế trái tối đại:");
		for (var i = 1; i <= maximumLhs.Count; i++)
		{
			Console.WriteLine(
				$"R{i} = {{{string.Join(", ", partitionedRuleSets[i - 1].Rules.Select(rule => rule.Name))}}} -> F{i} = {{{string.Join(", ", maximumLhs[i - 1])}}}");
		}

		var closures = maximumLhs
			.Select(lhs => RuleSet.GetClosure(lhs, remainingRuleSet))
			.ToList();
		Console.WriteLine();
		Console.WriteLine("Các bao đóng tương ứng:");
		for (var i = 1; i <= closures.Count; i++)
		{
			Console.WriteLine($"Bao đóng F{i}⁺ = {{{string.Join(", ", closures[i - 1].Facts)}}}");
		}

		// Kiểm tra mâu thuẫn không tường minh.
		bool isImplicitConflict = false;

		foreach (ClosureResult closure in closures)
		{
			(bool hasConflicts, HashSet<(string f1, string f2)>? conflictsInClosure) = closure.CheckConflicts();

			if (hasConflicts && conflictsInClosure is not null)
			{
				isImplicitConflict = true;
				Console.WriteLine();
				Console.WriteLine(
					$"=> Có mâu thuẫn không tường minh trong R' với bao đóng F⁺ = {{{string.Join(", ", closure.Facts)}}}:");
				foreach ((string? fact, string? negatedFact) in conflictsInClosure)
				{
					Console.WriteLine($"   Cặp mâu thuẫn: ({fact}, {negatedFact})");
				}
			}
			else
			{
				Console.WriteLine();
				Console.WriteLine(
					$"=> Không có mâu thuẫn không tường minh trong R' với bao đóng F⁺ = {{{string.Join(", ", closure.Facts)}}}.");
			}

		}

		if (!isImplicitConflict)
		{
			Console.WriteLine();
			Console.WriteLine("=> Không có mâu thuẫn không tường minh trong R'.");
		}
		else
		{
			Console.WriteLine();
			Console.WriteLine("=> Có mâu thuẫn không tường minh trong R'.");
		}

		// c) Xử lý mâu thuẫn không tường minh.
		Console.WriteLine();
		Console.WriteLine("============================================================");
		Console.WriteLine("c) Xử lý mâu thuẫn không tường minh");
		Console.WriteLine("============================================================");

		var finalRuleSet = new RuleSet(
			remainingRuleSet.Rules.Select(rule => rule));

		if (isImplicitConflict)
		{
			var rulesToRemove = closures
				.SelectMany(closure => closure.GetRulesToRemoveForConflicts())
				.ToHashSet();

			foreach (Rule ruleToRemove in rulesToRemove)
			{
				Console.WriteLine();
				Console.WriteLine(
					$"   Loại {ruleToRemove.Name} vì có trọng số nhỏ hơn.");

				finalRuleSet.RemoveRule(ruleToRemove);
			}

			Console.WriteLine();
			Console.WriteLine("Tập luật sau xử lý R'':");
			Console.WriteLine(finalRuleSet);
		}
		else
		{
			Console.WriteLine("Không có mâu thuẫn không tường minh trong R', không cần xử lý.");
		}


		// d) Xử lý luật và sự kiện dư thừa
		Console.WriteLine();
		Console.WriteLine("============================================================");
		Console.WriteLine("d) Xử lý luật và sự kiện dư thừa");
		Console.WriteLine("============================================================");
		Console.WriteLine("Tập luật đang xét R'':");
		Console.WriteLine(finalRuleSet);
		Console.WriteLine();
		var redundantRules = finalRuleSet.RemoveRedundantRules();
		if (redundantRules.Count == 0)
		{
			Console.WriteLine("Không có luật dư thừa trong R''.");
		}
		else
		{
			Console.WriteLine("Các luật dư thừa trong R'':");
			foreach (RedundantRuleResult redundantRule in redundantRules)
			{
				var derivationChain = redundantRule.Closure
					.GetDerivationRuleChain(redundantRule.Rule.RHS);

				Console.WriteLine($"   {redundantRule.Rule}");
				Console.WriteLine(
					$"      Vì {redundantRule.Rule.RHS} đã được suy ra từ: " +
					string.Join(" -> ", derivationChain.Select(rule => rule.Name)));
			}

			Console.WriteLine();
			Console.WriteLine("Tập luật sau khi loại bỏ các luật dư thừa:");
			Console.WriteLine(finalRuleSet);
		}

		Console.WriteLine();
		var redundantFacts = finalRuleSet.RemoveRedundantFacts();
		if (redundantFacts.Count == 0)
		{
			Console.WriteLine("Không có sự kiện dư thừa trong vế trái của các luật.");
		}
		else
		{
			Console.WriteLine("Các sự kiện dư thừa trong vế trái:");
			foreach (RedundantFactResult redundantFact in redundantFacts)
			{
				var derivationChain = redundantFact.Closure
					.GetDerivationRuleChain(redundantFact.Fact);

				Console.WriteLine(
					$"   {redundantFact.Rule.Name}: sự kiện {redundantFact.Fact} " +
					"là dư thừa trong vế trái.");
				Console.WriteLine(
					$"      Vì {redundantFact.Fact} được suy ra từ: " +
					string.Join(" -> ", derivationChain.Select(rule => rule.Name)));
			}

			Console.WriteLine();
			Console.WriteLine("Tập luật sau khi loại bỏ các sự kiện dư thừa:");
			Console.WriteLine(finalRuleSet);
		}

		Console.WriteLine();
		Console.WriteLine("============================================================");
		Console.WriteLine("e) Suy diễn tiến bằng hàng đợi");
		Console.WriteLine("============================================================");

		var forwardRuleSet = new RuleSet(
		[
			new Rule("r1", ["a", "b"], "d", 0),
			new Rule("r2", ["a"], "c", 0),
			new Rule("r3", ["b"], "e", 0),
			new Rule("r4", ["c", "d"], "f", 0),
			new Rule("r5", ["e", "f"], "g", 0),
			new Rule("r6", ["c"], "h", 0),
			new Rule("r7", ["e", "h"], "k", 0),
			new Rule("r8", ["g", "k"], "m", 0)
		]);

		Console.WriteLine();
		InferenceEngine.ForwardChaining(
			forwardRuleSet,
			new HashSet<string>(["a", "b"]),
			"g",
			new InferenceEngine.QueueThoaStrategy());



		var (rGt, rKl, hDict) = InferenceEngine.Rpg(
			forwardRuleSet,
			new HashSet<string>(["a", "b"]),
			"g");

		Console.WriteLine($"R(GT): {{{string.Join(", ", rGt.Rules.Select(x => x.Name))}}}");
		Console.WriteLine($"R(KL): {{{string.Join(", ", rKl.Rules.Select(x => x.Name))}}}");

		Console.WriteLine("h values:");
		foreach (var kvp in hDict)
		{
			var rule = kvp.Key;
			var hValue = kvp.Value;
			Console.WriteLine($"{rule.Name}: {(hValue == int.MaxValue ? "∞" : hValue.ToString())}");
		}


		InferenceEngine.ForwardChaining(
			forwardRuleSet,
			new HashSet<string>(["a", "b"]),
			"g",
			new InferenceEngine.RpgThoaStrategy(hDict));
	}
}
