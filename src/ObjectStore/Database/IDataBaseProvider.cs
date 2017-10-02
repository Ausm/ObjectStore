using System;
using System.Collections.Generic;
using System.Data.Common;

namespace ObjectStore.Database
{
    public interface IDataBaseProvider
    {
        IModifyableCommandBuilder GetSelectCommandBuilder();
        ICommandBuilder GetInsertCommandBuilder();
        ICommandBuilder GetUpdateCommandBuilder();
        ICommandBuilder GetDeleteCommandBuilder();

        IValueSource GetValueSource(DbCommand command);

        DbConnection GetConnection(string connectionString);

        DbCommand CombineCommands(IEnumerable<DbCommand> commands);

        void ReleaseConnection(DbConnection connection);

        IDatabaseInitializer GetDatabaseInitializer(string connectionString);

        event EventHandler ConnectionOpened;

    }
}
