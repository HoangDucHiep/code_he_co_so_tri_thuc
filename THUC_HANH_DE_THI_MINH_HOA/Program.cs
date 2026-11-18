using THUC_HANH_DE_THI_MINH_HOA.Bai1_Quan_He_Mo;
using THUC_HANH_DE_THI_MINH_HOA.Bai2_3_Luat;

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
Console.WriteLine($"   max B'(v) = max{{{string.Join(", ", output.Universe.GetAllElements().Select(v => output[v].ToString("0.###")))}}} = {maximum:0.###}");
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

(bool hasExplicitConflicts, List<(Rule First, Rule Second)>? explicitConflicts) = originalRuleSet.CheckExplicitConflicts();

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
(List<HashSet<string>>? maximumLhs, List<RuleSet>? partitionedRuleSets) = remainingRuleSet.PartitionByMaximumLhs();

Console.WriteLine("Các tập luật con được phân hoạch theo vế trái tối đại:");
for (var i = 1; i <= maximumLhs.Count; i++)
{
	Console.WriteLine($"R{i} = {{{string.Join(", ", partitionedRuleSets[i - 1].Rules.Select(rule => rule.Name))}}} -> F{i} = {{{string.Join(", ", maximumLhs[i - 1])}}}");
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
		Console.WriteLine($"=> Có mâu thuẫn không tường minh trong R' với bao đóng F⁺ = {{{string.Join(", ", closure.Facts)}}}:");
		foreach ((string? fact, string? negatedFact) in conflictsInClosure)
		{
			Console.WriteLine($"   Cặp mâu thuẫn: ({fact}, {negatedFact})");
		}
	}
	else
	{
		Console.WriteLine();
		Console.WriteLine($"=> Không có mâu thuẫn không tường minh trong R' với bao đóng F⁺ = {{{string.Join(", ", closure.Facts)}}}.");
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

InferenceEngine.ForwardChaining(
	forwardRuleSet,
	new HashSet<string>(["a", "b"]),
	"g",
	new InferenceEngine.QueueThoaStrategy());