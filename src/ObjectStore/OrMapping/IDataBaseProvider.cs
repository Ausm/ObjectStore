using System;
using System.Collections.Generic;
using System.Data.Common;

namespace ObjectStore.OrMapping
{
    public interface IDataBaseProvider
    {
        IModifyableCommandBuilder GetSelectCommandBuilder();
        ICommandBuilder GetInsertCommandBuilder();
        ICommandBuilder GetUpdateCommandBuilder();
        ICommandBuilder GetDeleteCommandBuilder();

        DbConnection GetConnection();
        DbConnection GetConnection(string name);

        DbCommand CombineCommands(IEnumerable<DbCommand> commands);

        void ReleaseConnection(DbConnection connection);

        event EventHandler ConnectionOpened;

    }
}
