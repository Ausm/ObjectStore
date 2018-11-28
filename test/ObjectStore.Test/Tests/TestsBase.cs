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
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Specialized;
using System.Xml.Linq;

namespace ObjectStore.Test.Tests
{
    public abstract class TestsBase
    {
        #region Fields
        IDatabaseFixture _databaseFixture;
        ITestOutputHelper _output;
        IQueryable<E.Test> _queryable;
        IQueryable<E.SubTest> _subQueryable;

        const string FirstRandomText = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.";
        const string SecondRandomText = "Duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum dolore eu feugiat nulla facilisis at vero eros et accumsan et iusto odio dignissim qui blandit praesent luptatum zzril delenit augue duis dolore te feugait nulla facilisi. Lorem ipsum dolor sit amet, consectetuer adipiscing elit, sed diam nonummy nibh euismod tincidunt ut laoreet dolore magna aliquam erat volutpat.";

        static readonly Guid FirstRandomGuid = Guid.NewGuid();
        static readonly Guid SecondRandomGuid = Guid.NewGuid();

        protected static readonly object[][] _subEntityData = new[] {
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

        protected static readonly object[][] _entityData = new[] {
            new object[] { 1, $"Testname {DateTime.Now:g}", FirstRandomText },
            new object[] { 2, $"Testname2 {DateTime.Now:g}", SecondRandomText }};

        protected static readonly object[][] _differentTypesEntityData = new[] {
            new object[] { 1, FirstRandomText, true, int.MaxValue, byte.MaxValue, short.MaxValue, long.MaxValue, new DateTime(9999, 12,31,23,59,59,999), FirstRandomGuid, FirstRandomGuid.ToByteArray(), 1234567890.12345m, new XElement("root", new XElement("sub1", "Value")) }
        };

        #endregion

        #region Constructor
        protected TestsBase(IDatabaseFixture databaseFixture, ITestOutputHelper output)
        {
            _databaseFixture = databaseFixture;
            _output = output;

            _databaseFixture.InitializeSupportedQueries(GetDefaultResult, GetColumnNames, GetQuerryPattern);

            _queryable = _databaseFixture.ObjectProvider.GetQueryable<E.Test>().ForceLoad();
            _subQueryable = _databaseFixture.ObjectProvider.GetQueryable<E.SubTest>().ForceLoad();
        }
        #endregion

        #region Tests
        #region Facts
        [Fact]
        public void TestSelect()
        {
            List<E.Test> result = Assert.ScriptCalled(_databaseFixture, Query.Select, () => _queryable.ToList()).OrderBy(x => x.Id).ToList();

            E.Test t = result.First();

            IQueryable<E.SubTest> queryable = _subQueryable.Where(x => x.Test == t);
            IQueryable<string> selectQueryable = queryable.Select(x => x.Name);

            List<string> selectResult = selectQueryable.ToList();
            List<E.SubTest> subResult = queryable.ToList();

            selectResult.Sort();

            Assert.Collection(selectResult,
                x => Assert.Equal("SubEntity0", x),
                x => Assert.Equal("SubEntity10", x),
                x => Assert.Equal("SubEntity12", x),
                x => Assert.Equal("SubEntity14", x),
                x => Assert.Equal("SubEntity16", x),
                x => Assert.Equal("SubEntity18", x),
                x => Assert.Equal("SubEntity2", x),
                x => Assert.Equal("SubEntity4", x),
                x => Assert.Equal("SubEntity6", x),
                x => Assert.Equal("SubEntity8", x));

            int removeCount = 0;

            NotifyCollectionChangedEventHandler collectionChangedHandler = (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Remove)
                    removeCount++;
            };

            try
            {
                ((INotifyCollectionChanged)selectQueryable).CollectionChanged += collectionChangedHandler;

                subResult.First().Test = result[1];

                Assert.Equal(1, removeCount);

                selectResult = selectQueryable.ToList();
                selectResult.Sort();

                Assert.Collection(selectResult,
                    x => Assert.Equal("SubEntity10", x),
                    x => Assert.Equal("SubEntity12", x),
                    x => Assert.Equal("SubEntity14", x),
                    x => Assert.Equal("SubEntity16", x),
                    x => Assert.Equal("SubEntity18", x),
                    x => Assert.Equal("SubEntity2", x),
                    x => Assert.Equal("SubEntity4", x),
                    x => Assert.Equal("SubEntity6", x),
                    x => Assert.Equal("SubEntity8", x));

                _subQueryable.DropChanges();

            }
            finally
            {
                ((INotifyCollectionChanged)selectQueryable).CollectionChanged -= collectionChangedHandler;
            }



        }

