using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data.Common;
using ObjectStore.OrMapping;

namespace ObjectStore.SqlClient
{
    internal class InsertCommandBuilder : IDbCommandBuilder
    {
        #region Membervariablen
        StringBuilder _beforInsert;
        StringBuilder _afterInsert;
        List<string> _insertFields;
        List<string> _insertValues;
        List<string> _selectFields;
        List<string> _whereClausel;
        List<SqlParameter> _parameters;
        string _tablename;
        #endregion

        #region Konstruktoren
        public InsertCommandBuilder()
        {
            _insertFields = new List<string>();
            _insertValues = new List<string>();
            _selectFields = new List<string>();
            _whereClausel = new List<string>();
            _parameters = new List<SqlParameter>();
            _beforInsert = new StringBuilder();
            _afterInsert = new StringBuilder();
        }
        #endregion

        #region Funktionen
        public void AddField(string fieldname, FieldType fieldtype)
        {
            if (!_selectFields.Contains(fieldname))
                _selectFields.Add(fieldname);

        }

        public void AddField(string fieldname, object value, FieldType fieldtype, KeyInitializer keyInitializer, bool isChanged)
        {
            if (!_selectFields.Contains(fieldname))
                _selectFields.Add(fieldname);

            if (_insertFields.Contains(fieldname))
                return;

            if (fieldtype == FieldType.KeyField)
            {

                if (keyInitializer == null || !keyInitializer.CheckEmpty(value))
                {
                    SqlParameter param = new SqlParameter(string.Format("@param{0}", _parameters.Count), value);
                    _insertFields.Add(fieldname);
                    _insertValues.Add(param.ParameterName);
                    _whereClausel.Add(string.Format("{0} = {1}", param.ParameterName, fieldname));
                    _parameters.Add(param);
                }
                else
                {
                    SqlParameter param = new SqlParameter(string.Format("@param{0}", _parameters.Count), keyInitializer.SqlDbType);
                    param.Value = DBNull.Value;
                    param.IsNullable = true;
                    if (!string.IsNullOrEmpty(keyInitializer.BeforInsert)) _beforInsert.AppendLine(keyInitializer.BeforInsert.Replace("{parameter}", param.ParameterName));
                    if (!string.IsNullOrEmpty(keyInitializer.AfterInsert)) _afterInsert.AppendLine(keyInitializer.AfterInsert.Replace("{parameter}", param.ParameterName));
                    if (keyInitializer.SetInInsert)
                    {
                        _insertFields.Add(fieldname);
                        _insertValues.Add(param.ParameterName);
                    }
                    _whereClausel.Add(string.Format("{0} = {1}", param.ParameterName, fieldname));
                    _parameters.Add(param);
                }
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
                    SqlParameter param = new SqlParameter(string.Format("@param{0}", _parameters.Count), value);
                    _insertFields.Add(fieldname);
                    _insertValues.Add(param.ParameterName);
                    _parameters.Add(param);
                }
            }
        }

        public DbCommand GetDbCommand()
        {
            SqlCommand command = new SqlCommand();
            command.Parameters.AddRange(_parameters.ToArray());
            command.CommandText =
                string.Format(_insertFields.Count == 0 ? "{5}INSERT {0} DEFAULT VALUES\r\n{6}SELECT {3} FROM {0} WHERE {4}" :
                    "{5}INSERT {0} ({1}) VALUES ({2})\r\n{6}SELECT {3} FROM {0} WHERE {4}",
                                    _tablename,
                                    string.Join(", ", _insertFields.ToArray()),
                                    string.Join(", ", _insertValues.ToArray()),
                                    string.Join(", ", _selectFields.ToArray()),
                                    string.Join(" AND ", _whereClausel.ToArray()),
                                    _beforInsert.ToString(),
                                    _afterInsert.ToString());
            return command;
        }
        #endregion

        #region Properties
        public string Tablename
        {
            get
            {
                return _tablename;
            }
            set
            {
                _tablename = value;
            }
        }
        #endregion
    }
}
