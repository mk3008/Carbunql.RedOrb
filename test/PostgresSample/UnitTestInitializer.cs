using RedOrb;
using System.Runtime.CompilerServices;

namespace PostgresSample;

internal static class UnitTestInitializer
{
	[ModuleInitializer]
	public static void Initialize()
	{
		ObjectRelationMapper.PlaceholderIdentifer = ":";
		ObjectRelationMapper.AddTypeHandler(DbTableDefinitionRepository.GetBlogDefinition());
		ObjectRelationMapper.AddTypeHandler(DbTableDefinitionRepository.GetPostDefinition());
		ObjectRelationMapper.AddTypeHandler(DbTableDefinitionRepository.GetCommentDefinition());
	}
}