        [Fact]
        public void TestInsert()
        {
            List<E.Test> cachedItems = Assert.ScriptCalled(_databaseFixture, Query.Select, () => _databaseFixture.ObjectProvider.GetQueryable<E.Test>().ForceLoad().ToList());
            int newId = cachedItems.Count == 0 ? 1 : cachedItems.Max(x => x.Id) + 1;
            string name = $"Testname {DateTime.Now:g}";
            string description = FirstRandomText;

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
        public void TestInsertNonAutoInitializedKey()
        {
            List<E.NonInitializedKey> cachedItems = Assert.ScriptCalled(_databaseFixture, Query.SelectNonInitializedKeyEntitiy, () => _databaseFixture.ObjectProvider.GetQueryable<E.NonInitializedKey>().ForceLoad().ToList());
            int newId = cachedItems.Count == 0 ? 1 : cachedItems.Max(x => x.Id) + 1;

            _databaseFixture.SetResult(Query.InsertNonInitializedKeyEntitiy, new[] { new object[] { newId } });

            E.NonInitializedKey entity = _databaseFixture.ObjectProvider.CreateObject<E.NonInitializedKey>();
            Assert.NotNull(entity);
            entity.Id = newId;

            Assert.ScriptCalled(_databaseFixture, Query.InsertNonInitializedKeyEntitiy, () => _databaseFixture.ObjectProvider.GetQueryable<E.NonInitializedKey>().Where(x => x == entity).Save());
            Assert.Equal(entity.Id, newId);
        }

        [Fact]
        public void TestUpdate()
        {
            E.Test entity = Assert.ScriptCalled(_databaseFixture, Query.Select, () => _queryable.ToList().First());

            string oldString = entity.Description;
            string newString = oldString == FirstRandomText ? SecondRandomText : FirstRandomText;

            _databaseFixture.SetResult(Query.Update, new[] { new object[] { entity.Id, entity.Name, newString } });

            Assert.PropertyChanged((INotifyPropertyChanged)entity, nameof(E.Test.Description),
                () => entity.Description = newString);

            Assert.ScriptCalled(_databaseFixture, Query.Update, () => _databaseFixture.ObjectProvider.GetQueryable<E.Test>().Where(x => x == entity).Save());
        }

        [Fact]
        public void TestDeleteSingle()
        {
            E.Test entity = Assert.ScriptCalled(_databaseFixture, Query.Select, () => Assert.Single(_queryable.ToList().Where(x => x.Id == 1)));

            // Quick fix to prevent the test to run in to issue #14 problem, needs to be removed in the future.
            entity.SubTests.ToList().Select(x => x.Test).ToList();

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
            EventHandler<HitCommandEventArgs> blockHandler = (s, e) =>
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

            Assert.ScriptCalled(_databaseFixture, Query.OrderByDescending, () =>
               Assert.Collection(_subQueryable.Where(x => x.Test == t).OrderByDescending(x => x.Second),
                   x => Assert.Equal(1, x.Id),
                   x => Assert.Equal(2, x.Id),
                   x => Assert.Equal(3, x.Id),
                   x => Assert.Equal(4, x.Id),
                   x => Assert.Equal(5, x.Id),
                   x => Assert.Equal(6, x.Id),
                   x => Assert.Equal(7, x.Id),
                   x => Assert.Equal(8, x.Id),
                   x => Assert.Equal(9, x.Id),
                   x => Assert.Equal(10, x.Id)));
        }

        [Fact]
        public void TestCheckChanged()
        {
            List<string> recordedPropertyChanged = new List<string>();
            List<E.Test> elements = Assert.ScriptCalled(_databaseFixture, Query.Select, () => _queryable.ToList());

            PropertyChangedEventHandler propertyChangedHandler = (s, e) =>
            {
                recordedPropertyChanged.Add(e.PropertyName);
            };

            ((INotifyPropertyChanged)elements[0]).PropertyChanged += propertyChangedHandler;

            try
            {

                string originalDescription = elements[0].Description;
                string newDescription = elements[0].Description == FirstRandomText ? SecondRandomText : FirstRandomText;

                Assert.False(_queryable.CheckChanged());

                elements[0].Description = newDescription;

                Assert.True(_queryable.CheckChanged());

                elements[0].Description = originalDescription;

                Assert.False(_queryable.CheckChanged());

                elements[0].Description = newDescription;

                Assert.True(_queryable.CheckChanged());

                _queryable.DropChanges();

                Assert.False(_queryable.CheckChanged());


                Assert.Collection(recordedPropertyChanged,
                    x => Assert.Equal(nameof(E.Test.Description), x),
                    x => Assert.Equal(nameof(E.Test.Description), x),
                    x => Assert.Equal(nameof(E.Test.Description), x),
                    x => Assert.True(string.IsNullOrEmpty(x)),
                    x => Assert.True(string.IsNullOrEmpty(x)));

            }
            finally
            {
                ((INotifyPropertyChanged)elements[0]).PropertyChanged -= propertyChangedHandler;
            }

        }

        [Fact]
        public void TestTake()
        {
            List<E.SubTest> subResult = Assert.ScriptCalled(_databaseFixture, Query.SelectSubTake10, () => _subQueryable.OrderBy(x => x.Id).Take(10).ToList());

            Assert.Collection(subResult,
                x => Assert.Equal(1, x.Id),
                x => Assert.Equal(2, x.Id),
                x => Assert.Equal(3, x.Id),
                x => Assert.Equal(4, x.Id),
                x => Assert.Equal(5, x.Id),
                x => Assert.Equal(6, x.Id),
                x => Assert.Equal(7, x.Id),
                x => Assert.Equal(8, x.Id),
                x => Assert.Equal(9, x.Id),
                x => Assert.Equal(10, x.Id));
        }

        [Fact]
        public virtual void TestUsingDifferentDataTypes()
        {
            E.DifferentTypes entity = _databaseFixture.ObjectProvider.CreateObject<E.DifferentTypes>();

            entity.Text = FirstRandomText;
            entity.Boolean = true;
            entity.Int = int.MaxValue;
            entity.Byte = byte.MaxValue;
            entity.Short = short.MaxValue;
            entity.Long = long.MaxValue;
            entity.DateTime = DateTime.MaxValue;
            entity.Guid = FirstRandomGuid;
            entity.Binary = FirstRandomGuid.ToByteArray();
            entity.Decimal = 1234567890.12345m;
            entity.Xml = new XElement("root", new XElement("sub1", "Value"));

            Assert.ScriptCalled(_databaseFixture, Query.InsertDifferentTypesEntity, () => _databaseFixture.ObjectProvider.GetQueryable<E.DifferentTypes>().Save());

            Assert.Equal(FirstRandomText, entity.Text);
            Assert.True(entity.Boolean);
            Assert.Equal(int.MaxValue, entity.Int);
            Assert.Equal(byte.MaxValue, entity.Byte);
            Assert.Equal(short.MaxValue, entity.Short);
            Assert.Equal(long.MaxValue, entity.Long);
            Assert.InRange(entity.DateTime, DatabaseMaxDate, DateTime.MaxValue);
            Assert.Equal(FirstRandomGuid, entity.Guid);
            Assert.Equal(FirstRandomGuid.ToByteArray(), entity.Binary);
            Assert.Equal(1234567890.12345m, entity.Decimal);
            Assert.NotNull(entity.Xml.Element("sub1"));

            entity = Assert.ScriptCalled(_databaseFixture, Query.SelectDifferentTypesEntity, () => _databaseFixture.ObjectProvider.GetQueryable<E.DifferentTypes>().ForceLoad().ToList().First());

            entity.Text = SecondRandomText;
            entity.Boolean = false;
            entity.Int = int.MinValue;
            entity.Byte = byte.MinValue;
            entity.Short = short.MinValue;
            entity.Long = long.MinValue;
            entity.DateTime = DateTime.MinValue;
            entity.Guid = SecondRandomGuid;
            entity.Binary = SecondRandomGuid.ToByteArray();
            entity.Decimal = 9876543210.54321m;
            entity.Xml = new XElement("root", new XElement("sub2", "Value"));

            Assert.ScriptCalled(_databaseFixture, Query.UpdateDifferentTypesEntity, () => _databaseFixture.ObjectProvider.GetQueryable<E.DifferentTypes>().Save());

            Assert.Equal(SecondRandomText, entity.Text);
            Assert.False(entity.Boolean);
            Assert.Equal(int.MinValue, entity.Int);
            Assert.Equal(byte.MinValue, entity.Byte);
            Assert.Equal(short.MinValue, entity.Short);
            Assert.Equal(long.MinValue, entity.Long);
            Assert.InRange(entity.DateTime, DateTime.MinValue, DatabaseMinDate);
            Assert.Equal(SecondRandomGuid, entity.Guid);
            Assert.Equal(SecondRandomGuid.ToByteArray(), entity.Binary);
            Assert.Equal(9876543210.54321m, entity.Decimal);
            Assert.NotNull(entity.Xml.Element("sub2"));

        }

        [Fact]
        public virtual void TestDifferentWritabilityLevels()
        {
            E.DifferentWritabilityLevels entity = _databaseFixture.ObjectProvider.CreateObject<E.DifferentWritabilityLevels>();

            entity.Writeable = 5;
            entity.Insertable = 10;
            entity.Updateable = 15;

            Assert.ScriptCalled(_databaseFixture, Query.InsertDifferentWritabilityLevels, () => _databaseFixture.ObjectProvider.GetQueryable<E.DifferentWritabilityLevels>().Save());

            Assert.Equal(5, entity.Writeable);
            Assert.Equal(10, entity.Insertable);
            Assert.Equal(1, entity.Updateable);
            Assert.Equal(1, entity.Readonly);

            entity = Assert.ScriptCalled(_databaseFixture, Query.SelectDifferentWritabilityLevels, () => _databaseFixture.ObjectProvider.GetQueryable<E.DifferentWritabilityLevels>().ToList().First());

            entity.Writeable = 20;
            entity.Insertable = 25;
            entity.Updateable = 30;

            Assert.ScriptCalled(_databaseFixture, Query.UpdateDifferentWritabilityLevels, () => _databaseFixture.ObjectProvider.GetQueryable<E.DifferentWritabilityLevels>().Save());

            Assert.Equal(20, entity.Writeable);
            Assert.Equal(10, entity.Insertable);
            Assert.Equal(30, entity.Updateable);
            Assert.Equal(2, entity.Readonly);
        }

        [Fact]
        public void TestInsertForeignObjectKeyEntity()
        {
            E.Test testKey = Assert.ScriptCalled(_databaseFixture, Query.Select, () => _databaseFixture.ObjectProvider.GetQueryable<E.Test>().ForceLoad().ToList()).Where(x => x.Id != 1).First();
            string text = FirstRandomText;

            _databaseFixture.SetResult(Query.InsertForeignObjectKeyEntity, new[] { new object[] { testKey.Id, text } });

            E.ForeignObjectKey entity = _databaseFixture.ObjectProvider.CreateObject<E.ForeignObjectKey>();
            Assert.NotNull(entity);
            entity.Test = testKey;
            entity.Value = text;

            Assert.ScriptCalled(_databaseFixture, Query.InsertForeignObjectKeyEntity, () => _databaseFixture.ObjectProvider.GetQueryable<E.ForeignObjectKey>().Where(x => x == entity).Save());
        }

        [Fact]
        public void TestUpdateForeignObjectKeyEntities()
        {
            E.Test testKey = Assert.ScriptCalled(_databaseFixture, Query.Select, () => _databaseFixture.ObjectProvider.GetQueryable<E.Test>().ForceLoad().ToList()).Where(x => x.Id == 1).First();
            string text = FirstRandomText;

            _databaseFixture.SetResult(Query.UpdateForeignObjectKeyEntity, new[] { new object[] { testKey.Id, text } });

            E.ForeignObjectKey entity = _databaseFixture.ObjectProvider.GetQueryable<E.ForeignObjectKey>().Where(x => x.Test == testKey).ForceLoad().FirstOrDefault();
            Assert.NotNull(entity);
            entity.Value = text;

            Assert.ScriptCalled(_databaseFixture, Query.UpdateForeignObjectKeyEntity, () => _databaseFixture.ObjectProvider.GetQueryable<E.ForeignObjectKey>().Where(x => x == entity).Save());
        }

        [Fact]
        public void TestDeleteForeignObjectKeyEntities()
        {
            E.Test testKey = Assert.ScriptCalled(_databaseFixture, Query.Select, () => _databaseFixture.ObjectProvider.GetQueryable<E.Test>().ForceLoad().ToList()).Where(x => x.Id == 1).First();

            E.ForeignObjectKey entity = _databaseFixture.ObjectProvider.GetQueryable<E.ForeignObjectKey>().Where(x => x.Test == testKey).FirstOrDefault();
            Assert.NotNull(entity);
            _databaseFixture.ObjectProvider.GetQueryable<E.ForeignObjectKey>().Where(x => x == entity).Delete();

            Assert.ScriptCalled(_databaseFixture, Query.DeleteForeignObjectKeyEntity, () => _databaseFixture.ObjectProvider.GetQueryable<E.ForeignObjectKey>().Where(x => x == entity).Save());
        }

        #endregion

        #region ExtTheories
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
        #endregion

        #region Methods
        protected abstract string GetQuerryPattern(Query key);

        protected virtual IEnumerable<object[]> GetDefaultResult(Query key)
        {
            switch (key)
            {
                case Query.Insert:
                    return new[] { new object[] { 1, $"Testname {DateTime.Now:g}", FirstRandomText } };
                case Query.SelectDifferentTypesEntity:
                case Query.InsertDifferentTypesEntity:
                    return new[] { new object[] { 1, FirstRandomText, true, int.MaxValue, byte.MaxValue, short.MaxValue, long.MaxValue, new DateTime(9999, 12,31,23,59,59,999), FirstRandomGuid, FirstRandomGuid.ToByteArray(), 1234567890.12345m, new XElement("root", new XElement("sub1", "Value")) } };
                case Query.SelectDifferentWritabilityLevels:
                case Query.InsertDifferentWritabilityLevels:
                    return new[] { new object[] { 1, 5, 1, 10, 1 } };
                case Query.UpdateDifferentWritabilityLevels:
                    return new[] { new object[] { 1, 20, 30, 10, 2 } };
                case Query.Update:
                    return new[] { new object[] { 1, $"Testname {DateTime.Now:g}", SecondRandomText } };
                case Query.UpdateDifferentTypesEntity:
                    return new[] { new object[] { 1, SecondRandomText, false, int.MinValue, byte.MinValue, short.MinValue, long.MinValue, new DateTime(1753, 1, 1), SecondRandomGuid, SecondRandomGuid.ToByteArray(), 9876543210.54321m, new XElement("root", new XElement("sub2", "Value")) } };
                case Query.Delete:
                    return Enumerable.Empty<object[]>();
                case Query.DeleteSub:
                    return Enumerable.Empty<object[]>();
                case Query.Select:
                    return GetEntitys(1, 2);
                case Query.SelectSub:
                    return GetSubEntitys(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
                case Query.SelectSubTake10:
                    return GetSubEntitys(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
                case Query.SelectForeignObjectKeyEntity:
                    return new[] { new object[] { 1, "Testentry" } };
                case Query.OrderBy:
                    return GetSubEntitys(10, 9, 8, 7, 6, 5, 4, 3, 2, 1);
                case Query.OrderByDescending:
                    return GetSubEntitys(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
                case Query.SimpleExpressionEqual:
                    return GetSubEntitys(6, 16);
                case Query.SimpleExpressionUnequal:
                    return GetSubEntitys(1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 17, 18, 19, 20);
                case Query.SimpleExpressionEqualToNull:
                    return GetSubEntitys(1, 2, 3, 4, 5, 6, 7, 9, 10, 11, 12, 13, 14, 15, 16, 17, 19, 20);
                case Query.SimpleExpressionUnequalToNull:
                    return GetSubEntitys(8, 18);
                case Query.SimpleExpressionAdd:
                    return GetSubEntitys(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
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
                case Query.SimpleExpressionOr:
                    return GetSubEntitys(4, 8, 14, 18);
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
                case Query.SelectForeignObjectKeyEntity:
                case Query.InsertForeignObjectKeyEntity:
                case Query.UpdateForeignObjectKeyEntity:
                    return new string[] { "Id", "Value" };
                case Query.SelectDifferentTypesEntity:
                case Query.InsertDifferentTypesEntity:
                case Query.UpdateDifferentTypesEntity:
                    return new string[] { "Id", "Text", "Boolean", "Int", "Byte", "Short", "Long", "DateTime", "Guid", "Binary", "Decimal", "Xml" };
                case Query.SelectDifferentWritabilityLevels:
                case Query.InsertDifferentWritabilityLevels:
                case Query.UpdateDifferentWritabilityLevels:
                    return new string[] { "Id", "Writeable", "Updateable", "Insertable", "Readonly" };
                case Query.SelectNonInitializedKeyEntitiy:
                case Query.InsertNonInitializedKeyEntitiy:
                    return new string[] { "Id" };
                case Query.Insert:
                case Query.Update:
                case Query.Select:
                    return new string[] { "Id", "Name", "Description" };
                case Query.SelectSub:
                case Query.SelectSubTake10:
                case Query.OrderBy:
                case Query.OrderByDescending:
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
                case Query.SimpleExpressionOr:
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

        #region Properties
        protected virtual DateTime DatabaseMaxDate
        {
            get
            {
                return DateTime.MaxValue;
            }
        }

        protected virtual DateTime DatabaseMinDate
        {
            get
            {
                return DateTime.MinValue;
            }
        }
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
                returnValue.Add(Query.SimpleExpressionOr, x => x.First == 3 || x.First == 7);
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
