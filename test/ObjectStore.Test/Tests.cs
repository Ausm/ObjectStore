using Xunit;
using System;
using System.Linq;
using ObjectStore.Test.Fixtures;
using Xunit.Abstractions;
using E = ObjectStore.Test.Entities;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.ComponentModel;
using System.Threading;
using ObjectStore.Test.Resources;

namespace ObjectStore.Test
{
    public class Tests : IClassFixture<InitEnitiesFixture>, IClassFixture<DatabaseFixture>
    {
        #region Fields
        DatabaseFixture _databaseFixture;
        ITestOutputHelper _output;
        IQueryable<E.Test> _queryable;
        IQueryable<E.SubTest> _subQueryable;
        EventWaitHandle _waithandle;
        #endregion

        #region Constructor
        public Tests(DatabaseFixture databaseFixture, InitEnitiesFixture initEntitiesFixture, ITestOutputHelper output)
        {
            _databaseFixture = databaseFixture;
            _output = output;

            initEntitiesFixture.Init(databaseFixture.ObjectProvider);

            _queryable = _databaseFixture.ObjectProvider.GetQueryable<E.Test>().ForceLoad();
            _subQueryable = _databaseFixture.ObjectProvider.GetQueryable<E.SubTest>().ForceLoad();
            _waithandle = new ManualResetEvent(false);
        }
        #endregion

        #region Tests
        [Fact]
        public void TestInsertUpdateDelete()
        {
            E.Test entity = CreateTestEntity($"Testname {DateTime.Now:g}", Resource.FirstRandomText);
            _databaseFixture.ObjectProvider.GetQueryable<E.Test>().Where(x => x == entity).Save();
            Assert.True(_waithandle.WaitOne(5000));
            Assert.NotEqual(entity.Id, 0);
            _output.WriteLine($"First entity saved, new Id: {entity.Id} -> passed");

            E.Test entity2 = CreateTestEntity($"Testname {DateTime.Now:g}", Resource.SecondRandomText);
            _waithandle.Reset();
            _databaseFixture.ObjectProvider.GetQueryable<E.Test>().Where(x => x == entity2).Save();
            Assert.True(_waithandle.WaitOne(5000));
            _output.WriteLine($"Second entity saved, new Id: {entity2.Id} -> passed");

            entity.Description = Resource.SecondRandomText;
            _databaseFixture.ObjectProvider.GetQueryable<E.Test>().Where(x => x == entity).Save();
            _output.WriteLine($"First entity updated and saved, Id: {entity2.Id} -> passed");

            int id = entity.Id;
            int count = _databaseFixture.ObjectProvider.GetQueryable<E.Test>().Count();
            IQueryable<E.Test> queryable = _databaseFixture.ObjectProvider.GetQueryable<E.Test>().Where(x => x == entity);
            queryable.Delete();
            queryable.Save();
            Assert.Empty(_databaseFixture.ObjectProvider.GetQueryable<E.Test>().Where(x => x.Id == id));
            Assert.Equal(count -1, _databaseFixture.ObjectProvider.GetQueryable<E.Test>().Count());
            _output.WriteLine($"Deleted entity, Id: {entity2.Id} -> passed");

            queryable = _databaseFixture.ObjectProvider.GetQueryable<E.Test>();
            queryable.Delete();
            queryable.Save();
            Assert.Empty(_databaseFixture.ObjectProvider.GetQueryable<E.Test>());
            _output.WriteLine($"Deleted all entities -> passed");
        }

        [ExtTheory, MemberData(nameof(SimpleExpressions))]
        public void TestSimpleExpression(string name, Expression<Func<E.SubTest, bool>> expression, int expectedCount)
        {
            _output.WriteLine($"Test {name} expression");
            List<E.SubTest> subResult = _subQueryable.Where(expression).ToList();
            Assert.Equal(expectedCount, subResult.Count);
            _output.WriteLine("... Done");
        }

        [ExtTheory, MemberData(nameof(ForeignObjectExpressions))]
        public void TestForeignObjectExpression(string name, Func<IQueryable<E.SubTest>, E.Test, IQueryable<E.SubTest>> function, int expectedCount)
        {
            _output.WriteLine($"Test {name} expression");
            List<E.SubTest> subResult = function(_subQueryable, _queryable.FirstOrDefault()).ToList();
            Assert.Equal(expectedCount, subResult.Count);
            _output.WriteLine("... Done");
        }
        #endregion

        #region Methods
        E.Test CreateTestEntity(string name, string description)
        {
            E.Test entity = _databaseFixture.ObjectProvider.CreateObject<E.Test>();
            Assert.NotNull(entity);
            entity.Name = name;
            entity.Description = description;

            _output.WriteLine($"Entity created, Name: {entity.Name}");

            ((INotifyPropertyChanged)entity).PropertyChanged += (s, e) => {
                _output.WriteLine($"Property Changed on Entity - Id:{((E.Test)s).Id}, PropertyName: {e.PropertyName}");
                if (e.PropertyName == nameof(E.Test.Id))
                    _waithandle.Set();
            };
            return entity;
        }
        #endregion

        #region MemberData Definitions
        public static TheoryData<string, Expression<Func<E.SubTest, bool>>, int> SimpleExpressions
        {
            get
            {
                TheoryData<string, Expression<Func<E.SubTest, bool>>, int> returnValue = new TheoryData<string, Expression<Func<E.SubTest, bool>>, int>();
                returnValue.Add("Equal", x => x.First == x.Second, 2);
                returnValue.Add("Equal to Null", x => x.Nullable == null, 18);
                returnValue.Add("Unequal to Null", x => x.Nullable != null, 2);
                returnValue.Add("Add", x => x.First + x.Second == 10, 20);
                returnValue.Add("Subtract", x => x.First - x.Second == 2, 2);
                returnValue.Add("Greater", x => x.First > x.Second, 8);
                returnValue.Add("GreaterEqual", x => x.First >= x.Second, 10);
                returnValue.Add("Less", x => x.First < x.Second, 10);
                returnValue.Add("LessEqual", x => x.First <= x.Second, 12);
                returnValue.Add("ConstantValue", x => x.First == 5, 2);
                returnValue.Add("Contains", x => new int[] { 2, 5, 7 }.Contains(x.First), 6);
                return returnValue;
            }
        }

        public static TheoryData<string, Func<IQueryable<E.SubTest>, E.Test, IQueryable<E.SubTest>>, int> ForeignObjectExpressions
        {
            get
            {
                TheoryData<string, Func<IQueryable<E.SubTest>, E.Test, IQueryable<E.SubTest>>, int> returnValue = new TheoryData<string, Func<IQueryable<E.SubTest>, E.Test, IQueryable<E.SubTest>>, int>();
                returnValue.Add("ForeignObject Equal", (s, t) => s.Where(x => x.Test == t), 10);
                returnValue.Add("ForeignObject Property Equal to", (s, t) => { string name = t.Name; return s.Where(x => x.Test.Name == name); } , 10);
                return returnValue;
            }
        }
        #endregion
    }
}
