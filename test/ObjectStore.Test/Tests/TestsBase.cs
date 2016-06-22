using Xunit;
using Assert = ObjectStore.Test.Common.Assert;
using System;
using System.Linq;
using ObjectStore.Test.Fixtures;
using Xunit.Abstractions;
using E = ObjectStore.Test.Entities;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.ComponentModel;
using ObjectStore.Test.Resources;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Specialized;

namespace ObjectStore.Test.Tests
{
    public abstract class TestsBase
    {
        #region Fields
        DatabaseFixture _databaseFixture;
        ITestOutputHelper _output;
        IQueryable<E.Test> _queryable;
        IQueryable<E.SubTest> _subQueryable;

        static readonly object[][] _subEntityData = new[] {
            new object[] { 1, 1, "SubEntity0", 0, 10, DBNull.Value},
            new object[] { 2, 1, "SubEntity2", 1, 9, DBNull.Value},
            new object[] { 3, 1, "SubEntity4", 2, 8, DBNull.Value},
            new object[] { 4, 1, "SubEntity6", 3, 7, DBNull.Value},
            new object[] { 5, 1, "SubEntity8", 4, 6, DBNull.Value},
            new object[] { 6, 1, "SubEntity10", 5, 5, DBNull.Value},
            new object[] { 7, 1, "SubEntity12", 6, 4, DBNull.Value},
            new object[] { 8, 1, "SubEntity14", 7, 3, DateTime.Now},
            new object[] { 9, 1, "SubEntity16", 8, 2, DBNull.Value},
            new object[] { 10, 1, "SubEntity18", 9, 1, DBNull.Value},
            new object[] { 11, 2, "SubEntity1", 0, 10, DBNull.Value},
            new object[] { 12, 2, "SubEntity3", 1, 9, DBNull.Value},
            new object[] { 13, 2, "SubEntity5", 2, 8, DBNull.Value},
            new object[] { 14, 2, "SubEntity7", 3, 7, DBNull.Value},
            new object[] { 15, 2, "SubEntity9", 4, 6, DBNull.Value},
            new object[] { 16, 2, "SubEntity11", 5, 5, DBNull.Value},
            new object[] { 17, 2, "SubEntity13", 6, 4, DBNull.Value},
            new object[] { 18, 2, "SubEntity15", 7, 3, DateTime.Now},
            new object[] { 19, 2, "SubEntity17", 8, 2, DBNull.Value},
            new object[] { 20, 2, "SubEntity19", 9, 1, DBNull.Value}};

        static readonly object[][] _entityData = new[] {
            new object[] { 1, $"Testname {DateTime.Now:g}", Resource.FirstRandomText },
            new object[] { 2, $"Testname2 {DateTime.Now:g}", Resource.SecondRandomText }};
        #endregion

        #region Constructor
        public TestsBase(DatabaseFixture databaseFixture, ITestOutputHelper output)
        {
            _databaseFixture = databaseFixture;
            _output = output;

            _databaseFixture.InitializeSupportedQueries(GetDefaultResult, GetColumnNames, GetQuerryPattern);

            _queryable = _databaseFixture.ObjectProvider.GetQueryable<E.Test>().ForceLoad();
            _subQueryable = _databaseFixture.ObjectProvider.GetQueryable<E.SubTest>().ForceLoad();
        }
        #endregion

        #region Tests

        [Fact]
        public void TestInsert()
        {
            List<E.Test> cachedItems = _databaseFixture.ObjectProvider.GetQueryable<E.Test>().ForceCache().ToList();
            int newId = cachedItems.Count == 0 ? 1 : cachedItems.Max(x => x.Id) + 1;
            string name = $"Testname {DateTime.Now:g}";
            string description = Resource.FirstRandomText;

            _databaseFixture.SetResult(Query.Insert, new[] { new object[] { newId, name, description } });

            E.Test entity = _databaseFixture.ObjectProvider.CreateObject<E.Test>();
            Assert.NotNull(entity);
            entity.Name = name;
            entity.Description = description;

            _output.WriteLine($"Entity created, Name: {entity.Name}");

            Assert.PropertyChanged((INotifyPropertyChanged)entity, nameof(E.Test.Id), 
                () => Assert.ScriptCalled(_databaseFixture, Query.Insert, () => _databaseFixture.ObjectProvider.GetQueryable<E.Test>().Where(x => x == entity).Save()));

            Assert.Equal(entity.Id, newId);
            _output.WriteLine($"First entity saved, new Id: {entity.Id} -> passed");
        }

