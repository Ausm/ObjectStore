using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Data.Common;
using ObjectStore.OrMapping;

namespace ObjectStore.Sqlite
{
    internal class UpdateCommandBuilder : ICommandBuilder
    {
        #region Membervariablen
        string _tablename;
        List<SqliteParameter> _parameters;
        Dictionary<string, string> _setValues;
        List<string> _whereClausel;
        List<string> _selectFields;
        #endregion

        #region Konstruktor
        public UpdateCommandBuilder()
        {
            _parameters = new List<SqliteParameter>();
            _selectFields = new List<string>();
            _setValues = new Dictionary<string, string>();
            _whereClausel = new List<string>();
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

            if (fieldtype == FieldType.KeyField)
            {
                if (value == null)
                {
                    _whereClausel.Add(string.Format("{0} IS NULL", fieldname));
                }
                else
                {
                    SqliteParameter param = DataBaseProvider.GetParameter($"@param{_parameters.Count}", value);
                    _whereClausel.Add($"{fieldname} = {param.ParameterName}");
                    _parameters.Add(param);
                }
            }
            else if (isChanged && (fieldtype == FieldType.WriteableField || fieldtype == FieldType.UpdateableField) && !_setValues.ContainsKey(fieldname))
            {
                if (value == null)
                {
                    _setValues.Add(fieldname, "NULL");
                }
                else
                {
                    SqliteParameter param = DataBaseProvider.GetParameter($"@param{_parameters.Count}", value);
                    _setValues.Add(fieldname, param.ParameterName);
                    _parameters.Add(param);
                }
            }

        }

        public DbCommand GetDbCommand()
        {
            string[] setStrings = new string[_setValues.Count];
            int i = 0;
            foreach (KeyValuePair<string, string> item in _setValues)
            {
                setStrings[i] = $"{item.Key} = {item.Value}";
                i++;
            }

            DbCommand command = DataBaseProvider.GetCommand();

            foreach (SqliteParameter parameter in _parameters)
                command.Parameters.Add(parameter);

            command.CommandText = $"UPDATE \"{_tablename}\" SET {string.Join(", ", setStrings)} WHERE {string.Join(" AND ", _whereClausel.ToArray())};\r\nSELECT {string.Join(", ", _selectFields.ToArray())} FROM \"{_tablename}\" WHERE {string.Join(" AND ", _whereClausel.ToArray())}";
            return command;
        }

        public void SetTablename(string tablename)
        {
            _tablename = tablename;
        }

        #endregion
    }
}
