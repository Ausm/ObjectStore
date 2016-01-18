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
        IQueryable<E.Test> _querryable;
        IQueryable<E.SubTest> _subQuerryable;

        public PreparedEntitiesTests(DatabaseFixture databaseFixture, InitEnitiesFixture initEntitiesFixture, ITestOutputHelper output)
        {
            _databaseFixture = databaseFixture;
            _output = output;

            initEntitiesFixture.Init(databaseFixture.ObjectProvider);
        }

        void TestSingleExpression(string name, Expression<Func<E.SubTest, bool>> expression, int expectedCount)
        {
            _output.WriteLine($"Test {name} expression");
            List<E.SubTest> subResult = _subQuerryable.Where(expression).ToList();
            Assert.Equal(expectedCount, subResult.Count);
            _output.WriteLine("... Done");
        }

        [Fact]
        public void TestBasicExpression()
        {
            _querryable = _databaseFixture.ObjectProvider.GetQueryable<E.Test>().ForceLoad();
            _subQuerryable = _databaseFixture.ObjectProvider.GetQueryable<E.SubTest>().ForceLoad();

            TestSingleExpression("Equal", x => x.First == x.Second, 2);
            TestSingleExpression("Equal to Null", x => x.Nullable == null, 18);
            TestSingleExpression("Unequal to Null", x => x.Nullable != null, 2);
            TestSingleExpression("Add", x => x.First + x.Second == 10, 20);
            TestSingleExpression("Subtract", x => x.First - x.Second == 2, 2);
            TestSingleExpression("Subtract", x => x.First - x.Second == 2, 2);
            TestSingleExpression("Greater", x => x.First > x.Second, 8);
            TestSingleExpression("GreaterEqual", x => x.First >= x.Second, 10);
            TestSingleExpression("Less", x => x.First < x.Second, 10);
            TestSingleExpression("LessEqual", x => x.First <= x.Second, 12);
            TestSingleExpression("ConstantValue", x => x.First == 5, 2);

            //TODO TestSingleExpression("ForeignObject", x => x.First == 5, 2);
        }


        //[Fact]
        //public void TestJoinExpression()
        //{
        //}

        //[Fact]
        //public void TestSubExpression()
        //{
        //}

    }
}
