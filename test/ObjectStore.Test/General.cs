using Xunit;
using System.Data.SqlClient;
using ObjectStore.OrMapping;
using System;
using System.Linq;
using System.ComponentModel;
using System.Threading;

namespace ObjectStore.Test
{
    public class General
    {
        const string MsSqlConnectionString = "data source=(local);Integrated Security=True;initial catalog=Test";
        const string MsSqlCreateDbObjects = "IF OBJECT_ID('dbo.TestTable') IS NULL CREATE TABLE dbo.TestTable(Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TestTable PRIMARY KEY, Name NVARCHAR(100) NOT NULL CONSTRAINT DF_TestTable_Name  DEFAULT (N''), [Description] NVARCHAR(MAX) NOT NULL)";
        const string FirstText = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.";
        const string SecondText = "Duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum dolore eu feugiat nulla facilisis at vero eros et accumsan et iusto odio dignissim qui blandit praesent luptatum zzril delenit augue duis dolore te feugait nulla facilisi. Lorem ipsum dolor sit amet, consectetuer adipiscing elit, sed diam nonummy nibh euismod tincidunt ut laoreet dolore magna aliquam erat volutpat.";

        static General()
        {
            ObjectStoreManager.DefaultObjectStore.RegisterObjectProvider(new RelationalObjectStore(MsSqlConnectionString, true));
        }

        void Init()
        {
            using (SqlConnection connection = new SqlConnection(MsSqlConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(MsSqlCreateDbObjects, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        [Fact]
        public void AddNewEntity()
        {
            Init();

            Entities.Test entity = ObjectStoreManager.DefaultObjectStore.CreateObject<Entities.Test>();
            Assert.NotNull(entity);
            entity.Name = $"Testname {DateTime.Now:g}";
            entity.Description = FirstText;

            AutoResetEvent autoResetEvent = new AutoResetEvent(false);

            ((INotifyPropertyChanged)entity).PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(Entities.Test.Id))
                        autoResetEvent.Set();
                };

            ObjectStoreManager.DefaultObjectStore.GetQueryable<Entities.Test>().Where(x => x == entity).Save();
            Assert.True(autoResetEvent.WaitOne(5000));
            Assert.NotEqual(entity.Id, 0);
        }

        [Fact]
        public void UpdateExisting()
        {
            Init();

            Entities.Test entity = ObjectStoreManager.DefaultObjectStore.GetQueryable<Entities.Test>().FirstOrDefault();
            if (entity == null)
            {
                AddNewEntity();
                entity = ObjectStoreManager.DefaultObjectStore.GetQueryable<Entities.Test>().FirstOrDefault();
            }

            entity.Description = entity.Description == FirstText ? SecondText : FirstText;
            ObjectStoreManager.DefaultObjectStore.GetQueryable<Entities.Test>().Where(x => x == entity).Save();
        }

        [Fact]
        public void DeleteSingle()
        {
            Init();
            Entities.Test entity = ObjectStoreManager.DefaultObjectStore.GetQueryable<Entities.Test>().FirstOrDefault();
            if (entity == null)
            {
                AddNewEntity();
                entity = ObjectStoreManager.DefaultObjectStore.GetQueryable<Entities.Test>().FirstOrDefault();
            }

            int id = entity.Id;

            IQueryable<Entities.Test> queryable = ObjectStoreManager.DefaultObjectStore.GetQueryable<Entities.Test>().Where(x => x == entity);
            queryable.Delete();
            queryable.Save();

            Assert.Empty(ObjectStoreManager.DefaultObjectStore.GetQueryable<Entities.Test>().Where(x => x.Id == id));
        }

        [Fact]
        public void DeleteAll()
        {
            Init();
            switch (ObjectStoreManager.DefaultObjectStore.GetQueryable<Entities.Test>().Count())
            {
                case 0:
                    AddNewEntity();
                    AddNewEntity();
                    break;
                case 1:
                    AddNewEntity();
                    break;
                default:
                    break;
            }

            IQueryable<Entities.Test> queryable = ObjectStoreManager.DefaultObjectStore.GetQueryable<Entities.Test>();
            queryable.Delete();
            queryable.Save();

            Assert.Empty(ObjectStoreManager.DefaultObjectStore.GetQueryable<Entities.Test>());
        }

    }
}
