namespace THUC_HANH_DE_THI_MINH_HOA.Bai1_Quan_He_Mo;


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
		foreach (var kv in memberships)
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

		foreach (var value in universe.GetAllElements())
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

		foreach (var value in universe.GetAllElements())
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

		foreach (var value in Universe.GetAllElements())
		{
			result.AppendLine(
				$"      μ({value}) = {this[value]:0.###}");
		}

		return result.ToString().TrimEnd();
	}
}