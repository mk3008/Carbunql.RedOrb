using Carbunql;
using Carbunql.Analysis.Parser;
using Carbunql.Building;
using Carbunql.Clauses;
using Carbunql.Definitions;
using Carbunql.Values;
using RedOrb;
using RedOrb.Attributes;
using RedOrb.Mapping;
using System;
using System.Reflection;

namespace RedOrb;

public interface IDbTableDefinition : IDbTable
{
    IEnumerable<DbColumnDefinition> ColumnDefinitions { get; }

    List<DbIndexDefinition> Indexes { get; }

    List<string> PKeyIdentifiers { get; }

    Type Type { get; }

    IEnumerable<DbParentRelationDefinition> ParentRelationDefinitions { get; }

    List<string> ChildIdentifiers { get; }
}

public static class IDbTableDefinitionExtention
{
    public static DbTableAttribute GetDbTableAttribute(this IDbTableDefinition source)
    {
        var table = source.Type.GetCustomAttributes(false).OfType<DbTableAttribute>().FirstOrDefault();
        if (table == null)
        {
            table = new DbTableAttribute(source.PKeyIdentifiers.ToArray())
            {
                Schema = source.SchemaName,
                Table = source.TableName,
            };
        }

        if (string.IsNullOrEmpty(table.Table))
        {
            table.Table = source.Type.Name.ToSnakeCase();
        }
        return table;
    }

    public static DefinitionQuerySet ToDefinitionQuerySet(this IDbTableDefinition source)
    {
        var table = source.GetDbTableAttribute();

        var ct = new CreateTableQuery(table)
        {
            HasIfNotExists = true
        };
        ct.DefinitionClause = new(ct);

        source.GetColumnDefinitions(table).ToList().ForEach(x => ct.DefinitionClause.Add(x));
        ct.DefinitionClause.Add(source.GetPKeyConstraint(table));

        var qs = new DefinitionQuerySet(ct);
        source.GetIndexQueries(table).ToList().ForEach(qs.AddAlterIndexQuery);
        source.GetRelationIndexQueries(table).ToList().ForEach(qs.AddAlterIndexQuery);
        return qs;
    }

    public static IEnumerable<ColumnDefinition> GetColumnDefinitions(this IDbTableDefinition source, DbTableAttribute table)
    {
        foreach (var column in source.ColumnDefinitions)
        {
            var c = new ColumnDefinition(table, column.ColumnName, column.ColumnType)
            {
                IsNullable = column.IsNullable,
            };
            if (!string.IsNullOrEmpty(column.AutoNumberCommand))
            {
                c.AutoNumberDefinition = ValueParser.Parse(column.AutoNumberCommand);
            }
            if (!string.IsNullOrEmpty(column.DefaultValue))
            {
                c.DefaultValue = ValueParser.Parse(column.DefaultValue);
            }
            yield return c;
        }
    }

    public static PrimaryKeyConstraint GetPKeyConstraint(this IDbTableDefinition source, DbTableAttribute table)
    {
        var name = table.ConstraintName;
        if (string.IsNullOrEmpty(name))
        {
            name = $"pk_{table.Table}";
        }

        var columns = new List<string>();
        foreach (var id in table.Identifiers)
        {
            var c = source.ColumnDefinitions.Where(x => x.Identifier == id).First();
            columns.Add(c.ColumnName);
        }
        if (!columns.Any()) throw new InvalidProgramException();

        return new PrimaryKeyConstraint(table)
        {
            ConstraintName = name,
            ColumnNames = columns
        };
    }

    public static IEnumerable<CreateIndexQuery> GetIndexQueries(this IDbTableDefinition source, DbTableAttribute table)
    {
        var indexes = source.Type.GetCustomAttributes(false).OfType<DbIndexAttribute>().ToList();

        foreach (var index in indexes)
        {
            var clause = new IndexOnClause(table);
            foreach (var id in index.Identifiers)
            {
                var c = source.ColumnDefinitions.Where(x => x.Identifier == id).First();
                clause.Add(ValueParser.Parse(c.ColumnName).ToSortable());
            }

            var name = index.ConstraintName;
            {
                var num = indexes.IndexOf(index);
                name = $"i{num}_{table.Table}";
            }

            yield return new CreateIndexQuery(clause)
            {
                IndexName = name,
                IsUnique = index.IsUnique,
                HasIfNotExists = true
            };
        }
    }

