using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data.Common;
using ObjectStore.OrMapping;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;

namespace ObjectStore.SqlClient
{
    internal abstract class SubQueryCommandBuilder : ISelectCommandBuilder, ISubQueryCommandBuilder, IServiceProvider, IParsingContext
    {
        #region Subklassen
        protected class Join
        {
            string _alias;
            SubQueryCommandBuilder _parent;
            MemberExpression _expression;

            public Join(SubQueryCommandBuilder parent, MemberExpression expression)
            {
                _parent = parent;
                _expression = expression;
                _alias = $"T{parent._databaseProvider.GetUniqe()}";
            }

            public override string ToString()
            {
                return $"JOIN {TableName} {_alias} ON {_alias}.{KeyName} = {ForeignAlias}.{ForeignKeyName}";
            }


            public string Alias
            {
                get
                {
                    return _alias;
                }
            }

            public MemberExpression Expression
            {
                get
                {
                    return _expression;
                }
            }

            string TableName
            {
                get
                {
                    return MappingInfo.GetMappingInfo(_expression.Type, true).TableName;
                }
            }

            string KeyName
            {
                get
                {
                    return MappingInfo.GetMappingInfo(_expression.Type, true).KeyMappingInfos.First().FieldName;
                }
            }

            string ForeignAlias
            {
                get
                {
                    if (_expression.Expression is ParameterExpression)
                        return _parent._alias;

                    if (_expression.Expression is MemberExpression)
                        return _parent._joins.Where(x => x._expression == _expression.Expression).First()._alias;

                    throw new InvalidOperationException();
                }
            }

            string ForeignKeyName
            {
                get
                {
                    return Mapping.GetMapping((PropertyInfo)_expression.Member).FieldName;
                }
            }
        }
        #endregion

        #region Membervariablen
        string _tablename;
        List<DbParameter> _parameters;
        List<LambdaExpression> _whereExpressions;
        string _alias;
        List<Join> _joins;
        DataBaseProvider _databaseProvider;
        #endregion

        #region Konstruktor
        public SubQueryCommandBuilder(DataBaseProvider databaseProvider)
        {
            _databaseProvider = databaseProvider;
            _parameters = new List<DbParameter>();
            _joins = new List<Join>();
            _alias = $"TE{_databaseProvider.GetUniqe()}";
        }
        #endregion

        #region Funktionen
        public virtual void AddField(string fieldname, FieldType fieldtype){}

        public virtual void AddField(string fieldname, object value, FieldType fieldtype, KeyInitializer keyInitializer, bool isChanged) {}

        protected string AddParameter(object value)
        {
            SqlParameter param = new SqlParameter(string.Format("@param{0}", _parameters.Count), value);
            _parameters.Add(param);
            return param.ParameterName;
        }

        public DbCommand GetDbCommand()
        {
            if (string.IsNullOrEmpty(_tablename)) throw new InvalidOperationException("Tablename is not set.");

            StringBuilder stringBuilder = new StringBuilder("IF EXISTS(SELECT * FROM ");
            stringBuilder.AppendFormat("{0} {1}", _tablename, _alias);

            string whereClause = GetWhereClause();

            foreach (Join join in _joins)
                stringBuilder.AppendFormat(" LEFT OUTER ").Append(join);

            if (!string.IsNullOrEmpty(whereClause))
                stringBuilder.AppendFormat(" WHERE ").Append(whereClause);

            stringBuilder.Append(")");
            stringBuilder.AppendLine("SELECT 1");
            stringBuilder.AppendLine("ELSE");
            stringBuilder.AppendLine("SELECT 0");

            SqlCommand command = new SqlCommand();
            command.Parameters.AddRange(_parameters.ToArray());
            command.CommandText = stringBuilder.ToString();
            return command;
        }

        protected string GetWhereClause()
        {
            if (_whereExpressions.Count == 0)
                return null;
            else if (_whereExpressions.Count == 1)
            {
                return _databaseProvider.ExpressionParser.ParseExpression(_whereExpressions[0], x => this.AddDbParameter(x).ParameterName, this);
            }
            else
            {
                string whereClause = null;
                foreach (LambdaExpression expression in _whereExpressions)
                {
                    if (whereClause == null)
                        whereClause = "(";
                    else
                        whereClause += " AND (";

                    whereClause += _databaseProvider.ExpressionParser.ParseExpression(expression, x => this.AddDbParameter(x).ParameterName, this) + ")";
                }
                return whereClause;
            }
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

        protected IEnumerable<Join> Joins
        {
            get
            {
                return _joins.AsReadOnly();
            }
        }
        #endregion

        #region IModifyableCommandBuilder Members

        public void AddJoin(string tablename, string onClausel)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<DbParameter> Parameters
        {
            get
            {
                return _parameters;
            }
        }

        public DbParameter AddDbParameter(object value)
        {
            return _databaseProvider.GetDbParameter(value);
        }
        public string Alias
        {
            get
            {
                return _alias;
            }
        }

        public void SetOrderBy(string expression) {}

        public void SetTop(int count) {}

        public void SetWhereClausel(LambdaExpression expression)
        {
        }
        #endregion

        #region ISubQueryCommandBuilder Members
        public abstract string SubQuery
        {
            get;
        }
        #endregion

        #region IServiceProvider
        object IServiceProvider.GetService(Type serviceType)
        {
            if (serviceType == typeof(IParsingContext))
                return this;

            throw new NotSupportedException($"Service {serviceType.FullName} is not providet.");
        }
        #endregion

        #region IParsingContext
        string IParsingContext.GetAlias(ParameterExpression expression)
        {
            if (_whereExpressions.Any(x => x.Parameters[0] == expression))
                return _alias;

            throw new ArgumentException("There is no Alias for this expression.", nameof(expression));
        }

        string IParsingContext.GetJoin(MemberExpression expression)
        {
            Join join = _joins.Where(x => x.Expression == expression).FirstOrDefault();

            if (join == null)
            {
                //MemberExpression
                _joins.Add(join = new Join(this, expression));
                if (expression.Expression is MemberExpression)
                    ((IParsingContext)this).GetJoin((MemberExpression)expression.Expression);
            }

            return join.Alias;
        }

        string IParsingContext.GetParameter(object value)
        {
            return AddDbParameter(value).ParameterName;
        }

        #endregion
    }
}
