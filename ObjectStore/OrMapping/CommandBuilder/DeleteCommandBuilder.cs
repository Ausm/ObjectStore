using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Data;

namespace Ausm.ObjectStore.OrMapping
{
    internal class DeleteCommandBuilder : ISqlCommandBuilder
    {
        #region Membervariablen
        string _tablename;
        List<SqlParameter> _parameters;
        List<string> _whereClausel;
        #endregion

        #region Konstruktor
        public DeleteCommandBuilder()
        {
            _parameters = new List<SqlParameter>();
            _whereClausel = new List<string>();
        }
        #endregion

        #region Funktionen
        public void AddField(string fieldname, FieldType fieldtype)
        {
        }

        public void AddField(string fieldname, object value, FieldType fieldtype, KeyInitializer keyInitializer)
        {
            if (fieldtype == FieldType.KeyField)
            {
                SqlParameter param = new SqlParameter(string.Format("@param{0}", _parameters.Count), value);
                _whereClausel.Add(string.Format("{0} = {1}", fieldname, param.ParameterName));
                _parameters.Add(param);
            }
        }

        public SqlCommand GetSqlCommand()
        {
            SqlCommand command = new SqlCommand();
            command.Parameters.AddRange(_parameters.ToArray());
            command.CommandText = _whereClausel.Count == 0 ?
                                    string.Format("DELETE {0}", _tablename) :
                                    string.Format("DELETE {0} WHERE {1}", _tablename, string.Join(" AND ", _whereClausel.ToArray()));
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
