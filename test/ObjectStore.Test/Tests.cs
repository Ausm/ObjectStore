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
        public Tests(DatabaseFixture databaseFixture, ITestOutputHelper output)
        {
            _databaseFixture = databaseFixture;
            _output = output;

            _queryable = _databaseFixture.ObjectProvider.GetQueryable<E.Test>().ForceLoad();
            _subQueryable = _databaseFixture.ObjectProvider.GetQueryable<E.SubTest>().ForceLoad();
            _waithandle = new ManualResetEvent(false);
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

            _databaseFixture.AddSupportedQuery("Insert", @"^INSERT\s+dbo\.TestTable\s*\(\[Name],\s*\[Description]\)\s*VALUES\s*\(@param\d+,\s*@param\d+\)\s*SET\s+(?<P>@param\d+)\s*=\s*ISNULL\(SCOPE_IDENTITY\(\),\s*@@IDENTITY\)\s*SELECT\s+Id,\s*\[Name],\s*\[Description]\s+FROM\s+dbo\.TestTable\s+WHERE\s+\k<P>\s*=\s*Id$",
                new string[] { "Id", "Name", "Description" }, new[] {
                new object[] { newId, name, description }
                });

            E.Test entity = _databaseFixture.ObjectProvider.CreateObject<E.Test>();
            Assert.NotNull(entity);
            entity.Name = name;
            entity.Description = description;

            _output.WriteLine($"Entity created, Name: {entity.Name}");

            Assert.PropertyChanged((INotifyPropertyChanged)entity, nameof(E.Test.Id), 
                () => _databaseFixture.ObjectProvider.GetQueryable<E.Test>().Where(x => x == entity).Save());

            Assert.Equal(entity.Id, newId);
            _output.WriteLine($"First entity saved, new Id: {entity.Id} -> passed");
        }

        [Fact]
        public void TestUpdate()
        {
            _databaseFixture.AddSupportedQuery("Select", @"^\s*SELECT\s+(?<T>T\d+)\.Id,\s*\k<T>\.\[Name],\s*\k<T>\.\[Description]\s+FROM\s+dbo\.TestTable\s+\k<T>\s*$",
                new string[] { "Id", "Name", "Description" }, new[] {
                new object[] { 1, $"Testname {DateTime.Now:g}", Resource.FirstRandomText }
                });

            E.Test entity = Assert.Single(_queryable);

            _databaseFixture.AddSupportedQuery("Update", @"^\s*UPDATE\s+dbo\.TestTable\s+SET\s+\[Description]\s*=\s*@param\d+\s+WHERE\s+Id\s*=\s*@param\d+\s+SELECT\s+Id,\s*\[Name],\s*\[Description]\s+FROM\s+dbo\.TestTable\s+WHERE\s+Id\s*=\s*@param\d+\s*$",
                new string[] { "Id", "Name", "Description" }, new[] {
                new object[] { entity.Id, entity.Name, Resource.SecondRandomText }
                });

            Assert.PropertyChanged((INotifyPropertyChanged)entity, nameof(E.Test.Description), 
                () => entity.Description = Resource.SecondRandomText);

            _databaseFixture.ObjectProvider.GetQueryable<E.Test>().Where(x => x == entity).Save();
        }

        [Fact]
        public void TestDeleteSingle()
        {
            _databaseFixture.AddSupportedQuery("Select", @"^\s*SELECT\s+(?<T>T\d+)\.Id,\s*\k<T>\.\[Name],\s*\k<T>\.\[Description]\s+FROM\s+dbo\.TestTable\s+\k<T>\s*$",
                new string[] { "Id", "Name", "Description" }, new[] {
                new object[] { 1, $"Testname {DateTime.Now:g}", Resource.FirstRandomText },
                new object[] { 2, $"Testname2 {DateTime.Now:g}", Resource.SecondRandomText }
                });

            E.Test entity = Assert.Single(_queryable.ToList().Where(x => x.Id == 1));

            IQueryable<E.Test> queryable = _databaseFixture.ObjectProvider.GetQueryable<E.Test>().Where(x => x == entity);
            queryable.Delete();

            _databaseFixture.AddSupportedQuery("Delete", @"^\s*DELETE\s+dbo\.TestTable\s+WHERE\s+Id\s*=\s*@param\d+\s*$", new string[0]);
            queryable.Save();
        }

        [Fact]
        public void TestDeleteAll()
        {
            _databaseFixture.AddSupportedQuery("Select", @"^\s*SELECT\s+(?<T>T\d+)\.Id,\s*\k<T>\.\[Name],\s*\k<T>\.\[Description]\s+FROM\s+dbo\.TestTable\s+\k<T>\s*$",
                new string[] { "Id", "Name", "Description" }, new[] {
                new object[] { 1, $"Testname {DateTime.Now:g}", Resource.FirstRandomText },
                new object[] { 2, $"Testname2 {DateTime.Now:g}", Resource.SecondRandomText }
                });

            IQueryable<E.Test> queryable = _databaseFixture.ObjectProvider.GetQueryable<E.Test>();
            queryable.ToList();
            queryable.Delete();

            _databaseFixture.AddSupportedQuery("Delete", @"^\s*DELETE\s+dbo\.TestTable\s+WHERE\s+Id\s*=\s*@param\d+\s*$", new string[0]);
            queryable.Save();
        }

        [ExtTheory, MemberData(nameof(SimpleExpressions))]
        public void TestSimpleExpression(string name, Expression<Func<E.SubTest, bool>> expression, string queryPattern, IEnumerable<object[]> values)
        {
            _databaseFixture.AddSupportedQuery(name, @"^\s*SELECT\s+(?<T>T\d+)\.Id,\s+\k<T>\.Test,\s+\k<T>\.\[Name],\s+\k<T>\.\[First],\s+\k<T>\.\[Second],\s+\k<T>\.\[Nullable]\s+FROM\s+dbo\.SubTestTable\s+\k<T>\s+WHERE\s+" + queryPattern + "$", new string[] { "Id", "Test", "Name", "First", "Second", "Nullable" }, values.ToArray());

            _output.WriteLine($"Test {name} expression");
            List<E.SubTest> subResult = _subQueryable.Where(expression).ToList();
            Assert.Equal(values.Count(), subResult.Count);
            _output.WriteLine("... Done");
        }

        //[ExtTheory, MemberData(nameof(ForeignObjectExpressions))]
        //public void TestForeignObjectExpression(string name, Func<IQueryable<E.SubTest>, E.Test, IQueryable<E.SubTest>> function, int expectedCount)
        //{
        //    _output.WriteLine($"Test {name} expression");
        //    List<E.SubTest> subResult = function(_subQueryable, _queryable.FirstOrDefault()).ToList();
        //    Assert.Equal(expectedCount, subResult.Count);
        //    _output.WriteLine("... Done");
        //}
        #endregion

        #region Methods
        E.Test CreateTestEntity(string name, string description)
        {
            E.Test entity = _databaseFixture.ObjectProvider.CreateObject<E.Test>();
            Assert.NotNull(entity);
            entity.Name = name;
            entity.Description = description;

            _output.WriteLine($"Entity created, Name: {entity.Name}");

            ((INotifyPropertyChanged)entity).PropertyChanged += (s, e) =>
            {
                _output.WriteLine($"Property Changed on Entity - Id:{((E.Test)s).Id}, PropertyName: {e.PropertyName}");
                if (e.PropertyName == nameof(E.Test.Id))
                    _waithandle.Set();
            };
            return entity;
        }
        #endregion

        #region MemberData Definitions
        public static TheoryData<string, Expression<Func<E.SubTest, bool>>, string, IEnumerable<object[]>> SimpleExpressions
        {
            get
            {
                TheoryData<string, Expression<Func<E.SubTest, bool>>, string, IEnumerable<object[]>> returnValue = new TheoryData<string, Expression<Func<E.SubTest, bool>>, string, IEnumerable<object[]>>();
                returnValue.Add("Equal", x => x.First == x.Second, @"\k<T>\.\[First]\s*=\s*\k<T>\.\[Second]", new[] {
                    new object[] { 6, 1, "SubEntity10", 5, 5, DBNull.Value },
                    new object[] { 16, 2, "SubEntity11", 5, 5, DBNull.Value }});
                returnValue.Add("Equal to Null", x => x.Nullable == null, @"\k<T>\.\[Nullable]\s+IS\s+NULL", new[] {
                    new object[] { 1, 1, "SubEntity0", 0, 10, DBNull.Value},
                    new object[] { 2, 1, "SubEntity2", 1, 9, DBNull.Value},
                    new object[] { 3, 1, "SubEntity4", 2, 8, DBNull.Value},
                    new object[] { 4, 1, "SubEntity6", 3, 7, DBNull.Value},
                    new object[] { 5, 1, "SubEntity8", 4, 6, DBNull.Value},
                    new object[] { 6, 1, "SubEntity10", 5, 5, DBNull.Value},
                    new object[] { 7, 1, "SubEntity12", 6, 4, DBNull.Value},
                    new object[] { 9, 1, "SubEntity16", 8, 2, DBNull.Value},
                    new object[] { 10, 1, "SubEntity18", 9, 1, DBNull.Value},
                    new object[] { 11, 2, "SubEntity1", 0, 10, DBNull.Value},
                    new object[] { 12, 2, "SubEntity3", 1, 9, DBNull.Value},
                    new object[] { 13, 2, "SubEntity5", 2, 8, DBNull.Value},
                    new object[] { 14, 2, "SubEntity7", 3, 7, DBNull.Value},
                    new object[] { 15, 2, "SubEntity9", 4, 6, DBNull.Value},
                    new object[] { 16, 2, "SubEntity11", 5, 5, DBNull.Value},
                    new object[] { 17, 2, "SubEntity13", 6, 4, DBNull.Value},
                    new object[] { 19, 2, "SubEntity17", 8, 2, DBNull.Value},
                    new object[] { 20, 2, "SubEntity19", 9, 1, DBNull.Value}});
                returnValue.Add("Unequal to Null", x => x.Nullable != null, @"\k<T>\.\[Nullable]\s+IS\s+NOT\s+NULL", new[] {
                    new object[] { 8, 1, "SubEntity14", 7, 3, DateTime.Now},
                    new object[] { 18, 2, "SubEntity15", 7, 3, DateTime.Now}});
                returnValue.Add("Add", x => x.First + x.Second == 10, @"\k<T>\.\[First]\s*\+\s*\k<T>\.\[Second]\s*=\s*@param\d+", new[] {
                    new object[] { 1, 1, "SubEntity0", 0, 10, DBNull.Value},
                    new object[] { 2, 1, "SubEntity2", 1, 9, DBNull.Value},
                    new object[] { 3, 1, "SubEntity4", 2, 8, DBNull.Value},
                    new object[] { 4, 1, "SubEntity6", 3, 7, DBNull.Value},
                    new object[] { 5, 1, "SubEntity8", 4, 6, DBNull.Value},
                    new object[] { 6, 1, "SubEntity10", 5, 5, DBNull.Value},
                    new object[] { 11, 2, "SubEntity1", 0, 10, DBNull.Value},
                    new object[] { 12, 2, "SubEntity3", 1, 9, DBNull.Value},
                    new object[] { 13, 2, "SubEntity5", 2, 8, DBNull.Value},
                    new object[] { 14, 2, "SubEntity7", 3, 7, DBNull.Value},
                    new object[] { 15, 2, "SubEntity9", 4, 6, DBNull.Value},
                    new object[] { 16, 2, "SubEntity11", 5, 5, DBNull.Value},
                    new object[] { 17, 2, "SubEntity13", 6, 4, DBNull.Value},
                    new object[] { 18, 2, "SubEntity15", 7, 3, DateTime.Now},
                    new object[] { 19, 2, "SubEntity17", 8, 2, DBNull.Value },
                    new object[] { 20, 2, "SubEntity19", 9, 1, DBNull.Value }});
                returnValue.Add("Subtract", x => x.First - x.Second == 2, @"\k<T>\.\[First]\s*\-\s*\k<T>\.\[Second]\s*=\s*@param\d+", new[] {
                    new object[] { 7, 1, "SubEntity12", 6, 4, DBNull.Value},
                    new object[] { 17, 2, "SubEntity13", 6, 4, DBNull.Value}});
                returnValue.Add("Greater", x => x.First > x.Second, @"\k<T>\.\[First]\s*\>\s*\k<T>\.\[Second]", new[] {
                    new object[] { 7, 1, "SubEntity12", 6, 4, DBNull.Value},
                    new object[] { 8, 1, "SubEntity14", 7, 3, DateTime.Now},
                    new object[] { 9, 1, "SubEntity16", 8, 2, DBNull.Value},
                    new object[] { 10, 1, "SubEntity18", 9, 1, DBNull.Value},
                    new object[] { 17, 2, "SubEntity13", 6, 4, DBNull.Value},
                    new object[] { 18, 2, "SubEntity15", 7, 3, DateTime.Now},
                    new object[] { 19, 2, "SubEntity17", 8, 2, DBNull.Value },
                    new object[] { 20, 2, "SubEntity19", 9, 1, DBNull.Value }});
                returnValue.Add("GreaterEqual", x => x.First >= x.Second, @"\k<T>\.\[First]\s*>=\s*\k<T>\.\[Second]", new[] {
                    new object[] { 6, 1, "SubEntity10", 5, 5, DBNull.Value},
                    new object[] { 7, 1, "SubEntity12", 6, 4, DBNull.Value},
                    new object[] { 8, 1, "SubEntity14", 7, 3, DateTime.Now},
                    new object[] { 9, 1, "SubEntity16", 8, 2, DBNull.Value},
                    new object[] { 10, 1, "SubEntity18", 9, 1, DBNull.Value},
                    new object[] { 16, 2, "SubEntity11", 5, 5, DBNull.Value},
                    new object[] { 17, 2, "SubEntity13", 6, 4, DBNull.Value},
                    new object[] { 18, 2, "SubEntity15", 7, 3, DateTime.Now},
                    new object[] { 19, 2, "SubEntity17", 8, 2, DBNull.Value},
                    new object[] { 20, 2, "SubEntity19", 9, 1, DBNull.Value}});
                returnValue.Add("Less", x => x.First < x.Second, @"\k<T>\.\[First]\s*<\s*\k<T>\.\[Second]", new[] {
                    new object[] { 1, 1, "SubEntity0", 0, 10, DBNull.Value},
                    new object[] { 2, 1, "SubEntity2", 1, 9, DBNull.Value},
                    new object[] { 3, 1, "SubEntity4", 2, 8, DBNull.Value},
                    new object[] { 4, 1, "SubEntity6", 3, 7, DBNull.Value},
                    new object[] { 5, 1, "SubEntity8", 4, 6, DBNull.Value},
                    new object[] { 11, 2, "SubEntity1", 0, 10, DBNull.Value},
                    new object[] { 12, 2, "SubEntity3", 1, 9, DBNull.Value},
                    new object[] { 13, 2, "SubEntity5", 2, 8, DBNull.Value},
                    new object[] { 14, 2, "SubEntity7", 3, 7, DBNull.Value},
                    new object[] { 15, 2, "SubEntity9", 4, 6, DBNull.Value}});
                returnValue.Add("LessEqual", x => x.First <= x.Second, @"\k<T>\.\[First]\s*<=\s*\k<T>\.\[Second]", new[] {
                    new object[] { 1, 1, "SubEntity0", 0, 10, DBNull.Value},
                    new object[] { 2, 1, "SubEntity2", 1, 9, DBNull.Value},
                    new object[] { 3, 1, "SubEntity4", 2, 8, DBNull.Value},
                    new object[] { 4, 1, "SubEntity6", 3, 7, DBNull.Value},
                    new object[] { 5, 1, "SubEntity8", 4, 6, DBNull.Value},
                    new object[] { 6, 1, "SubEntity10", 5, 5, DBNull.Value },
                    new object[] { 11, 2, "SubEntity1", 0, 10, DBNull.Value},
                    new object[] { 12, 2, "SubEntity3", 1, 9, DBNull.Value},
                    new object[] { 13, 2, "SubEntity5", 2, 8, DBNull.Value},
                    new object[] { 14, 2, "SubEntity7", 3, 7, DBNull.Value},
                    new object[] { 15, 2, "SubEntity9", 4, 6, DBNull.Value},
                    new object[] { 16, 2, "SubEntity11", 5, 5, DBNull.Value}});
                returnValue.Add("ConstantValue", x => x.First == 5, @"\k<T>\.\[First]\s*=\s*@param\d+", new[] {
                    new object[] { 6, 1, "SubEntity10", 5, 5, DBNull.Value },
                    new object[] { 16, 2, "SubEntity11", 5, 5, DBNull.Value }});
                returnValue.Add("Contains", x => new int[] { 2, 5, 7 }.Contains(x.First), @"\k<T>\.\[First]\s*IN\s*\(@param\d+,\s*@param\d+,\s*@param\d+\)", new[] {
                    new object[] { 3, 1, "SubEntity4", 2, 8, DBNull.Value},
                    new object[] { 6, 1, "SubEntity10", 5, 5, DBNull.Value},
                    new object[] { 8, 1, "SubEntity14", 7, 3, DateTime.Now},
                    new object[] { 13, 2, "SubEntity5", 2, 8, DBNull.Value},
                    new object[] { 16, 2, "SubEntity11", 5, 5, DBNull.Value},
                    new object[] { 18, 2, "SubEntity15", 7, 3, DateTime.Now}});
                return returnValue;
            }
        }

        public static TheoryData<string, Func<IQueryable<E.SubTest>, E.Test, IQueryable<E.SubTest>>, int> ForeignObjectExpressions
        {
            get
            {
                TheoryData<string, Func<IQueryable<E.SubTest>, E.Test, IQueryable<E.SubTest>>, int> returnValue = new TheoryData<string, Func<IQueryable<E.SubTest>, E.Test, IQueryable<E.SubTest>>, int>();
                returnValue.Add("ForeignObject Equal", (s, t) => s.Where(x => x.Test == t), 10);
                returnValue.Add("ForeignObject Property Equal to", (s, t) => { string name = t.Name; return s.Where(x => x.Test.Name == name); }, 10);
                return returnValue;
            }
        }
        #endregion
    }
}
