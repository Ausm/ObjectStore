using Xunit;
using Xunit.Abstractions;
using System;
using ObjectStore.Test.Tests;
using System.Collections.Generic;
using System.Xml.Linq;

namespace ObjectStore.Test.SqlClient
{
    public class SqlClientTests : TestsBase, IClassFixture<SqlClientDatabaseFixture>
    {
        #region Constructor
        public SqlClientTests(SqlClientDatabaseFixture databaseFixture, ITestOutputHelper output) :
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
                    if (row[i] is XElement)
                        row[i] = new System.Data.SqlTypes.SqlXml(((XElement)row[i]).CreateReader());
                }
            }

            return returnValue;
        }

        protected override string GetQuerryPattern(Query key)
        {
            switch (key)
            {
                case Query.Insert:
                    return @"^\s*INSERT\s+dbo\.TestTable\s*\(\[Name],\s*\[Description]\)\s*VALUES\s*\(@param\d+,\s*@param\d+\)\s*SET\s+(?<P>@param\d+)\s*=\s*ISNULL\(SCOPE_IDENTITY\(\),\s*@@IDENTITY\)\s*SELECT\s+Id,\s*\[Name],\s*\[Description]\s+FROM\s+dbo\.TestTable\s+WHERE\s+\k<P>\s*=\s*Id$";
                case Query.InsertNonInitializedKeyEntitiy:
                    return @"^\s*INSERT\s+dbo\.NonInitializedKey\s*\(\s*Id\s*\)\s*VALUES\s*\((?<P>@param\d+)\)\s*SELECT\s+Id\s+FROM\s+dbo\.NonInitializedKey\s+WHERE\s+\k<P>\s*=\s*Id\s*$";
                case Query.InsertDifferentTypesEntity:
                    return @"^\s*INSERT\s+dbo\.DifferentTypesTable\s*\((\[(Text|Int|Byte|Short|Long|DateTime|Guid|Binary|Decimal|Xml)](,\s*|\s*(?=\)))){10}\)\s*VALUES\s*\(@param\d+,\s*@param\d+,\s*@param\d+,\s*@param\d+,\s*@param\d+,\s*@param\d+,\s*@param\d+,\s*@param\d+,\s*@param\d+,\s*@param\d+\s*\)\s*SET\s+(?<P>@param\d+)\s*=\s*ISNULL\(SCOPE_IDENTITY\(\),\s*@@IDENTITY\)\s*SELECT\s+Id,\s*\[Text],\s*\[Int],\s*\[Byte],\s*\[Short],\s*\[Long],\s*\[DateTime],\s*\[Guid],\s*\[Binary],\s*\[Decimal],\s*\[Xml]\s+FROM\s+dbo\.DifferentTypesTable\s+WHERE\s+\k<P>\s*=\s*Id$";
                case Query.InsertDifferentWritabilityLevels:
                    return @"^\s*INSERT\s+dbo\.DifferentWritabilityLevels\s*\(((Writeable|Insertable)(,\s*|\s*(?=\)))){0,2}\)\s*VALUES\s*\(@param\d+,\s*@param\d+\s*\)\s*SET\s+(?<P>@param\d+)\s*=\s*ISNULL\(SCOPE_IDENTITY\(\),\s*@@IDENTITY\)\s*SELECT\s+((Id|Writeable|Updateable|Insertable|Readonly)(,\s*|\s+(?=FROM))){5}FROM\s+dbo\.DifferentWritabilityLevels\s+WHERE\s+\k<P>\s*=\s*Id$";
                case Query.InsertForeignObjectKeyEntity:
                    return @"^\s*INSERT\s+dbo\.ForeignObjectKeyTable\s*\(\s*Id\s*,\s*Value\s*\)\s*VALUES\s*\(\s*(?<P>@param\d+)\s*,\s*@param\d+\s*\)\s*SELECT\s+((Id|Value)(,\s*|\s+(?=FROM))){2}FROM\s+dbo\.ForeignObjectKeyTable\s+WHERE\s+\k<P>\s*=\s*Id\s*$";
                case Query.Update:
                    return @"^\s*UPDATE\s+dbo\.TestTable\s+SET\s+\[Description]\s*=\s*@param\d+\s+WHERE\s+Id\s*=\s*@param\d+\s+SELECT\s+Id,\s*\[Name],\s*\[Description]\s+FROM\s+dbo\.TestTable\s+WHERE\s+Id\s*=\s*@param\d+\s*$";
                case Query.UpdateDifferentTypesEntity:
                    return @"^\s*UPDATE\s+dbo\.DifferentTypesTable\s+SET\s+(\[(Text|Int|Byte|Short|Long|DateTime|Guid|Binary|Decimal|Xml)]\s*=\s*@param\d+(,\s*|\s+(?=WHERE))){10}WHERE\s+Id\s*=\s*(?<P>@param\d+)\s+SELECT\s+((Id|(\[(Text|Int|Byte|Short|Long|DateTime|Guid|Binary|Decimal|Xml)]))(,\s*|\s+(?=FROM))){11}FROM\s+dbo\.DifferentTypesTable\s+WHERE\s+Id\s*=\s*\k<P>\s*$";
                case Query.UpdateDifferentWritabilityLevels:
                    return @"^\s*UPDATE\s+dbo\.DifferentWritabilityLevels\s+SET\s+((Writeable|Updateable)\s*=\s*@param\d+(,\s*|\s+(?=WHERE))){2}WHERE\s+Id\s*=\s*(?<P>@param\d+)\s+SELECT\s+((Id|Writeable|Updateable|Insertable|Readonly)(,\s*|\s+(?=FROM))){5}FROM\s+dbo\.DifferentWritabilityLevels\s+WHERE\s+Id\s*=\s*\k<P>\s*$";
                case Query.UpdateForeignObjectKeyEntity:
                    return @"^\s*UPDATE\s+dbo\.ForeignObjectKeyTable\s+SET\s+Value\s*=\s*@param\d+\s+WHERE\s+Id\s*=\s*(?<P>@param\d+)\s+SELECT\s+((Id|Value)(,\s*|\s+(?=FROM))){2}FROM\s+dbo\.ForeignObjectKeyTable\s+WHERE\s+Id\s*=\s*\k<P>\s*$";
                case Query.Delete:
                    return @"^\s*DELETE\s+dbo\.TestTable\s+WHERE\s+Id\s*=\s*@param\d+\s*$";
                case Query.DeleteSub:
                    return @"^\s*DELETE\s+dbo\.SubTestTable\s+WHERE\s+Id\s*=\s*@param\d+\s*$";
                case Query.DeleteForeignObjectKeyEntity:
                    return @"^\s*DELETE\s+dbo\.ForeignObjectKeyTable\s+WHERE\s+Id\s*=\s*@param\d+\s*$";
                case Query.Select:
                    return @"^\s*SELECT\s+(?<T>T\d+)\.Id,\s*\k<T>\.\[Name],\s*\k<T>\.\[Description]\s+FROM\s+dbo\.TestTable\s+\k<T>\s*$";
                case Query.SelectSub:
                    return @"^\s*SELECT\s+(?=(?<T>T\d+))(\k<T>\.(Id|Test|\[Name]|\[First]|\[Second]|\[Nullable])(,\s*|\s+(?=FROM))){6}FROM\s+dbo\.SubTestTable\s+\k<T>\s*$";
                case Query.SelectForeignObjectKeyEntity:
                    return @"^\s*SELECT\s+(?=(?<T>T\d+))(\k<T>\.(Id|Value)(,\s*|\s+(?=FROM))){2}FROM\s+dbo\.ForeignObjectKeyTable\s+\k<T>\s+WHERE\s+\k<T>\.Id\s*=\s*@param\d+\s*$";
                case Query.SelectSubTake10:
                    return @"^\s*SELECT\s+TOP\s+10\s+(?=(?<T>T\d+))(\k<T>\.(Id|Test|\[Name]|\[First]|\[Second]|\[Nullable])(,\s*|\s+(?=FROM))){6}FROM\s+dbo\.SubTestTable\s+\k<T>\s+ORDER\s+BY\s+\k<T>\.Id\s*$";
                case Query.SelectNonInitializedKeyEntitiy:
                    return @"^\s*SELECT\s+(?<T>T\d+)\.Id\s+FROM\s+dbo\.NonInitializedKey\s+\k<T>\s*$";
                case Query.SelectDifferentTypesEntity:
                    return @"^\s*SELECT\s+(?=(?<T>T\d+))(\k<T>\.((Id|(\[(Text|Int|Byte|Short|Long|DateTime|Guid|Binary|Decimal|Xml)])))(,\s*|\s+(?=FROM))){11}FROM\s+dbo\.DifferentTypesTable\s+\k<T>\s*$";
                case Query.SelectDifferentWritabilityLevels:
                    return @"^\s*SELECT\s+(?=(?<T>T\d+))(\k<T>\.(Id|Writeable|Updateable|Insertable|Readonly)(,\s*|\s+(?=FROM))){5}FROM\s+dbo\.DifferentWritabilityLevels\s+\k<T>\s*$";
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
                case Query.ForeignObjectEqual:
                    return GetForeignObjectExpressionPattern(@"WHERE\s+\k<T>\.Test\s*=\s*@param\d+");
                case Query.ForeignObjectPropertyEqualTo:
                    return GetForeignObjectExpressionPattern(@"LEFT\s+(OUTER\s+)?JOIN\s+dbo\.TestTable\s(?<T2>T\d+)\s+ON\s+\k<T2>\.Id\s*=\s*\k<T>\.Test\s+WHERE\s+\k<T2>\.\[Name]\s*=\s*@param\d+");
                default:
                    throw new NotSupportedException("This querry is not supported");
            }
        }
        string GetSimpleExpressionPattern(string wherePattern)
        {
            return @"^\s*SELECT\s+(?=(?<T>T\d+))(\k<T>\.(Id|Test|\[Name]|\[First]|\[Second]|\[Nullable])(,\s*|\s+(?=FROM))){6}FROM\s+dbo\.SubTestTable\s+\k<T>\s+WHERE\s+" + wherePattern + "$";
        }
        string GetForeignObjectExpressionPattern(string joinPattern)
        {
            return @"^\s*SELECT\s+(?=(?<T>T\d+))(\k<T>\.(Id|Test|\[Name]|\[First]|\[Second]|\[Nullable])(,\s*|\s+(?=FROM))){6}FROM\s+dbo\.SubTestTable\s+\k<T>\s+" + joinPattern + "$";
        }
        #endregion

        #region Properties
        protected override DateTime DatabaseMaxDate
        {
            get
            {
                return DateTime.MaxValue.Subtract(new TimeSpan(0,0,1));
            }
        }

        protected override DateTime DatabaseMinDate
        {
            get
            {
                return new DateTime(1753,1,1);
            }
        }
        #endregion
    }
}
