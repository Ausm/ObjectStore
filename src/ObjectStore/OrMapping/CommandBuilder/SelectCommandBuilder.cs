using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Data;

namespace ObjectStore.OrMapping
{
    internal class SelectCommandBuilder : ISqlCommandBuilder, IModifyableCommandBuilder
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
        string _alias;
        List<SqlParameter> _parameters;
        string _whereClausel;
        List<string> _selectFields;
        List<string> _orderbyExpressions;
        List<Join> _joins;
        int _top;
        #endregion

        #region Konstruktor
        public SelectCommandBuilder()
        {
            _parameters = new List<SqlParameter>();
            _selectFields = new List<string>();
            _orderbyExpressions = new List<string>();
            _joins = new List<Join>();
            _top = -1;
            _alias = string.Format("T{0}", ObjectStoreManager.CurrentUniqe());
        }

        public SelectCommandBuilder(string alias) : this()
        {
            _alias = alias;
        }
        #endregion

        #region Funktionen
        public void AddField(string fieldname, FieldType fieldtype)
        {
            if (!_selectFields.Contains(fieldname))
                _selectFields.Add(fieldname);
        }

        public void AddField(string fieldname, object value, FieldType fieldtype, KeyInitializer keyInitializer)
        {
            if (!_selectFields.Contains(fieldname))
                _selectFields.Add(fieldname);

            if (fieldtype == FieldType.KeyField)
            {
                if (string.IsNullOrEmpty(_whereClausel))
                    _whereClausel = string.Format("({0} = {1})", fieldname, AddParameter(value));
                else
                    _whereClausel = string.Format("{0} AND ({1} = {2})", _whereClausel, fieldname, AddParameter(value));
            }

        }

        public void AddJoin(string tablename, string onClausel)
        {
            _joins.Add(new Join() { TableName = tablename, On = onClausel });
        }

        protected string AddParameter(object value)
        {
            SqlParameter param = new SqlParameter(string.Format("@param{0}", ObjectStoreManager.CurrentUniqe()), value);
            _parameters.Add(param);
            return param.ParameterName;
        }

        public void ResetOrder()
        {
            _orderbyExpressions = new List<string>();
        }

        public void SetOrderBy(string expression)
        {
            _orderbyExpressions.Add(expression);
        }

        public void SetTop(int count)
        {
            _top = count;
        }

        public SqlCommand GetSqlCommand()
        {
            if (string.IsNullOrEmpty(_tablename)) throw new InvalidOperationException("Tablename is not set.");

            StringBuilder stringBuilder = new StringBuilder("SELECT");

            if(_top > -1) stringBuilder.AppendFormat(" TOP {0}", _top);

            stringBuilder.AppendFormat(" {2}.{0} FROM {1} {2}", 
                string.Join(string.Format(", {0}.", _alias), _selectFields.ToArray()), _tablename, _alias);

            foreach (Join join in _joins)
                stringBuilder.AppendFormat(" LEFT OUTER JOIN {0} ON {1}", join.TableName, join.On);

            if(!string.IsNullOrEmpty(_whereClausel))
                stringBuilder.AppendFormat(" WHERE {0}", _whereClausel);

            if (_orderbyExpressions.Count != 0)
                stringBuilder.AppendFormat(" ORDER BY {0}", string.Join(", ", _orderbyExpressions.ToArray()));

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
        #endregion
    }
}
