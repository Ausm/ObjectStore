using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Data.Common;

namespace ObjectStore.OrMapping.Expressions
{
    public class ParsedExpression : Expression
    {
        #region Subclassen
        public class Join
        {
            string _fieldAlias;

            public Join()
            {
                Alias = string.Format("J{0}", ObjectStoreManager.CurrentUniqe());
            }
            public System.Linq.Expressions.ParameterExpression ParameterExpression { get; set; }
            public string Alias { get; private set; }
            public string Table { get; set; }
            public string ForeignField { get; set; }
            public string Field { get; set; }
            public string FieldAlias { get { return FieldAliasJoin == null ? _fieldAlias : FieldAliasJoin.Alias; } set { _fieldAlias = value; } }
            public Join FieldAliasJoin { get; set; }

            public string SqlString { get { return null; } }
        }
        #endregion

        #region Membervariablen
        LambdaExpression _parsedLambdaExpression;
        ParsedExpression _parentParsedExpression;
        Expression _parsedExpression;
        Func<object ,DbParameter> _getParamFunction;
        Dictionary<ValueComparedExpression<System.Linq.Expressions.ParameterExpression>, string> _aliases;
        Dictionary<System.Reflection.PropertyInfo, Join> _joinAliases;
        #endregion

        #region Konstruktoren
        internal ParsedExpression(LambdaExpression expression, Func<object, DbParameter> getParamFunction) : base(null)
        {
            _parsedLambdaExpression = expression;
            _getParamFunction = getParamFunction;
            _parentParsedExpression = null;

            _aliases = new Dictionary<ValueComparedExpression<System.Linq.Expressions.ParameterExpression>, string>();
            foreach(System.Linq.Expressions.ParameterExpression paramExpression in expression.Parameters)
                _aliases.Add(paramExpression, null);
            _parsedExpression = ParseExpression(expression.Body, this, this);
        }

        internal ParsedExpression(LambdaExpression expression, Expression parentExpression, ParsedExpression parentParsedExpression) : base(parentExpression)
        {
            _parentParsedExpression = parentParsedExpression;
            _parsedLambdaExpression = expression;
            _getParamFunction = parentParsedExpression._getParamFunction;
            _aliases = new Dictionary<ValueComparedExpression<System.Linq.Expressions.ParameterExpression>, string>();
            foreach (System.Linq.Expressions.ParameterExpression paramExpression in expression.Parameters)
                _aliases.Add(paramExpression, null);
            _parsedExpression = ParseExpression(expression.Body, this, this);
        }
        #endregion

        #region Methoden
        internal DbParameter AquireSqlParameter(object value)
        {
            return _getParamFunction(value);
        }

        internal Join AquireJoin(System.Reflection.PropertyInfo property, System.Linq.Expressions.ParameterExpression paramExpression, Join previousJoin)
        {
            if(_joinAliases == null)
                _joinAliases = new Dictionary<System.Reflection.PropertyInfo, Join>();

            if (_joinAliases.ContainsKey(property))
                return _joinAliases[property];

            Join returnValue = _joinAliases[property] = Mapping.GetMapping(property).GetJoinForProperty();
            returnValue.ParameterExpression = paramExpression;
            if (previousJoin != null)
                returnValue.FieldAliasJoin = previousJoin;
            return returnValue;
        }
        #endregion

        #region Properties
        public LambdaExpression Expression
        {
            get
            {
                return _parsedLambdaExpression;
            }
        }

        public override string SqlExpression
        {
            get
            {
                return _parsedExpression.SqlExpression;
            }
        }

        public string this[System.Linq.Expressions.ParameterExpression paramExpression]
        {
            get
            {
                if (_aliases.ContainsKey(paramExpression))
                {
                    return _aliases[paramExpression];
                }
                else if (_parentParsedExpression != null)
                {
                    return _parentParsedExpression[paramExpression];
                }
                else
                {
                    string newAlias = string.Format("T{0}", _aliases.Count);
                    _aliases.Add(paramExpression, newAlias);
                    return newAlias;
                }
            }
            set
            {
                if (_aliases.ContainsKey(paramExpression))
                {
                    _aliases[paramExpression] = value;
                }
                else if (_parentParsedExpression != null)
                {
                    _parentParsedExpression[paramExpression] = value;
                }
                else
                {
                    _aliases.Add(paramExpression, value);
                }

            }
        }

        public IEnumerable<Join> Joins 
        { 
            get 
            {
                if (_joinAliases == null)
                    return Enumerable.Empty<Join>();

                foreach (Join join in _joinAliases.Values)
                    join.FieldAlias = this[join.ParameterExpression];

                return _joinAliases.Values; 
            } 
        }
        #endregion
    }
}
