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



	public static class ThoaStrategy
	{
		public static QueueThoaStrategy QUEUE = new();

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

}