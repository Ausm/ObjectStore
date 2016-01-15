using Xunit;
using System.Data.SqlClient;
using ObjectStore.OrMapping;
using System;
using System.Linq;
using System.ComponentModel;
using System.Threading;
using ObjectStore.Test.Fixtures;
using ObjectStore.Test.Resources;
using Xunit.Abstractions;

namespace ObjectStore.Test
{
    [Collection("Database collection")]
    public class EmptyDatabaseTests
    {
        DatabaseFixture _fixture;
        ITestOutputHelper _output;
        EventWaitHandle _waithandle;

        public EmptyDatabaseTests(DatabaseFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            _waithandle = new ManualResetEvent(false);
        }

        Entities.Test CreateTestEntity(string name, string description)
        {
            Entities.Test entity = _fixture.ObjectProvider.CreateObject<Entities.Test>();
            Assert.NotNull(entity);
            entity.Name = name;
            entity.Description = description;

            _output.WriteLine($"Entity created, Name: {entity.Name}");

            ((INotifyPropertyChanged)entity).PropertyChanged += General_PropertyChanged;
            return entity;
        }

        void General_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _output.WriteLine($"Property Changed on Entity - Id:{((Entities.Test)sender).Id}, PropertyName: {e.PropertyName}");
            if (e.PropertyName == nameof(Entities.Test.Id))
                _waithandle.Set();
        }

        [Fact]
        public void TestInsertUpdateDelete()
        {
            Entities.Test entity = CreateTestEntity($"Testname {DateTime.Now:g}", Resource.FirstRandomText);
            _fixture.ObjectProvider.GetQueryable<Entities.Test>().Where(x => x == entity).Save();
            Assert.True(_waithandle.WaitOne(5000));
            Assert.NotEqual(entity.Id, 0);
            _output.WriteLine($"First entity saved, new Id: {entity.Id}");

            Entities.Test entity2 = CreateTestEntity($"Testname {DateTime.Now:g}", Resource.SecondRandomText);
            _waithandle.Reset();
            _fixture.ObjectProvider.GetQueryable<Entities.Test>().Where(x => x == entity2).Save();
            Assert.True(_waithandle.WaitOne(5000));
            _output.WriteLine($"Second entity saved, new Id: {entity2.Id}");

            entity.Description = Resource.SecondRandomText;
            _fixture.ObjectProvider.GetQueryable<Entities.Test>().Where(x => x == entity).Save();
            _output.WriteLine($"First entity updated and saved, Id: {entity2.Id}");

            int id = entity.Id;
            IQueryable<Entities.Test> queryable = _fixture.ObjectProvider.GetQueryable<Entities.Test>().Where(x => x == entity);
            queryable.Delete();
            queryable.Save();
            Assert.Empty(_fixture.ObjectProvider.GetQueryable<Entities.Test>().Where(x => x.Id == id));
            Assert.Equal(1, _fixture.ObjectProvider.GetQueryable<Entities.Test>().Count());
            _output.WriteLine($"Deleted entity, Id: {entity2.Id}");

            queryable = _fixture.ObjectProvider.GetQueryable<Entities.Test>();
            queryable.Delete();
            queryable.Save();
            Assert.Empty(_fixture.ObjectProvider.GetQueryable<Entities.Test>());
            _output.WriteLine($"Deleted all entities");
        }
    }
}
