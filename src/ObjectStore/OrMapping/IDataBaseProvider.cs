using System;
using System.Data.Common;

namespace ObjectStore.OrMapping
{
    interface IDataBaseProvider
    {
        ISelectCommandBuilder GetSelectCommandBuilder();
        IDbCommandBuilder GetInsertCommandBuilder();
        IDbCommandBuilder GetUpdateCommandBuilder();
        IDbCommandBuilder GetDeleteCommandBuilder();
        ISubQueryCommandBuilder GetExistsCommandBuilder();
        ISubQueryCommandBuilder GetInCommandBuilder(string outherAlias);

        DbConnection GetConnection();
        DbConnection GetConnection(string name);

        void ReleaseConnection(DbConnection connection);

        event EventHandler ConnectionOpened;

    }
}
