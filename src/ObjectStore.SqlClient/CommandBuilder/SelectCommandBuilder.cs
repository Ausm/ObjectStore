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
    internal class SelectCommandBuilder : IModifyableCommandBuilder, IParsingContext
    {
        #region Subklassen
        class Join
        {
            string _alias;
            SelectCommandBuilder _parent;
            MemberExpression _expression;

            public Join(SelectCommandBuilder parent, MemberExpression expression)
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
                    return TypeMapping.GetMappingInfo(_expression.Type).TableName;
                }
            }

            string KeyName
            {
                get
                {
                    return TypeMapping.GetMappingInfo(_expression.Type).KeyMappingInfos.First().FieldName;
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
                    return MemberMapping.GetMapping((PropertyInfo)_expression.Member).FieldName;
                }
            }
        }
        #endregion

        #region Fields
        string _tablename;
        string _alias;
        List<DbParameter> _parameters;
        List<LambdaExpression> _whereExpressions;
        List<string> _selectFields;
        List<Tuple<LambdaExpression, bool>> _orderbyExpressions;
        List<Join> _joins;
        int _top;
        DataBaseProvider _databaseProvider;
        #endregion

        #region Konstruktor
        public SelectCommandBuilder(DataBaseProvider databaseProvider)
        {
            _databaseProvider = databaseProvider;
            _parameters = new List<DbParameter>();
            _selectFields = new List<string>();
            _orderbyExpressions = new List<Tuple<LambdaExpression, bool>>();
            _joins = new List<Join>();
            _top = -1;
            _alias = $"T{_databaseProvider.GetUniqe()}";
            _whereExpressions = new List<LambdaExpression>();
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
                throw new NotImplementedException("TODO");
                //if (string.IsNullOrEmpty(_whereClausel))
                //    _whereClausel = $"({fieldname} = {AddDbParameter(value).ParameterName})";
                //else
                //    _whereClausel = $"{_whereClausel} AND ({fieldname} = {AddDbParameter(value).ParameterName})";
            }

        }

        public void SetOrderBy(LambdaExpression expression, bool descending)
        {
            _orderbyExpressions.Add(Tuple.Create(expression, descending));
        }

        public void SetTop(int count)
        {
            _top = count;
        }

        public DbParameter AddDbParameter(object value)
        {
            DbParameter returnValue = DataBaseProvider.GetParameter($"@param{_databaseProvider.GetUniqe()}", value);
            _parameters.Add(returnValue);
            return returnValue;
        }

        public DbCommand GetDbCommand()
        {
            if (string.IsNullOrEmpty(_tablename)) throw new InvalidOperationException("Tablename is not set.");

            StringBuilder stringBuilder = new StringBuilder("SELECT");

            if(_top > -1) stringBuilder.AppendFormat(" TOP {0}", _top);

            stringBuilder.AppendFormat(" {2}.{0} FROM {1} {2}", 
                string.Join(string.Format(", {0}.", _alias), _selectFields.ToArray()), _tablename, _alias);

            string whereClause = null;

            if (_whereExpressions.Count == 1)
            {
                whereClause = _databaseProvider.ExpressionParser.ParseExpression(_whereExpressions[0], this);
            }
            else if (_whereExpressions.Count > 1)
            {
                foreach (LambdaExpression expression in _whereExpressions)
                {
                    if (whereClause == null)
                        whereClause = "(";
                    else
                        whereClause += " AND (";

                    whereClause +=  _databaseProvider.ExpressionParser.ParseExpression(expression, this) + ")";
                }
            }

            foreach (Join join in _joins)
                stringBuilder.Append(" LEFT OUTER ").Append(join);

            if (!string.IsNullOrWhiteSpace(whereClause))
                stringBuilder.Append(" WHERE ").Append(whereClause);

            if (_orderbyExpressions.Count != 0)
                stringBuilder.Append(" ORDER BY ").Append(string.Join(", ", _orderbyExpressions.Select(
                        exp => exp.Item2 ?
                            _databaseProvider.ExpressionParser.ParseExpression(exp.Item1, this) + " DESC":
                            _databaseProvider.ExpressionParser.ParseExpression(exp.Item1, this)
                        ).ToArray()));


            DbCommand command = DataBaseProvider.GetCommand();
            command.Parameters.AddRange(_parameters.ToArray());
            command.CommandText = stringBuilder.ToString();
            return command;
        }

        public void SetWhereClausel(LambdaExpression expression)
        {
            _whereExpressions.Add(expression);
        }

        public void SetTablename(string tablename)
        {
            _tablename = tablename;
        }

        #region IParsingContext
        string IParsingContext.GetAlias(ParameterExpression expression)
        {
            if (_whereExpressions.Any(x => x.Parameters[0] == expression) ||
                _orderbyExpressions.Any(x => x.Item1.Parameters[0] == expression))
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
        #endregion
    }
}