        [Fact]
        public void TestUpdate()
        {
            _databaseFixture.SetResult(Query.Select, GetEntitys(1));

            E.Test entity = Assert.ScriptCalled(_databaseFixture, Query.Select, () => Assert.Single(_queryable));

            _databaseFixture.SetResult(Query.Update, new[] { new object[] { entity.Id, entity.Name, Resource.SecondRandomText } });

            Assert.PropertyChanged((INotifyPropertyChanged)entity, nameof(E.Test.Description),
                () => entity.Description = Resource.SecondRandomText);

            Assert.ScriptCalled(_databaseFixture, Query.Update, () => _databaseFixture.ObjectProvider.GetQueryable<E.Test>().Where(x => x == entity).Save());
        }

        [Fact]
        public void TestDeleteSingle()
        {
            E.Test entity = Assert.ScriptCalled(_databaseFixture, Query.Select, () => Assert.Single(_queryable.ToList().Where(x => x.Id == 1)));

            IQueryable<E.Test> queryable = _databaseFixture.ObjectProvider.GetQueryable<E.Test>().Where(x => x == entity);
            queryable.Delete();

            Assert.ScriptCalled(_databaseFixture, Query.Delete, () => queryable.Save());
        }

        [Fact]
        public void TestDeleteAll()
        {
            Assert.ScriptCalled(_databaseFixture, Query.Select, () => _queryable.ToList());
            _queryable.Delete();

            Assert.ScriptsCalled(_databaseFixture, () => _queryable.Save(), 2, Query.Delete);
        }

        [Fact]
        public async Task TestBeginFetch()
        {
            int collectionChangedCounter = 0;
            List<Query> hittedCommands = new List<Query>();
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            ManualResetEvent manualResetEvent2 = new ManualResetEvent(false);
            EventHandler<DatabaseFixture.HitCommandEventArgs> blockHandler = (s, e) =>
            {
                hittedCommands.Add(e.Key);
                manualResetEvent2.Set();
                manualResetEvent.WaitOne();
            };

            NotifyCollectionChangedEventHandler collectionChangedHandler = (s, e) =>
            {
                lock (this)
                {
                    collectionChangedCounter++;
                }
            };

            IQueryable<E.Test> testQueryable = _databaseFixture.ObjectProvider.GetQueryable<E.Test>();
            IQueryable<E.SubTest> subTestQueryable = _databaseFixture.ObjectProvider.GetQueryable<E.SubTest>();


            _databaseFixture.HitCommand += blockHandler;
            try
            {

                ((INotifyCollectionChanged)testQueryable).CollectionChanged += collectionChangedHandler;
                ((INotifyCollectionChanged)subTestQueryable).CollectionChanged += collectionChangedHandler;

                Task[] tasks = new Task[] {
                        subTestQueryable.FetchAsync(),
                        testQueryable.FetchAsync(),
                        subTestQueryable.FetchAsync()};

                manualResetEvent2.WaitOne();
                Assert.Collection(hittedCommands,
                    x => Assert.Equal(x, Query.SelectSub));

                manualResetEvent.Set();

                await tasks[0];
                await tasks[1];
                await tasks[2];

                Assert.Collection(hittedCommands,
                    x => Assert.Equal(x, Query.SelectSub),
                    x => Assert.Equal(x, Query.Select),
                    x => Assert.Equal(x, Query.SelectSub));

                Assert.InRange(collectionChangedCounter, 20, int.MaxValue);
            }
            finally
            {
                _databaseFixture.HitCommand -= blockHandler;
                ((INotifyCollectionChanged)testQueryable).CollectionChanged -= collectionChangedHandler;
                ((INotifyCollectionChanged)subTestQueryable).CollectionChanged -= collectionChangedHandler;
            }
        }

        [Fact]
        public void TestOrderBy()
        {
            E.Test t = Assert.Single(Assert.ScriptCalled(_databaseFixture, Query.Select, () => _queryable.ToList().Where(x => x.Id == 1)));

            Assert.ScriptCalled(_databaseFixture, Query.OrderBy, () =>
               Assert.Collection(_subQueryable.Where(x => x.Test == t).OrderBy(x => x.Second),
                   x => Assert.Equal(10, x.Id),
                   x => Assert.Equal(9, x.Id),
                   x => Assert.Equal(8, x.Id),
                   x => Assert.Equal(7, x.Id),
                   x => Assert.Equal(6, x.Id),
                   x => Assert.Equal(5, x.Id),
                   x => Assert.Equal(4, x.Id),
                   x => Assert.Equal(3, x.Id),
                   x => Assert.Equal(2, x.Id),
                   x => Assert.Equal(1, x.Id)));
        }