    public static IEnumerable<CreateIndexQuery> GetRelationIndexQueries(this IDbTableDefinition source, DbTableAttribute table)
    {
        var idx = 0;
        foreach (var prop in source.Type.GetProperties())
        {
            var columns = new List<string>();
            foreach (var atr in prop.GetCustomAttributes(false).OfType<DbParentRelationColumnAttribute>())
            {
                var column = atr.ColumnName;
                if (string.IsNullOrEmpty(column)) column = prop.Name.ToSnakeCase() + "_id";
                columns.Add(column);
            }
            if (!columns.Any()) continue;

            var clause = new IndexOnClause(table);
            foreach (var item in columns)
            {
                clause.Add(ValueParser.Parse(item).ToSortable());
            }

            var name = $"r{idx}_{table.Table}";
            idx++;

            yield return new CreateIndexQuery(clause)
            {
                IndexName = name,
                HasIfNotExists = true
            };
        }
    }

    //[Obsolete]
    //public static IEnumerable<string> ToCreateIndexCommandTexts(this IDbTableDefinition source)
    //{
    //	var id = 0;
    //	foreach (var index in source.Indexes)
    //	{
    //		id++;
    //		yield return index.ToCommandText(source, id);
    //	}
    //}

    public static string GetColumnName(this IDbTableDefinition source, string identifer)
    {
        return source.ColumnDefinitions.Where(x => x.Identifier == identifer).Select(x => x.ColumnName).First();
    }

    public static DbColumnDefinition? GetSequenceOrDefault(this IDbTableDefinition source)
    {
        return source.ColumnDefinitions.Where(x => x.IsAutoNumber).FirstOrDefault();
    }

    public static DbColumnDefinition GetSequence(this IDbTableDefinition source)
    {
        var seq = source.GetSequenceOrDefault();
        if (seq == null) throw new NotSupportedException($"Sequence column not defined in {source.GetTableFullName()}");
        return seq;
    }

    public static List<DbColumnDefinition> GetPrimaryKeys(this IDbTableDefinition source)
    {
        var table = source.GetDbTableAttribute();
        return source.ColumnDefinitions.Where(x => table.Identifiers.Contains(x.Identifier)).ToList();
    }

    public static List<DbIndexDefinition> GetUniqueKeyIndexes(this IDbTableDefinition source)
    {
        var lst = source.Indexes.Where(x => x.IsUnique).ToList();
        if (!lst.Any()) throw new NotSupportedException($"Unique key column not defined in {source.GetTableFullName()}");
        return lst;
    }

    private static (SelectQuery, TypeMap) CreateSelectQuery<T>(this DbTableDefinition def)
    {
        var map = new TypeMap()
        {
            TableAlias = "t0",
            Type = typeof(T),
            ColumnMaps = new()
        };

        var sq = new SelectQuery();
        var table = ValueParser.Parse(def.GetTableFullName()).ToText();
        sq.From(table).As(map.TableAlias);

        sq.AddSelectPrimarykeyColumns(def, map);
        sq.AddSelectColumnsWithoutPrimaryKeys(def, map);

        return (sq, map);
    }

    public static (SelectQuery Query, List<TypeMap> Maps) ToSelectQueryMap<T>(this DbTableDefinition source, ICascadeReadRule? rule = null)
    {
        var (sq, fromMap) = source.CreateSelectQuery<T>();
        var maps = new List<TypeMap>() { fromMap };
        var from = sq.FromClause!.Root;

        rule ??= new FullCascadeReadRule();

        source.ParentRelationDefinitions.ToList().ForEach(relation =>
        {
            var doCascade = rule.DoCascade(source.Type!, relation.IdentiferType);
            maps.AddRange(sq.AddJoin(relation, fromMap, rule, doSelectPKeyOnly: !doCascade));
        });

        return (sq, maps);
    }

