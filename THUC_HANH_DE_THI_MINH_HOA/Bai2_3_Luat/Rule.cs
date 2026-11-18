namespace THUC_HANH_DE_THI_MINH_HOA.Bai2_3_Luat;

public class Rule
{
	public string Name { get; init; }
	public HashSet<string> LHS { get; init; }
	public string RHS { get; init; }  // vế phải chỉ có 1 sự kiện
	public int Weight { get; init; } // Trọng số

	public Rule(string name, IEnumerable<string> lhs, string rhs, int weight)
	{
		Name = name;
		LHS = new HashSet<string>(lhs);
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
	public bool HasLhsContaining(Rule other)
	{
		return other.LHS.IsSubsetOf(LHS);       // other là subset của this
	}

	public bool IsLhsSubsetOf(Rule other)
	{
		return LHS.IsSubsetOf(other.LHS);
	}

	public bool IsConflictingWith(Rule other)
	{
		return RHS == Negate(other.RHS) && (HasLhsContaining(other) || other.HasLhsContaining(this));
	}
}

public class RuleSet
{
	public List<Rule> Rules { get; init; }

	public RuleSet(IEnumerable<Rule> rules)
	{
		Rules = new List<Rule>(rules);
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
		var lhsKeys = GetAllLhsKeys();
		var maximumLhs = new List<HashSet<string>>();

		// O(n^2)
		foreach (var lhs in lhsKeys)
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
		var maximumLhs = GetMaximumLhs();
		var partitionedRuleSets = new List<RuleSet>();

		foreach (var lhs in maximumLhs)
		{
			var rulesInPartition = Rules.Where(rule => rule.LHS.IsSubsetOf(lhs)).ToList();
			partitionedRuleSets.Add(new RuleSet(rulesInPartition));
		}

		return (maximumLhs, partitionedRuleSets);
	}

	public HashSet<string> GetClosureOfFacts(HashSet<string> initialFacts)
	{
		var closure = new HashSet<string>(initialFacts);

		foreach (var rule in Rules)
		{
			if (rule.LHS.IsSubsetOf(closure))
			{
				closure.Add(rule.RHS);
			}
		}

		return closure;
	}

	public override string ToString()
	{
		return string.Join(Environment.NewLine, Rules.Select(r => r.ToString()));
	}
}


public class InferenceResult
{
	public bool Success { get; set; }                                // Có đạt được mục tiêu không
	public HashSet<string> DerivedFacts { get; set; }                // Toàn bộ sự kiện suy ra được (Bao đóng)
	public List<Rule> FiredRules { get; set; }                       // Danh sách các luật đã kích hoạt theo thứ tự
	public Dictionary<string, Rule> FactProvenance { get; set; }     // Vết: sự kiện nào do luật nào trực tiếp sinh ra
}

public class InferenceEngine
{
	public static InferenceResult ForwardChaining(RuleSet ruleSet, HashSet<string> initialFacts, string goal,
		bool useWeightPriority = false)
	{
		throw new NotImplementedException("Chưa triển khai thuật toán suy diễn tiến tới (forward chaining).");
	}
}