        [ExtTheory, MemberData(nameof(SimpleExpressions))]
        public void TestSimpleExpression(Query query, Expression<Func<E.SubTest, bool>> expression)
        {
            _output.WriteLine($"Test {query} expression");
            List<E.SubTest> subResult = Assert.ScriptCalled(_databaseFixture, query, () => _subQueryable.Where(expression).ToList());
            Assert.Equal(GetDefaultResult(query).Count(), subResult.Count);
            _output.WriteLine("... Done");
        }

        [ExtTheory, MemberData(nameof(ForeignObjectExpressions))]
        public void TestForeignObjectExpression(Query query, Func<IQueryable<E.SubTest>, E.Test, IQueryable<E.SubTest>> function, string queryPattern, IEnumerable<object[]> values)
        {
            _output.WriteLine($"Test {query} expression");

            E.Test t = Assert.Single(Assert.ScriptCalled(_databaseFixture, Query.Select, () => _queryable.ToList().Where(x => x.Id == 1)));

            List<E.SubTest> subResult = Assert.ScriptCalled(_databaseFixture, query, () => function(_subQueryable, t).ToList());
            Assert.Equal(values.Count(), subResult.Count);
            _output.WriteLine("... Done");
        }
        #endregion

        #region Methods
        protected abstract string GetQuerryPattern(Query key);