    public static (InsertQuery Query, DbColumnDefinition? Sequence) ToInsertQuery<T>(this IDbTableDefinition source, T instance, string placeholderIdentifer)
    {
        var row = new ValueCollection();
        var cols = new List<string>();

        foreach (var item in source.ColumnDefinitions.Where(x => x.SpecialColumn == SpecialColumn.None))
        {
            if (string.IsNullOrEmpty(item.Identifier)) continue;
            if (item.IsAutoNumber) continue;

            var prop = item.Identifier.ToPropertyInfo<T>();
            var pv = prop.ToParameterValue(instance, placeholderIdentifer);

            row.Add(pv);
            cols.Add(item.ColumnName);
        }

        foreach (var item in source.ColumnDefinitions.Where(x => x.SpecialColumn == SpecialColumn.CreateTimestamp || x.SpecialColumn == SpecialColumn.UpdateTimestamp))
        {
            if (string.IsNullOrEmpty(item.Identifier)) continue;
            if (item.IsAutoNumber) continue;

            var command = !string.IsNullOrEmpty(item.DefaultValue) ? item.DefaultValue : "current_timestamp";

            row.Add(command);
            cols.Add(item.ColumnName);
        }

        foreach (var item in source.ColumnDefinitions.Where(x => x.SpecialColumn == SpecialColumn.VersionNumber))
        {
            row.Add("1");
            cols.Add(item.ColumnName);
        }

        foreach (var parent in source.ParentRelationDefinitions)
        {
            var parentProp = parent.Identifer.ToPropertyInfo<T>();
            var parentType = parentProp.PropertyType;
            var parentInstance = parentProp.GetValue(instance);

            var def = ObjectRelationMapper.FindFirst(parent.IdentiferType);
            var pkeys = def.GetPrimaryKeys();

            foreach (var relation in parent.Relations)
            {
                var prop = relation.ParentIdentifer.ToPropertyInfo(parentType);
                if (parentInstance != null)
                {
                    var pv = prop.ToParameterValue(parentInstance, placeholderIdentifer);
                    row.Add(pv);
                }
                else
                {
                    var pv = prop.ToParameterNullValue(placeholderIdentifer);
                    row.Add(pv);
                }
                cols.Add(relation.ColumnName);
            }
        }

        var vq = new ValuesQuery(new List<ValueCollection>() { row });
        var query = vq.ToSelectQuery(cols).ToInsertQuery(source.GetTableFullName());

        var seq = source.GetSequenceOrDefault();
        if (seq != null) query.Returning(new ColumnValue(seq.ColumnName));

        return (query, seq);
    }

    public static UpdateQuery ToUpdateQuery<T>(this IDbTableDefinition source, T instance, string placeholderIdentifer)
    {
        var pkeys = source.GetPrimaryKeys();

        var row = new ValueCollection();
        var cols = new List<string>();

        foreach (var item in source.ColumnDefinitions.Where(x => x.SpecialColumn == SpecialColumn.None || x.SpecialColumn == SpecialColumn.ParentRelation))
        {
            if (string.IsNullOrEmpty(item.Identifier)) continue;

            var prop = item.Identifier.ToPropertyInfo<T>();
            var pv = prop.ToParameterValue(instance, placeholderIdentifer);

            row.Add(pv);
            cols.Add(item.ColumnName);
        }

        foreach (var item in source.ColumnDefinitions.Where(x => x.SpecialColumn == SpecialColumn.UpdateTimestamp))
        {
            var command = !string.IsNullOrEmpty(item.DefaultValue) ? item.DefaultValue : "current_timestamp";

            row.Add(command);
            cols.Add(item.ColumnName);
        }

        // version increment
        foreach (var item in source.ColumnDefinitions.Where(x => x.SpecialColumn == SpecialColumn.VersionNumber))
        {
            var prop = item.Identifier.ToPropertyInfo<T>();
            var pv = prop.ToParameterValue(instance, placeholderIdentifer);
            pv.AddOperatableValue("+", "1");
            row.Add(pv);
            cols.Add(item.ColumnName);
        }

        var vq = new ValuesQuery(new List<ValueCollection>() { row });
        var uq = vq.ToSelectQuery(cols).ToUpdateQuery(source.GetTableFullName(), pkeys.Select(x => x.ColumnName));

        // version check
        foreach (var item in source.ColumnDefinitions.Where(x => x.SpecialColumn == SpecialColumn.VersionNumber))
        {
            var prop = item.Identifier.ToPropertyInfo<T>();
            var pv = prop.ToParameterValue(instance, placeholderIdentifer);
            uq.WhereClause!.Condition.And(uq.UpdateClause!.Table.Alias, item.ColumnName).Equal(pv);
        }

        return uq;
    }

    public static DeleteQuery ToDeleteQuery<T>(this IDbTableDefinition source, T instance, string placeholderIdentifer)
    {
        var pkeys = source.GetPrimaryKeys();

        var row = new ValueCollection();
        var cols = new List<string>();

        foreach (var item in pkeys)
        {
            var prop = item.Identifier.ToPropertyInfo<T>();
            var pv = prop.ToParameterValue(instance, placeholderIdentifer);

            row.Add(pv);
            cols.Add(item.ColumnName);
        }

        var vq = new ValuesQuery(new List<ValueCollection>() { row });
        return vq.ToSelectQuery(cols).ToDeleteQuery(source.GetTableFullName());
    }
}

