namespace THUC_HANH_DE_THI_MINH_HOA.Bai1_Quan_He_Mo;

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