        IEnumerable<object[]> GetDefaultResult(Query key)
        {
            switch (key)
            {
                case Query.Insert:
                    return new[] { new object[] { 1, $"Testname {DateTime.Now:g}", Resource.FirstRandomText } };
                case Query.Update:
                    return new[] { new object[] { 1, $"Testname {DateTime.Now:g}", Resource.SecondRandomText } };
                case Query.Delete:
                    return Enumerable.Empty<object[]>();
                case Query.DeleteSub:
                    return Enumerable.Empty<object[]>();
                case Query.Select:
                    return GetEntitys(1, 2);
                case Query.SelectSub:
                    return GetSubEntitys(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
                case Query.OrderBy:
                    return GetSubEntitys(10, 9, 8, 7, 6, 5, 4, 3, 2, 1);
                case Query.SimpleExpressionEqual:
                    return GetSubEntitys(6, 16);
                case Query.SimpleExpressionUnequal:
                    return GetSubEntitys(1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 17, 18, 19, 20);
                case Query.SimpleExpressionEqualToNull:
                    return GetSubEntitys(1, 2, 3, 4, 5, 6, 7, 9, 10, 11, 12, 13, 14, 15, 16, 17, 19, 20);
                case Query.SimpleExpressionUnequalToNull:
                    return GetSubEntitys(8, 18);
                case Query.SimpleExpressionAdd:
                    return GetSubEntitys(1, 2, 3, 4, 5, 6, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
                case Query.SimpleExpressionSubtract:
                    return GetSubEntitys(7, 17);
                case Query.SimpleExpressionGreater:
                    return GetSubEntitys(7, 8, 9, 10, 17, 18, 19, 20);
                case Query.SimpleExpressionGreaterEqual:
                    return GetSubEntitys(6, 7, 8, 9, 10, 16, 17, 18, 19, 20);
                case Query.SimpleExpressionLess:
                    return GetSubEntitys(1, 2, 3, 4, 5, 11, 12, 13, 14, 15);
                case Query.SimpleExpressionLessEqual:
                    return GetSubEntitys(1, 2, 3, 4, 5, 6, 11, 12, 13, 14, 15, 16);
                case Query.SimpleExpressionConstantValue:
                    return GetSubEntitys(6, 16);
                case Query.SimpleExpressionContains:
                    return GetSubEntitys(3, 6, 8, 13, 16, 18);
                case Query.SimpleExpressionAnd:
                    return GetSubEntitys(4, 14);
                case Query.ForeignObjectEqual:
                    return GetSubEntitys(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
                case Query.ForeignObjectPropertyEqualTo:
                    return GetSubEntitys(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
                default:
                    return Enumerable.Empty<object[]>();
            }
        }

        string[] GetColumnNames(Query key)
        {
            switch (key)
            {
                case Query.Insert:
                case Query.Update:
                case Query.Select:
                    return new string[] { "Id", "Name", "Description" };
                case Query.SelectSub:
                case Query.OrderBy:
                case Query.SimpleExpressionEqual:
                case Query.SimpleExpressionUnequal:
                case Query.SimpleExpressionEqualToNull:
                case Query.SimpleExpressionUnequalToNull:
                case Query.SimpleExpressionAdd:
                case Query.SimpleExpressionSubtract:
                case Query.SimpleExpressionGreater:
                case Query.SimpleExpressionGreaterEqual:
                case Query.SimpleExpressionLess:
                case Query.SimpleExpressionLessEqual:
                case Query.SimpleExpressionConstantValue:
                case Query.SimpleExpressionContains:
                case Query.SimpleExpressionAnd:
                case Query.ForeignObjectEqual:
                case Query.ForeignObjectPropertyEqualTo:
                    return new string[] { "Id", "Test", "Name", "First", "Second", "Nullable" };
                case Query.Delete:
                case Query.DeleteSub:
                default:
                    return new string[0];
            }
        }

        #region Static Methods
        static IEnumerable<object[]> GetSubEntitys(params int[] ids)
        {
            foreach (int id in ids)
                yield return _subEntityData[id - 1];
        }

        static IEnumerable<object[]> GetEntitys(params int[] ids)
        {
            foreach (int id in ids)
                yield return _entityData[id - 1];
        }
        #endregion
        #endregion

        #region MemberData Definitions
        public static TheoryData<Query, Expression<Func<E.SubTest, bool>>> SimpleExpressions
        {
            get
            {
                TheoryData<Query, Expression<Func<E.SubTest, bool>>> returnValue = new TheoryData<Query, Expression<Func<E.SubTest, bool>>>();
                returnValue.Add(Query.SimpleExpressionEqual, x => x.First == x.Second);
                returnValue.Add(Query.SimpleExpressionUnequal, x => x.First != x.Second);
                returnValue.Add(Query.SimpleExpressionEqualToNull, x => x.Nullable == null);
                returnValue.Add(Query.SimpleExpressionEqualToNull, x => null == x.Nullable);
                returnValue.Add(Query.SimpleExpressionUnequalToNull, x => x.Nullable != null);
                returnValue.Add(Query.SimpleExpressionUnequalToNull, x => null != x.Nullable);
                returnValue.Add(Query.SimpleExpressionAdd, x => x.First + x.Second == 10);
                returnValue.Add(Query.SimpleExpressionSubtract, x => x.First - x.Second == 2);
                returnValue.Add(Query.SimpleExpressionGreater, x => x.First > x.Second);
                returnValue.Add(Query.SimpleExpressionGreaterEqual, x => x.First >= x.Second);
                returnValue.Add(Query.SimpleExpressionLess, x => x.First < x.Second);
                returnValue.Add(Query.SimpleExpressionLessEqual, x => x.First <= x.Second);
                returnValue.Add(Query.SimpleExpressionConstantValue, x => x.First == 5);
                returnValue.Add(Query.SimpleExpressionContains, x => new int[] { 2, 5, 7 }.Contains(x.First));
                returnValue.Add(Query.SimpleExpressionAnd, x => x.First == 3 && x.Second == 7);
                return returnValue;
            }
        }

        public static TheoryData<Query, Func<IQueryable<E.SubTest>, E.Test, IQueryable<E.SubTest>>, string, IEnumerable<object[]>> ForeignObjectExpressions
        {
            get
            {
                TheoryData<Query, Func<IQueryable<E.SubTest>, E.Test, IQueryable<E.SubTest>>, string, IEnumerable<object[]>> returnValue = new TheoryData<Query, Func<IQueryable<E.SubTest>, E.Test, IQueryable<E.SubTest>>, string, IEnumerable<object[]>>();
                returnValue.Add(Query.ForeignObjectEqual,
                    (s, t) => s.Where(x => x.Test == t), @"WHERE\s+\k<T>\.Test\s*=\s*@param\d+", GetSubEntitys(1, 2, 3, 4, 5, 6, 7, 8, 9, 10));
                returnValue.Add(Query.ForeignObjectPropertyEqualTo,
                    (s, t) => { string name = t.Name; return s.Where(x => x.Test.Name == name); }, @"LEFT\s+(OUTER\s+)?JOIN\s+dbo\.TestTable\s(?<T2>T\d+)\s+ON\s+\k<T2>\.Id\s*=\s*\k<T>\.Test\s+WHERE\s+\k<T2>\.\[Name]\s*=\s*@param\d+", GetSubEntitys(1, 2, 3, 4, 5, 6, 7, 8, 9, 10));
                return returnValue;
            }
        }
        #endregion
    }
}
