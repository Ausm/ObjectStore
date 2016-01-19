using Xunit;
using System;
using System.Linq;
using ObjectStore.Test.Fixtures;
using Xunit.Abstractions;
using E = ObjectStore.Test.Entities;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ObjectStore.Test
{
    [Collection("Database collection")]
    public class PreparedEntitiesTests : IClassFixture<InitEnitiesFixture>
    {
        DatabaseFixture _databaseFixture;
        ITestOutputHelper _output;
        IQueryable<E.Test> _queryable;
        IQueryable<E.SubTest> _subQueryable;

        public PreparedEntitiesTests(DatabaseFixture databaseFixture, InitEnitiesFixture initEntitiesFixture, ITestOutputHelper output)
        {
            _databaseFixture = databaseFixture;
            _output = output;

            initEntitiesFixture.Init(databaseFixture.ObjectProvider);

            _queryable = _databaseFixture.ObjectProvider.GetQueryable<E.Test>().ForceLoad();
            _subQueryable = _databaseFixture.ObjectProvider.GetQueryable<E.SubTest>().ForceLoad();
        }

        [Theory, MemberData(nameof(SimpleExpressions))]
        public void TestSimpleExpression(string name, Expression<Func<E.SubTest, bool>> expression, int expectedCount)
        {
            _output.WriteLine($"Test {name} expression");
            List<E.SubTest> subResult = _subQueryable.Where(expression).ToList();
            Assert.Equal(expectedCount, subResult.Count);
            _output.WriteLine("... Done");
        }

        [Theory, MemberData(nameof(ForeignObjectExpressions))]
        public void TestForeignObjectExpression(string name, Func<IQueryable<E.SubTest>, E.Test, IQueryable<E.SubTest>> function, int expectedCount)
        {
            _output.WriteLine($"Test {name} expression");
            List<E.SubTest> subResult = function(_subQueryable, _queryable.FirstOrDefault()).ToList();
            Assert.Equal(expectedCount, subResult.Count);
            _output.WriteLine("... Done");
        }

        #region MemberData Definitions
        public static IEnumerable<object[]> SimpleExpressions
        {
            get
            {
                yield return GetSimpleExpressionParams("Equal", x => x.First == x.Second, 2);
                yield return GetSimpleExpressionParams("Equal to Null", x => x.Nullable == null, 18);
                yield return GetSimpleExpressionParams("Unequal to Null", x => x.Nullable != null, 2);
                yield return GetSimpleExpressionParams("Add", x => x.First + x.Second == 10, 20);
                yield return GetSimpleExpressionParams("Subtract", x => x.First - x.Second == 2, 2);
                yield return GetSimpleExpressionParams("Greater", x => x.First > x.Second, 8);
                yield return GetSimpleExpressionParams("GreaterEqual", x => x.First >= x.Second, 10);
                yield return GetSimpleExpressionParams("Less", x => x.First < x.Second, 10);
                yield return GetSimpleExpressionParams("LessEqual", x => x.First <= x.Second, 12);
                yield return GetSimpleExpressionParams("ConstantValue", x => x.First == 5, 2);
                yield return GetSimpleExpressionParams("Contains", x => new int[] { 2, 5, 7 }.Contains(x.First), 6);
            }
        }

        public static IEnumerable<object[]> ForeignObjectExpressions
        {
            get
            {
                yield return GetForeignObjectExpressionParams("ForeignObject Equal", (s, t) => s.Where(x => x.Test == t), 10);
                yield return GetForeignObjectExpressionParams("ForeignObject Property Equal to", (s, t) => { string name = t.Name; return s.Where(x => x.Test.Name == name); } , 10);
            }
        }

        static object[] GetSimpleExpressionParams(string name, Expression<Func<E.SubTest, bool>> expression, int expectedCount)
        {
            return new object[] { name, expression, expectedCount };
        }

        static object[] GetForeignObjectExpressionParams(string name, Func<IQueryable<E.SubTest>, E.Test, IQueryable<E.SubTest>> function, int expectedCount)
        {
            return new object[] { name, function, expectedCount };
        }
        #endregion
    }
}
