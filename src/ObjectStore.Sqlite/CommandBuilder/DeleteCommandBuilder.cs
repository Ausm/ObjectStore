using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Data.Common;
using ObjectStore.OrMapping;

namespace ObjectStore.Sqlite
{
    internal class DeleteCommandBuilder : ICommandBuilder
    {
        #region Membervariablen
        string _tablename;
        List<DbParameter> _parameters;
        List<string> _whereClausel;
        #endregion

        #region Konstruktor
        public DeleteCommandBuilder()
        {
            _parameters = new List<DbParameter>();
            _whereClausel = new List<string>();
        }
        #endregion

        #region Methods
        public void AddField(string fieldname, FieldType fieldtype)
        {
        }

        public void AddField(string fieldname, object value, FieldType fieldtype, Type keyInitializerType, bool isChanged)
        {
            if (fieldtype == FieldType.KeyField)
            {
                SqliteParameter param = DataBaseProvider.GetParameter($"@param{_parameters.Count}", value);
                _whereClausel.Add($"{fieldname} = {param.ParameterName}");
                _parameters.Add(param);
            }
        }

        public DbCommand GetDbCommand()
        {
            DbCommand command = DataBaseProvider.GetCommand();

            foreach(SqliteParameter parameter in _parameters)
                command.Parameters.Add(parameter);

            command.CommandText = _whereClausel.Count == 0 ? $"DELETE FROM \"{_tablename}\"" : $"DELETE FROM \"{_tablename}\" WHERE {string.Join(" AND ", _whereClausel.ToArray())}";
            return command;
        }
        
        public void SetTablename(string tablename)
        {
            _tablename = tablename;
        }

        #endregion
    }
}
