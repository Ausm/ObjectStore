using System;
using System.Data.SqlClient;

namespace ObjectStore.Interfaces
{
    public interface IConnectionProvider
    {
        SqlConnection GetConnection();
        SqlConnection GetConnection(string name);

        void ReleaseConnection(SqlConnection connection);
        event EventHandler ConnectionOpened;
    }
}
