using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Data;

namespace ObjectStore.OrMapping
{
    internal class ExistsCommandBuilder : ISqlCommandBuilder, IModifyableCommandBuilder, ISubQueryCommandBuilder
    {
        #region Subklassen
        class Join
        {
            public string TableName { get; set; }
            public string On { get; set; }
        }
        #endregion

        #region Membervariablen
        string _tablename;
        List<SqlParameter> _parameters;
        string _whereClausel;
        string _alias;
        List<Join> _joins;
        #endregion

        #region Konstruktor
        public ExistsCommandBuilder()
        {
            _parameters = new List<SqlParameter>();
            _joins = new List<Join>();
            _alias = string.Format("TE{0}", ObjectStoreManager.CurrentUniqe());
        }
        #endregion

        #region Funktionen
        public void AddField(string fieldname, FieldType fieldtype){}

        public void AddField(string fieldname, object value, FieldType fieldtype, KeyInitializer keyInitializer){}

        protected string AddParameter(object value)
        {
            SqlParameter param = new SqlParameter(string.Format("@param{0}", _parameters.Count), value);
            _parameters.Add(param);
            return param.ParameterName;
        }

        public SqlCommand GetSqlCommand()
        {
            if (string.IsNullOrEmpty(_tablename)) throw new InvalidOperationException("Tablename is not set.");

            StringBuilder stringBuilder = new StringBuilder("IF EXISTS(SELECT * FROM ");
            stringBuilder.AppendFormat("{0} {1}", _tablename, _alias);

            foreach (Join join in _joins)
                stringBuilder.AppendFormat(" LEFT OUTER JOIN {0} ON {1}", join.TableName, join.On);

            if (!string.IsNullOrEmpty(_whereClausel))
                stringBuilder.AppendFormat(" WHERE {0}", _whereClausel);

            stringBuilder.Append(")");
            stringBuilder.AppendLine("SELECT 1");
            stringBuilder.AppendLine("ELSE");
            stringBuilder.AppendLine("SELECT 0");

            SqlCommand command = new SqlCommand();
            command.Parameters.AddRange(_parameters.ToArray());
            command.CommandText = stringBuilder.ToString();
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

        #region IModifyableCommandBuilder Members

        public void AddJoin(string tablename, string onClausel)
        {
            _joins.Add(new Join() { TableName = tablename, On = onClausel });
        }

        public List<SqlParameter> Parameters
        {
            get
            {
                return _parameters;
            }
            set
            {
                _parameters.Clear();
                _parameters.AddRange(value);
            }
        }

        public string Alias
        {
            get
            {
                return _alias;
            }
        }

        public string WhereClausel
        {
            get
            {
                return _whereClausel;
            }
            set
            {
                _whereClausel = value;
            }
        }

        public void SetOrderBy(string expression) {}

        public void SetTop(int count) {}

        #endregion

        #region ISubQueryCommandBuilder Members
        public string SubQuery
        {
            get
            {
                if (string.IsNullOrEmpty(_tablename)) throw new InvalidOperationException("Tablename is not set.");

                StringBuilder stringBuilder = new StringBuilder("EXISTS(SELECT * FROM ");
                stringBuilder.Append(_tablename).Append(" ").Append(_alias);


                if (_joins.Count > 0)
                    foreach (Join join in _joins)
                        stringBuilder.AppendFormat(" LEFT OUTER JOIN {0} ON {1}", join.TableName, join.On);

                if (string.IsNullOrEmpty(_whereClausel))
                    stringBuilder.Append(")");
                else
                    stringBuilder.AppendFormat(" WHERE {0})", _whereClausel);

                return stringBuilder.ToString();
            }
        }
        #endregion
    }
}
