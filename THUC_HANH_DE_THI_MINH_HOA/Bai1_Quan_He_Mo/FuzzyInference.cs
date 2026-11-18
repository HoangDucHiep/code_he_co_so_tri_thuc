namespace THUC_HANH_DE_THI_MINH_HOA.Bai1_Quan_He_Mo;

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

		var combinedRelation = relations
			.Skip(1)
			.Aggregate(
				relations[0],
				(first, second) => FuzzyRelation<TInput, TOutput>.Intersect(first, second));

		return combinedRelation.Compose(input, compositionStrategy);
	}
}
