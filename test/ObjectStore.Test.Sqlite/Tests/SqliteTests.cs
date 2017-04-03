using Xunit;
using Xunit.Abstractions;
using System;
using ObjectStore.Test.Tests;
using ObjectStore.Sqlite;
using System.Collections.Generic;

namespace ObjectStore.Test.Sqlite
{
    public class SqliteTests : TestsBase, IClassFixture<SqliteDatabaseFixture>
    {
        #region Constructor
        static SqliteTests()
        {
        }

        public SqliteTests(SqliteDatabaseFixture databaseFixture, ITestOutputHelper output) :
            base(databaseFixture, output)
        {
        }
        #endregion

        #region Methods
        protected override IEnumerable<object[]> GetDefaultResult(Query key)
        {
            IEnumerable<object[]> returnValue = base.GetDefaultResult(key);

            foreach (object[] row in returnValue)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    if (row[i] is DateTime)
                        row[i] = ((DateTime)row[i]).ToString("yyyy-MM-dd HH:mm:ss");
                }
            }

            return returnValue;
        }

        protected override string GetQuerryPattern(Query key)
        {
            switch (key)
            {
                case Query.Insert:
                    return @"^\s*INSERT\s+INTO\s+""dbo\.TestTable""\s*\(\[Name], \[Description]\)\s*VALUES\s*\(@param\d+,\s*@param\d+\);\s*SELECT\s+Id,\s+\[Name],\s*\[Description]\s+FROM\s+\""dbo\.TestTable\""\s+WHERE\s+Id\s*=\s*\(SELECT\s+seq\s+FROM\s+sqlite_sequence\s+WHERE\s+name\s*=\s*""dbo\.TestTable""\)\s*$";
                case Query.InsertNonInitializedKeyEntitiy:
                    return @"^\s*INSERT\s+INTO\s+""dbo\.NonInitializedKey""\s*\(\s*Id\s*\)\s*VALUES\s*\((?<P>@param\d+)\);\s*SELECT\s+Id\s+FROM\s+""dbo\.NonInitializedKey""\s+WHERE\s+\k<P>\s*=\s*Id\s*$";
                case Query.InsertDifferentTypesEntity:
                    return @"^\s*INSERT\s+INTO\s+""dbo\.DifferentTypesTable""\s*\(((\[(Text|Boolean|Int|Byte|Short|Long|DateTime|Guid|Binary|Decimal|Xml)\])(,\s|\s*(?=\)))){11}\)\s*VALUES\s*\((@param\d+(,\s*|\s*(?=\)))){11}\);\s*SELECT\s+(((\[(Text|Boolean|Int|Byte|Short|Long|DateTime|Guid|Binary|Decimal|Xml)\])|Id)(,\s*|\s+(?=FROM))){12}FROM\s+""dbo\.DifferentTypesTable""\s+WHERE\s+Id\s*=\s*\(SELECT\s+seq\s+FROM\s+sqlite_sequence\s+WHERE\s+name\s*=\s*""dbo\.DifferentTypesTable""\)\s*$";
                case Query.InsertDifferentWritabilityLevels:
                    return @"^\s*INSERT\s+INTO\s+""dbo\.DifferentWritabilityLevels""\s*\(((Writeable|Insertable)(,\s|\s*(?=\)))){2}\)\s*VALUES\s*\((@param\d+(,\s*|\s*(?=\)))){2}\);\s*SELECT\s+((Id|Writeable|Updateable|Insertable|Readonly)(,\s*|\s+(?=FROM))){5}FROM\s+""dbo.DifferentWritabilityLevels""\s+WHERE\s+Id\s*=\s*\(SELECT\s+seq\s+FROM\s+sqlite_sequence\s+WHERE\s+name\s*=\s*""dbo\.DifferentWritabilityLevels""\)\s*$";
                case Query.InsertForeignObjectKeyEntity:
                    return @"^\s*INSERT\s+INTO\s+""dbo\.ForeignObjectKeyTable""\s*\(((Id|Value)(,\s|\s*(?=\)))){2}\)\s*VALUES\s*\((@param\d+(,\s*|\s*(?=\)))){2}\);\s*SELECT\s+((Id|Value)(,\s*|\s+(?=FROM))){2}FROM\s+""dbo.ForeignObjectKeyTable""\s+WHERE\s+@param\d+\s*=\s*Id\s*$";
                case Query.Update:
                    return @"^\s*UPDATE\s+""dbo\.TestTable""\s+SET\s+\[Description]\s*=\s*@param\d+\s+WHERE\s+Id\s*=\s*@param\d+\s*;\s*SELECT\s+Id,\s*\[Name],\s*\[Description]\s+FROM\s+""dbo\.TestTable""\s+WHERE\s+Id\s*=\s*@param\d+\s*$";
                case Query.UpdateDifferentTypesEntity:
                    return @"^\s*UPDATE\s+""dbo\.DifferentTypesTable""\s+SET\s+(\[(Text|Boolean|Int|Byte|Short|Long|DateTime|Guid|Binary|Decimal|Xml)\]\s*=\s*@param\d+(,\s*|\s+(?=WHERE))){11}WHERE\s+Id\s*=\s*(?<P>@param\d+);\s*SELECT\s+(((\[(Text|Boolean|Int|Byte|Short|Long|DateTime|Guid|Binary|Decimal|Xml)\])|Id)(,\s*|\s+(?=FROM))){12}FROM\s+""dbo.DifferentTypesTable""\s+WHERE\s+Id\s*=\s*\k<P>\s*$";
                case Query.UpdateDifferentWritabilityLevels:
                    return @"^\s*UPDATE\s+""dbo\.DifferentWritabilityLevels""\s+SET\s+((Writeable|Updateable)\s*=\s*@param\d+(,\s*|\s+(?=WHERE))){2}WHERE\s+Id\s*=\s*(?<P>@param\d+);\s*SELECT\s+((Id|Writeable|Updateable|Insertable|Readonly)(,\s*|\s+(?=FROM))){5}FROM\s+""dbo.DifferentWritabilityLevels""\s+WHERE\s+Id\s*=\s*\k<P>\s*$";
                case Query.UpdateForeignObjectKeyEntity:
                    return @"^\s*UPDATE\s+""dbo\.ForeignObjectKeyTable""\s+SET\s+Value\s*=\s*@param\d+\s+WHERE\s+Id\s*=\s*(?<P>@param\d+);\s*SELECT\s+((Id|Value)(,\s*|\s+(?=FROM))){2}FROM\s+""dbo\.ForeignObjectKeyTable""\s+WHERE\s+Id\s*=\s*\k<P>\s*$";
                case Query.Delete:
                    return @"^\s*DELETE\s+FROM\s+""dbo\.TestTable""\s+WHERE\s+Id\s*=\s*@param\d+\s*$";
                case Query.DeleteSub:
                    return @"^\s*DELETE\s+FROM\s+""dbo\.SubTestTable""\s+WHERE\s+Id\s*=\s*@param\d+\s*$";
                case Query.DeleteForeignObjectKeyEntity:
                    return @"^\s*DELETE\s+FROM\s+""dbo\.ForeignObjectKeyTable""\s+WHERE\s+Id\s*=\s*@param\d+\s*$";
                case Query.Select:
                    return @"^\s*SELECT\s+(?=(?<T>T\d+))(\k<T>\.(?<C>Id|\[Name]|\[Description]|\[Second]|\[Nullable])\s+\k<C>\s*(,\s*|\s+(?=FROM))){3}FROM\s+""dbo\.TestTable""\s+\k<T>\s*$";
                case Query.SelectSub:
                    return @"^\s*SELECT\s+(?=(?<T>T\d+))(\k<T>\.(?<C>Id|Test|\[Name]|\[First]|\[Second]|\[Nullable])\s+\k<C>\s*(,\s*|\s+(?=FROM))){6}FROM\s+""dbo\.SubTestTable""\s+\k<T>\s*$";
                case Query.SelectSubTake10:
                    return @"^\s*SELECT\s+(?=(?<T>T\d+))(\k<T>\.(?<C>Id|Test|\[Name]|\[First]|\[Second]|\[Nullable])\s+\k<C>\s*(,\s*|\s+(?=FROM))){6}FROM\s+""dbo\.SubTestTable""\s+\k<T>\s+ORDER\s+BY\s+\k<T>\.Id\s+LIMIT\s+10\s*$";
                case Query.SelectNonInitializedKeyEntitiy:
                    return @"^\s*SELECT\s+(?<T>T\d+)\.Id\s+Id\s+FROM\s+""dbo\.NonInitializedKey""\s+\k<T>\s*$";
                case Query.SelectDifferentTypesEntity:
                    return @"^\s*SELECT\s+(?=(?<T>T\d+))(\k<T>\.((?<C>(\[(Text|Boolean|Int|Byte|Short|Long|DateTime|Guid|Binary|Decimal|Xml)\])|Id))\s+\k<C>\s*(,\s*|\s+(?=FROM))){12}FROM\s+""dbo\.DifferentTypesTable""\s+\k<T>\s*$";
                case Query.SelectDifferentWritabilityLevels:
                    return @"^\s*SELECT\s+(?=(?<T>T\d+))(\k<T>\.(?<C>(Id|Writeable|Updateable|Insertable|Readonly))\s+\k<C>\s*(,\s*|\s+(?=FROM))){5}FROM\s+""dbo\.DifferentWritabilityLevels""\s+\k<T>\s*$";
                case Query.SelectForeignObjectKeyEntity:
                    return @"^\s*SELECT\s+(?=(?<T>T\d+))(\k<T>\.(?<C>(Id|Value))\s+\k<C>\s*(,\s*|\s+(?=FROM))){2}FROM\s+""dbo\.ForeignObjectKeyTable""\s+\k<T>\s+WHERE\s+\k<T>\.Id\s*=\s*@param\d+\s*$";
                case Query.OrderBy:
                    return GetSimpleExpressionPattern(@"\k<T>\.Test\s*=\s*@param\d+\s+ORDER\s+BY\s+\k<T>\.\[Second]\s*");
                case Query.OrderByDescending:
                    return GetSimpleExpressionPattern(@"\k<T>\.Test\s*=\s*@param\d+\s+ORDER\s+BY\s+\k<T>\.\[Second]\s+DESC\s*");
                case Query.SimpleExpressionEqual:
                    return GetSimpleExpressionPattern(@"\k<T>\.\[First]\s*=\s*\k<T>\.\[Second]");
                case Query.SimpleExpressionUnequal:
                    return GetSimpleExpressionPattern(@"\k<T>\.\[First]\s*!=\s*\k<T>\.\[Second]");
                case Query.SimpleExpressionEqualToNull:
                    return GetSimpleExpressionPattern(@"\k<T>\.\[Nullable]\s+IS\s+NULL");
                case Query.SimpleExpressionUnequalToNull:
                    return GetSimpleExpressionPattern(@"\k<T>\.\[Nullable]\s+IS\s+NOT\s+NULL");
                case Query.SimpleExpressionAdd:
                    return GetSimpleExpressionPattern(@"\k<T>\.\[First]\s*\+\s*\k<T>\.\[Second]\s*=\s*@param\d+");
                case Query.SimpleExpressionSubtract:
                    return GetSimpleExpressionPattern(@"\k<T>\.\[First]\s*\-\s*\k<T>\.\[Second]\s*=\s*@param\d+");
                case Query.SimpleExpressionGreater:
                    return GetSimpleExpressionPattern(@"\k<T>\.\[First]\s*\>\s*\k<T>\.\[Second]");
                case Query.SimpleExpressionGreaterEqual:
                    return GetSimpleExpressionPattern(@"\k<T>\.\[First]\s*>=\s*\k<T>\.\[Second]");
                case Query.SimpleExpressionLess:
                    return GetSimpleExpressionPattern(@"\k<T>\.\[First]\s*<\s*\k<T>\.\[Second]");
                case Query.SimpleExpressionLessEqual:
                    return GetSimpleExpressionPattern(@"\k<T>\.\[First]\s*<=\s*\k<T>\.\[Second]");
                case Query.SimpleExpressionConstantValue:
                    return GetSimpleExpressionPattern(@"\k<T>\.\[First]\s*=\s*@param\d+");
                case Query.SimpleExpressionContains:
                    return GetSimpleExpressionPattern(@"\k<T>\.\[First]\s*IN\s*\(@param\d+,\s*@param\d+,\s*@param\d+\)");
                case Query.SimpleExpressionAnd:
                    return GetSimpleExpressionPattern(@"\(\k<T>\.\[First]\s*=\s*@param\d+\s*\)\s*AND\s*\(\s*\k<T>\.\[Second]\s*=\s*@param\d+\)\s*");
                case Query.SimpleExpressionOr:
                    return GetSimpleExpressionPattern(@"\(\k<T>\.\[First]\s*=\s*@param\d+\s*\)\s*OR\s*\(\s*\k<T>\.\[First]\s*=\s*@param\d+\)\s*");
                case Query.ForeignObjectEqual:
                    return GetForeignObjectExpressionPattern(@"WHERE\s+\k<T>\.Test\s*=\s*@param\d+");
                case Query.ForeignObjectPropertyEqualTo:
                    return GetForeignObjectExpressionPattern(@"LEFT\s+(OUTER\s+)?JOIN\s+""dbo\.TestTable""\s(?<T2>T\d+)\s+ON\s+\k<T2>\.Id\s*=\s*\k<T>\.Test\s+WHERE\s+\k<T2>\.\[Name]\s*=\s*@param\d+");
                default:
                    throw new NotSupportedException("This querry is not supported");
            }
        }
        string GetSimpleExpressionPattern(string wherePattern)
        {
            return @"^\s*SELECT\s+(?=(?<T>T\d+))(\k<T>\.(?<C>Id|Test|\[Name]|\[First]|\[Second]|\[Nullable])\s+\k<C>\s*(,\s*|\s+(?=FROM))){6}FROM\s+""dbo\.SubTestTable""\s+\k<T>\s+WHERE\s+" + wherePattern + "$";
        }
        string GetForeignObjectExpressionPattern(string joinPattern)
        {
            return @"^\s*SELECT\s+(?=(?<T>T\d+))(\k<T>\.(?<C>Id|Test|\[Name]|\[First]|\[Second]|\[Nullable])\s+\k<C>\s*(,\s*|\s+(?=FROM))){6}FROM\s+""dbo\.SubTestTable""\s+\k<T>\s+" + joinPattern + "$";
        }
        #endregion
    }

    public class SqliteAdditionalTests
    {
        #region Tests
        [Fact]
        public void TestInitialize()
        {
            OrMapping.RelationalObjectStore relationalObjectProvider;
            
            ObjectStoreManager.DefaultObjectStore.RegisterObjectProvider(relationalObjectProvider = new OrMapping.RelationalObjectStore("Data Source=file::memory:?cache=shared;", DataBaseProvider.Instance, new MappingOptions.MappingOptionsSet().AddDefaultRules(), true));

            relationalObjectProvider.Register<Entities.Test>();
            relationalObjectProvider.Register<Entities.SubTest>();
            relationalObjectProvider.Register<Entities.DifferentTypes>();
            relationalObjectProvider.Register<Entities.DifferentWritabilityLevels>();
            relationalObjectProvider.Register<Entities.ForeignObjectKey>();
            relationalObjectProvider.Register<Entities.NonInitializedKey>();
            relationalObjectProvider.InitializeDatabase();

            Assert.True(false);

        }
        #endregion

    }
}
