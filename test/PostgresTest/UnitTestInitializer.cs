using System.Runtime.CompilerServices;

namespace PostgresTest;

internal static class UnitTestInitializer
{
	[ModuleInitializer]
	public static void Initialize()
	{
		//Carbunql.Orb
		//var destdef = DBTestModels.DefinitionRepository.GetDestinationTableDefinition();
		//var sourcedef = DBTestModels.DefinitionRepository.GetDatasourceTableDefinition();
		//ObjectRelationMapper.AddTypeHandler(destdef);
		//ObjectRelationMapper.AddTypeHandler(sourcedef);
		//ObjectRelationMapper.AddTypeHandler(DbTableDefinitionRepository.GetTextFileDefinition());
		//ObjectRelationMapper.AddTypeHandler(DbTableDefinitionRepository.GetTextFolderDefinition());

		////Dapper
		//SqlMapper.AddTypeHandler(new JsonTypeHandler<DbTable>());
		//SqlMapper.AddTypeHandler(new JsonTypeHandler<Sequence?>());
		//SqlMapper.AddTypeHandler(new JsonTypeHandler<DbTableDefinition?>());
		//SqlMapper.AddTypeHandler(new JsonTypeHandler<List<string>>());
		//SqlMapper.AddTypeHandler(new ValidateOptionTypeHandler());
		//SqlMapper.AddTypeHandler(new FlipOptionTypeHandler());
		//SqlMapper.AddTypeHandler(new DeleteOptionTypeHandler());
	}
}