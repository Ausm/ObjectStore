using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace ObjectStore.Sqlite
{
    public interface ITableInfo
    {
        string TableName { get; }

        IEnumerable<string> FieldNames { get; }
    }

    public partial class DataBaseProvider
    {
        class TableInfo : ITableInfo
        {
            string _tableName;
            List<string> _fieldNames;

            public TableInfo(string tableName, IEnumerable<string> fieldNames)
            {
                _tableName = tableName;
                _fieldNames = fieldNames.ToList();
            }

            public string TableName => _tableName;

            public IEnumerable<string> FieldNames => _fieldNames.AsReadOnly();
        }

        internal ITableInfo GetTableInfo(string tableName, string connectionString)
        {
            using (DbConnection connection = GetConnection(connectionString))
            {
                connection.Open();

                using (DbCommand command = GetCommand())
                {
                    command.Connection = connection;
                    command.CommandText = $"SELECT COUNT(*) FROM sqlite_master WHERE tbl_name = '{tableName.Replace("'", "''")}' AND type in ('table', 'view')";

                    object value = command.ExecuteScalar();
                    if (!(value is int) || (int)value != 1)
                        return null;
                }

                using (DbCommand command = GetCommand())
                {
                    command.Connection = connection;
                    command.CommandText = $"PRAGMA TABLE_INFO('{tableName.Replace("'", "''")}');";
                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        List<string> fieldNames = new List<string>(reader.RecordsAffected);
                        int nameOrdinal = reader.GetOrdinal("name");

                        while (reader.Read())
                            fieldNames.Add(reader.GetString(nameOrdinal));

                        return new TableInfo(tableName, fieldNames);
                    }
                }
            }
        }
    }

}
