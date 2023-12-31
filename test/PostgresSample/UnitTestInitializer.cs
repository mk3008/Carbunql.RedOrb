using RedOrb;
using System.Runtime.CompilerServices;

namespace PostgresSample;

internal static class UnitTestInitializer
{
	[ModuleInitializer]
	public static void Initialize()
	{
		ObjectRelationMapper.PlaceholderIdentifer = ":";
		ObjectRelationMapper.Converter = Converter;
		ObjectRelationMapper.AddTypeHandler(DbTableDefinitionRepository.GetBlogDefinition());
		ObjectRelationMapper.AddTypeHandler(DbTableDefinitionRepository.GetPostDefinition());
		ObjectRelationMapper.AddTypeHandler(DbTableDefinitionRepository.GetCommentDefinition());
	}

	private static DbTableDefinition Converter(DbTableDefinition def)
	{
		//foreach (var item in def.ColumnDefinitions) 
		//{
		//	item.ColumnName = item.ColumnName.ToUpper();
		//}
		return def;
	}
}