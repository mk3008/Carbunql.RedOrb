using Carbunql;
using Carbunql.Dapper;
using RedOrb.Mapping;
using System.Data;

namespace RedOrb;

public class DbAccessor
{

	public static List<T> Load<T>(IDbConnection cn, Action<SelectQuery>? injector = null, ICascadeRule? rule = null)
	{
		var def = ObjectRelationMapper.FindFirst<T>();
		var val = def.ToSelectQueryMap<T>();
		var sq = val.Query;
		var typeMaps = val.Maps;

		if (injector != null) injector(sq);

		var lst = new List<T>();
		using var r = cn.ExecuteReader(sq);

		while (r.Read())
		{
			var mapper = CreateMapper(typeMaps);
			var root = mapper.Execute(r);
			if (root == null) continue;
			lst.Add((T)root);
		}

		return lst;
	}

	private static Mapper CreateMapper(List<TypeMap> typeMaps)
	{
		var lst = new Mapper();
		foreach (var map in typeMaps)
		{
			lst.Add(new() { TypeMap = map, Item = Activator.CreateInstance(map.Type)! });
		}
		return lst;
	}
}
