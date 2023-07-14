namespace RedOrb;

public interface ICascadeReadRule
{
	bool DoCascade(Type from, Type to);
}

public class CascadeReadRuleContainer : ICascadeReadRule
{
	public List<ICascadeReadRule> Rules { get; set; } = new();

	public bool DoCascade(Type from, Type to)
	{
		if (Rules.Where(rule => !rule.DoCascade(from, to)).Any())
		{
			return false;
		}
		return true;
	}
}

public class FullCascadeReadRule : ICascadeReadRule
{
	public bool DoCascade(Type from, Type to)
	{
		return true;
	}
}

public class NoCascadeReadRule : ICascadeReadRule
{
	public bool DoCascade(Type from, Type to)
	{
		return false;
	}
}

public class TierCascadeReadRule : ICascadeReadRule
{
	public TierCascadeReadRule(Type rootType)
	{
		TypeTiers.Add(rootType, 0);
	}

	private Dictionary<Type, int> TypeTiers { get; set; } = new();

	public int UpperTier { get; set; } = 0;

	public bool DoCascade(Type from, Type to)
	{
		if (!TypeTiers.ContainsKey(from)) return false;
		var fromTier = TypeTiers[from];

		if (UpperTier < fromTier) return false;

		if (TypeTiers.ContainsKey(to)) return false;
		TypeTiers[to] = fromTier + 1;

		return true;
	}
}

public class CascadeReadRule : ICascadeReadRule
{
	public List<CascadeRelation> CascadeRelationRules { get; set; } = new();

	public bool IsNegative { get; set; } = false;

	public bool DoCascade(Type from, Type to)
	{
		var val = CascadeRelationRules.Where(x => x.FromType.Equals(from) && x.ToType.Equals(to)).Any();
		if (IsNegative) return !val;
		return val;
	}
}

public class CascadeRelation
{
	public required Type FromType { get; set; }
	public required Type ToType { get; set; }
}