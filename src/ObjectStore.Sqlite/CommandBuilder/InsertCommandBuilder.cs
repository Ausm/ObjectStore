using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.Sqlite;
using System.Data.Common;
using ObjectStore.OrMapping;

namespace ObjectStore.Sqlite
{
    internal class InsertCommandBuilder : ICommandBuilder
    {
        #region Membervariablen
        List<string> _insertFields;
        List<string> _insertValues;
        List<string> _selectFields;
        List<string> _whereClausel;
        List<SqliteParameter> _parameters;
        string _tablename;
        #endregion

        #region Konstruktoren
        public InsertCommandBuilder()
        {
            _insertFields = new List<string>();
            _insertValues = new List<string>();
            _selectFields = new List<string>();
            _whereClausel = new List<string>();
            _parameters = new List<SqliteParameter>();
        }
        #endregion

        #region Methods
        public void AddField(string fieldname, FieldType fieldtype)
        {
            if (!_selectFields.Contains(fieldname))
                _selectFields.Add(fieldname);

        }

        public void AddField(string fieldname, object value, FieldType fieldtype, Type keyInitializerType, bool isChanged)
        {
            if (!_selectFields.Contains(fieldname))
                _selectFields.Add(fieldname);

            if (_insertFields.Contains(fieldname))
                return;

            if (fieldtype == FieldType.KeyField)
            {
                KeyInitializer keyInitializer = keyInitializerType != null ? KeyInitializer.GetInitializer(keyInitializerType) : null;
                if (keyInitializer == null || !keyInitializer.CheckEmpty(value))
                {
                    SqliteParameter param = DataBaseProvider.GetParameter($"@param{_parameters.Count}", value);
                    _insertFields.Add(fieldname);
                    _insertValues.Add(param.ParameterName);
                    _whereClausel.Add(string.Format("{0} = {1}", param.ParameterName, fieldname));
                    _parameters.Add(param);
                }
                else
                    _whereClausel.Add(keyInitializer.GetWhereClause(fieldname, _tablename));
            }
            else if (fieldtype == FieldType.WriteableField || fieldtype == FieldType.InsertableField)
            {
                if (value == null)
                {
                    _insertFields.Add(fieldname);
                    _insertValues.Add("NULL");
                }
                else
                {
                    SqliteParameter param = DataBaseProvider.GetParameter($"@param{_parameters.Count}", value);
                    _insertFields.Add(fieldname);
                    _insertValues.Add(param.ParameterName);
                    _parameters.Add(param);
                }
            }
        }

        public DbCommand GetDbCommand()
        {
            DbCommand command = DataBaseProvider.GetCommand();

            foreach (SqliteParameter parameter in _parameters)
                command.Parameters.Add(parameter);

            command.CommandText = _insertFields.Count == 0 ? 
                    $"INSERT INTO \"{_tablename}\" DEFAULT VALUES;\r\nSELECT {string.Join(", ", _selectFields.ToArray())} FROM {_tablename} WHERE {string.Join(" AND ", _whereClausel.ToArray())}" :
                    $"INSERT INTO \"{_tablename}\" ({string.Join(", ", _insertFields.ToArray())}) VALUES ({string.Join(", ", _insertValues.ToArray())});\r\nSELECT {string.Join(", ", _selectFields.ToArray())} FROM \"{_tablename}\" WHERE {string.Join(" AND ", _whereClausel.ToArray())}";

            return command;
        }

        public void SetTablename(string tablename)
        {
            _tablename = tablename;
        }

        #endregion
    }
}
