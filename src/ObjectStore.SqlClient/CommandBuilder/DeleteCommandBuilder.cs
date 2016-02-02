using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.Common;
using ObjectStore.OrMapping;

namespace ObjectStore.SqlClient
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

        #region Funktionen
        public void AddField(string fieldname, FieldType fieldtype)
        {
        }

        public void AddField(string fieldname, object value, FieldType fieldtype, KeyInitializer keyInitializer, bool isChanged)
        {
            if (fieldtype == FieldType.KeyField)
            {
                SqlParameter param = new SqlParameter(string.Format("@param{0}", _parameters.Count), value);
                _whereClausel.Add(string.Format("{0} = {1}", fieldname, param.ParameterName));
                _parameters.Add(param);
            }
        }

        public DbCommand GetDbCommand()
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
