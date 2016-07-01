using Xunit;
using Xunit.Abstractions;
using System;
using ObjectStore.Test.Tests;
using System.Linq;

namespace ObjectStore.Test.Sqlite
{
    public class SqliteTests : TestsBase, IClassFixture<SqliteDatabaseFixture>
    {
        #region Constructor
        static SqliteTests()
        {
            foreach (object[] values in _subEntityData.Where(x => x[5] is DateTime))
                values[5] = ((DateTime)values[5]).ToString("yyyy-MM-dd HH:mm:ss");
        }

        public SqliteTests(SqliteDatabaseFixture databaseFixture, ITestOutputHelper output) :
            base(databaseFixture, output)
        {
        }
        #endregion

        #region Methods
        protected override string GetQuerryPattern(Query key)
        {
            switch (key)
            {
                case Query.Insert:
                    return @"^\s*INSERT\s+INTO\s+""dbo\.TestTable""\s*\(\[Name], \[Description]\)\s*VALUES\s*\(@param\d+,\s*@param\d+\);\s*SELECT\s+Id,\s+\[Name],\s*\[Description]\s+FROM\s+\""dbo.TestTable\""\s+WHERE\s+Id\s*=\s*last_insert_rowid\(\)\s*$";
                case Query.Update:
                    return @"^\s*UPDATE\s+""dbo\.TestTable""\s+SET\s+\[Description]\s*=\s*@param\d+\s+WHERE\s+Id\s*=\s*@param\d+\s*;\s*SELECT\s+Id,\s*\[Name],\s*\[Description]\s+FROM\s+""dbo\.TestTable""\s+WHERE\s+Id\s*=\s*@param\d+\s*$";
                case Query.Delete:
                    return @"^\s*DELETE\s+FROM\s+""dbo\.TestTable""\s+WHERE\s+Id\s*=\s*@param\d+\s*$";
                case Query.DeleteSub:
                    return @"^\s*DELETE\s+FROM\s+""dbo\.SubTestTable""\s+WHERE\s+Id\s*=\s*@param\d+\s*$";
                case Query.Select:
                    return @"^\s*SELECT\s+(?<T>T\d+)\.Id,\s*\k<T>\.\[Name],\s*\k<T>\.\[Description]\s+FROM\s+""dbo\.TestTable""\s+\k<T>\s*$";
                case Query.SelectSub:
                    return @"^\s*SELECT\s+(?=(?<T>T\d+))(\k<T>\.(Id|Test|\[Name]|\[First]|\[Second]|\[Nullable])(,\s*|\s+(?=FROM))){6}FROM\s+""dbo\.SubTestTable""\s+\k<T>\s*$";
                case Query.OrderBy:
                    return GetSimpleExpressionPattern(@"\k<T>\.Test\s*=\s*@param\d+\s+ORDER\s+BY\s+\k<T>\.\[Second]\s*");
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
            return @"^\s*SELECT\s+(?=(?<T>T\d+))(\k<T>\.(Id|Test|\[Name]|\[First]|\[Second]|\[Nullable])(,\s*|\s+(?=FROM))){6}FROM\s+""dbo\.SubTestTable""\s+\k<T>\s+WHERE\s+" + wherePattern + "$";
        }
        string GetForeignObjectExpressionPattern(string joinPattern)
        {
            return @"^\s*SELECT\s+(?=(?<T>T\d+))(\k<T>\.(Id|Test|\[Name]|\[First]|\[Second]|\[Nullable])(,\s*|\s+(?=FROM))){6}FROM\s+""dbo\.SubTestTable""\s+\k<T>\s+" + joinPattern + "$";
        }
        #endregion
    }
}
