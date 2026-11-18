namespace THUC_HANH_DE_THI_MINH_HOA.Bai1_Quan_He_Mo;

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

