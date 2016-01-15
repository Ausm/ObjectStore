using ObjectStore.Interfaces;
using ObjectStore.OrMapping;
using ObjectStore.Test.Resources;
using System;
using System.Data.SqlClient;
using System.IO;
using Xunit;

namespace ObjectStore.Test.Fixtures
{
    public class DatabaseFixture : IDisposable
    {
        IObjectProvider _objectProvider;

        public DatabaseFixture()
        {
            if (_objectProvider == null)
                ObjectStoreManager.DefaultObjectStore.RegisterObjectProvider(_objectProvider = new RelationalObjectStore(Resource.MsSqlConnectionString, true));

            ExecuteQuery(File.ReadAllText("Resources\\MsSql_InitDatabase.sql"));
        }

        public IObjectProvider ObjectProvider
        {
            get
            {
                return _objectProvider;
            }
        }

        void ExecuteQuery(string script)
        {
            string[] queries = script.Split(new string[] { "\r\nGO\r\n", "\nGO\n" }, StringSplitOptions.RemoveEmptyEntries);
            using (SqlConnection connection = new SqlConnection(Resource.MsSqlConnectionString))
            {
                connection.Open();
                foreach (string query in queries)
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public void Dispose()
        {
        }
    }

    [CollectionDefinition("Database collection")]
    public class DatabaseCollection : ICollectionFixture<DatabaseFixture> {}
}
